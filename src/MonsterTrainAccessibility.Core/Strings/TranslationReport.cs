using System.Collections.Generic;

namespace MonsterTrainAccessibility.Core.Strings
{
    /// <summary>
    /// What <see cref="Strings.LoadTranslation"/> applied and what it set aside, for the caller to log:
    /// a translation file is a player's or a translator's, and a silent drop would leave a line in
    /// English with nothing to say why.
    /// </summary>
    public sealed class TranslationReport
    {
        /// <summary>The entries now overriding the English defaults.</summary>
        public int Applied;
        /// <summary>The numbers (one-based) of the lines that are no "key=value".</summary>
        public readonly List<int> Malformed = new List<int>();
        /// <summary>Keys the strings table does not define.</summary>
        public readonly List<string> UnknownKeys = new List<string>();
        /// <summary>Keys with nothing after the '='.</summary>
        public readonly List<string> EmptyKeys = new List<string>();
        /// <summary>Keys whose value has a "{n}" slot the English lacks, or unbalanced braces.</summary>
        public readonly List<string> BadSlots = new List<string>();

        /// <summary>One log line per kind of fault, none for a clean file.</summary>
        public IEnumerable<string> Problems()
        {
            if (Malformed.Count > 0) yield return "no key=value on line " + string.Join(", ", Malformed);
            if (UnknownKeys.Count > 0) yield return "unknown key " + string.Join(", ", UnknownKeys);
            if (EmptyKeys.Count > 0) yield return "empty value for " + string.Join(", ", EmptyKeys);
            if (BadSlots.Count > 0) yield return "slots that do not fit the English of " + string.Join(", ", BadSlots);
        }
    }
}
