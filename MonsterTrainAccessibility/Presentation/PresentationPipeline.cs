using System;
using System.Collections.Generic;
using MonsterTrainAccessibility.Core;

namespace MonsterTrainAccessibility.Presentation
{
    internal sealed class PresentationPipeline<TSource>
    {
        private readonly List<IPhase<TSource>> _phases;

        public PresentationPipeline(IEnumerable<IPhase<TSource>> phases)
        {
            _phases = phases != null ? new List<IPhase<TSource>>(phases) : new List<IPhase<TSource>>();
        }

        public Presentation Build(TSource source)
        {
            PresentationBuilder builder = new PresentationBuilder();
            if (source == null)
            {
                return builder.Build();
            }

            PresentationContext<TSource> context = new PresentationContext<TSource>(source);
            for (int i = 0; i < _phases.Count; i++)
            {
                IPhase<TSource> phase = _phases[i];
                if (phase == null)
                {
                    continue;
                }

                try
                {
                    if (phase.Matches(context))
                    {
                        phase.Apply(context, builder);
                    }
                }
                catch (Exception ex)
                {
                    Log.Info("[AccessibilityMod] Presentation phase failed: " + phase.GetType().Name + ": " + ex);
                }
            }

            return builder.Build();
        }
    }
}
