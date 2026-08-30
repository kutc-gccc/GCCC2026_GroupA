using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Config
{
    public abstract class CellEffectConfig : ScriptableObject
    {
        [SerializeField] private string effectId;
        [SerializeField] private CellEffectLifetime lifetime;

        public string EffectId => effectId;

        public CellEffectLifetime Lifetime => lifetime;

        public CellEffectDefinition CreateDefinition()
        {
            return new CellEffectDefinition(effectId, lifetime);
        }

        public abstract ICellEffectHandler CreateHandler();
    }
}
