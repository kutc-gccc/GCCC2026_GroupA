using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Fusion
{
    public sealed class FusionResolution
    {
        private FusionResolution(bool isSuccessful, PieceState resultingPiece, int bonus)
        {
            IsSuccessful = isSuccessful;
            ResultingPiece = resultingPiece;
            Bonus = bonus;
        }

        /// <summary>
        /// 合体を試みた結果、実際に駒が合体したかどうか。
        /// false の場合、合体自体は正当な試みだったが判定に失敗しており、
        /// ResultingPiece は null になる（この場合もターンは消費される）。
        /// </summary>
        public bool IsSuccessful { get; }

        public PieceState ResultingPiece { get; }

        /// <summary>成功時に上乗せされた戦闘力。成功=1、大成功=2、失敗時は0。</summary>
        public int Bonus { get; }

        public static FusionResolution Success(PieceState resultingPiece, int bonus)
        {
            return new FusionResolution(true, resultingPiece, bonus);
        }

        public static FusionResolution Attempted()
        {
            return new FusionResolution(false, null, 0);
        }
    }
}
