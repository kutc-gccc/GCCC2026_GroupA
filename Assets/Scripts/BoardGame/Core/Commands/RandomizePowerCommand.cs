using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    /// <summary>
    /// 自分の駒の戦闘力を1〜3のランダムな値に変更するコマンド要求
    /// </summary>
    public sealed class RandomizePowerCommand : GameCommand
    {
        public PieceId PieceId { get; }

        public RandomizePowerCommand(PlayerId player, PieceId pieceId)
            : base(player)
        {
            PieceId = pieceId;
        }
    }
}
