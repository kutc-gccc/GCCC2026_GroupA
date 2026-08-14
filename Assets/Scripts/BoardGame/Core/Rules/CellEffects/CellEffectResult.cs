using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public sealed class CellEffectResult
    {
        public CellEffectResult(PieceState piece, IEnumerable<GameEvent> events = null)
        {
            Piece = piece;
            Events = new ReadOnlyCollection<GameEvent>(
                (events ?? Array.Empty<GameEvent>()).ToArray());
        }

        public PieceState Piece { get; }

        public IReadOnlyList<GameEvent> Events { get; }
    }
}
