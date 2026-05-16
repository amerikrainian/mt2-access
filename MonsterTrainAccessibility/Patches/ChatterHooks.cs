using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Events;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.Patches
{
    internal static class ChatterHooks
    {
        private static readonly Stack<ChatterContext> ContextStack = new Stack<ChatterContext>();

        public static void Chatter_DisplayChatter_Prefix(
            ChatterExpressionType expressionType,
            CharacterState character,
            float delay,
            CharacterTriggerData.Trigger trigger)
        {
            ContextStack.Push(new ChatterContext(expressionType, character, trigger));
        }

        public static void Chatter_DisplayChatter_Postfix()
        {
            if (ContextStack.Count > 0)
            {
                ContextStack.Pop();
            }
        }

        public static void ChatterExpression_Express_Prefix(
            ChatterExpressionType expressionType,
            CharacterState character,
            string translatedText)
        {
            try
            {
                if (PopupNotificationManagerBase.ArePopupsSilencedForTFBDeath() ||
                    CharacterVitalsSignalTracker.ShouldSkip(character) ||
                    !Message.ShouldAdd(Message.Clean(translatedText)))
                {
                    return;
                }

                ChatterContext context = FindContext(expressionType, character);
                bool hasTrigger = context.HasValue && expressionType == ChatterExpressionType.CharacterTrigger;
                CharacterTriggerData.Trigger trigger = context.HasValue
                    ? context.Trigger
                    : CharacterTriggerData.Trigger.OnDeath;

                EventDispatcher.Enqueue(CharacterChatterEvent.Create(
                    character,
                    expressionType,
                    hasTrigger,
                    trigger,
                    translatedText));
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Character chatter hook failed: " + ex);
            }
        }

        private static ChatterContext FindContext(ChatterExpressionType expressionType, CharacterState character)
        {
            foreach (ChatterContext context in ContextStack)
            {
                if (context.ExpressionType == expressionType && ReferenceEquals(context.Character, character))
                {
                    return context;
                }
            }

            return default;
        }

        private readonly struct ChatterContext
        {
            public ChatterContext(
                ChatterExpressionType expressionType,
                CharacterState character,
                CharacterTriggerData.Trigger trigger)
            {
                HasValue = true;
                ExpressionType = expressionType;
                Character = character;
                Trigger = trigger;
            }

            public bool HasValue { get; }
            public ChatterExpressionType ExpressionType { get; }
            public CharacterState Character { get; }
            public CharacterTriggerData.Trigger Trigger { get; }
        }
    }
}
