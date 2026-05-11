using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.UI.Elements;
using UnityEngine;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal abstract class ListNavigationGameScreen : GameScreen
    {
        protected readonly ListContainer RootList = new ListContainer
        {
            AnnounceName = false,
            AnnouncePosition = true,
            NavigationAxis = NavigationAxis.Vertical
        };

        private string _signature;
        private HudNavigationScreen _hudNavigationScreen;

        public override void OnPush()
        {
            base.OnPush();
            RootList.FocusFirst();
        }

        public override void OnFocus()
        {
            base.OnFocus();
            if (RootList.FocusIndex >= 0)
            {
                RootList.SetFocusIndex(RootList.FocusIndex);
                return;
            }

            RootList.FocusFirst();
        }

        public override void OnUpdate()
        {
            string nextSignature = BuildSignature();
            if (!string.Equals(nextSignature, _signature, System.StringComparison.Ordinal))
            {
                int oldIndex = RootList.FocusIndex;
                UIElement oldFocused = RootList.FocusedChild;
                string oldAnnouncement = FocusAnnouncementText(oldFocused);
                BuildRegistry();
                RestoreFocusAfterRebuild(oldIndex, oldFocused);
                string newAnnouncement = FocusAnnouncementText(RootList.FocusedChild);
                if (ShouldSuppressUnchangedRebuildAnnouncement(oldFocused, RootList.FocusedChild, oldAnnouncement, newAnnouncement) &&
                    !string.IsNullOrEmpty(oldAnnouncement) &&
                    string.Equals(oldAnnouncement, newAnnouncement, System.StringComparison.Ordinal))
                {
                    UIManager.SuppressNextFocusAnnouncementIfTextMatches(newAnnouncement);
                }
            }
        }

        protected bool SyncHudNavigation()
        {
            global::Hud hud = Core.GameManagers.GetScreenManager()?.GetScreen(global::ScreenName.Hud) as global::Hud;
            bool active = hud != null && hud.IsHudNavigationEnabled();
            if (active)
            {
                if (_hudNavigationScreen == null || _hudNavigationScreen.Parent == null)
                {
                    _hudNavigationScreen = new HudNavigationScreen(hud);
                    PushChild(_hudNavigationScreen);
                }

                return true;
            }

            if (_hudNavigationScreen != null && _hudNavigationScreen.Parent != null)
            {
                RemoveChild(_hudNavigationScreen);
            }

            _hudNavigationScreen = null;
            return false;
        }

        protected override void BuildRegistry()
        {
            ClearRegistry();
            RootList.Clear();
            RootElement = RootList;
            PopulateList();
            _signature = BuildSignature();
        }

        protected abstract void PopulateList();

        protected virtual string BuildSignature()
        {
            return RootList.Children.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        protected virtual void RestoreFocusAfterRebuild(int oldIndex, UIElement oldFocused)
        {
            RootList.SetFocusIndex(oldIndex);
        }

        protected virtual bool ShouldSuppressUnchangedRebuildAnnouncement(
            UIElement oldFocused,
            UIElement newFocused,
            string oldAnnouncement,
            string newAnnouncement)
        {
            return true;
        }

        protected void AddElement(UIElement element, params GameObject[] targets)
        {
            if (element == null)
            {
                return;
            }

            RootList.Add(element);
            Register(element, targets);
        }

        private static string FocusAnnouncementText(UIElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            try
            {
                return new FocusContext().BuildAnnouncement(element)?.Resolve() ?? string.Empty;
            }
            catch (System.Exception ex)
            {
                Log.Info("[AccessibilityMod] Focus rebuild comparison failed: " + ex);
                return string.Empty;
            }
        }
    }
}
