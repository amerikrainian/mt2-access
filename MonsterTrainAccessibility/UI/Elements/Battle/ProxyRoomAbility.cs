using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Presentation;
using MonsterTrainAccessibility.UI.Screens;
using ShinyShoe;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class ProxyRoomAbility : UIElement, IActivatableElement, INavigationTargetElement
    {
        private readonly int _roomIndex;
        private readonly TrainRoomAttachmentState _attachment;
        private readonly IRoomNavigationSource _roomNavigation;
        private readonly BattleScreen _screen;

        public ProxyRoomAbility(
            int roomIndex,
            TrainRoomAttachmentState attachment,
            IRoomNavigationSource roomNavigation,
            BattleScreen screen)
        {
            _roomIndex = roomIndex;
            _attachment = attachment;
            _roomNavigation = roomNavigation;
            _screen = screen;
        }

        public CardState Card => ResolveAbilityCard(_attachment);
        internal int RoomIndex => _roomIndex;
        internal TrainRoomAttachmentState Attachment => _attachment;
        internal string StableFocusKey => GetStableFocusKey(_roomIndex, _attachment);

        public override bool IsVisible => _attachment != null && _attachment.HasRoomAbility && Card != null;

        public override Message GetLabel() => ProxyCombatCard.FocusSummary(Card);

        public override Message GetStatusString()
        {
            if (_attachment == null)
            {
                return null;
            }

            return _attachment.CurrentAbilityCooldown > 0
                ? Message.Localized("combat", "CREATURE.ABILITY_COOLDOWN", new { turns = _attachment.CurrentAbilityCooldown })
                : Message.Localized("combat", "CREATURE.ABILITY_READY");
        }

        public override Message GetTooltip() => ProxyCombatCard.AccessibilitySummary(Card);

        public bool Activate()
        {
            return _screen != null && _screen.ActivateRoomAbility(_roomIndex, _attachment);
        }

        public void SelectForNavigation()
        {
            _roomNavigation?.SelectRoom(_roomIndex);
        }

        internal override string HandleBuffers(BufferManager buffers)
        {
            base.HandleBuffers(buffers);
            PresentationBuffer<CardState> buffer = buffers?.GetBuffer("card") as PresentationBuffer<CardState>;
            CardState card = Card;
            if (buffer == null || card == null)
            {
                return "ui";
            }

            buffer.Bind(card);
            buffers.EnableBuffer("card", true);
            return "card";
        }

        internal static CardState ResolveAbilityCard(TrainRoomAttachmentState attachment)
        {
            CardState attachmentCard = attachment?.CardState;
            if (attachmentCard == null ||
                !attachmentCard.TryGetCardEffectState<CardEffectAttachTrainRoomAttachment>(
                    out CardEffectAttachTrainRoomAttachment _,
                    out CardEffectState effect))
            {
                return null;
            }

            CardData ability = effect.GetParamCardUpgradeData()?.GetRoomAbilityUpgrade();
            if (ability == null)
            {
                return null;
            }

            SaveManager saveManager = AllGameManagers.Instance.OrNull()?.GetSaveManager();
            return UnitOrRoomAbilityCardStateCache.Instance.Get(ability, saveManager?.RelicManager, saveManager);
        }

        internal static string GetStableFocusKey(int roomIndex, TrainRoomAttachmentState attachment)
        {
            CardState ability = ResolveAbilityCard(attachment);
            return roomIndex + ":" + attachment?.Guid + ":" + ability?.GetID();
        }
    }
}
