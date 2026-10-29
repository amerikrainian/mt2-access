using System;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Core.Input;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Navigation = MonsterTrainAccessibility.Core.UI.Navigation;

namespace MonsterTrainAccessibility.Module.UI
{
    // The game declares its own InputManager in the global namespace.
    using InputManager = Core.Input.InputManager;

    /// <summary>
    /// Editing one of the game's text fields with the game's own editor: the field is selected and
    /// activated as a click does, so the caret, the selection, Ctrl+A, the clipboard, Delete and any
    /// character the OS types are TextMesh Pro's, which reads its keys from Unity's legacy event
    /// queue (untouched by focus mode, which blanks the game's own keyboard driver alone). While it
    /// lasts the mod's keys stand down (<see cref="OwnsKeyboard"/>, <c>InputManager.TextFieldFocused</c>)
    /// and each step is echoed from the field's state before and after it (<see cref="EditEcho"/>).
    /// Enter, Escape and Tab end it: the first two are the field's own way out, and Escape keeps the
    /// text (the field would put the old one back, and with it undo a filter the player just typed);
    /// Tab and Shift+Tab go on to the next or the previous stop, as they do from any control. A
    /// single-line field has no use for Up and Down, so they end it too and move to the control above
    /// or below, as they do from any control: such a field sits among the others of its stop. The pad,
    /// which cannot type, has the same ways out: the shoulders as Tab, the D-pad and left stick as the
    /// arrows, B as Escape.
    /// </summary>
    public static class TextEdit
    {
        private const int FocusWaitFrames = 60;

        private static TMP_InputField _field;
        private static bool _pending;      // begun, waiting for the Enter that began it to come up
        private static bool _seenFocus;
        private static int _deadline;
        private static bool _activated;    // the field's Escape setting is ours to give back
        private static bool _restoreOnEscape;
        private static bool _holdKeys;     // ended, waiting for the key that ended it to come up
        // The field as the last step left it: what the next echo is measured against, not a cache.
        private static EditState _last;

        public static bool Active => _field != null;

        /// <summary>Whether the keyboard is the field's: while editing, and until the key that ended it
        /// is released (the Escape that leaves the field must not also close the screen).</summary>
        public static bool OwnsKeyboard => Active || _holdKeys;

        /// <summary>Edit the field. <paramref name="announce"/> says so ("editing": Enter on a field
        /// at rest); a field edited on landing has just been read as one.</summary>
        public static void Begin(TMP_InputField field, bool announce)
        {
            if (field == null || Active) return;
            _field = field;
            _pending = true;
            _seenFocus = false;
            _activated = false;
            if (announce) Speech.Say(Strings.EditBegin, interrupt: true);
        }

        public static void Tick()
        {
            if (_holdKeys && !EndKeyHeld()) _holdKeys = false;
            if (_field == null) return;
            try
            {
                if (!_field.gameObject.activeInHierarchy) { End(announce: false); return; }
                if (_pending)
                {
                    // The field would read the Enter that began the edit as its submit.
                    if (EndKeyHeld()) return;
                    _pending = false;
                    _restoreOnEscape = _field.restoreOriginalTextOnEscape;
                    _activated = true;
                    _field.restoreOriginalTextOnEscape = false;
                    var es = EventSystem.current;
                    if (es != null) es.SetSelectedGameObject(_field.gameObject);
                    _field.ActivateInputField();
                    _deadline = Time.frameCount + FocusWaitFrames;
                    _last = new EditState(_field.text, (_field.text ?? "").Length, (_field.text ?? "").Length);
                    return;
                }
                if (!_field.isFocused)
                {
                    if (_seenFocus) End(announce: true);
                    else if (Time.frameCount > _deadline)
                    {
                        CoreLog.Warning("TextEdit: the field never took focus");
                        End(announce: true);
                    }
                    return;
                }
                bool first = !_seenFocus;
                _seenFocus = true;
                string onward = Onward(_field);
                // An arrow with nothing that way keeps the edit (a field alone in its stop).
                var move = onward != null ? InputManager.Find(onward) : null;
                if (move != null && Navigation.WouldHandle(move))
                {
                    _field.DeactivateInputField();
                    End(announce: false);
                    InputManager.Dispatch(onward);
                    return;
                }
                if (Pressed(ControllerInput.B))
                {
                    End(announce: true);
                    return;
                }
                var now = Read(_field);
                bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                string say = EditEcho.Describe(_last, now, ctrl);
                _last = now;
                // The field selects its text as it takes focus: said after the landing, not over it.
                if (say != null) Speech.Say(say, interrupt: !first);
            }
            catch (Exception e)
            {
                CoreLog.Warning("TextEdit: " + e.Message);
                End(announce: false);
            }
        }

