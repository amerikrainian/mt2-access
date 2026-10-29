using System;
using MonsterTrainAccessibility.Core.Strings;

namespace MonsterTrainAccessibility.Module.Combat
{
    /// <summary>
    /// What the coming combat does to a unit, from the game's own preview of it (the copy of the unit's
    /// state the game plays the combat out on, which it redraws as the board changes): eaten, burns out,
    /// dies, despawns, its health after, its armor after, or no change. The first that holds wins, in
    /// that order.
    /// </summary>
    internal static class Outcomes
    {
        /// <summary>The outcome's words ("dies", "12 health"), or null for no change.</summary>
        public static string Of(CharacterState unit)
        {
            if (unit == null || unit.IsDestroyed) return null;
            var preview = unit._previewStateInformation;
            if (preview == null) return null;
            if (preview.diedFromEaten) return Strings.Battle("outcome.eaten");
            if (preview.burnedOut) return Strings.Battle("outcome.burns_out");
            if (unit.WillDieInPreview()) return Strings.Battle("outcome.dies");
            if (preview.despawned) return Strings.Battle("outcome.despawns");
            if (!Changed(unit, preview)) return null;
            int hp = unit.GetHP();
            int previewHp = Math.Max(0, unit.PreviewHP());
            int armor = Armor(unit._primaryStateInformation);
            int previewArmor = Armor(preview);
            if (previewHp != hp || (armor > 0 && previewArmor <= 0)) return Strings.Battle("outcome.health", previewHp);
            if (previewArmor != armor) return Strings.Battle("outcome.armor", previewArmor);
            return null;
        }

        private static bool Changed(CharacterState unit, CharacterState.StateInformation preview)
        {
            if (unit.PreviewDamageDelta != 0 || preview.hpDirtied) return true;
            var dirtied = unit._primaryStateInformation?.statusEffectsDirtied;
            if (dirtied == null) return false;
            foreach (var id in dirtied)
                if (string.Equals(id, "armor", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static int Armor(CharacterState.StateInformation state)
            => state?.statusEffects != null && state.statusEffects.TryGetValue("armor", out var stack) && stack != null ? stack.Count : 0;
    }
}
