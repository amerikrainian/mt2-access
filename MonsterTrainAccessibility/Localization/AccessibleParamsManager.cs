using global::I2.Loc;

namespace MonsterTrainAccessibility.Localization
{
    internal sealed class AccessibleParamsManager : ILocalizationParamsManager
    {
        public TranslationToken ParseParam(string param, ILocalizationParameterContext context)
        {
            return null;
        }

        public string GetParameterValue(TranslationToken token, ILocalizationParameterContext context)
        {
            if (!AccessibilityLocalizationScope.IsActive || token?.type != TranslationToken.Type.ControllerButton)
            {
                return null;
            }

            TranslationTokenControllerButton controllerButton = token as TranslationTokenControllerButton;
            if (controllerButton == null)
            {
                return null;
            }

            global::InputManager inputManager = global::InputManager.Inst;
            if (inputManager == null)
            {
                return null;
            }

            inputManager.GetSpriteAndLabelForMapping(controllerButton.control, out var _, out var _, out string label);
            return string.IsNullOrWhiteSpace(label) ? null : label;
        }

        public int? GetParameterPluralAmount(TranslationToken token, ILocalizationParameterContext context)
        {
            return null;
        }
    }
}
