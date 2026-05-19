using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Presentation.Verbosity;

namespace MonsterTrainAccessibility.Presentation
{
    internal sealed class PresentationContext<TSource>
    {
        public PresentationContext(TSource source)
        {
            Source = source;
            AllGameManagers managers = AllGameManagers.Instance.OrNull();
            SaveManager = managers?.GetSaveManager();
            RelicManager = managers?.GetRelicManager();
            StatusEffectManager = StatusEffectManager.Instance.OrNull();
            Tooltips = new Lazy<List<TooltipContent>>(() => new List<TooltipContent>());
            StatusEffects = new Lazy<List<StatusEffectStackData>>(() => new List<StatusEffectStackData>());
            Profile = VerbosityRegistry.ForSource<TSource>();
        }

        public TSource Source { get; }
        public SaveManager SaveManager { get; }
        public RelicManager RelicManager { get; }
        public StatusEffectManager StatusEffectManager { get; }
        public Lazy<List<TooltipContent>> Tooltips { get; }
        public Lazy<List<StatusEffectStackData>> StatusEffects { get; }
        public VerbosityProfile Profile { get; }
    }
}
