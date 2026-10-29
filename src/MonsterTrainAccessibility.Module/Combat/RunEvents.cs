using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Events;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Module.Interop;

namespace MonsterTrainAccessibility.Module.Combat
{
    /// <summary>
    /// The run's events outside any one screen: coin gained or spent ("Coin +25.") and forge points
    /// ("Forge 3."), wherever they change (a shop, an event, a battle). Listens to the live save manager,
    /// re-attaching when the game replaces it; every listener comes off in <see cref="Shutdown"/>.
    /// </summary>
    internal static class RunEvents
    {
        private static SaveManager s_save;

        public static void Tick()
        {
            var save = AllGameManagers.Instance != null ? AllGameManagers.Instance.GetSaveManager() : null;
            if (ReferenceEquals(save, s_save)) return;
            Shutdown();
            s_save = save;
            if (save == null) return;
            save.goldChangedSignal.AddListener(OnGold);
            save.forgePointsChangedSignal.AddListener(OnForge);
        }

        public static void Shutdown()
        {
            if (s_save == null) return;
            s_save.goldChangedSignal.RemoveListener(OnGold);
            s_save.forgePointsChangedSignal.RemoveListener(OnForge);
            s_save = null;
        }

        private static void OnGold(SaveManager.GoldChangedSignalData data)
        {
            if (data.IsTotalReset) return;
            int delta = data.newGold - data.prevGold;
            if (delta == 0) return;
            string coin = GameText.Term("HudTooltip_Gold_Title");
            GameEvents.Raise(EventKinds.Gold, Strings.Event(delta > 0 ? "gold_gained" : "gold_lost", coin, System.Math.Abs(delta)));
        }

        private static void OnForge(int newPoints, int oldPoints)
        {
            int now = s_save != null ? s_save.GetForgePoints() : newPoints;
            if (now != oldPoints) GameEvents.Raise(EventKinds.ForgePoints, Strings.Event("forge_points", now));
        }
    }

    /// <summary>
    /// The units' chatter as the game shows it in their speech bubbles ("Fel summoned, "..."), to the
    /// monster quotes buffer (spoken only when the player turns a kind on). The game picks the line and
    /// its reason in <c>Chatter.DisplayChatter</c>, which hands it straight to a bubble; the reason a
    /// trigger line was said is noted on the way in, the line taken as the bubble gets it.
    /// </summary>
    [HarmonyPatch(typeof(Chatter), nameof(Chatter.DisplayChatter))]
    internal static class ChatterEvents
    {
        private static CharacterTriggerData.Trigger? s_trigger;

        private static void Prefix(ChatterExpressionType expressionType, CharacterTriggerData.Trigger trigger)
            => s_trigger = expressionType == ChatterExpressionType.CharacterTrigger ? trigger : (CharacterTriggerData.Trigger?)null;

        private static void Postfix() => s_trigger = null;

        [HarmonyPatch(typeof(ChatterExpression), nameof(ChatterExpression.Express))]
        private static class Bubble
        {
            private static void Prefix(ChatterExpressionType expressionType, CharacterState character, string translatedText)
            {
                try { Say(expressionType, character, translatedText); }
                catch (System.Exception e) { CoreLog.Warning("chatter: " + e.Message); }
            }
        }

        private static void Say(ChatterExpressionType type, CharacterState character, string quote)
        {
            if (PopupNotificationManagerBase.ArePopupsSilencedForTFBDeath() || VitalsTracker.Skip(character)) return;
            if (string.IsNullOrWhiteSpace(quote) || quote.Trim() == ".") return;
            string name = VitalsTracker.Name(character);
            if (string.IsNullOrWhiteSpace(name)) return;
            EventKind kind;
            string reason;
            switch (type)
            {
                case ChatterExpressionType.CharacterAdded: kind = EventKinds.ChatterSummoned; reason = Strings.Event("chatter.summoned"); break;
                case ChatterExpressionType.CharacterAttacking: kind = EventKinds.ChatterAttacking; reason = Strings.Event("chatter.attacking"); break;
                case ChatterExpressionType.CharacterSlayed: kind = EventKinds.ChatterSlay; reason = Strings.Event("chatter.slay"); break;
                case ChatterExpressionType.CharacterIdle: kind = EventKinds.ChatterIdle; reason = Strings.Event("chatter.idle"); break;
                case ChatterExpressionType.CharacterTrigger:
                    kind = EventKinds.ChatterTrigger;
                    reason = s_trigger.HasValue ? CharacterTriggerData.GetKeywordText(s_trigger.Value, inBold: false) : null;
                    break;
                default: return;
            }
            string text = string.IsNullOrWhiteSpace(reason)
                ? Strings.Event("chatter", name, quote.Trim())
                : Strings.Event("chatter_reason", name, reason, quote.Trim());
            GameEvents.Raise(kind, text, VitalsTracker.Side(character));
        }
    }
}
