using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.CCVar
{
    [CVarDefs]
    public sealed class HorizonCCVars : CVars
    {
        /*
         * Barks
         */
        public static readonly CVarDef<bool> BarksEnabled =
            CVarDef.Create("barks.enabled", true, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMaxPitch =
            CVarDef.Create("barks.max_pitch", 1.5f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMinPitch =
            CVarDef.Create("barks.min_pitch", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMinDelay =
            CVarDef.Create("barks.min_delay", 0.1f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksMaxDelay =
            CVarDef.Create("barks.max_delay", 0.6f, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<bool> ReplaceTTSWithBarks =
            CVarDef.Create("barks.replace_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<float> BarksVolume =
            CVarDef.Create("barks.volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        ///     URL of the Discord webhook which will relay bans.
        /// </summary>
        public static readonly CVarDef<string> DiscordBanWebhook =
            CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

        public static readonly CVarDef<bool> EnableCustomFonts =
            CVarDef.Create("lang.enable_fonts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        ///     Paths to data
        /// </summary>
        public static readonly CVarDef<string> ShutdownTimersPath =
            CVarDef.Create("paths.default_shutdown_path", "shutdown_timers.yml", CVar.SERVERONLY | CVar.CONFIDENTIAL);

        public static readonly CVarDef<bool> ShutdownEnabled =
            CVarDef.Create("paths.shutdown_enabled", false, CVar.SERVERONLY);
    }
}
