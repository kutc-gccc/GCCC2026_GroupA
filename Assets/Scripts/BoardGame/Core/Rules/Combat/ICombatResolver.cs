using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Combat
{
    public interface ICombatResolver
    {
        CombatResolution Resolve(PieceState attacker, PieceState defender);
    }
}
