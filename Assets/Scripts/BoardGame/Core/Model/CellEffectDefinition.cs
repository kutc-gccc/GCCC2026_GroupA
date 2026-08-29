using System;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class CellEffectDefinition
    {
        public CellEffectDefinition(string effectId, CellEffectLifetime lifetime)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Cell effect ID must not be empty.", nameof(effectId));
            }

            EffectId = effectId;
            Lifetime = lifetime;
        }

        public string EffectId { get; }

        public CellEffectLifetime Lifetime { get; }
    }
}
