using System;
using System.Globalization;
using MonsterTrainAccessibility.Input;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.Speech;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrainAccessibility.UI.Elements
{
    internal sealed class SliderElement : GameObjectElement, INavigationActionHandler
    {
        private readonly SelectableSliderHelper _helper;
        private readonly Slider _slider;
        private readonly int _step;
        private bool _listening;
        private bool _suppressSync;

        public SliderElement(
            SelectableSliderHelper helper,
            Func<Message> label = null,
            int step = 1,
            Func<Message> tooltip = null,
            Func<bool> visibility = null)
            : this(
                target: helper != null ? helper.gameObject : null,
                helper: helper,
                slider: helper != null ? helper.GetComponent<Slider>() : null,
                label: label,
                step: step,
                tooltip: tooltip,
                visibility: visibility)
        {
        }

        public SliderElement(
            Slider slider,
            Func<Message> label = null,
            int step = 1,
            Func<Message> tooltip = null,
            Func<bool> visibility = null)
            : this(
                target: slider != null ? slider.gameObject : null,
                helper: slider != null ? slider.GetComponent<SelectableSliderHelper>() : null,
                slider: slider,
                label: label,
                step: step,
                tooltip: tooltip,
                visibility: visibility)
        {
        }

        private SliderElement(
            GameObject target,
            SelectableSliderHelper helper,
            Slider slider,
            Func<Message> label,
            int step,
            Func<Message> tooltip,
            Func<bool> visibility)
            : base(
                target: target,
                typeKey: "slider",
                label: label ?? (() => DefaultLabel(target)),
                status: () => Message.RawCleaned(FormatValue(CurrentValue(helper, slider))),
                tooltip: tooltip,
                visibility: visibility)
        {
            _helper = helper;
            _slider = slider;
            _step = Math.Max(1, step);
        }

        public bool HandleAction(InputAction action)
        {
            if (action == null)
            {
                return false;
            }

            switch (action.Key)
            {
                case "ui_left":
                    return Decrement();
                case "ui_right":
                    return Increment();
                case "ui_home":
                    return SetToBoundary(minimum: true);
                case "ui_end":
                    return SetToBoundary(minimum: false);
                default:
                    return false;
            }
        }

        public bool Increment()
        {
            return ChangeBy(_step);
        }

        public bool Decrement()
        {
            return ChangeBy(-_step);
        }

        private bool ChangeBy(int delta)
        {
            if (_slider == null)
            {
                return false;
            }

            int current = CurrentValue(_helper, _slider);
            return ChangeTo(current + delta);
        }

        private bool SetToBoundary(bool minimum)
        {
            if (_slider == null)
            {
                return false;
            }

            int target = Mathf.RoundToInt(minimum ? _slider.minValue : _slider.maxValue);
            return ChangeTo(target);
        }

        private bool ChangeTo(int value)
        {
            if (_slider == null)
            {
                return false;
            }

            int current = CurrentValue(_helper, _slider);
            int min = Mathf.RoundToInt(_slider.minValue);
            int max = Mathf.RoundToInt(_slider.maxValue);
            int next = Mathf.Clamp(value, min, max);
            if (next == current)
            {
                SpeechManager.Output(Message.RawCleaned(FormatValue(next)));
                return true;
            }

            if (_helper != null)
            {
                _suppressSync = true;
                try
                {
                    _helper.ChangeValue(next);
                }
                finally
                {
                    _suppressSync = false;
                }
            }
            else
            {
                _suppressSync = true;
                try
                {
                    _slider.value = next;
                }
                finally
                {
                    _suppressSync = false;
                }
            }

            SpeechManager.Output(Message.RawCleaned(FormatValue(next)));
            return true;
        }

        public void SyncFromControl()
        {
            if (_suppressSync)
            {
                return;
            }

            SpeechManager.Output(Message.RawCleaned(FormatValue(CurrentValue(_helper, _slider))));
        }

        protected override void OnFocus()
        {
            if (_helper == null || _listening)
            {
                return;
            }

            _helper.ValueChangedSignal.AddListener(HandleValueChanged);
            _listening = true;
        }

        protected override void OnUnfocus()
        {
            if (_helper == null || !_listening)
            {
                return;
            }

            _helper.ValueChangedSignal.RemoveListener(HandleValueChanged);
            _listening = false;
        }

        private void HandleValueChanged(int value)
        {
            if (_suppressSync)
            {
                return;
            }

            SpeechManager.Output(Message.RawCleaned(FormatValue(value)));
        }

        private static int CurrentValue(SelectableSliderHelper helper, Slider slider)
        {
            if (helper != null)
            {
                return helper.Value;
            }

            return slider != null ? Mathf.RoundToInt(slider.value) : 0;
        }

        private static string FormatValue(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
