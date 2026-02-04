using Robust.Shared.Utility;

namespace Content.Shared._Horizon.Sponsors.Systems
{
    public sealed class SpriteOverrideSystem : EntitySystem
    {
        /// <summary>Базовый путь к RSI спонзорских призраков (относительно /Textures).</summary>
        private static readonly ResPath SponsorsGhostsPath = new("_Horizon/Sponsors/Ghosts");

        /// <summary>Состояние RSI для призраков (все спонзорские RSI используют "animated").</summary>
        private const string GhostRsiState = "animated";

        // Ник игрока
        private readonly Dictionary<string, ResPath> _playerSprites = new()
        {
            {"localhost@EvilBug", SponsorsGhostsPath / "evilneko.rsi"},
            {"EvilBug", SponsorsGhostsPath / "evilneko.rsi"},
            {"Joulerk", SponsorsGhostsPath / "joulerk.rsi"},
            {"Lemird", SponsorsGhostsPath / "evilneko.rsi"},
            {"EXPERRIENCEE", SponsorsGhostsPath / "joker.rsi"},
            {"Xigovir", SponsorsGhostsPath / "uas.rsi"},
            {"TheGypsyBaron", SponsorsGhostsPath / "baron.rsi"},
            {"Cvartet", SponsorsGhostsPath / "cvartet.rsi"},
            {"DesBy", SponsorsGhostsPath / "kitsunes.rsi"},
        };

        /// <summary>
        /// Получаем спецификатор спрайта для игрока по его нику.
        /// </summary>
        /// <param name="playerName">Ник игрока</param>
        /// <returns>Спецификатор спрайта</returns>
        public SpriteSpecifier? GetSpriteForPlayer(string playerName)
        {
            if (_playerSprites.TryGetValue(playerName, out var path))
                return new SpriteSpecifier.Rsi(path, GhostRsiState);

            return null;
        }
    }
}
