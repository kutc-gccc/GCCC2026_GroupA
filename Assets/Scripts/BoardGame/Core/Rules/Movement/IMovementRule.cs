using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Movement
{
    public interface IMovementRule
    {
        IReadOnlyList<GridPosition> GetLegalDestinations(GameSnapshot snapshot, PieceState piece);
    }
}
