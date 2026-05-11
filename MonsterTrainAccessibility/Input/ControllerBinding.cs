using System;

namespace MonsterTrainAccessibility.Input
{
    public class ControllerBinding : InputBinding
    {
        public override string Type => "controller";
        public ControllerInput Input { get; }
        public ControllerInput? Modifier { get; }

        public ControllerBinding(ControllerInput input, ControllerInput? modifier = null)
        {
            Input = input;
            Modifier = modifier;
        }

        public bool Matches(ControllerInput input, Func<ControllerInput, bool> isHeld)
        {
            if (isHeld == null || input != Input || !isHeld(Input))
            {
                return false;
            }

            return Modifier == null || isHeld(Modifier.Value);
        }

        public bool Uses(ControllerInput input) => Input == input || Modifier == input;

        public override string TypeLabel => "Controller";
        public override string ComboName
        {
            get
            {
                string name = GetDisplayName(Input);
                if (Modifier != null)
                {
                    return GetDisplayName(Modifier.Value) + "+" + name;
                }
                return name;
            }
        }

        public static string GetDisplayName(ControllerInput input)
        {
            switch (input)
            {
                case ControllerInput.DpadUp: return "D-pad Up";
                case ControllerInput.DpadDown: return "D-pad Down";
                case ControllerInput.DpadLeft: return "D-pad Left";
                case ControllerInput.DpadRight: return "D-pad Right";
                case ControllerInput.LeftShoulder: return "LB";
                case ControllerInput.RightShoulder: return "RB";
                case ControllerInput.LeftTrigger: return "LT";
                case ControllerInput.RightTrigger: return "RT";
                case ControllerInput.LeftStickUp: return "LS Up";
                case ControllerInput.LeftStickDown: return "LS Down";
                case ControllerInput.LeftStickLeft: return "LS Left";
                case ControllerInput.LeftStickRight: return "LS Right";
                case ControllerInput.RightStickUp: return "RS Up";
                case ControllerInput.RightStickDown: return "RS Down";
                case ControllerInput.RightStickLeft: return "RS Left";
                case ControllerInput.RightStickRight: return "RS Right";
                case ControllerInput.LeftStickClick: return "LS Click";
                case ControllerInput.RightStickClick: return "RS Click";
                default: return input.ToString();
            }
        }

        public override string Serialize()
        {
            if (Modifier != null)
            {
                return Modifier.Value.ToString() + "+" + Input.ToString();
            }
            return Input.ToString();
        }

        public static ControllerBinding Parse(string s)
        {
            if (s.Contains("+"))
            {
                int idx = s.IndexOf('+');
                string modStr = s.Substring(0, idx);
                string inputStr = s.Substring(idx + 1);

                ControllerInput mod;
                ControllerInput input;
                if (Enum.TryParse(modStr, out mod) && Enum.TryParse(inputStr, out input))
                {
                    return new ControllerBinding(input, mod);
                }
                return null;
            }

            ControllerInput solo;
            if (Enum.TryParse(s, out solo))
            {
                return new ControllerBinding(solo);
            }
            return null;
        }
    }
}
