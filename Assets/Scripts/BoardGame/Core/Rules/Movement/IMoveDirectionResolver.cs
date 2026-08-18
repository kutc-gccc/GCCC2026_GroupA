using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Movement
{
    public interface IMoveDirectionResolver
    {
        MoveDirections Resolve(PieceState piece);
    }
}
