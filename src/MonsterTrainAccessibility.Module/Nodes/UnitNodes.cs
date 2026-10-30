using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Combat;
using MonsterTrainAccessibility.Module.UI;

namespace MonsterTrainAccessibility.Module.Nodes
{
    /// <summary>
    /// A unit on the train (<see cref="CharacterState"/>), either side, the pyre included, read live:
    /// <list type="bullet">
    /// <item>the focus line: its name, then its subtypes ("Steward", the player's units only), size (the
    /// player's units), attack (when it has any, or the pyre's), armor and health ("2 armor, 5 of 8
    /// health"), its statuses with their stacks ("Rage 3"), its equipment ("Windblade"), its
    /// ability and where it stands ("Quick Strike, ready"), its triggers by keyword ("Slay");</item>
    /// <item>the buffer: that line, then a boss's next action ("targets floor 2"), then the game's own
    /// tooltips for the unit (its equipment, statuses, abilities, triggers, the room's effects on it),
    /// one per line, in the game's order (<see cref="CharacterTooltipHelper"/>).</item>
    /// </list>
    /// </summary>
    internal static class UnitNodes
    {
        public static NodeVtable Unit(CharacterState unit, Action focus)
        {
            Func<string> name = () => Live(unit) ? unit.GetName() : null;
            return new NodeVtable
            {
                ControlType = ControlTypes.Text,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(name),
                    new NodeAnnouncement(() => Live(unit) ? string.Join(", ", Summary(unit)) : null, kind: AnnouncementKinds.Value),
                },
                SearchText = name,
                Subject = () => unit,
                OnFocus = focus,
                Details = () => Live(unit) ? GameNodes.Lines(Details(unit)) : null,
            };
        }

        public static bool Live(CharacterState unit) => unit != null && !unit.IsDestroyed;

        /// <summary>Everything the focus line says after the name, in order.</summary>
        public static List<string> Summary(CharacterState unit)
        {
            var parts = new List<string>();
            bool ours = unit.GetTeamType() == Team.Type.Monsters && !unit.IsPyreHeart();
            if (unit.GetTeamType() == Team.Type.Monsters)
                Add(parts, CharacterTooltipHelper.GetMainTooltipContent(unit).body);
            if (ours && unit.GetSize() > 0) parts.Add(Strings.Battle("size", unit.GetSize()));
            int attack = unit.GetAttackDamage();
            if (attack > 0 || unit.IsPyreHeart()) parts.Add(Strings.Battle("attack", attack));
            int armor = unit.GetStatusEffectStacks("armor");
            parts.Add(armor > 0
                ? Strings.Battle("armor_hp", armor, unit.GetHP(), unit.GetMaxHP())
                : Strings.Battle("hp", unit.GetHP(), unit.GetMaxHP()));
            parts.AddRange(Statuses(unit));
            parts.AddRange(Equipment(unit));
            Add(parts, Ability(unit));
            parts.AddRange(Triggers(unit));
            return parts;
        }

        // Each status the unit shows, by its name and stacks as the game words it ("Rage 3"); armor is
        // in the health part, the ability's cooldown in the ability's.
        private static IEnumerable<string> Statuses(CharacterState unit)
        {
            var stacks = new List<CharacterState.StatusEffectStack>();
            unit.GetStatusEffects(ref stacks);
            foreach (var stack in stacks)
            {
                var state = stack?.State;
                if (state == null || stack.Count <= 0 || state.IsHidden()) continue;
                string id = state.GetStatusId();
                if (id == "armor" || id == "unit_ability" || id == "cooldown") continue;
                string name = StatusEffectManager.GetLocalizedName(id, stack.Count, inBold: false, showStacks: state.ShowStackCount());
                if (!string.IsNullOrWhiteSpace(name)) yield return name;
            }
        }

        // Each piece of equipment the unit carries, by its card's title ("Windblade"); what it
        // does is the game's tooltip, in the buffer.
        private static IEnumerable<string> Equipment(CharacterState unit)
        {
            var equipment = unit.GetEquipment();
            if (equipment == null) yield break;
            foreach (var card in equipment)
            {
                string title = card?.GetTitle();
                if (!string.IsNullOrWhiteSpace(title)) yield return title.Trim();
            }
        }

        // "Quick Strike, ready": the ability card's title and its availability now.
        private static string Ability(CharacterState unit)
        {
            if (!unit.HasUnitAbility()) return null;
            string name = unit.GetUnitAbilityCardState()?.GetTitle();
            if (string.IsNullOrWhiteSpace(name)) return null;
            string state = AbilityState(unit, unit.GetUnitAbilityAvailability(AllGameManagers.Instance?.GetCombatManager()));
            return Strings.Battle("ability", name, state);
        }

        /// <summary>Where an ability stands: "ready", "2 turns", "deployment phase".</summary>
        public static string AbilityState(CharacterState unit, CharacterState.UnitAbilityAvailability availability)
        {
            switch (availability)
            {
                case CharacterState.UnitAbilityAvailability.CanActivate: return Strings.Battle("ability.ready");
                case CharacterState.UnitAbilityAvailability.Cooldown: return Strings.Battle("ability.cooldown", unit.GetStatusEffectStacks("cooldown"));
                case CharacterState.UnitAbilityAvailability.DeploymentPhase: return Strings.Battle("ability.deployment");
                case CharacterState.UnitAbilityAvailability.HandFull: return Strings.Battle("ability.hand_full");
                case CharacterState.UnitAbilityAvailability.Disabled: return Strings.Battle("ability.disabled");
                default: return Strings.Battle("ability.unavailable");
            }
        }

        // The triggers the game shows on the unit, by keyword, each once.
        private static IEnumerable<string> Triggers(CharacterState unit)
        {
            var seen = new HashSet<string>();
            foreach (var trigger in unit.GetTriggers())
            {
                if (trigger == null || !trigger.ShouldDisplayOnCharacterTooltips()) continue;
                string keyword = trigger.GetKeywordText();
                if (!string.IsNullOrWhiteSpace(keyword) && seen.Add(keyword)) yield return keyword;
            }
        }

        private static List<string> Details(CharacterState unit)
        {
            var lines = new List<string>();
            lines.AddRange(BossIntent(unit));
            var managers = AllGameManagers.Instance;
            var tooltips = new List<TooltipContent>();
            foreach (var t in CharacterTooltipHelper.GetAbilityTooltips(unit, managers.GetStatusEffectManager(), managers.GetSaveManager()))
                tooltips.Add(t.content);
            foreach (var t in CharacterTooltipHelper.GetAdditionalTooltipContents(unit, false, managers.GetStatusEffectManager(), managers.GetSaveManager()))
                tooltips.Add(t.content);
            lines.AddRange(TooltipReader.Lines(tooltips));
            return lines;
        }

        /// <summary>A boss's next action: what it does, the floor it targets, the floor it then moves to.</summary>
        public static List<string> BossIntent(CharacterState unit)
        {
            var lines = new List<string>();
            if (unit == null || !unit.IsOuterTrainBoss() || !unit.IsAlive) return lines;
            var action = unit.GetNextBossAction();
            if (action == null) return lines;
            Add(lines, action.GetTooltipDescription());
            int target = action.GetTargetedRoomIndex();
            if (target >= 0) lines.Add(Strings.Battle("boss_targets", target + 1));
            int after = action.GetPostActionRoomIndex();
            if (!action.IsRoomDestroyAction() && after >= 0 && after != target) lines.Add(Strings.Battle("boss_moves", after + 1));
            return lines;
        }

        private static void Add(List<string> parts, string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) parts.Add(text.Trim());
        }
    }
}
