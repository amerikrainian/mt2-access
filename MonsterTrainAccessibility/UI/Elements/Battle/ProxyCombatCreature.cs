using System;
using MonsterTrainAccessibility.Util;
using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.Presentation.Verbosity;
using MonsterTrainAccessibility.UI;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed partial class ProxyCombatCreature : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly CharacterState _character;
        private readonly ICreatureNavigationSource _navigation;
        private readonly int _roomIndex;

        public ProxyCombatCreature(CharacterState character, int roomIndex, ICreatureNavigationSource navigation)
        {
            _character = character;
            _roomIndex = roomIndex;
            _navigation = navigation;
        }

        public CharacterState Character => _character;
        public override bool IsVisible => _character != null && !_character.IsDestroyed && _character.GetCharacterUI() != null && _character.GetCharacterUI().gameObject.activeInHierarchy;
        public override Message GetLabel() => FocusSummary(_character);
        public override Message GetTooltip() => Tooltip(_character);

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CharacterState> buffer = buffers?.GetBuffer("creature") as PresentationBuffer<CharacterState>;
            if (buffer == null || _character == null)
            {
                return "ui";
            }

            buffer.Bind(_character);
            buffers.EnableBuffer("creature", true);
            return "creature";
        }

        public void SelectForNavigation()
        {
            _navigation?.ViewCharacter(_character, _roomIndex);
        }

        public bool Activate()
        {
            return true;
        }

        internal static Message Label(CharacterState character)
        {
            return Message.RawCleaned(AccessibilityLocalizationScope.Run(() => character?.GetName()));
        }

        internal static Message FocusSummary(CharacterState character)
        {
            return character != null
                ? PresentationRenderer.FocusSummary(
                    PhaseRegistry.Creatures.Build(character),
                    VerbosityRegistry.ForSource(character))
                : null;
        }

        internal static IReadOnlyList<Message> StatParts(CharacterState character)
        {
            List<Message> parts = new List<Message>();
            if (character == null)
            {
                return parts;
            }

            if (character.GetTeamType() == Team.Type.Monsters && !character.IsPyreHeart())
            {
                int size = character.GetSize();
                if (size > 0)
                {
                    parts.Add(Message.Localized("ui", "CARD_STATS.SIZE", new { size }));
                }
            }

            int attack = character.GetAttackDamage();
            if (attack > 0 || character.IsPyreHeart())
            {
                parts.Add(Message.Localized("combat", "CREATURE.ATTACK", new { attack }));
            }

            int armor = character.GetStatusEffectStacks("armor");
            if (armor > 0)
            {
                parts.Add(Message.Localized("combat", "CREATURE.ARMOR_HP", new { armor, hp = character.GetHP(), maxHp = character.GetMaxHP() }));
            }
            else
            {
                parts.Add(Message.Localized("combat", "CREATURE.HP", new { hp = character.GetHP(), maxHp = character.GetMaxHP() }));
            }

            return parts;
        }

        internal static Message Tooltip(CharacterState character)
        {
            if (character == null)
            {
                return null;
            }
            return BuildTooltip(character, CollectSemanticTooltips(character));
        }

        internal static Message MainTooltipBody(CharacterState character)
        {
            if (character == null)
            {
                return null;
            }

            TooltipContent main = AccessibilityLocalizationScope.Run(() =>
                CharacterTooltipHelper.GetMainTooltipContent(character));
            return Message.FromText(main.body);
        }

        private static Message BuildTooltip(CharacterState character, List<TooltipContent> tooltips)
        {
            List<Message> parts = new List<Message>();
            MessageList.Add(parts, MainTooltipBody(character));
            if (character.IsOuterTrainBoss())
            {
                MessageList.Add(parts, BossIntent(character).Tooltip);
            }
            MessageList.Add(parts, MessageList.TooltipList(tooltips));
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal static List<TooltipContent> CollectSemanticTooltips(CharacterState character)
        {
            return CollectSemanticTooltips(character, isFromCompendium: false);
        }

        internal static List<TooltipContent> CollectSemanticTooltips(CharacterState character, bool isFromCompendium, CovenantData covenantData = null)
        {
            List<TooltipContent> tooltips = new List<TooltipContent>();
            if (character == null)
            {
                return tooltips;
            }

            StatusEffectManager statusEffectManager = StatusEffectManager.Instance;
            SaveManager saveManager = AllGameManagers.Instance.OrNull()?.GetSaveManager();
            if (statusEffectManager == null || saveManager == null)
            {
                return tooltips;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                IEnumerable<(TooltipContent content, bool allowDuplicates)> abilityTooltips = AccessibilityLocalizationScope.Run(() =>
                    CharacterTooltipHelper.GetAbilityTooltips(character, statusEffectManager, saveManager));
                foreach (var abilityTooltip in abilityTooltips)
                {
                    AddTooltip(tooltips, seen, abilityTooltip.content, abilityTooltip.allowDuplicates);
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect creature ability tooltips: " + ex);
            }

            try
            {
                IEnumerable<(TooltipContent content, bool allowDuplicates)> additionalTooltips = AccessibilityLocalizationScope.Run(() =>
                    CharacterTooltipHelper.GetAdditionalTooltipContents(character, isFromCompendium, statusEffectManager, saveManager, covenantData));
                foreach (var tooltip in additionalTooltips)
                {
                    AddTooltip(tooltips, seen, tooltip.content, tooltip.allowDuplicates);
                }
            }
            catch (Exception ex)
            {
                Log.Info("[AccessibilityMod] Failed to collect creature tooltips: " + ex);
            }

            return tooltips;
        }

        internal static HashSet<string> VisibleStatusIds(CharacterState character)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (character == null)
            {
                return ids;
            }

            List<CharacterState.StatusEffectStack> statusEffects = new List<CharacterState.StatusEffectStack>();
            character.GetStatusEffects(ref statusEffects);
            for (int i = 0; i < statusEffects.Count; i++)
            {
                string statusId = statusEffects[i].State?.GetStatusId();
                if (!string.IsNullOrWhiteSpace(statusId) &&
                    !string.Equals(statusId, "armor", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusId, "unit_ability", StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(statusId);
                }
            }

            return ids;
        }

        internal static Message UnitAbilitySummary(CharacterState character)
        {
            if (character == null || !character.HasUnitAbility())
            {
                return null;
            }

            CardState abilityState = character.GetUnitAbilityCardState();
            CardData ability = character.GetUnitAbility();
            Message abilityName = Message.FromText(AccessibilityLocalizationScope.Run(() =>
                abilityState?.GetTitle() ?? ability?.GetName()));
            if (abilityName == null)
            {
                return null;
            }

            Message status = UnitAbilityStatus(character);
            return status != null
                ? Message.Localized("combat", "CREATURE.ABILITY", new { ability = abilityName.Resolve(), status = status.Resolve() })
                : abilityName;
        }

        private static Message UnitAbilityStatus(CharacterState character)
        {
            CombatManager combatManager = AllGameManagers.Instance.OrNull()?.GetCombatManager();
            CharacterState.UnitAbilityAvailability availability = character.GetUnitAbilityAvailability(combatManager);
            switch (availability)
            {
                case CharacterState.UnitAbilityAvailability.CanActivate:
                    return Message.Localized("combat", "CREATURE.ABILITY_READY");
                case CharacterState.UnitAbilityAvailability.Cooldown:
                    return Message.Localized("combat", "CREATURE.ABILITY_COOLDOWN", new { turns = character.GetStatusEffectStacks("cooldown") });
                case CharacterState.UnitAbilityAvailability.DeploymentPhase:
                    return Message.Localized("combat", "CREATURE.ABILITY_DEPLOYMENT");
                case CharacterState.UnitAbilityAvailability.HandFull:
                    return Message.Localized("combat", "CREATURE.ABILITY_HAND_FULL");
                case CharacterState.UnitAbilityAvailability.Disabled:
                    return Message.Localized("combat", "CREATURE.ABILITY_DISABLED");
                default:
                    return Message.Localized("combat", "CREATURE.ABILITY_UNAVAILABLE");
            }
        }

        private static void AddTooltip(List<TooltipContent> tooltips, HashSet<string> seen, TooltipContent tooltip, bool allowDuplicates)
        {
            MessageList.TryAddTooltip(tooltips, seen, tooltip, allowDuplicates);
        }

        internal static (Message Status, Message Tooltip, IReadOnlyList<Message> Parts) CurrentBossIntent()
        {
            CharacterState boss = global::AllGameManagers.Instance?.GetHeroManager()?.GetOuterTrainBossCharacter();
            return BossIntent(boss);
        }

        internal static (Message Status, Message Tooltip, IReadOnlyList<Message> Parts) BossIntent(CharacterState boss)
        {
            if (boss == null || !boss.IsOuterTrainBoss() || !boss.IsAlive)
            {
                return (null, null, Array.Empty<Message>());
            }

            BossActionState action = boss.GetNextBossAction();
            if (action == null)
            {
                return (null, null, Array.Empty<Message>());
            }

            Message description = Message.FromText(
                AccessibilityLocalizationScope.Run(() => action.GetTooltipDescription()));
            Message targetFloor = action.GetTargetedRoomIndex() >= 0
                ? Message.Localized("ui", "HUD.BOSS_TARGET_FLOOR", new { floor = action.GetTargetedRoomIndex() + 1 })
                : null;
            Message postActionFloor = ShouldAnnouncePostAction(action)
                ? Message.Localized("ui", "HUD.BOSS_POST_ACTION_FLOOR", new { floor = action.GetPostActionRoomIndex() + 1 })
                : null;

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, description);
            MessageList.Add(parts, targetFloor);
            MessageList.Add(parts, postActionFloor);

            List<Message> statusParts = new List<Message>();
            MessageList.Add(statusParts, targetFloor);
            MessageList.Add(statusParts, description);

            return (
                statusParts.Count > 0 ? Message.Join(", ", statusParts) : null,
                parts.Count > 0 ? Message.Join(". ", parts) : null,
                parts);
        }

        private static bool ShouldAnnouncePostAction(BossActionState action)
        {
            if (action == null || action.IsRoomDestroyAction())
            {
                return false;
            }

            int targeted = action.GetTargetedRoomIndex();
            int postAction = action.GetPostActionRoomIndex();
            return postAction >= 0 && postAction != targeted;
        }
    }
}
