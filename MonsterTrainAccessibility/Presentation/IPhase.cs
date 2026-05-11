namespace MonsterTrainAccessibility.Presentation
{
    internal interface IPhase<TSource>
    {
        bool Matches(PresentationContext<TSource> context);
        void Apply(PresentationContext<TSource> context, PresentationBuilder builder);
    }
}
