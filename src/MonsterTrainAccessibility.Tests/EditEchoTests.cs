using MonsterTrainAccessibility.Core.UI;
using Xunit;

namespace MonsterTrainAccessibility.Tests
{
    // What is spoken for one step of editing a text field (English strings: no translation loaded).
    public class EditEchoTests
    {
        private static EditState At(string text, int caret) => new EditState(text, caret, caret);
        private static EditState Sel(string text, int anchor, int caret) => new EditState(text, caret, anchor);

        [Fact]
        public void TypingEchoesTheCharacter()
        {
            Assert.Equal("u", EditEcho.Describe(At("y", 1), At("yu", 2), false));
            Assert.Equal("space", EditEcho.Describe(At("yu", 2), At("yu ", 3), false));
            Assert.Equal("x", EditEcho.Describe(At("yu", 1), At("yxu", 2), false));
        }

        [Fact]
        public void DeletingEchoesWhatWent()
        {
            Assert.Equal("u", EditEcho.Describe(At("yu", 2), At("y", 1), false));
            Assert.Equal("y", EditEcho.Describe(At("yu", 0), At("u", 0), false));
            Assert.Equal("yuuna", EditEcho.Describe(Sel("yuuna", 0, 5), At("", 0), false));
        }

        [Fact]
        public void PastingAndTypingOverASelectionEchoTheNewText()
        {
            Assert.Equal("nyx", EditEcho.Describe(At("", 0), At("nyx", 3), false));
            Assert.Equal("k", EditEcho.Describe(Sel("yuuna", 0, 5), At("k", 1), false));
        }

        [Fact]
        public void AMoveReadsTheCharacterAtTheCaret()
        {
            Assert.Equal("u", EditEcho.Describe(At("yu", 2), At("yu", 1), false));
            Assert.Equal("blank", EditEcho.Describe(At("yu", 1), At("yu", 2), false));
            Assert.Equal("space", EditEcho.Describe(At("a b", 0), At("a b", 1), false));
            Assert.Null(EditEcho.Describe(At("yu", 1), At("yu", 1), false));
        }

        [Fact]
        public void AWordMoveReadsTheWord()
        {
            Assert.Equal("banner", EditEcho.Describe(At("guild banner", 0), At("guild banner", 6), true));
            Assert.Equal("blank", EditEcho.Describe(At("guild", 0), At("guild", 5), true));
        }

        [Fact]
        public void ASelectionReadsWhatItGainedOrLost()
        {
            Assert.Equal("yuuna selected", EditEcho.Describe(At("yuuna", 5), Sel("yuuna", 0, 5), false));
            Assert.Equal("u selected", EditEcho.Describe(Sel("yuuna", 0, 1), Sel("yuuna", 0, 2), false));
            Assert.Equal("u unselected", EditEcho.Describe(Sel("yuuna", 0, 2), Sel("yuuna", 0, 1), false));
            Assert.Equal("a selected", EditEcho.Describe(Sel("yuuna", 5, 5), Sel("yuuna", 5, 4), false));
            Assert.Null(EditEcho.Describe(Sel("yuuna", 0, 2), Sel("yuuna", 0, 2), false));
        }

        [Fact]
        public void ACollapsedSelectionReadsTheCaret()
        {
            Assert.Equal("blank", EditEcho.Describe(Sel("yu", 0, 2), At("yu", 2), false));
            Assert.Equal("y", EditEcho.Describe(Sel("yu", 0, 2), At("yu", 0), false));
        }
    }
}
