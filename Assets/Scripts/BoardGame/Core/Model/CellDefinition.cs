using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class CellDefinition
    {
        public CellDefinition(
            GridPosition position,
            PlayerId? territoryOwner = null,
            IEnumerable<string> effectIds = null)
        {
            Position = position;
            TerritoryOwner = territoryOwner;
            string[] copiedEffects = effectIds?.ToArray() ?? Array.Empty<string>();
            EffectIds = new ReadOnlyCollection<string>(copiedEffects);
        }

        public GridPosition Position { get; }

        public PlayerId? TerritoryOwner { get; }

        public IReadOnlyList<string> EffectIds { get; }
    }
}
