namespace GCCC.BoardGame.Core.Model
{
    /// <summary>マス効果がいつ発動し、どこまで効き続けるか。</summary>
    public enum CellEffectLifetime
    {
        /// <summary>止まっている間だけ効く。離れると消える。</summary>
        WhileOccupied,

        /// <summary>1駒につき1回だけ発動する。効いた履歴は駒に残り続ける。</summary>
        PermanentOncePerPiece,

        /// <summary>止まるたびに毎回発動する。履歴を残さないので回数に上限がない。</summary>
        EveryStop
    }
}
