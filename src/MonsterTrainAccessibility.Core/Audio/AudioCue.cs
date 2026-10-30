namespace MonsterTrainAccessibility.Core.Audio
{
    /// <summary>
    /// A named one-shot cue (the engine owns the sound file behind each name, so Core stays free of
    /// paths). One cue per file under assets/audio; a placeholder is replaced 1:1 by dropping in a file
    /// with the same name. The dd2a11y (Harkest Dungeon) pattern, by way of Guildrun.
    /// </summary>
    public enum AudioCue
    {
        // Navigation (assets/audio/navigation).
        /// <summary>Focus crosses, along a floor, from the enemy's units onto the player's.</summary>
        TeamFriendly,
        /// <summary>Focus crosses, along a floor, from the player's units onto the enemy's.</summary>
        TeamEnemy,
    }

    /// <summary>The sounds screen's grouping of cues, mirroring the assets/audio folders.</summary>
    public enum AudioCueGroup
    {
        Navigation,
    }

    public static class AudioCues
    {
        /// <summary>A cue's group: every cue is a navigation cue so far.</summary>
        public static AudioCueGroup GroupOf(AudioCue cue) => AudioCueGroup.Navigation;

        /// <summary>The file a cue plays, under the group's folder, without an extension:
        /// "team_friendly" for <see cref="AudioCue.TeamFriendly"/> (the enum name in snake case).</summary>
        public static string FileName(AudioCue cue)
        {
            string name = cue.ToString();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>A cue's file under the asset root: "navigation/team_friendly.wav".</summary>
        public static string RelativePath(AudioCue cue)
            => GroupOf(cue).ToString().ToLowerInvariant() + "/" + FileName(cue) + ".wav";
    }
}
