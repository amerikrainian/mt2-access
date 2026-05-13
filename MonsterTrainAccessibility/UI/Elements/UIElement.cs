using System.Collections.Generic;
using MonsterTrainAccessibility.Buffers;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    public abstract class UIElement
    {
        public Container Parent { get; set; }

        public virtual bool IsVisible => true;

        public abstract Message GetLabel();
        public virtual Message GetExtrasString() => null;
        public virtual string GetTypeKey() => null;
        public virtual string GetSubtypeKey() => null;
        public virtual Message GetStatusString() => null;
        public virtual Message GetTooltip() => null;

        internal virtual string HandleBuffers(BufferManager buffers)
        {
            LineBuffer uiBuffer = buffers?.GetBuffer("ui");
            if (uiBuffer != null)
            {
                uiBuffer.Clear();
                uiBuffer.Add(GetLabel());
                uiBuffer.Add(GetStatusString());
                uiBuffer.Add(GetExtrasString());
                uiBuffer.Add(GetTooltip());
                buffers.EnableBuffer("ui", true);
            }

            return "ui";
        }

        public bool IsFocused { get; private set; }

        public void Focus()
        {
            IsFocused = true;
            OnFocus();
        }

        public void Unfocus()
        {
            IsFocused = false;
            OnUnfocus();
        }

        public virtual void Update()
        {
            OnUpdate();
        }

        protected virtual void OnFocus() { }
        protected virtual void OnUnfocus() { }
        protected virtual void OnUpdate() { }

        public Message GetFocusMessage()
        {
            Message label = GetLabel();
            Message typePart = BuildTypePart();

            if (label == null)
            {
                return typePart ?? Message.Empty;
            }

            if (typePart == null)
            {
                return label;
            }

            return Message.Join(" ", label, typePart);
        }

        public string GetFocusString() => GetFocusMessage().Resolve();

        private Message BuildTypePart()
        {
            List<Message> parts = new List<Message>();
            string typeKey = GetTypeKey();
            string subtypeKey = GetSubtypeKey();

            if (!string.IsNullOrEmpty(subtypeKey))
            {
                parts.Add(Message.Localized("ui", "TYPES." + subtypeKey.ToUpperInvariant()));
            }

            if (!string.IsNullOrEmpty(typeKey))
            {
                parts.Add(Message.Localized("ui", "TYPES." + typeKey.ToUpperInvariant()));
            }

            Message status = GetStatusString();
            if (status != null)
            {
                parts.Add(status);
            }

            return parts.Count > 0 ? Message.Join(" ", parts) : null;
        }
    }
}
