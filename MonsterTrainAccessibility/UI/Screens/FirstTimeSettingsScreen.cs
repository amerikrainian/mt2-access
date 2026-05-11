using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal sealed class FirstTimeSettingsScreen : SettingsControlsScreen
    {
        private static readonly FieldInfo GlobalVolumeControlField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "globalVolumeControl")!;
        private static readonly FieldInfo SfxVolumeControlField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "sfxVolumeControl")!;
        private static readonly FieldInfo MusicVolumeControlField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "musicVolumeControl")!;
        private static readonly FieldInfo BackgroundMuteToggleField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "backgroundMuteToggle")!;
        private static readonly FieldInfo ResolutionDropdownField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "resolutionDropdown")!;
        private static readonly FieldInfo FullScreenDropdownField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "fullScreenDropdown")!;
        private static readonly FieldInfo LanguageDropdownField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "languageDropdown")!;
        private static readonly FieldInfo ConfirmButtonField = AccessTools.Field(typeof(global::FirstTimeSettingsScreen), "confirmButton")!;

        private readonly global::FirstTimeSettingsScreen _screen;

        public FirstTimeSettingsScreen(global::FirstTimeSettingsScreen screen)
        {
            _screen = screen;
        }

        protected override void BuildRegistry()
        {
            ListContainer root = new ListContainer { AnnounceName = false, AnnouncePosition = false };
            RootElement = root;

            AddAudioGroup(root);
            AddGraphicsGroup(root);
            AddOtherGroup(root);
        }

        private void AddAudioGroup(ListContainer root)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(_screen, GlobalVolumeControlField), group);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(_screen, SfxVolumeControlField), group);
            RegisterSettingsVolumeControl(Get<global::VolumeControl>(_screen, MusicVolumeControlField), group);
            RegisterSettingsToggle(Get<GameUISelectableToggle>(_screen, BackgroundMuteToggleField), group);
        }

        private void AddGraphicsGroup(ListContainer root)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(_screen, ResolutionDropdownField), group);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(_screen, FullScreenDropdownField), group);
        }

        private void AddOtherGroup(ListContainer root)
        {
            ListContainer group = AddSettingsGroup(root);
            RegisterSettingsDropdown(Get<GameUISelectableDropdown>(_screen, LanguageDropdownField), group);
            RegisterSettingsButton(Get<GameUISelectableButton>(_screen, ConfirmButtonField), group);
        }
    }
}
