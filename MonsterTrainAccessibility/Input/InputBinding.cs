namespace MonsterTrainAccessibility.Input
{
    public abstract class InputBinding
    {
        public abstract string Serialize();
        public abstract string Type { get; }
        public abstract string TypeLabel { get; }
        public abstract string ComboName { get; }
        public string DisplayName => TypeLabel + ": " + ComboName;

        public static InputBinding Deserialize(string type, string binding)
        {
            switch (type)
            {
                case "keyboard":
                    return KeyboardBinding.Parse(binding);
                case "controller":
                    return ControllerBinding.Parse(binding);
                default:
                    return null;
            }
        }
    }
}
