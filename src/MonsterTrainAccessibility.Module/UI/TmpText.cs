using System;
using System.Reflection;
using MonsterTrainAccessibility.Core;
using TMPro;

namespace MonsterTrainAccessibility.Module.UI
{
    /// <summary>
    /// A TextMeshPro label's current content, as the label draws it (its rich-text source, tags and
    /// all). This game's TextMeshPro leaves the <c>text</c> property behind when a label is set with
    /// numbers (<c>SetText("{0}", health)</c>, the game's <c>SetTextSafe(format, value)</c>): the property
    /// keeps the prefab's placeholder ("6" health on every card, "13" on the Dragon's Hoard) while the
    /// label draws the real value, which it keeps in its backing array. Every set fills that array, so a
    /// label set since load reads from there; one never set (the array still empty) keeps its authored
    /// text in the property. Every read of a game label goes through here, never through <c>text</c>.
    /// </summary>
    public static class TmpText
    {
        private static readonly Func<TMP_Text, string> Backing = Bind();

        public static string Content(this TMP_Text label)
        {
            if (label == null) return null;
            if (Backing != null)
            {
                try
                {
                    string drawn = Backing(label);
                    if (!string.IsNullOrEmpty(drawn)) return drawn;
                }
                catch (Exception e)
                {
                    CoreLog.Warning("TmpText: reading " + label.name + "'s backing text failed: " + e.Message);
                }
            }
            return label.text;
        }

        private static Func<TMP_Text, string> Bind()
        {
            var method = typeof(TMP_Text).GetMethod("InternalTextBackingArrayToString", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null || method.ReturnType != typeof(string) || method.GetParameters().Length != 0)
            {
                CoreLog.Warning("TmpText: TextMeshPro has no InternalTextBackingArrayToString(); labels set with numbers read stale");
                return null;
            }
            return (Func<TMP_Text, string>)Delegate.CreateDelegate(typeof(Func<TMP_Text, string>), method);
        }
    }
}
