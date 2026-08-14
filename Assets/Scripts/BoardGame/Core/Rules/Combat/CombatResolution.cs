namespace GCCC.BoardGame.Core.Rules.Combat
{
    public readonly struct CombatResolution
    {
        public CombatResolution(int attackerRemainingPower, int defenderRemainingPower)
        {
            AttackerRemainingPower = attackerRemainingPower;
            DefenderRemainingPower = defenderRemainingPower;
        }

        public int AttackerRemainingPower { get; }

        public int DefenderRemainingPower { get; }
    }
}
