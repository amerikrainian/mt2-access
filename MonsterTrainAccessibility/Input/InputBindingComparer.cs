namespace MonsterTrainAccessibility.Input
{
    internal static class InputBindingComparer
    {
        public static bool Same(InputBinding left, InputBinding right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            KeyboardBinding leftKeyboard = left as KeyboardBinding;
            KeyboardBinding rightKeyboard = right as KeyboardBinding;
            if (leftKeyboard != null && rightKeyboard != null)
            {
                return leftKeyboard.Keycode == rightKeyboard.Keycode &&
                    leftKeyboard.Ctrl == rightKeyboard.Ctrl &&
                    leftKeyboard.Shift == rightKeyboard.Shift &&
                    leftKeyboard.Alt == rightKeyboard.Alt;
            }

            ControllerBinding leftController = left as ControllerBinding;
            ControllerBinding rightController = right as ControllerBinding;
            if (leftController != null && rightController != null)
            {
                return leftController.Input == rightController.Input &&
                    leftController.Modifier == rightController.Modifier;
            }

            return false;
        }
    }
}
