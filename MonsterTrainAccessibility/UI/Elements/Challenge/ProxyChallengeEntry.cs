using System;
using System.Collections.Generic;
using System.Globalization;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeEntry : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly global::ChallengeEntryUI _entry;
        private readonly global::SaveManager _saveManager;
        private readonly global::AllGameData _allGameData;
        private readonly Func<bool> _activate;

        public ProxyChallengeEntry(
            global::ChallengeEntryUI entry,
            global::SaveManager saveManager,
            global::AllGameData allGameData,
            Func<bool> activate)
        {
            _entry = entry;
            _saveManager = saveManager;
            _allGameData = allGameData;
            _activate = activate;
        }

        public override bool IsVisible => _entry != null && _entry.gameObject.activeInHierarchy && _entry.ChallengeData != null;
        public override string GetTypeKey() => "button";

        public override Message GetLabel()
        {
            global::ChallengeData challenge = _entry?.ChallengeData;
            if (challenge == null)
            {
                return null;
            }

            return Message.Localized("ui", "CHALLENGE.ENTRY", new
            {
                sharecode = challenge.GetSharecodeOrID(),
                creator = CreatorName(challenge)
            });
        }

        public override Message GetStatusString()
        {
            global::ChallengeData challenge = _entry?.ChallengeData;
            if (challenge == null)
            {
                return null;
            }

            StartingConditions conditions = challenge.GetStartingConditionsCopy();
            List<Message> parts = new List<Message>
            {
                ChallengePresentation.MainClass(FindClass(conditions.ClassId), conditions.MainChampionIndex),
                ChallengePresentation.AlliedClass(FindClass(conditions.SubclassId), conditions.SubChampionIndex),
                conditions.AscensionLevel > 0 ? ChallengePresentation.Covenant(conditions.AscensionLevel) : null,
                EntryCount(challenge),
                RankOrProgress(challenge)
            };
            parts.RemoveAll(part => part == null);
            return Message.Join(", ", parts);
        }

        public override Message GetTooltip()
        {
            List<Message> parts = DetailLines();
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                uiBuffer.Add(GetLabel());
                uiBuffer.Add(GetExtrasString());
                foreach (Message line in DetailLines())
                {
                    uiBuffer.Add(line);
                }
                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }

        public void SelectForNavigation()
        {
            if (_entry?.Button != null && InputManager.Inst != null)
            {
                InputManager.Inst.SelectGameUIComponent(_entry.Button, allowClearingSelection: false);
            }
        }

        public bool Activate() => _activate != null && _activate();

        private Message EntryCount(global::ChallengeData challenge)
        {
            return Message.Localized("ui", "CHALLENGE.ENTRY_COUNT", new
            {
                count = challenge.GetLeaderboardEntryCount().ToString("N0", CultureInfo.InvariantCulture)
            });
        }

        private Message RankOrProgress(global::ChallengeData challenge)
        {
            int rank = challenge.GetTargetPlayerRank();
            if (rank > -1)
            {
                return Message.Localized("ui", "CHALLENGE.ENTRY_RANK", new
                {
                    rank = rank.ToString(CultureInfo.InvariantCulture)
                });
            }

            string sharecode = challenge.GetInitiatorData().ShareCode;
            return _saveManager != null && !string.IsNullOrEmpty(sharecode) && _saveManager.HasRun(RunType.Share, sharecode)
                ? Message.Localized("ui", "CHALLENGE.ENTRY_IN_PROGRESS")
                : null;
        }

        private Message MutatorsSummary(IReadOnlyList<string> mutatorIds)
        {
            if (mutatorIds == null || mutatorIds.Count == 0)
            {
                return Message.Localized("ui", "CHALLENGE.MUTATORS_NONE");
            }

            List<Message> names = new List<Message>();
            for (int i = 0; i < mutatorIds.Count; i++)
            {
                MutatorData mutator = FindMutator(mutatorIds[i]);
                Message name = ChallengePresentation.MutatorLabel(mutator);
                if (name != null)
                {
                    names.Add(name);
                }
            }

            return names.Count > 0
                ? Message.Localized("ui", "CHALLENGE.MUTATORS", new { mutators = Message.Join(", ", names).Resolve() })
                : Message.Localized("ui", "CHALLENGE.MUTATORS_NONE");
        }

        private IReadOnlyList<Message> MutatorDetails(IReadOnlyList<string> mutatorIds)
        {
            List<Message> details = new List<Message>();
            if (mutatorIds == null)
            {
                return details;
            }

            for (int i = 0; i < mutatorIds.Count; i++)
            {
                MutatorData mutator = FindMutator(mutatorIds[i]);
                Message name = ChallengePresentation.MutatorLabel(mutator);
                Message description = ChallengePresentation.MutatorTooltip(mutator);
                if (name != null && description != null)
                {
                    details.Add(Message.Localized("ui", "CHALLENGE.MUTATOR_DETAIL", new { name = name.Resolve(), description = description.Resolve() }));
                }
            }

            return details;
        }

        private List<Message> DetailLines()
        {
            List<Message> parts = new List<Message>();
            global::ChallengeData challenge = _entry?.ChallengeData;
            if (challenge == null)
            {
                return parts;
            }

            StartingConditions conditions = challenge.GetStartingConditionsCopy();
            ClassData mainClass = FindClass(conditions.ClassId);
            ClassData alliedClass = FindClass(conditions.SubclassId);
            CharacterData pyre = FindCharacter(conditions.PyreCharacterId);

            MessageList.Add(parts, ChallengePresentation.MainClass(mainClass, conditions.MainChampionIndex));
            MessageList.Add(parts, ChallengePresentation.ClassDescription(mainClass, mainClass: true));
            MessageList.Add(parts, ChallengePresentation.AlliedClass(alliedClass, conditions.SubChampionIndex));
            MessageList.Add(parts, ChallengePresentation.ClassDescription(alliedClass, mainClass: false));
            if (conditions.AscensionLevel > 0)
            {
                MessageList.Add(parts, ChallengePresentation.Covenant(conditions.AscensionLevel));
                parts.AddRange(ChallengePresentation.CovenantTooltipLines(conditions.AscensionLevel, conditions.EnabledDlcs));
            }
            MessageList.Add(parts, ChallengePresentation.PyreHeartLabel(pyre));
            MessageList.Add(parts, ChallengePresentation.PyreHeartTooltip(pyre));
            MessageList.Add(parts, MutatorsSummary(conditions.Mutators));
            parts.AddRange(MutatorDetails(conditions.Mutators));
            MessageList.Add(parts, EntryCount(challenge));
            MessageList.Add(parts, RankOrProgress(challenge));
            return parts;
        }

        private string CreatorName(global::ChallengeData challenge)
        {
            InitiatorData initiator = challenge.GetInitiatorData();
            string userId = initiator.GetPlatformUserId();
            string displayName = initiator.GetPlatformUserDisplayName();
            if (_saveManager?.GetPreferencesManager()?.StreamerModeEnabled == true &&
                userId != AppManager.PlatformServices.GetPlayerUniquePlatformID())
            {
                return StreamSafeIdUtility.GetStreamSafeName(userId ?? string.Empty);
            }

            return Message.Clean(displayName);
        }

        private ClassData FindClass(string id)
        {
            return !string.IsNullOrEmpty(id) ? _allGameData?.FindClassData(id) : null;
        }

        private CharacterData FindCharacter(string id)
        {
            return !string.IsNullOrEmpty(id) ? _allGameData?.FindCharacterData(id) : null;
        }

        private MutatorData FindMutator(string id)
        {
            return !string.IsNullOrEmpty(id) ? _allGameData?.FindMutatorData(id) : null;
        }
    }
}
