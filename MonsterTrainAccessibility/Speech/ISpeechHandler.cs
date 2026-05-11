namespace MonsterTrainAccessibility.Speech
{
    internal interface ISpeechHandler
    {
        string Key { get; }

        string Label { get; }

        bool IsLoaded { get; }

        bool TryLoad();

        void Unload();

        bool Speak(string text, bool interrupt);

        bool Output(string text, bool interrupt);

        void Silence();
    }
}