        /// <summary>End an edit under way without a word: focus has left the field.</summary>
        public static void Stop()
        {
            if (Active) End(announce: false);
        }

        /// <summary>Module teardown: the field goes back to the game as it was.</summary>
        public static void Shutdown()
        {
            if (Active) End(announce: false);
            _holdKeys = false;
        }

        // The move that ends the edit and goes on, if one was pressed this frame: Tab and the shoulders
        // to the next or previous stop; in a single-line field, a plain Up or Down (Shift with them
        // selects) to the control above or below.
        private static string Onward(TMP_InputField field)
        {
            bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) return shift ? UiActions.Prev : UiActions.Next;
            if (Pressed(ControllerInput.RightShoulder)) return UiActions.Next;
            if (Pressed(ControllerInput.LeftShoulder)) return UiActions.Prev;
            if (field.lineType != TMP_InputField.LineType.SingleLine) return null;
            if ((UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) && !shift) || Pressed(ControllerInput.DpadUp) || Pressed(ControllerInput.LeftStickUp)) return UiActions.Up;
            if ((UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) && !shift) || Pressed(ControllerInput.DpadDown) || Pressed(ControllerInput.LeftStickDown)) return UiActions.Down;
            return null;
        }

        private static bool Pressed(ControllerInput input) => Gamepad.IsHeld(input) && !Gamepad.WasHeld(input);

        // The pad's ways in and out of an edit.
        private static readonly ControllerInput[] PadKeys =
        {
            ControllerInput.A, ControllerInput.B, ControllerInput.LeftShoulder, ControllerInput.RightShoulder,
            ControllerInput.DpadUp, ControllerInput.DpadDown, ControllerInput.LeftStickUp, ControllerInput.LeftStickDown,
        };

        private static EditState Read(TMP_InputField field)
            => new EditState(field.text, field.selectionStringFocusPosition, field.selectionStringAnchorPosition);

        // A key that begins or ends an edit, still down: the edit waits for it to come up before it
        // starts, and the mod's keys wait for it after it ends (a held arrow must not also repeat).
        private static bool EndKeyHeld()
        {
            if (UnityEngine.Input.GetKey(KeyCode.Return) || UnityEngine.Input.GetKey(KeyCode.KeypadEnter)
                || UnityEngine.Input.GetKey(KeyCode.Escape) || UnityEngine.Input.GetKey(KeyCode.Tab)
                || UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) return true;
            foreach (var input in PadKeys)
                if (Gamepad.IsHeld(input)) return true;
            return false;
        }

        private static void End(bool announce)
        {
            var field = _field;
            _field = null;
            _pending = false;
            _holdKeys = true;
            try
            {
                if (field != null && _activated)
                {
                    field.restoreOriginalTextOnEscape = _restoreOnEscape;
                    if (field.isFocused) field.DeactivateInputField();
                }
                // Focus mode keeps the game's own selection empty.
                var es = EventSystem.current;
                if (es != null && field != null && es.currentSelectedGameObject == field.gameObject) es.SetSelectedGameObject(null);
            }
            catch (Exception e) { CoreLog.Warning("TextEdit: giving the field back failed: " + e.Message); }
            if (announce) Navigation.AnnounceCurrent();
        }
    }
}
