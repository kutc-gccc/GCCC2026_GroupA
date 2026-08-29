using System;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class ActiveCellEffectState
    {
        public ActiveCellEffectState(string effectId, int temporaryCombatPower)
        {
            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new ArgumentException(
                    "Cell effect ID must not be empty.", nameof(effectId));
            }

            if (temporaryCombatPower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(temporaryCombatPower));
            }

            EffectId = effectId;
            TemporaryCombatPower = temporaryCombatPower;
        }

        public string EffectId { get; }

        public int TemporaryCombatPower { get; }

        public ActiveCellEffectState WithTemporaryCombatPower(int value)
        {
            return new ActiveCellEffectState(EffectId, value);
        }
    }
}
