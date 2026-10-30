using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core.Audio;
using MonsterTrainAccessibility.Core.Graph;
using MonsterTrainAccessibility.Core.Strings;
using MonsterTrainAccessibility.Core.UI;
using MonsterTrainAccessibility.Module.UI;
using Screen = MonsterTrainAccessibility.Core.Screens.Screen;

namespace MonsterTrainAccessibility.Module.Screens
{
    /// <summary>
    /// The mod's sounds (a child of the mod menu; Guildrun's, from dd2a11y): the master volume, then for
    /// each cue a row naming what the mod plays it for, its volume as the value (Left and Right step it,
    /// Enter plays the sound once at that level, the sound alone being the feedback). Guildrun's interval
    /// rows wait for a cue that fires often enough to want one (a cue's interval is in the settings store
    /// all the same). The values live in the host's settings store, so they survive reloads and restarts.
    /// </summary>
    public sealed class ModSoundsScreen : Screen
    {
        public override string Key => "mod.sounds";
        public override bool IsActive() => false; // only ever a child of the mod menu

        public override void Build(GraphBuilder b)
        {
            var sounds = ModuleMain.Sounds;
            var engine = ModuleMain.CueEngine;
            b.PushContext(Strings.ModSounds, Strings.RoleList);
            if (sounds == null || engine == null)
            {
                b.AddItem(ControlId.Structural("modsounds:none"), GameNodes.Text(() => Strings.NoTooltip));
                b.PopContext();
                return;
            }
            b.AddItem(ControlId.Structural("modsounds:master"), Adjustable(() => Strings.SoundMaster,
                () => Strings.SoundPercent(sounds.Master.Value), sign => sounds.Master.Adjust(sign), null));
            foreach (var volume in sounds.All)
            {
                var v = volume;
                b.AddItem(ControlId.Structural("modsounds:volume:" + AudioCues.FileName(v.Cue)), Adjustable(() => Strings.SoundLabel(v.Cue),
                    () => Strings.SoundPercent(v.Value), sign => v.Adjust(sign), () => engine.PlayCue(v.Cue, 1f, 0f)));
            }
            b.PopContext();
        }

        // A volume row: a slider in all but the widget (Left and Right step it, the percent read at
        // once); Enter previews the cue when it has one.
        private static NodeVtable Adjustable(Func<string> label, Func<string> value, Func<int, bool> adjust, Action preview)
        {
            return new NodeVtable
            {
                ControlType = ControlTypes.Slider,
                Announcements = new List<NodeAnnouncement>
                {
                    GameNodes.LabelPart(label),
                    new NodeAnnouncement(value, live: true, kind: AnnouncementKinds.Value),
                },
                SearchText = label,
                OnAdjust = (sign, large) => { for (int i = 0; i < (large ? 5 : 1); i++) adjust(sign); },
                StateText = value,
                OnActivate = preview,
            };
        }

        public override IEnumerable<ElementAction> GetActions()
        {
            yield return new ElementAction(ActionIds.Back, Strings.Get("bind.ui.back"), _ => ParentScreen?.RemoveChild(this));
        }
    }
}
