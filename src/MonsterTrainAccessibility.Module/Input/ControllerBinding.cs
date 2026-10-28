using System;

namespace MonsterTrainAccessibility.Module.Input
{
    /// <summary>
    /// A gamepad combo, the keyboard binding's sibling: one control, optionally held with a modifier
    /// (a trigger: "LT+A"). Polled against <see cref="Gamepad"/>, which the game's own gamepad driver
    /// feeds once per frame. A modifier must match exactly, as the keyboard's do: a bare A does not
    /// fire while a trigger is held, so LT+A never also fires A.
    /// </summary>
    public sealed class ControllerBinding : Core.Input.InputBinding
    {
        public ControllerInput Input { get; }
        public ControllerInput? Modifier { get; }

        public ControllerBinding(ControllerInput input, ControllerInput? modifier = null)
        {
            Input = input;
            Modifier = modifier;
        }

        // The triggers are the modifiers; a binding of a trigger itself never counts its own press.
        private static bool IsModifier(ControllerInput input)
            => input == ControllerInput.LeftTrigger || input == ControllerInput.RightTrigger;

        private bool ModifiersMatch(Func<ControllerInput, bool> held)
        {
            foreach (ControllerInput m in new[] { ControllerInput.LeftTrigger, ControllerInput.RightTrigger })
            {
                if (m == Input) continue;
                if (held(m) != (Modifier == m)) return false;
            }
            return true;
        }

        public override bool JustPressed() => ModifiersMatch(Gamepad.IsHeld) && Gamepad.IsHeld(Input) && !Gamepad.WasHeld(Input);
        public override bool Held() => ModifiersMatch(Gamepad.IsHeld) && Gamepad.IsHeld(Input);
        public override bool Released() => ModifiersMatch(Gamepad.WasHeld) && Gamepad.WasHeld(Input) && !Gamepad.IsHeld(Input);

        public override string DisplayName
            => Modifier != null ? Name(Modifier.Value) + "+" + Name(Input) : Name(Input);

        // A control as a player names it, for the key help.
        private static string Name(ControllerInput input)
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
                case ControllerInput.LeftStickUp: return "Left Stick Up";
                case ControllerInput.LeftStickDown: return "Left Stick Down";
                case ControllerInput.LeftStickLeft: return "Left Stick Left";
                case ControllerInput.LeftStickRight: return "Left Stick Right";
                case ControllerInput.RightStickUp: return "Right Stick Up";
                case ControllerInput.RightStickDown: return "Right Stick Down";
                case ControllerInput.RightStickLeft: return "Right Stick Left";
                case ControllerInput.RightStickRight: return "Right Stick Right";
                case ControllerInput.LeftStickClick: return "Left Stick Click";
                case ControllerInput.RightStickClick: return "Right Stick Click";
                default: return input.ToString();
            }
        }

        public override string Type => "controller";

        // "A" or "LeftTrigger+A": the modifier first, as it is held first.
        public override string Serialize() => Modifier != null ? Modifier.Value + "+" + Input : Input.ToString();

        public static ControllerBinding Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;
            int plus = data.IndexOf('+');
            if (plus < 0)
                return Enum.TryParse(data, out ControllerInput solo) ? new ControllerBinding(solo) : null;
            if (!Enum.TryParse(data.Substring(0, plus), out ControllerInput modifier)) return null;
            if (!Enum.TryParse(data.Substring(plus + 1), out ControllerInput input)) return null;
            return IsModifier(modifier) ? new ControllerBinding(input, modifier) : null;
        }
    }
}
