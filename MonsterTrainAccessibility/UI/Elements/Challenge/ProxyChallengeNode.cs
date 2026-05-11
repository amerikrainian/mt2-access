using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Util;
using ShinyShoe;
using UnityEngine.EventSystems;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyChallengeNode : GameObjectElement
    {
        private readonly global::ChallengeNode _node;
        private readonly global::ChallengeSelectionUI _selection;

        public ProxyChallengeNode(global::ChallengeNode node, global::ChallengeSelectionUI selection)
            : base(
                node?.Button,
                typeKey: "challenge",
                label: null)
        {
            _node = node;
            _selection = selection;
        }

        private global::SpChallengeData Challenge => _node?.Challenge;

        public override bool IsVisible => _node != null &&
            _node.gameObject.activeInHierarchy &&
            Challenge != null;

        public override Message GetLabel()
        {
            string key = Challenge?.GetNameKey();
            return !string.IsNullOrWhiteSpace(key)
                ? Message.RawCleaned(key.Localize())
                : null;
        }

        public override Message GetStatusString()
        {
            List<Message> parts = new List<Message>();

            if (_selection != null && ReferenceEquals(_selection.CurrentChallenge, _node))
            {
                parts.Add(Message.Localized("messages", "state.selected"));
            }

            if (_node != null && !_node.IsAlwaysUnlocked && _node.IsLocked)
            {
                parts.Add(Message.Localized("messages", "state.locked"));
            }

            if (_node != null && _node.IsCompleted)
            {
                parts.Add(Message.Localized("messages", "state.completed"));
            }

            return parts.Count > 0 ? Message.Join(", ", parts) : null;
        }

        public override Message GetTooltip()
        {
            return Details(Challenge);
        }

        public override void SelectForNavigation()
        {
            ClearNativeSelection();
        }

        public override bool Activate()
        {
            if (_selection != null && Challenge != null)
            {
                _selection.SelectChallenge(Challenge);
                _node?.NavInputChallengeSelectedSignal.Dispatch(_node);
                ClearNativeSelection();
                Core.UIManager.ForceReannounceCurrentFocus();
                return true;
            }

            return false;
        }

        private static Message Details(global::SpChallengeData challenge)
        {
            if (challenge == null)
            {
                return null;
            }

            List<Message> parts = new List<Message>();
            MessageList.Add(parts, Message.RawCleaned(challenge.GetDescriptionKey().Localize()));
            MessageList.Add(parts, ChallengePresentation.Covenant(challenge.GetCovenantLevel()));
            parts.AddRange(ChallengePresentation.CovenantTooltipLines(challenge.GetCovenantLevel(), null));
            MessageList.Add(parts, ChallengeClass("CHALLENGE.MAIN_CLAN", challenge.GetMainClan()));
            MessageList.Add(parts, ChallengeClass("CHALLENGE.ALLIED_CLAN", challenge.GetAlliedClan()));
            MessageList.Add(parts, ChallengePyreHeart(challenge.GetPyreHeartCharacterData()));
            MessageList.Add(parts, ChallengeMutatorsSummary(challenge.GetMutators()));
            parts.AddRange(ChallengeMutatorDetails(challenge.GetMutators()));
            return parts.Count > 0 ? Message.JoinLines(parts) : null;
        }

        private static Message ChallengeClass(string key, global::ClassData classData)
        {
            return classData != null
                ? Message.Localized("ui", key, new { clan = classData.GetTitle() })
                : null;
        }

        private static Message ChallengePyreHeart(global::CharacterData pyreHeart)
        {
            return pyreHeart != null
                ? Message.Localized("ui", "CHALLENGE.PYRE_HEART", new { pyre = pyreHeart.GetName() })
                : null;
        }

        private static Message ChallengeMutatorsSummary(IReadOnlyList<global::MutatorData> mutators)
        {
            if (mutators == null || mutators.Count == 0)
            {
                return null;
            }

            List<Message> names = new List<Message>();
            for (int i = 0; i < mutators.Count; i++)
            {
                Message name = ChallengePresentation.MutatorLabel(mutators[i]);
                if (name != null)
                {
                    names.Add(name);
                }
            }

            return names.Count > 0
                ? Message.Localized("ui", "CHALLENGE.MUTATORS", new { mutators = Message.Join(", ", names).Resolve() })
                : null;
        }

        private static IReadOnlyList<Message> ChallengeMutatorDetails(IReadOnlyList<global::MutatorData> mutators)
        {
            List<Message> details = new List<Message>();
            if (mutators == null)
            {
                return details;
            }

            for (int i = 0; i < mutators.Count; i++)
            {
                Message name = ChallengePresentation.MutatorLabel(mutators[i]);
                Message description = ChallengePresentation.MutatorTooltip(mutators[i]);
                if (name != null && description != null)
                {
                    details.Add(Message.Localized("ui", "CHALLENGE.MUTATOR_DETAIL", new { name = name.Resolve(), description = description.Resolve() }));
                }
            }

            return details;
        }

        private static void ClearNativeSelection()
        {
            global::InputManager.Inst?.SelectGameUIComponent(null, allowClearingSelection: true);
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
