using MonsterTrainAccessibility.Input;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal interface INavigationActionHandler
    {
        bool HandleAction(InputAction action);
    }
}
