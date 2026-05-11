using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxySettingsVolumeMute : GameObjectElement
    {
        private static readonly FieldInfo VolumeMutedField = AccessTools.Field(typeof(global::VolumeControl), "muted")!;

        private readonly global::VolumeControl _control;
        private readonly GameUISelectableButton _button;

        public ProxySettingsVolumeMute(global::VolumeControl control, GameUISelectableButton button)
            : base(
                button != null ? button.gameObject : null,
                typeKey: "toggle",
                label: null)
        {
            _control = control;
            _button = button;
        }

        public override bool IsVisible => _button != null && _button.gameObject.activeInHierarchy;
        public override Message GetLabel() => Message.RawCleaned(ProxySettingsButton.FindLabelInSettingsEntry(_control));
        public override Message GetStatusString() => StateMessage(IsVolumeMuted(_control));

        public static Message StateMessage(bool value)
        {
            return new Message(value ? "state.on" : "state.off");
        }

        public static bool IsVolumeMuted(global::VolumeControl control)
        {
            object value = VolumeMutedField.GetValue(control);
            return value is bool muted && muted;
        }
    }
}
