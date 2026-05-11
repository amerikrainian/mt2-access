using MonsterTrainAccessibility.Input;

namespace MonsterTrainAccessibility.UI.Elements.Compendium
{
    internal interface ICompendiumExpandableFilter
    {
        bool IsExpanded { get; }
        bool HandleExpandedAction(InputAction action);
        void Collapse(bool announce);
    }
}
