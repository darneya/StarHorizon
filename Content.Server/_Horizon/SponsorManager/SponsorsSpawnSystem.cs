using System.IO;
using System.Linq;
using Content.Server.GameTicking.Events;
using Content.Shared._Horizon.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server._Horizon.SponsorManager
{
    public sealed class SponsorsSpawnSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private readonly Dictionary<string, string[]> _sponsorItems = new();

        public override void Initialize()
        {
            base.Initialize();
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
            _sponsorItems.Clear();

            var sponsorItemsPath = _cfg.GetCVar(HorizonCCVars.SponsorSystemItemsPath);
            foreach (var line in File.ReadLines(sponsorItemsPath))
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
            }
        }
    }
}
