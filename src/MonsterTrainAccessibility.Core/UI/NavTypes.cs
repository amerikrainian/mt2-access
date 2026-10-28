using System;

namespace MonsterTrainAccessibility.Core.UI
{
    public enum NavDirection { Up, Down, Left, Right }

    /// <summary>
    /// An action a screen advertises at screen level (Back/Escape and the like), dispatched by id.
    /// Navigators discover actions by <see cref="Id"/> and invoke them; they never switch on screen type.
    /// </summary>
    public sealed class ElementAction
    {
        public string Id { get; }
        public string Label { get; }
        private readonly Action<object> _execute;

        public ElementAction(string id, string label, Action<object> execute)
        {
            Id = id;
            Label = label;
            _execute = execute;
        }

        public void Execute(object args = null) => _execute?.Invoke(args);
    }

    /// <summary>Standard action ids navigators map inputs to.</summary>
    public static class ActionIds
    {
        public const string Activate = "activate";  // primary action (Enter / left click)
        public const string Context = "context";    // secondary action (Backspace / right click)
        public const string Back = "back";           // screen-level back/close (Escape)
        public const string Increase = "increase";
        public const string Decrease = "decrease";
        public const string SetValue = "setValue";
        public const string Reset = "reset";
    }

    /// <summary>
    /// The per-frame engine inputs the navigator needs for type-ahead and its idle throttle, behind a
    /// seam so Core stays engine-free: the module supplies the Unity-backed implementation, tests a fake.
    /// </summary>
    public interface INavInput
    {
        int FrameCount { get; }
        float UnscaledTime { get; }
        float UnscaledDeltaTime { get; }
        bool CtrlHeld { get; }
        bool AltHeld { get; }
        bool ShiftHeld { get; }
        bool EscapeDown { get; }
        bool UpHeld { get; }
        bool DownHeld { get; }
        /// <summary>The characters typed this frame (letters and space), or empty.</summary>
        string TypedText { get; }
    }

    /// <summary>The active <see cref="INavInput"/>; a silent null implementation until the module installs one.</summary>
    public static class NavInput
    {
        public static INavInput Current = new NullNavInput();

        private sealed class NullNavInput : INavInput
        {
            public int FrameCount => 0;
            public float UnscaledTime => 0f;
            public float UnscaledDeltaTime => 0f;
            public bool CtrlHeld => false;
            public bool AltHeld => false;
            public bool ShiftHeld => false;
            public bool EscapeDown => false;
            public bool UpHeld => false;
            public bool DownHeld => false;
            public string TypedText => "";
        }
    }
}
