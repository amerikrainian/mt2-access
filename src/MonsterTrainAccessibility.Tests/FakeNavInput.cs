using MonsterTrainAccessibility.Core.UI;

namespace MonsterTrainAccessibility.Tests
{
    /// <summary>The engine inputs the navigator reads, with a frame counter the test advances by hand
    /// (the idle rebuild is throttled to every few frames, as in the game).</summary>
    internal sealed class FakeNavInput : INavInput
    {
        public int FrameCount { get; set; }
        public float UnscaledTime => FrameCount / 60f;
        public float UnscaledDeltaTime => 1f / 60f;
        public bool CtrlHeld => false;
        public bool AltHeld => false;
        public bool ShiftHeld => false;
        public bool EscapeDown => false;
        public bool UpHeld => false;
        public bool DownHeld => false;
        public string TypedText { get; set; } = "";
    }
}
