using System;
using BepInEx.Logging;

namespace Monster_Train_2_Accessibility.Plugin
{
    public static class ScreenReader
    {
        private static SpeechCore? _speechCore;
        private static ManualLogSource? _logger;
        private static bool _initialized = false;

        public static void Initialize(ManualLogSource logger)
        {
            _logger = logger;
            
            try
            {
                _speechCore = new SpeechCore();
                
                if (_speechCore.IsLoaded())
                {
                    _initialized = true;
                    _logger?.LogInfo($"Screen reader initialized: {_speechCore.CurrentDriver()}");
                }
                else
                {
                    _logger?.LogWarning("No screen reader/speech engine available");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to initialize screen reader: {ex.Message}");
            }
        }

        public static void Speak(string text, bool interrupt = true)
        {
            if (!_initialized || _speechCore == null || string.IsNullOrEmpty(text))
                return;

            try
            {
                _speechCore.Speak(text, interrupt);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Speech failed: {ex.Message}");
            }
        }

        public static void Stop()
        {
            if (!_initialized || _speechCore == null)
                return;

            try
            {
                _speechCore.Stop();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Stop speech failed: {ex.Message}");
            }
        }

        public static bool IsSpeaking()
        {
            if (!_initialized || _speechCore == null)
                return false;

            try
            {
                return _speechCore.IsSpeaking();
            }
            catch
            {
                return false;
            }
        }

        public static void SetRate(float rate)
        {
            if (!_initialized || _speechCore == null)
                return;

            try
            {
                _speechCore.SetRate(rate);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Set rate failed: {ex.Message}");
            }
        }

        public static void SetVolume(float volume)
        {
            if (!_initialized || _speechCore == null)
                return;

            try
            {
                _speechCore.SetVolume(volume);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Set volume failed: {ex.Message}");
            }
        }

        public static void Dispose()
        {
            if (_speechCore != null)
            {
                try
                {
                    _speechCore.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Dispose failed: {ex.Message}");
                }
                finally
                {
                    _speechCore = null;
                    _initialized = false;
                }
            }
        }
    }
}