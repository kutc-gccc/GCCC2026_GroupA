using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Combat
{
    public sealed class SimultaneousCombatResolver : ICombatResolver
    {
        public CombatResolution Resolve(PieceState attacker, PieceState defender)
        {
            return new CombatResolution(
                attacker.CombatPower - defender.CombatPower,
                defender.CombatPower - attacker.CombatPower);
        }
    }
}
