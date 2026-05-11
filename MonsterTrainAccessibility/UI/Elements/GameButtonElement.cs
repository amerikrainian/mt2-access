using System;
using MonsterTrainAccessibility.Localization;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class GameButtonElement : GameObjectElement
    {
        public GameButtonElement(
            GameObject target,
            Func<Message> label = null,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null)
            : base(
                target: target,
                typeKey: "button",
                label: label ?? (() => DefaultLabel(target)),
                status: status ?? (() => StateMessage(Button(target))),
                tooltip: tooltip ?? (() => TooltipText.ForComponent(target != null ? target.transform : null)),
                extras: extras,
                visibility: visibility)
        {
        }

        private static global::ShinyShoe.GameUISelectableButton Button(GameObject target)
        {
            return target != null ? target.GetComponent<global::ShinyShoe.GameUISelectableButton>() : null;
        }

        public static Message StateMessage(global::ShinyShoe.GameUISelectableButton button)
        {
            if (button == null)
            {
                return null;
            }

            if (button.state == global::ShinyShoe.GameUISelectableButton.State.Locked)
            {
                return Message.Localized("messages", "state.locked");
            }

            if (!button.interactable || button.state == global::ShinyShoe.GameUISelectableButton.State.Disabled)
            {
                return Message.Localized("messages", "state.disabled");
            }

            if (button.state == global::ShinyShoe.GameUISelectableButton.State.Activated)
            {
                return Message.Localized("messages", "state.selected");
            }

            return null;
        }
    }
}
