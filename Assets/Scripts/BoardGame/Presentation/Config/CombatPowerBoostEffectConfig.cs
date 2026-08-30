using GCCC.BoardGame.Core.Rules.CellEffects;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Config
{
    [CreateAssetMenu(
        menuName = "GCCC/Cell Effects/Combat Power Boost",
        fileName = "CombatPowerBoostEffect")]
    public sealed class CombatPowerBoostEffectConfig : CellEffectConfig
    {
        [SerializeField, Min(1)] private int amount = 1;

        public override ICellEffectHandler CreateHandler()
        {
            return new CombatPowerBoostCellEffectHandler(EffectId, amount);
        }
    }
}
