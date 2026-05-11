using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Screens
{
    public abstract class GameScreen : Screen
    {
        private readonly Dictionary<GameObject, UIElement> _registry = new Dictionary<GameObject, UIElement>();
        private readonly List<Action> _unsubscribes = new List<Action>();

        protected GameScreen(Transform autoRegisterRoot = null)
        {
        }

        public override void OnPush()
        {
            _registry.Clear();
            BuildRegistry();
            int elementCount = CountRegisteredElements();
            Log.Info("[AccessibilityMod] Screen pushed: " + GetType().Name + " (" + elementCount + " elements, " + _registry.Count + " targets)");
        }

        public override void OnPop()
        {
            for (int i = _unsubscribes.Count - 1; i >= 0; i--)
            {
                try { _unsubscribes[i]?.Invoke(); }
                catch (Exception ex) { Log.Warn("Unsubscribe failed: " + ex); }
            }
            _unsubscribes.Clear();
            _registry.Clear();
        }

        protected void TrackUnsubscribe(Action unsubscribe)
        {
            if (unsubscribe != null)
            {
                _unsubscribes.Add(unsubscribe);
            }
        }

        public override UIElement GetElement(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            UIElement element;
            if (_registry.TryGetValue(go, out element))
            {
                return element;
            }

            return null;
        }

        protected void Register(GameObject go, UIElement element)
        {
            if (go == null || element == null)
            {
                return;
            }
            _registry[go] = element;
        }

        protected void Register(UIElement element, params GameObject[] targets)
        {
            if (element == null || targets == null)
            {
                return;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                Register(targets[i], element);
            }
        }

        protected void AddToContainer(Container container, GameObject go, UIElement element)
        {
            if (container == null || element == null)
            {
                return;
            }
            container.Add(element);
            Register(go, element);
        }

        protected void ClearRegistry()
        {
            _registry.Clear();
        }

        protected IEnumerable<KeyValuePair<GameObject, UIElement>> GetRegisteredControls()
        {
            return _registry;
        }

        protected abstract void BuildRegistry();

        private int CountRegisteredElements()
        {
            HashSet<UIElement> elements = new HashSet<UIElement>();
            foreach (KeyValuePair<GameObject, UIElement> pair in _registry)
            {
                if (pair.Value != null)
                {
                    elements.Add(pair.Value);
                }
            }

            return elements.Count;
        }

        protected static bool IsUsable(GameObject go)
        {
            if (go == null)
            {
                return false;
            }
            return go.activeInHierarchy;
        }

        protected static bool IsVisible(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }

        protected static bool IsUsable(Selectable selectable)
        {
            return selectable != null && selectable.gameObject.activeInHierarchy && selectable.interactable;
        }
    }
}
