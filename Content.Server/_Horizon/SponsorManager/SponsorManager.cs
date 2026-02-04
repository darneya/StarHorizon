using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared._Horizon.CCVar;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        private ISawmill _sawmill = default!;

        private ResPath _sponsorsFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemSponsorsPath));
        private ResPath _dsSponsorsFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemDiscordSponsorsPath));
        private ResPath _disposableFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemDisposablePath));
        private ResPath _sponsorItemsFilePath => NormalizePath(_cfg.GetCVar(HorizonCCVars.SponsorSystemItemsPath));

        private ResPath NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Remove all leading '/' characters to ensure path is treated as relative to UserData
            // This prevents issues when paths are configured with absolute paths like /ss14_data/...
            var normalized = path.TrimStart('/');

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ArgumentException("Path cannot be only slashes", nameof(path));

            // Create ResPath and ensure it's rooted (for ResPath's internal structure)
            // This creates a ResPath like /sponsorSystem/sponsors.txt which is relative to UserData root
            return new ResPath(normalized).ToRootedPath();
        }

        private static HashSet<string> _sponsors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sponsorsAndBalances = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _sponsorSlots = new(StringComparer.OrdinalIgnoreCase);

        public void Initialize()
        {
            _sawmill = _logManager.GetSawmill("sponsor");
            _sawmill.Info("SponsorManager initialized successfully");
        }

        #region Check files
        public void LoadSponsorsInfoFile()
        {
            try
            {
                EnsureFileExists(_dsSponsorsFilePath);
                EnsureFileExists(_sponsorsFilePath);
                EnsureFileExists(_disposableFilePath);
                EnsureFileExists(_sponsorItemsFilePath);
                _sawmill.Info("Sponsor system files checked/created successfully");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to load sponsor info files: {ex}");
                throw;
            }
        }

        private void EnsureFileExists(ResPath filePath)
        {
            try
            {
                // Log the path being processed for debugging
                _sawmill.Debug($"Ensuring file exists: {filePath}");

                // Log UserData root directory if available
                var rootDir = _resourceManager.UserData.RootDir;
                if (rootDir != null)
                {
                    _sawmill.Debug($"UserData root directory: {rootDir}");
                }

                // Create directory if it doesn't exist
                _resourceManager.UserData.CreateDir(filePath.Directory);

                // Create file if it doesn't exist
                if (!_resourceManager.UserData.Exists(filePath))
                {
                    _resourceManager.UserData.WriteAllText(filePath, string.Empty);
                    _sawmill.Debug($"Created empty sponsor file: {filePath}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var rootDir = _resourceManager.UserData.RootDir ?? "unknown (virtual provider)";
                _sawmill.Error($"Permission denied when creating file: {filePath}. " +
                              $"UserData root: {rootDir}. " +
                              $"Error: {ex.Message}. " +
                              $"Make sure the server process has write permissions to the UserData directory.");
                throw;
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to ensure file exists: {filePath}, Error: {ex}");
                throw;
            }
        }
        #endregion Check files

        #region Discord (sync only at round start)
        /// <summary>
        /// Синхронизирует список спонсоров из discord_sponsors с основным файлом и памятью.
        /// Вызывается только при старте раунда, чтобы изменения файла во время раунда не сбрасывали балансы.
        /// </summary>
        public void SyncDiscordSponsorsAtRoundStart()
        {
            try
            {
                _sawmill.Info("Syncing Discord sponsors at round start...");
                ReadSponsorsFile();

                var discordLines = SafeReadAllLines(_dsSponsorsFilePath);
                _sawmill.Debug($"Read {discordLines.Length} lines from discord_sponsors");

                ProcessDiscordSponsors(discordLines);
                _sawmill.Info("Discord sponsors sync at round start completed");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to sync Discord sponsors at round start: {ex}");
            }
        }

        private void ProcessDiscordSponsors(string[] discordLines)
        {
            var discordSponsors = new Dictionary<string, (string originalCkey, string discordId)>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in discordLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 3)
                    continue;

                var originalCkey = parts[1].Trim();
                var normalizedCkey = NormalizeUserName(originalCkey);
                var discordId = parts[2].Trim();

                if (string.IsNullOrWhiteSpace(normalizedCkey) || string.IsNullOrWhiteSpace(discordId))
                    continue;

                // Используем нормализованный ckey как ключ для поиска
                // Но сохраняем оригинальное имя для записи в файл
                discordSponsors[normalizedCkey] = (originalCkey, discordId);
            }

            var currentSponsors = new HashSet<string>(_sponsors, StringComparer.OrdinalIgnoreCase);

            // Добавляем или обновляем спонсоров из Discord (для существующих сохраняем текущий баланс)
            var updatedCount = 0;
            var addedCount = 0;
            foreach (var (normalizedCkey, (originalCkey, discordId)) in discordSponsors)
            {
                var slots = CalculateSlots(discordId);
                var tokens = CalculateTokens(discordId);

                if (currentSponsors.Contains(normalizedCkey))
                {
                    // При старте раунда восстанавливаем полный лимит токенов по тиру
                    SaveSponsors(originalCkey, slots, tokens);
                    updatedCount++;
                }
                else
                {
                    AddSponsor(originalCkey, slots, tokens);
                    addedCount++;
                }
            }

            // Удаляем спонсоров, которых больше нет в Discord списке
            var removedCount = 0;
            foreach (var sponsor in currentSponsors)
            {
                if (!discordSponsors.ContainsKey(sponsor))
                {
                    RemoveSponsorFromFile(sponsor);
                    removedCount++;
                }
            }

            if (addedCount > 0 || updatedCount > 0 || removedCount > 0)
            {
                _sawmill.Info($"Discord sync completed: {addedCount} added, {updatedCount} updated, {removedCount} removed");
            }
        }

        private string[] SafeReadAllLines(ResPath filePath, int maxRetries = 3, int delay = 1000)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var sr = _resourceManager.UserData.OpenText(filePath);

                    var lines = new List<string>();
                    string? line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    return lines.ToArray();
                }
                catch (IOException ex)
                {
                    if (attempt < maxRetries - 1)
                    {
                        _sawmill.Debug($"Failed to read file {filePath} (attempt {attempt + 1}/{maxRetries}), retrying...");
                        Task.Delay(delay).Wait();
                    }
                    else
                    {
                        _sawmill.Error($"Failed to read file {filePath} after {maxRetries} attempts: {ex}");
                    }
                }
            }

            return Array.Empty<string>();
        }


        private int CalculateSlots(string discordId)
        {
            return discordId switch
            {
                "1349080752209395833" => 2, // Спонсор I - Авантюрист
                "1349080829334257856" => 5, // Спонсор II - Наемник
                "1349080858224623717" => 10, // Спонсор III - Шериф
                "1349080888927064216" => 20, // Спонсор IV - Представитель
                "1349080921537773568" => 20, // Спонсор V - Легенда
                "1349080947399725136" => 20, // Спонсор VI - пока нету
                _ => 0
            };
        }

        private int CalculateTokens(string discordId)
        {
            return discordId switch
            {
                "1349080829334257856" => 10, // Спонсор II - Наемник
                "1349080858224623717" => 15, // Спонсор III - Шериф
                "1349080888927064216" => 30, // Спонсор IV - Представитель
                "1349080921537773568" => 50, // Спонсор V - Легенда
                "1349080947399725136" => 100, // Спонсор VI - пока нету
                _ => 0
            };
        }
        #endregion Discord

        #region Read/Write File
        public void ReadSponsorsFile()
        {
            try
            {
                _sponsors.Clear();
                _sponsorsAndBalances.Clear();
                _sponsorSlots.Clear();

                if (!_resourceManager.UserData.Exists(_sponsorsFilePath))
                {
                    _sawmill.Debug($"Sponsors file does not exist: {_sponsorsFilePath}");
                    return;
                }

                var sponsorCount = 0;
                using var reader = _resourceManager.UserData.OpenText(_sponsorsFilePath);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(';');
                    if (parts.Length < 3)
                        continue;

                    var userName = NormalizeUserName(parts[0].Trim());
                    if (string.IsNullOrWhiteSpace(userName))
                        continue;

                    if (int.TryParse(parts[1], out var slots))
                    {
                        _sponsorSlots[userName] = slots;
                    }

                    if (int.TryParse(parts[2], out var balance))
                    {
                        _sponsorsAndBalances[userName] = balance;
                    }

                    _sponsors.Add(userName);
                    sponsorCount++;
                }

                _sawmill.Debug($"Loaded {sponsorCount} sponsors from file");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to read sponsors file: {ex}");
            }
        }

        public void AddSponsor(string userName, int slot, int token)
        {
            try
            {
                var normalizedName = NormalizeUserName(userName);
                _sponsors.Add(normalizedName);
                _sponsorSlots[normalizedName] = slot;
                _sponsorsAndBalances[normalizedName] = token;

                // Передаем оригинальное имя для сохранения в файл
                SaveSponsors(userName, slot, token);
                _sawmill.Info($"Added new sponsor: {userName} (slots: {slot}, tokens: {token})");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to add sponsor {userName}: {ex}");
                throw;
            }
        }

        public void SaveSponsors(string userName, int slot, int token)
        {
            try
            {
                var normalizedName = NormalizeUserName(userName);

                var lines = new List<string>();
                if (_resourceManager.UserData.Exists(_sponsorsFilePath))
                {
                    using var reader = _resourceManager.UserData.OpenText(_sponsorsFilePath);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                var index = lines.FindIndex(line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return false;

                    var parts = line.Split(';');
                    if (parts.Length == 0)
                        return false;

                    var fileUserName = NormalizeUserName(parts[0].Trim());
                    return fileUserName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase);
                });

                if (index != -1)
                {
                    // Сохраняем оригинальное имя из файла, чтобы не менять регистр
                    var existingLine = lines[index];
                    var existingParts = existingLine.Split(';');
                    var existingName = existingParts.Length > 0 ? existingParts[0].Trim() : userName;

                    // Используем оригинальное имя из файла, если оно есть
                    lines[index] = $"{existingName};{slot};{token}";
                    _sawmill.Debug($"Updated sponsor in file: {userName} (slots: {slot}, tokens: {token})");
                }
                else
                {
                    lines.Add($"{userName};{slot};{token}");
                    _sawmill.Debug($"Added sponsor to file: {userName} (slots: {slot}, tokens: {token})");
                }

                _resourceManager.UserData.CreateDir(_sponsorsFilePath.Directory);
                using var writer = _resourceManager.UserData.OpenWriteText(_sponsorsFilePath);
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }

                // В памяти используем нормализованное имя для консистентности
                _sponsors.Add(normalizedName);
                _sponsorSlots[normalizedName] = slot;
                _sponsorsAndBalances[normalizedName] = token;
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to save sponsor {userName}: {ex}");
                throw;
            }
        }

        public void RemoveSponsorFromFile(string userName)
        {
            try
            {
                var normalizedName = NormalizeUserName(userName);
                _sponsors.Remove(normalizedName);
                _sponsorsAndBalances.Remove(normalizedName);
                _sponsorSlots.Remove(normalizedName);

                if (!_resourceManager.UserData.Exists(_sponsorsFilePath))
                {
                    _sawmill.Debug($"Cannot remove sponsor {userName}: file does not exist");
                    return;
                }

                var lines = new List<string>();
                using (var reader = _resourceManager.UserData.OpenText(_sponsorsFilePath))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                var index = lines.FindIndex(line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return false;

                    var parts = line.Split(';');
                    if (parts.Length == 0)
                        return false;

                    var fileUserName = NormalizeUserName(parts[0].Trim());
                    return fileUserName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase);
                });

                if (index != -1)
                {
                    lines.RemoveAt(index);
                    using var writer = _resourceManager.UserData.OpenWriteText(_sponsorsFilePath);
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    _sawmill.Info($"Removed sponsor from file: {userName}");
                }
                else
                {
                    _sawmill.Debug($"Sponsor {userName} not found in file for removal");
                }
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to remove sponsor {userName}: {ex}");
                throw;
            }
        }
        #endregion Read/Write File

        #region Methods of finding
        private string NormalizeUserName(string userName)
        {
            return userName?.Trim() ?? string.Empty;
        }

        public bool IsSponsor(string userName)
        {
            var normalizedName = NormalizeUserName(userName);
            return _sponsors.Contains(normalizedName);
        }

        public int GetCharacterSlots(string userName)
        {
            var maxCharacterSlots = _cfg.GetCVar(CCVars.GameMaxCharacterSlots);
            var normalizedName = NormalizeUserName(userName);

            if (_sponsorSlots.TryGetValue(normalizedName, out var slot))
            {
                return maxCharacterSlots + slot;
            }

            return maxCharacterSlots;
        }

        public int GetBalance(string userName)
        {
            var normalizedName = NormalizeUserName(userName);
            if (_sponsorsAndBalances.TryGetValue(normalizedName, out var balance))
            {
                return balance;
            }

            return 0;
        }

        public void UpdateSponsorsAndBalances()
        {
            try
            {
                // Сначала обновляем данные из основного файла спонсоров
                // Это синхронизирует все три структуры данных: _sponsors, _sponsorsAndBalances, _sponsorSlots
                var sponsorCount = 0;
                if (_resourceManager.UserData.Exists(_sponsorsFilePath))
                {
                    using var reader = _resourceManager.UserData.OpenText(_sponsorsFilePath);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var parts = line.Split(';');
                        if (parts.Length < 3)
                            continue;

                        var userName = NormalizeUserName(parts[0].Trim());
                        if (string.IsNullOrWhiteSpace(userName))
                            continue;

                        // Добавляем/обновляем в HashSet спонсоров
                        _sponsors.Add(userName);

                        // Обновляем слоты
                        if (int.TryParse(parts[1], out var slots))
                        {
                            _sponsorSlots[userName] = slots;
                        }

                        // Обновляем балансы (начинаем с базового баланса из файла)
                        if (int.TryParse(parts[2], out var balance))
                        {
                            _sponsorsAndBalances[userName] = balance;
                        }

                        sponsorCount++;
                    }
                }

                // Затем добавляем дополнительные токены из disposable.txt
                var disposableLines = SafeReadAllLines(_disposableFilePath);
                var additionalTokensCount = 0;

                foreach (var line in disposableLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',');
                    if (parts.Length < 3)
                        continue;

                    var ckey = NormalizeUserName(parts[0].Trim());
                    if (string.IsNullOrWhiteSpace(ckey))
                        continue;

                    if (int.TryParse(parts[2], out var additionalTokens))
                    {
                        if (_sponsorsAndBalances.ContainsKey(ckey))
                        {
                            _sponsorsAndBalances[ckey] += additionalTokens;
                            additionalTokensCount++;
                        }
                    }
                }

                _sawmill.Info($"Updated sponsors and balances: {sponsorCount} sponsors loaded, {additionalTokensCount} additional tokens applied");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to update sponsors and balances: {ex}");
                throw;
            }
        }


        public void DeductBalance(string userName, int cost)
        {
            var normalizedName = NormalizeUserName(userName);
            if (_sponsorsAndBalances.TryGetValue(normalizedName, out var balance))
            {
                if (balance >= cost)
                {
                    balance -= cost;
                    _sponsorsAndBalances[normalizedName] = balance;
                    var slots = _sponsorSlots.TryGetValue(normalizedName, out var s) ? s : 0;
                    SaveSponsors(userName, slots, balance);
                    _sawmill.Debug($"Deducted {cost} tokens from {userName}, new balance: {balance}");
                }
                else
                {
                    _sawmill.Warning($"Insufficient balance for {userName}: has {balance}, needs {cost}");
                }
            }
            else
            {
                _sawmill.Warning($"Attempted to deduct balance for non-sponsor: {userName}");
            }
        }

        public string GetColor(string userName)
        {
            var normalizedName = NormalizeUserName(userName);

            var lines = SafeReadAllLines(_dsSponsorsFilePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 2)
                    continue;

                var ckey = NormalizeUserName(parts[1].Trim());
                if (ckey.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    if (parts.Length > 4)
                        return parts[4].Trim();
                }
            }

            return "#FF0000";
        }
        #endregion Methods of finding
    }
}
