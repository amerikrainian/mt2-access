using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrainAccessibility.Core;
using MonsterTrainAccessibility.Localization;
using MonsterTrainAccessibility.UI;
using TMPro;
using UnityEngine;

namespace MonsterTrainAccessibility.Presentation.Compendium
{
    internal sealed class CompendiumEnemyPresentationSource
    {
        private static readonly FieldInfo NameLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "nameLabel")!;
        private static readonly FieldInfo SinDetailsLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "sinDetailsLabel")!;
        private static readonly FieldInfo DetailsLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "detailsLabel")!;
        private static readonly FieldInfo LoreLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "loreLabel")!;
        private static readonly FieldInfo AttackLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "attackDamageLabel")!;
        private static readonly FieldInfo HealLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "healAmountLabel")!;
        private static readonly FieldInfo HealthLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "healthLabel")!;
        private static readonly FieldInfo ArmorLabelField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "armorLabel")!;
        private static readonly FieldInfo ArtistAttributionRootField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "artistAttributionRoot")!;
        private static readonly FieldInfo ArtistAttributionField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "artistAttribution")!;
        private static readonly FieldInfo CharacterStateField = AccessTools.Field(typeof(global::CompendiumEnemyDetailsUI), "existingCharacterInstanceState")!;

        private CompendiumEnemyPresentationSource(
            CharacterState character,
            Message name,
            IReadOnlyList<Message> stats,
            Message lore,
            Message artist,
            IReadOnlyList<EffectBlock> fallbackEffects)
        {
            Character = character;
            Name = name;
            Stats = stats;
            Lore = lore;
            Artist = artist;
            FallbackEffects = fallbackEffects;
        }

        public CharacterState Character { get; }
        public Message Name { get; }
        public IReadOnlyList<Message> Stats { get; }
        public Message Lore { get; }
        public Message Artist { get; }
        public IReadOnlyList<EffectBlock> FallbackEffects { get; }

        public static CompendiumEnemyPresentationSource FromDetails(global::CompendiumEnemyDetailsUI details)
        {
            if (details == null || !details.gameObject.activeInHierarchy)
            {
                return null;
            }

            Message name = Text(ReflectionUtil.Get<TMP_Text>(details, NameLabelField));
            List<Message> stats = BuildStats(details);
            Message lore = Text(ReflectionUtil.Get<TMP_Text>(details, LoreLabelField));
            Message artist = BuildArtist(details);
            CharacterState character = ReflectionUtil.Get<CharacterState>(details, CharacterStateField);
            List<EffectBlock> fallbackEffects = new List<EffectBlock>();
            AddEffectBlocks(fallbackEffects, TextValue(ReflectionUtil.Get<TMP_Text>(details, SinDetailsLabelField)), "sin");
            AddEffectBlocks(fallbackEffects, TextValue(ReflectionUtil.Get<TMP_Text>(details, DetailsLabelField)), "details");
            return new CompendiumEnemyPresentationSource(character, name, stats, lore, artist, fallbackEffects);
        }

        private static List<Message> BuildStats(global::CompendiumEnemyDetailsUI details)
        {
            List<Message> stats = new List<Message>();
            AddStat(stats, details, AttackLabelField, "COMPENDIUM.ENEMY.ATTACK", skipZero: true);
            AddStat(stats, details, HealLabelField, "COMPENDIUM.ENEMY.HEAL", skipZero: true);
            AddStat(stats, details, HealthLabelField, "COMPENDIUM.ENEMY.HEALTH");
            AddStat(stats, details, ArmorLabelField, "COMPENDIUM.ENEMY.ARMOR");
            return stats;
        }

        private static void AddStat(List<Message> stats, global::CompendiumEnemyDetailsUI details, FieldInfo field, string key, bool skipZero = false)
        {
            string text = TextValue(ReflectionUtil.Get<TMP_Text>(details, field));
            if (skipZero && IsZeroStat(text))
            {
                return;
            }

            if (Message.ShouldAdd(text))
            {
                stats.Add(Message.Localized("ui", key, new { value = text }));
            }
        }

        private static bool IsZeroStat(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string normalized = text.Trim().TrimStart('+');
            return int.TryParse(
                normalized,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int value) && value == 0;
        }

        private static Message BuildArtist(global::CompendiumEnemyDetailsUI details)
        {
            GameObject root = ReflectionUtil.Get<GameObject>(details, ArtistAttributionRootField);
            if (root == null || !root.activeInHierarchy)
            {
                return null;
            }

            Message artist = Text(ReflectionUtil.Get<TMP_Text>(details, ArtistAttributionField));
            return artist != null ? Message.Localized("ui", "COMPENDIUM.ENEMY.ARTIST", new { artist = artist.Resolve() }) : null;
        }

        private static Message Text(TMP_Text label)
        {
            return Message.FromText(TextValue(label));
        }

        private static string TextValue(TMP_Text label)
        {
            return Message.Clean(AccessibilityText.ReadLocalizedText(label));
        }

        private static void AddEffectBlocks(List<EffectBlock> effects, string text, string keyPrefix)
        {
            if (effects == null || !Message.ShouldAdd(text))
            {
                return;
            }

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] rawBlocks = normalized.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int blockIndex = 0; blockIndex < rawBlocks.Length; blockIndex++)
            {
                List<string> lines = NonEmptyLines(rawBlocks[blockIndex]);
                if (lines.Count == 0)
                {
                    continue;
                }

                string title = lines[0];
                string body = lines.Count > 1 ? string.Join(" ", lines.GetRange(1, lines.Count - 1)) : null;
                effects.Add(new EffectBlock(
                    Message.FromText(title),
                    Message.FromText(body),
                    keyPrefix + ":" + blockIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
        }

        private static List<string> NonEmptyLines(string block)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrWhiteSpace(block))
            {
                return lines;
            }

            string[] rawLines = block.Split('\n');
            for (int i = 0; i < rawLines.Length; i++)
            {
                string line = Message.Clean(rawLines[i]);
                if (Message.ShouldAdd(line))
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        internal sealed class EffectBlock
        {
            public EffectBlock(Message title, Message body, string key)
            {
                Title = title;
                Body = body;
                Key = key;
            }

            public Message Title { get; }
            public Message Body { get; }
            public string Key { get; }
        }
    }
}
