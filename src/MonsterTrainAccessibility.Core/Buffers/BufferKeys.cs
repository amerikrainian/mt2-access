namespace MonsterTrainAccessibility.Core.Buffers
{
    /// <summary>
    /// The registered buffer keys, in cycling order. The ui buffer carries the focused control's own
    /// lines (<see cref="NodeLines"/>); the hero and item buffers are filled by the focused control
    /// through <see cref="Graph.NodeVtable.SideLines"/> (the hero it concerns, the items it carries),
    /// and so is the quest buffer (the quests of those items, or of the focused item or relic);
    /// the relics buffer lists the run's relics; the party and enemies buffers read the battlefield
    /// and are empty outside a fight; the combat buffer is the battle events log.
    /// </summary>
    public static class BufferKeys
    {
        public const string Ui = "ui";
        public const string Hero = "hero";
        public const string Quest = "quest";
        /// <summary>Not a buffer: the same quests without their reward lines, what the quests glance
        /// key speaks. A control answers it through <see cref="Graph.NodeVtable.SideLines"/> as it
        /// does a buffer's key.</summary>
        public const string QuestBrief = "quest.brief";
        public const string Item = "item";
        public const string Relic = "relic";
        public const string Party = "party";
        public const string Enemies = "enemies";
        public const string Combat = "combat";
    }
}
