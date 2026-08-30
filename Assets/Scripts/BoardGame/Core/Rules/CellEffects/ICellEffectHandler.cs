namespace GCCC.BoardGame.Core.Rules.CellEffects
{
    public interface ICellEffectHandler
    {
        string EffectId { get; }

        bool BlocksPowerRandomization { get; }

        CellEffectResult Apply(CellEffectContext context);
    }
}
