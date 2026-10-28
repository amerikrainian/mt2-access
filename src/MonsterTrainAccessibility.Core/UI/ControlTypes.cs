using MonsterTrainAccessibility.Core.Graph;

namespace MonsterTrainAccessibility.Core.UI
{
    /// <summary>
    /// The control-type registry: each entry is a <see cref="ControlType"/> VALUE, its settings key, the
    /// speak order of its announcement kinds, and the parts common to every control of the type (the
    /// localized role word). A node factory sets the type and gets the role word, the ordering, and the
    /// user's per-type announcement settings for free.
    /// </summary>
    public static class ControlTypes
    {
        private static readonly string[] StandardOrder =
        {
            AnnouncementKinds.Label,
            AnnouncementKinds.Role,
            AnnouncementKinds.Value,
            AnnouncementKinds.Selected,
            AnnouncementKinds.Enabled,
            AnnouncementKinds.Tooltip,
            AnnouncementKinds.Position,
        };

        private static NodeAnnouncement[] RoleWord(string word)
            => new[] { new NodeAnnouncement(() => Strings.Strings.Role(word), kind: AnnouncementKinds.Role) };

        public static readonly ControlType Button = new ControlType
        {
            Key = "button",
            Order = StandardOrder,
            Common = () => RoleWord("button"),
        };

        public static readonly ControlType Toggle = new ControlType
        {
            Key = "toggle",
            Order = StandardOrder,
            Common = () => RoleWord("toggle"),
        };

        public static readonly ControlType Slider = new ControlType
        {
            Key = "slider",
            Order = StandardOrder,
            Common = () => RoleWord("slider"),
        };

        /// <summary>A text field: the screen that declares one types into it while it is focused
        /// (type-ahead standing down there) and echoes each character.</summary>
        public static readonly ControlType Edit = new ControlType
        {
            Key = "edit",
            Order = StandardOrder,
            Common = () => RoleWord("edit"),
        };

        /// <summary>One option of a single-select group (dropdown options, tab rows).</summary>
        public static readonly ControlType RadioButton = new ControlType
        {
            Key = "radio_button",
            Order = StandardOrder,
            Common = () => RoleWord("radio button"),
        };

        /// <summary>A dropdown: value = the current option; activation opens the option submenu.</summary>
        public static readonly ControlType ComboBox = new ControlType
        {
            Key = "combo_box",
            Order = StandardOrder,
            Common = () => RoleWord("combo box"),
        };

        /// <summary>A tab in a tab strip (settings pages, window pages).</summary>
        public static readonly ControlType Tab = new ControlType
        {
            Key = "tab",
            Order = StandardOrder,
            Common = () => RoleWord("tab"),
        };

        /// <summary>One binding slot of a key-binding row (label + current combo; rebind/clear).</summary>
        public static readonly ControlType KeyBinding = new ControlType
        {
            Key = "key_binding",
            Order = StandardOrder,
            Common = () => RoleWord("key binding"),
        };

        /// <summary>An inventory/relic/shop item ("label, item[, state]").</summary>
        public static readonly ControlType Item = new ControlType
        {
            Key = "item",
            Order = StandardOrder,
            Common = () => RoleWord("item"),
        };

        /// <summary>A hyperlink-like control that opens something elsewhere (community, wishlist).</summary>
        public static readonly ControlType Link = new ControlType
        {
            Key = "link",
            Order = StandardOrder,
            Common = () => RoleWord("link"),
        };

        /// <summary>An expandable group header (a tree section). No role word of its own; the announcer
        /// appends the expanded/collapsed state word.</summary>
        public static readonly ControlType Group = new ControlType
        {
            Key = "group",
            Order = StandardOrder,
        };

        /// <summary>A read-only text line: no role word; typed so its parts are still user-configurable.</summary>
        public static readonly ControlType Text = new ControlType
        {
            Key = "text",
            Order = StandardOrder,
        };

        /// <summary>Every registered type, for settings registration. New types are added here.</summary>
        public static readonly ControlType[] All = { Button, Toggle, Slider, RadioButton, ComboBox, Tab, KeyBinding, Item, Link, Edit, Group, Text };
    }
}
