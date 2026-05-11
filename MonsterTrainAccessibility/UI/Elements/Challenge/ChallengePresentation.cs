using System.Collections.Generic;
using System.Globalization;
using MonsterTrainAccessibility.Localization;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal static class ChallengePresentation
    {
        internal static Message ClassWithChampion(ClassData classData, int championIndex)
        {
            Message clan = Message.FromText(classData?.GetTitle());
            Message champion = Message.FromText(ChampionName(classData, championIndex));
            return champion != null
                ? Message.Localized("ui", "CHALLENGE.CLAN_WITH_CHAMPION", new { clan = clan?.Resolve(), champion = champion.Resolve() })
                : clan;
        }

        internal static Message ClassDescription(ClassData classData, bool mainClass)
        {
            if (classData == null)
            {
                return null;
            }

            return Message.FromText(mainClass ? classData.GetDescription() : classData.GetSubclassDescription());
        }

        internal static Message MainClass(ClassData classData, int championIndex)
        {
            Message value = ClassWithChampion(classData, championIndex);
            return value != null
                ? Message.Localized("ui", "CHALLENGE.MAIN_CLAN", new { clan = value.Resolve() })
                : null;
        }

        internal static Message AlliedClass(ClassData classData, int championIndex)
        {
            Message value = ClassWithChampion(classData, championIndex);
            return value != null
                ? Message.Localized("ui", "CHALLENGE.ALLIED_CLAN", new { clan = value.Resolve() })
                : null;
        }

        internal static Message Covenant(int level)
        {
            return Message.Localized("ui", "CHALLENGE.COVENANT", new
            {
                level = CovenantLevelString(level)
            });
        }

        internal static Message CovenantTooltip(int level, IReadOnlyList<DLC> enabledDlcs)
        {
            IReadOnlyList<Message> lines = CovenantTooltipLines(level, enabledDlcs);
            return lines.Count > 0 ? Message.JoinLines(lines) : null;
        }

        internal static IReadOnlyList<Message> CovenantTooltipLines(int level, IReadOnlyList<DLC> enabledDlcs)
        {
            if (level < 1)
            {
                return new List<Message>();
            }

            AllGameData allGameData = AllGameManagers.Instance?.GetAllGameData();
            ChallengeCovenantDisplayData display = allGameData?.GetChallengeCovenantDisplayData();
            if (allGameData == null || display == null)
            {
                return new List<Message>();
            }

            IReadOnlyList<CovenantData> covenants = allGameData.GetAllCovenantsForLevel(level);
            List<Message> lines = new List<Message>();
            IReadOnlyList<DLC> dlcs = enabledDlcs ?? new List<DLC>();
            for (int i = 0; i < covenants.Count; i++)
            {
                CovenantData covenant = covenants[i];
                if (covenant == null || !covenant.IsAscension)
                {
                    continue;
                }

                Message line = Message.FromText(display.GetTooltipContent(covenant, dlcs));
                if (line != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        internal static Message PyreHeartLabel(CharacterData character)
        {
            Message pyre = Message.FromText(character?.GetName());
            return pyre != null
                ? Message.Localized("ui", "CHALLENGE.PYRE_HEART", new { pyre = pyre.Resolve() })
                : null;
        }

        internal static Message PyreHeartTooltip(CharacterData character)
        {
            PyreHeartData pyre = character?.GetPyreHeartData();
            if (pyre == null)
            {
                return null;
            }

            PyreArtifactData artifact = pyre.GetPyreArtifact();
            List<Message> parts = new List<Message>
            {
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_ATTACK", new { attack = pyre.GetAttack() }),
                Message.Localized("ui", "RUN_SETUP.PYRE_HEART_HEALTH", new { health = pyre.GetStartingHP() }),
                artifact != null ? Message.Localized("ui", "RUN_SETUP.PYRE_HEART_BONUS", new { bonus = artifact.GetName() }) : null,
                Message.FromText(artifact?.GetDescription())
            };
            parts.RemoveAll(part => part == null);
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal static Message MutatorLabel(MutatorData mutator)
        {
            return Message.FromText(mutator?.GetName());
        }

        internal static Message MutatorLabel(MutatorState mutator)
        {
            return Message.FromText(mutator?.GetName());
        }

        internal static Message MutatorTooltip(MutatorData mutator)
        {
            return Message.FromText(mutator?.GetDescription());
        }

        internal static Message MutatorTooltip(MutatorState mutator)
        {
            return Message.FromText(mutator?.GetDescription());
        }

        private static string ChampionName(ClassData classData, int championIndex)
        {
            ChampionData champion = classData?.GetChampionData(championIndex);
            return champion?.championCardData != null ? champion.championCardData.GetName() : string.Empty;
        }

        private static string CovenantLevelString(int level)
        {
            ChallengeCovenantDisplayData display = AllGameManagers.Instance?.GetAllGameData()?.GetChallengeCovenantDisplayData();
            return display != null
                ? display.GetChallengeLevelString(level)
                : level.ToString(CultureInfo.InvariantCulture);
        }
    }
}
