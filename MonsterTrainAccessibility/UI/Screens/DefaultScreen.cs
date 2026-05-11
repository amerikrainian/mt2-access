using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Help;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;

namespace MonsterTrainAccessibility.UI.Screens
{
    public sealed class DefaultScreen : Screen
    {
        public DefaultScreen()
        {
            ClaimAction("help");
            ClaimAction("buffer_prev_item");
            ClaimAction("buffer_next_item");
            ClaimAction("buffer_prev");
            ClaimAction("buffer_next");
            ClaimAction("mod_settings");
            ClaimAction("debug_commands");
            ClaimAction("read_gold");
            ClaimAction("read_pyre_health");
        }

        public override bool OnActionJustPressed(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "help":
                    OpenHelpScreen();
                    return true;
                case "buffer_prev_item":
                    BufferManager.Instance.PreviousItem();
                    return true;
                case "buffer_next_item":
                    BufferManager.Instance.NextItem();
                    return true;
                case "buffer_prev":
                    BufferManager.Instance.PreviousBuffer();
                    return true;
                case "buffer_next":
                    BufferManager.Instance.NextBuffer();
                    return true;
                case "mod_settings":
                    ScreenManager.PushScreen(new ModSettingsScreen(global::MonsterTrainAccessibility.ModSettings.ModSettings.Root));
                    return true;
                case "debug_commands":
                    return OpenDebugCommandsScreen();
                case "read_gold":
                    return ReadGold();
                case "read_pyre_health":
                    return ReadPyreHealth();
            }

            return false;
        }

        public override bool IsActionAvailable(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "read_gold":
                case "read_pyre_health":
                    return IsInRunContext();
                case "debug_commands":
                    return Settings.DebugCommandsEnabled != null && Settings.DebugCommandsEnabled.Value;
                default:
                    return true;
            }
        }

        private static void OpenHelpScreen()
        {
            HelpScreenBuilder builder = new HelpScreenBuilder();
            ScreenManager.PushScreen(new HelpScreen(builder.Build()));
        }

        private static bool OpenDebugCommandsScreen()
        {
            if (Settings.DebugCommandsEnabled == null || !Settings.DebugCommandsEnabled.Value)
            {
                SpeechManager.Output(Message.Localized("ui", "DEBUG.DISABLED"));
                return true;
            }

            ScreenManager.PushScreen(new DebugCommandsScreen());
            return true;
        }

        private static bool ReadGold()
        {
            if (!IsInRunContext())
            {
                return false;
            }

            SaveManager saveManager = GameManagers.GetSaveManager();
            SpeechManager.Output(Message.Localized("ui", "SHORTCUT.GOLD", new { count = saveManager.GetGold() }));
            return true;
        }

        private static bool ReadPyreHealth()
        {
            if (!IsInRunContext())
            {
                return false;
            }

            SaveManager saveManager = GameManagers.GetSaveManager();
            SpeechManager.Output(Message.Localized("ui", "SHORTCUT.PYRE_HEALTH", new
            {
                hp = saveManager.GetTowerHP(),
                max = saveManager.GetMaxTowerHP()
            }));
            return true;
        }

        private static bool IsInRunContext()
        {
            SaveManager saveManager = GameManagers.GetSaveManager();
            global::ScreenManager screenManager = GameManagers.GetScreenManager();
            return saveManager != null &&
                saveManager.HasRun() &&
                saveManager.IsAlive() &&
                screenManager != null &&
                !screenManager.GetScreenActive(global::ScreenName.MainMenu);
        }

    }
}
