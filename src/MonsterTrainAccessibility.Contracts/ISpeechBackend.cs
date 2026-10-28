namespace MonsterTrainAccessibility.Contracts
{
    /// <summary>
    /// The engine seam for speech output. The real implementation (host) wraps prism.dll; tests inject a
    /// fake. The pipeline owns policy (clean, interrupt); a backend only emits the already-prepared text.
    /// </summary>
    public interface ISpeechBackend
    {
        bool IsAvailable { get; }

        void Speak(string text, bool interrupt);

        void Stop();
    }
}
