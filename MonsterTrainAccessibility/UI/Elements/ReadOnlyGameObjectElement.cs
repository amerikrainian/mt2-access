using System;
using MonsterTrainAccessibility.Localization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ReadOnlyGameObjectElement : CustomElement, INavigationTargetElement
    {
        public GameObject Target { get; }

        public ReadOnlyGameObjectElement(
            GameObject target,
            Func<Message> label,
            Func<Message> status = null,
            Func<Message> tooltip = null,
            Func<Message> extras = null,
            Func<bool> visibility = null,
            string typeKey = null)
            : base(
                label: label,
                status: status,
                tooltip: tooltip,
                extras: extras,
                visibility: visibility ?? (() => target == null || target.activeInHierarchy),
                typeKey: typeKey)
        {
            Target = target;
        }

        public void SelectForNavigation()
        {
            if (Target == null)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(Target);
            }
        }
    }
}
