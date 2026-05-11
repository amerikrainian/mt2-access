namespace MonsterTrainAccessibility.Speech
{
    internal sealed class PrismHandler : ISpeechHandler
    {
        private readonly PrismBackend _backend = new PrismBackend();

        public string Key => "prism";

        public string Label => "Prism";

        public bool IsLoaded => _backend.IsInitialized && _backend.IsAvailable;

        public bool TryLoad()
        {
            return _backend.Initialize();
        }

        public void Unload()
        {
            _backend.Shutdown();
        }

        public bool Speak(string text, bool interrupt)
        {
            if (!_backend.IsAvailable || string.IsNullOrEmpty(text))
            {
                return false;
            }

            _backend.Say(text, interrupt);
            return true;
        }

        public bool Output(string text, bool interrupt)
        {
            return Speak(text, interrupt);
        }

        public void Silence()
        {
            _backend.Stop();
        }
    }
}
