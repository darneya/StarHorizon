using System.IO;
using System.Linq;
using Content.Server.GameTicking.Events;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.SponsorManager;

public sealed class SponsorsSpawnSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _resManager = null!;
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

        var file = _resManager.ContentFileReadText(new ResPath(Path.Combine(_resManager.UserData.RootDir!, "Sponsors/sponsor_items.txt")));
        while (!file.EndOfStream)
        {
            var line = file.ReadLine();
            if (line == null)
                continue;

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
