using System.IO;
using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared._Horizon.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorsSpawnSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;

        private readonly Dictionary<string, string[]> _sponsorItems = new();
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = Logger.GetSawmill("sponsor");
            _sawmill.Info("SponsorsSpawnSystem initialized");
            SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        }

        public string[] GetItemsForPlayer(string playerName)
        {
            return _sponsorItems.TryGetValue(playerName, out var items) ? items : Array.Empty<string>();
        }

        private void OnRoundStarting(RoundStartingEvent ev)
        {
            LoadSponsorItems();
        }

        private void LoadSponsorItems()
        {
            try
            {
                _sponsorItems.Clear();

                var sponsorItemsPath = new ResPath(_cfg.GetCVar(HorizonCCVars.SponsorSystemItemsPath)).ToRootedPath();

                if (!_resourceManager.UserData.Exists(sponsorItemsPath))
                {
                    _sawmill.Debug($"Sponsor items file does not exist: {sponsorItemsPath}");
                    return;
                }

                var loadedCount = 0;
                using var reader = _resourceManager.UserData.OpenText(sponsorItemsPath);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var separatorIndex = line.IndexOf(',');
                    if (separatorIndex == -1)
                        continue;

                    var playerName = line[..separatorIndex].Trim();
                    var itemsString = line[(separatorIndex + 1)..].Trim();

                    var items = itemsString.Trim('(', ')')
                        .Split(',')
                        .Select(item => item.Trim())
                        .ToArray();

                    _sponsorItems[playerName] = items;
                    loadedCount++;
                    _sawmill.Debug($"Loaded sponsor items for {playerName}: {string.Join(", ", items)}");
                }

                _sawmill.Info($"Loaded sponsor items for {loadedCount} players");
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to load sponsor items: {ex}");
            }
        }
    }
}
