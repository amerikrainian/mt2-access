using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI.Elements;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Screens
{
    internal static class CombatOutcomeDescriber
    {
        private static readonly FieldInfo PrimaryStateInformationField = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation")!;
        private static readonly FieldInfo PreviewStateInformationField = AccessTools.Field(typeof(CharacterState), "_previewStateInformation")!;
        private static readonly Type StateInformationType = PreviewStateInformationField.FieldType;
        private static readonly FieldInfo HpDirtiedField = AccessTools.Field(StateInformationType, "hpDirtied")!;
        private static readonly FieldInfo StatusEffectsDirtiedField = AccessTools.Field(StateInformationType, "statusEffectsDirtied")!;
        private static readonly FieldInfo StatusEffectsField = AccessTools.Field(StateInformationType, "statusEffects")!;
        private static readonly FieldInfo DespawnedField = AccessTools.Field(StateInformationType, "despawned")!;
        private static readonly FieldInfo DiedFromEatenField = AccessTools.Field(StateInformationType, "diedFromEaten")!;
        private static readonly FieldInfo BurnedOutField = AccessTools.Field(StateInformationType, "burnedOut")!;
        private static readonly FieldInfo IsSacrificeField = AccessTools.Field(StateInformationType, "isSacrifice")!;
        private static readonly PropertyInfo StatusEffectStackCountProperty = AccessTools.Property(typeof(CharacterState.StatusEffectStack), "Count")!;

        public static Message Describe(CharacterState character, bool includeName)
        {
            if (character == null)
            {
                return null;
            }

            Message outcome = DescribeOutcome(GetOutcome(character));
            if (!includeName)
            {
                return outcome;
            }

            Message name = Message.RawCleaned(AccessibilityLocalizationScope.Run(() => character.GetName()));
            return Message.Join(", ", name, outcome);
        }

        public static bool HasChange(CharacterState character)
        {
            return character != null && GetOutcome(character).Kind != OutcomeKind.NoChange;
        }

        private static Message DescribeOutcome(Outcome outcome)
        {
            switch (outcome.Kind)
            {
                case OutcomeKind.Eaten:
                    return Message.Localized("combat", "COMBAT_OUTCOME.EATEN");
                case OutcomeKind.BurnsOut:
                    return Message.Localized("combat", "COMBAT_OUTCOME.BURNS_OUT");
                case OutcomeKind.Dies:
                    return Message.Localized("combat", "COMBAT_OUTCOME.DIES");
                case OutcomeKind.Despawns:
                    return Message.Localized("combat", "COMBAT_OUTCOME.DESPAWNS");
                case OutcomeKind.Health:
                    return Message.Localized("combat", "COMBAT_OUTCOME.HEALTH", new { hp = outcome.Hp });
                case OutcomeKind.Armor:
                    return Message.Localized("combat", "COMBAT_OUTCOME.ARMOR", new { armor = outcome.Armor });
                default:
                    return Message.Localized("combat", "COMBAT_OUTCOME.NO_CHANGE");
            }
        }

        private static Outcome GetOutcome(CharacterState character)
        {
            object preview = PreviewStateInformationField.GetValue(character);
            if (preview == null)
            {
                return Outcome.NoChange;
            }

            if (GetBool(preview, DiedFromEatenField))
            {
                return new Outcome(OutcomeKind.Eaten, 0);
            }

            if (GetBool(preview, BurnedOutField))
            {
                return new Outcome(OutcomeKind.BurnsOut, 0);
            }

            bool willDie = character.WillDieInPreview();
            if (willDie)
            {
                return new Outcome(OutcomeKind.Dies, 0);
            }

            if (GetBool(preview, DespawnedField))
            {
                return new Outcome(OutcomeKind.Despawns, 0);
            }

            if (!StateHasChanged(character, preview))
            {
                return Outcome.NoChange;
            }

            int currentHp = character.GetHP();
            int previewHp = Math.Max(0, character.PreviewHP());
            int currentArmor = GetStatusEffectStacks(PrimaryStateInformationField.GetValue(character), "armor");
            int previewArmor = GetStatusEffectStacks(preview, "armor");

            if (previewHp != currentHp || currentArmor > 0 && previewArmor <= 0)
            {
                return new Outcome(OutcomeKind.Health, previewHp);
            }

            if (previewArmor != currentArmor)
            {
                return new Outcome(OutcomeKind.Armor, 0, previewArmor);
            }

            return Outcome.NoChange;
        }

        private static bool StateHasChanged(CharacterState character, object preview)
        {
            if (character.PreviewDamageDelta != 0 || GetBool(preview, HpDirtiedField))
            {
                return true;
            }

            object primary = PrimaryStateInformationField.GetValue(character);
            IList dirtied = primary != null ? StatusEffectsDirtiedField.GetValue(primary) as IList : null;
            if (dirtied == null)
            {
                return false;
            }

            for (int i = 0; i < dirtied.Count; i++)
            {
                if (string.Equals(dirtied[i] as string, "armor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool GetBool(object instance, FieldInfo field)
        {
            return instance != null && field.GetValue(instance) is bool value && value;
        }

        private static int GetStatusEffectStacks(object stateInformation, string statusId)
        {
            IDictionary statusEffects = stateInformation != null ? StatusEffectsField.GetValue(stateInformation) as IDictionary : null;
            if (statusEffects == null || string.IsNullOrWhiteSpace(statusId))
            {
                return 0;
            }

            string key = statusId.ToLowerInvariant();
            if (!statusEffects.Contains(key))
            {
                return 0;
            }

            object stack = statusEffects[key];
            return stack != null && StatusEffectStackCountProperty.GetValue(stack) is int count
                ? count
                : 0;
        }

        private enum OutcomeKind
        {
            NoChange,
            Health,
            Armor,
            Dies,
            Despawns,
            Eaten,
            BurnsOut
        }

        private readonly struct Outcome
        {
            public static readonly Outcome NoChange = new Outcome(OutcomeKind.NoChange, 0);

            public Outcome(OutcomeKind kind, int hp)
            {
                Kind = kind;
                Hp = hp;
                Armor = 0;
            }

            public Outcome(OutcomeKind kind, int hp, int armor)
            {
                Kind = kind;
                Hp = hp;
                Armor = armor;
            }

            public OutcomeKind Kind { get; }
            public int Hp { get; }
            public int Armor { get; }
        }
    }
}
