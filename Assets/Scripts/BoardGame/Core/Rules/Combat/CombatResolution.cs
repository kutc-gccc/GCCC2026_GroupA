namespace GCCC.BoardGame.Core.Rules.Combat
{
    public readonly struct CombatResolution
    {
        public CombatResolution(int damageToAttacker, int damageToDefender)
        {
            DamageToAttacker = damageToAttacker;
            DamageToDefender = damageToDefender;
        }

        public int DamageToAttacker { get; }

        public int DamageToDefender { get; }
    }
}
