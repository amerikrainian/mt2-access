using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MonsterTrainAccessibility.Core.Text
{
    /// <summary>
    /// One definition the game can show: its id (the same in every language: "status:armor"), its name as
    /// the game prints it in the current language, its body as the game renders it (markup kept), its
    /// buffer line, and the ids of the definitions its text refers to, worked out from the game's source
    /// text rather than from this language's wording (see <see cref="Glossary"/>); null when it has no
    /// source text to read them from, and its rendered body's exact names stand in. Body and line are
    /// read live: a definition's numbers can depend on the run (a status's per-stack amount with relics).
    /// </summary>
    public sealed class GlossaryEntry
    {
        public GlossaryEntry(string name, string id, Func<string> body, Func<string> line, IEnumerable<string> references = null)
        {
            Name = name;
            Id = id;
            Body = body;
            Line = line;
            References = references != null ? new List<string>(references) : null;
        }

        public string Name { get; }
        public string Id { get; }
        public Func<string> Body { get; }
        public Func<string> Line { get; }
        public IReadOnlyList<string> References { get; }
    }

    /// <summary>
    /// The definitions a node's text leaves out. The links between definitions are the game's own, read
    /// by the module from the source text of each definition's key: the game's replacement tokens
    /// (<c>[armor]</c>), in this language's text and in the English, and the English text's own bold. A
    /// translator who writes a keyword out by hand ("броню" for Броня) changes nothing: Valor refers to
    /// Armor in every language because its English text does. In the text a node shows (a card's, an
    /// artifact's), a bold span is a mention when it is exactly a definition's name, as every token
    /// renders: rich text and any stack count stripped, "Valor 1" names Valor.
    /// <para>A translation's own bold in a definition is never read where the source text is: it can be
    /// written out by hand ("броню"), or name another keyword's exact name for its own word.</para>
    /// <para>Expanding a node's lines finds the entries it mentions and the entries those refer to, until
    /// nothing new turns up; anything the node already shows (its name, a detail's title) is never added,
    /// though what it refers to is.</para>
    /// <para>Inflected forms are matched only where the text is English, the source the links are read
    /// from ("Abilities" for Ability, "Forge Point" for Forge Points): the same number of words, each equal
    /// or sharing a prefix of at least four letters, with at most two letters of the entry's word and three
    /// of the mention's past it; of the entries qualifying, the one fewest letters away, and none on a tie.
    /// Another language's spelling is never guessed at: its words can name another keyword entirely (the
    /// German "Verzauberungen" in Dormant's text is Enchant, while "Verzauberung" is Incant).</para>
    /// </summary>
    public sealed class Glossary
    {
        private static readonly Regex Bold = new Regex("<b>(.*?)</b>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        // A stack count or amount before or after the name: "Valor 1", "+2 Rage", "Spikes {0}", "x2", "1x Valor",
        // "Алчность х10" (a Cyrillic x), "30X". Han, kana and Hangul write it without the space ("連撃1").
        private const string Amount = @"[+\-]?(\d+[x×хX]?|[x×хX]\d+|\{\d+\})%?";
        private static readonly Regex Count = new Regex("^" + Amount + @"\s+|\s+" + Amount + "$|(?<=[⺀-￯])" + Amount + "$|^" + Amount + "$", RegexOptions.Compiled);
        private static readonly char[] Edges = { ' ', '«', '»', '"', '“', '”', '„', '\'', ':', '.', ',', '!', '?', '；', '：', '。', '、', '「', '」', '『', '』' };

        private readonly Dictionary<string, GlossaryEntry> _byName = new Dictionary<string, GlossaryEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GlossaryEntry> _byId = new Dictionary<string, GlossaryEntry>(StringComparer.Ordinal);
        private readonly List<GlossaryEntry> _entries = new List<GlossaryEntry>();
        // Resolution is a pure function of the entries' names: an inflected mention is worked out once.
        private readonly Dictionary<string, GlossaryEntry> _inflected = new Dictionary<string, GlossaryEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The entries in order; a name already taken keeps its first entry, and the id of the one
        /// set aside then means the kept one (reported through <see cref="Duplicates"/>). With
        /// <paramref name="inflect"/> (an English glossary) mentions also match their inflected forms.</summary>
        public Glossary(IEnumerable<GlossaryEntry> entries, bool inflect = false)
        {
            Inflected = inflect;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Name)) continue;
                if (_byName.TryGetValue(entry.Name, out var first))
                {
                    Duplicates.Add(new KeyValuePair<GlossaryEntry, GlossaryEntry>(first, entry));
                    if (!_byId.ContainsKey(entry.Id)) _byId[entry.Id] = first;
                    continue;
                }
                _byName[entry.Name] = entry;
                _byId[entry.Id] = entry;
                _entries.Add(entry);
            }
        }

        public IReadOnlyList<GlossaryEntry> Entries => _entries;

        /// <summary>Whether mentions match inflected forms (the glossary is English's).</summary>
        public bool Inflected { get; }

        /// <summary>Names claimed twice: the entry kept, then the one set aside.</summary>
        public List<KeyValuePair<GlossaryEntry, GlossaryEntry>> Duplicates { get; } = new List<KeyValuePair<GlossaryEntry, GlossaryEntry>>();

        /// <summary>A name or mention as matched: rich text, a stack count and surrounding quotes and
        /// punctuation gone. Empty when no word is left ("+1" and an icon, a lone "x").</summary>
        public static string Normalize(string text)
        {
            string s = TextUtil.StripRichText(text);
            if (string.IsNullOrEmpty(s)) return string.Empty;
            s = s.Trim(Edges);
            s = Count.Replace(s, "");
            s = s.Trim(Edges);
            return Word(s) ? s : string.Empty;
        }

        // At least one letter, and more than one where letters are an alphabet's (a Han keyword can be a
        // single character).
        private static bool Word(string s)
        {
            bool letter = false;
            foreach (char c in s)
                if (char.IsLetter(c)) { letter = true; break; }
            if (!letter) return false;
            return s.Length > 1 || s[0] >= '֐';
        }

        /// <summary>The mentions in a text: its bold spans, normalized, in order, blanks dropped.</summary>
        public static IEnumerable<string> Mentions(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (Match m in Bold.Matches(text))
            {
                string name = Normalize(m.Groups[1].Value);
                if (name.Length > 0) yield return name;
            }
        }

        /// <summary>The entry of exactly this name (after <see cref="Normalize"/>), or null.</summary>
        public GlossaryEntry Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _byName.TryGetValue(name, out var entry);
            return entry;
        }

        /// <summary>The entry of this id (one set aside as a duplicate gives the entry kept), or null.</summary>
        public GlossaryEntry ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _byId.TryGetValue(id, out var entry);
            return entry;
        }

        /// <summary>The entry a mention names: its exact name, else, in an inflecting glossary, the entry it
        /// inflects, the closest when several qualify ("Verbrauche" is Verbrauch before Verbraucht); a
        /// tie names none.</summary>
        public GlossaryEntry Resolve(string mention)
        {
            var exact = Find(mention);
            if (exact != null || !Inflected || string.IsNullOrEmpty(mention)) return exact;
            if (_inflected.TryGetValue(mention, out var known)) return known;
            GlossaryEntry found = null;
            int best = int.MaxValue;
            foreach (var entry in _entries)
            {
                int distance = Distance(mention, entry.Name);
                if (distance < 0 || distance > best) continue;
                found = distance < best ? entry : null;
                best = distance;
            }
            _inflected[mention] = found;
            return found;
        }

        /// <summary>Whether a mention is an inflected form of a name (the rule in the class summary).</summary>
        public static bool Inflects(string mention, string name) => Distance(mention, name) >= 0;

        // How far an inflected mention is from a name: the letters past their shared prefixes, summed over
        // the words; -1 when it is no inflection of it (an equal name is an exact match, not one).
        private static int Distance(string mention, string name)
        {
            var a = mention.Split(' ');
            var b = name.Split(' ');
            if (a.Length != b.Length) return -1;
            int distance = 0;
            for (int i = 0; i < a.Length; i++)
            {
                string x = a[i].ToLowerInvariant(), y = b[i].ToLowerInvariant();
                if (x == y) continue;
                if (!Alphabetic(x) || !Alphabetic(y)) return -1;
                int p = 0;
                while (p < x.Length && p < y.Length && x[p] == y[p]) p++;
                if (p < 4 || y.Length - p > 2 || x.Length - p > 3) return -1;
                distance += (x.Length - p) + (y.Length - p);
            }
            return distance > 0 ? distance : -1;
        }

        // Latin letters (and the Greek and Cyrillic an English text never needs): word endings inflect.
        private static bool Alphabetic(string word)
        {
            if (word.Length == 0) return false;
            foreach (char c in word)
                if (c >= '֐' || !(char.IsLetter(c) || c == '-' || c == '\'')) return false;
            return true;
        }

        /// <summary>
        /// The entries a node's texts mention and it does not show, in the order met: its head parts
        /// (its name among them) and its details ("title: body", one per tooltip), then the entries those
        /// entries refer to in turn. The lines of the panel the node is one line of (<paramref name="panel"/>)
        /// show what they name or title too, though they are not followed: each line's own node does that.
        /// </summary>
        public List<GlossaryEntry> Expand(IReadOnlyList<string> head, IReadOnlyList<string> details, IReadOnlyList<string> panel = null)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            if (panel != null)
                foreach (var line in panel)
                {
                    var named = Find(Normalize(line)) ?? Find(Normalize(Title(line)));
                    if (named != null) visited.Add(named.Id);
                }
            var shown = new List<GlossaryEntry>();
            if (head != null)
                foreach (var part in head) MarkShown(Find(Normalize(part)), visited, shown);
            if (details != null)
                foreach (var line in details) MarkShown(Find(Normalize(Title(line))), visited, shown);

            var queue = new Queue<GlossaryEntry>();
            if (head != null)
                foreach (var part in head) Enqueue(part, queue);
            if (details != null)
                foreach (var line in details) Enqueue(line, queue);
            foreach (var entry in shown) EnqueueReferences(entry, queue);

            var added = new List<GlossaryEntry>();
            while (queue.Count > 0)
            {
                var entry = queue.Dequeue();
                if (!visited.Add(entry.Id)) continue;
                added.Add(entry);
                EnqueueReferences(entry, queue);
            }
            return added;
        }

        private static void MarkShown(GlossaryEntry entry, HashSet<string> visited, List<GlossaryEntry> shown)
        {
            if (entry != null && visited.Add(entry.Id)) shown.Add(entry);
        }

        private void Enqueue(string text, Queue<GlossaryEntry> queue)
        {
            foreach (var mention in Mentions(text))
            {
                var entry = Resolve(mention);
                if (entry != null) queue.Enqueue(entry);
            }
        }

        // What an entry refers to: its source text's links. Only a definition with no source text is read
        // by the names its rendered body prints exactly: a translation can bold another keyword's exact
        // name for its own word (the Traditional Chinese Dormant's 強化 is Enchant, though Enhance's name).
        private void EnqueueReferences(GlossaryEntry entry, Queue<GlossaryEntry> queue)
        {
            if (entry.References == null)
            {
                Enqueue(BodyOf(entry), queue);
                return;
            }
            foreach (var id in entry.References)
            {
                var target = ById(id);
                if (target != null) queue.Enqueue(target);
            }
        }

        // A detail line's title: what precedes the ": " the tooltip lines join title and body with.
        private static string Title(string line)
        {
            if (string.IsNullOrEmpty(line)) return null;
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            return colon > 0 ? line.Substring(0, colon) : null;
        }

        private static string BodyOf(GlossaryEntry entry)
        {
            try { return entry.Body?.Invoke(); }
            catch (Exception e)
            {
                CoreLog.Warning("Glossary: " + entry.Id + "'s body threw: " + e.Message);
                return null;
            }
        }
    }
}
