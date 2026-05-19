using System.Collections.Generic;
using MonsterTrainAccessibility.Localization;

namespace MonsterTrainAccessibility.UI.Elements
{
    public abstract class Container : UIElement
    {
        private readonly List<UIElement> _children = new List<UIElement>();

        public IReadOnlyList<UIElement> Children => _children;
        public string ContainerLabel { get; set; }
        public bool AnnounceName { get; set; } = true;
        public bool AnnouncePosition { get; set; } = true;

        public override Message GetLabel() => ContainerLabel != null ? Message.Raw(ContainerLabel) : null;

        public void Add(UIElement child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        public void Remove(UIElement child)
        {
            if (_children.Remove(child))
            {
                child.Parent = null;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Parent = null;
            }
            _children.Clear();
        }

        public int IndexOf(UIElement child) => _children.IndexOf(child);

        public void Swap(int indexA, int indexB)
        {
            if (indexA == indexB ||
                indexA < 0 ||
                indexA >= _children.Count ||
                indexB < 0 ||
                indexB >= _children.Count)
            {
                return;
            }

            UIElement temporary = _children[indexA];
            _children[indexA] = _children[indexB];
            _children[indexB] = temporary;
        }

        public override void Update()
        {
            OnUpdate();
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].Update();
            }
        }

        public abstract Message GetPositionString(UIElement child);
    }
}
