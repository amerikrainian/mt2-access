using MonsterTrainAccessibility.Core;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    public class SpeechTests
    {
        [Theory]
        [InlineData("Crit", "Crit")]
        [InlineData("Shard_S", "Shard")]
        [InlineData("ManaRegen", "Mana Regen")]
        [InlineData("AttackSpeed", "Attack Speed")]
        [InlineData("Difficulty_SSS", "Difficulty SSS")]
        [InlineData("Health_XL", "Health")]
        [InlineData("", "")]
        public void SpriteName_speaks_the_words_of_the_name(string sprite, string spoken)
        {
            Assert.Equal(spoken, Speech.SpriteName(sprite));
        }

        [Fact]
        public void SpriteNames_replaces_named_sprites_by_their_words()
        {
            Assert.Equal(" Mana Regen +2.  Attack Speed +10",
                Speech.SpriteNames("<sprite name=ManaRegen>+2. <sprite name=\"AttackSpeed\">+10"));
        }

        [Fact]
        public void SpriteNames_drops_unnamed_sprites_and_the_parentheses_they_leave_empty()
        {
            Assert.Equal("cost  15 ", Speech.SpriteNames("cost <sprite=3>15 (<sprite index=2>)"));
        }

        [Fact]
        public void SpriteNames_leaves_plain_text_alone()
        {
            Assert.Equal("Vault Spark", Speech.SpriteNames("Vault Spark"));
            Assert.Null(Speech.SpriteNames(null));
        }
    }
}
