using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;

namespace GCCC.BoardGame.Core.Internal
{
    internal sealed class CellEffectProcessor
    {
        private readonly GameDefinition definition;
        private readonly IReadOnlyDictionary<string, ICellEffectHandler> handlers;
        private readonly Func<GameSnapshot> getSnapshot;
        private readonly Action<PieceState> setPiece;
        private readonly Action<ReservePieceGrant, ICollection<GameEvent>> addReservePiece;
        private readonly Action<PieceState, PieceState> validateResult;

        public CellEffectProcessor(
            GameDefinition definition,
            IReadOnlyDictionary<string, ICellEffectHandler> handlers,
            Func<GameSnapshot> getSnapshot,
            Action<PieceState> setPiece,
            Action<ReservePieceGrant, ICollection<GameEvent>> addReservePiece,
            Action<PieceState, PieceState> validateResult)
        {
            this.definition = definition;
            this.handlers = handlers;
            this.getSnapshot = getSnapshot;
            this.setPiece = setPiece;
            this.addReservePiece = addReservePiece;
            this.validateResult = validateResult;
        }

        public PieceState Apply(
            PieceState piece,
            CellDefinition cell,
            ICollection<GameEvent> events)
        {
            PieceState currentPiece = piece;
            foreach (string effectId in cell.EffectIds)
            {
                if (!definition.TryGetCellEffectDefinition(
                        effectId, out CellEffectDefinition effectDefinition))
                {
                    throw new InvalidOperationException(
                        $"Cell effect '{effectId}' is not registered.");
                }

                if (HasAlreadyApplied(
                        currentPiece, effectId, effectDefinition.Lifetime))
                {
                    continue;
                }

                ICellEffectHandler handler = handlers[effectId];
                int previousPower = currentPiece.EffectiveCombatPower;
                CellEffectResult result = handler.Apply(
                    new CellEffectContext(
                        getSnapshot(), currentPiece, cell, effectDefinition));
                validateResult(currentPiece, result.Piece);

                PieceState updatedPiece =
                    RecordApplication(result.Piece, effectId, effectDefinition.Lifetime);

                events.Add(new CellEffectTriggered(
                    effectId, currentPiece.Id, cell.Position));
                if (previousPower != updatedPiece.EffectiveCombatPower)
                {
                    events.Add(new PiecePowerChanged(
                        currentPiece.Id,
                        previousPower,
                        updatedPiece.EffectiveCombatPower));
                }

                foreach (ReservePieceGrant grant in result.ReservePieceGrants)
                {
                    addReservePiece(grant, events);
                }

                foreach (GameEvent additionalEvent in result.Events)
                {
                    events.Add(additionalEvent);
                }

                currentPiece = updatedPiece;
                setPiece(currentPiece);
            }

            return currentPiece;
        }

        /// <summary>
        /// この駒には、もうこの効果が効いているか。
        /// <see cref="CellEffectLifetime.EveryStop"/>は履歴を見ないので、止まるたびに効く。
        /// </summary>
        private static bool HasAlreadyApplied(
            PieceState piece, string effectId, CellEffectLifetime lifetime)
        {
            switch (lifetime)
            {
                case CellEffectLifetime.PermanentOncePerPiece:
                    return piece.HasAppliedPermanentEffect(effectId);
                case CellEffectLifetime.EveryStop:
                    return false;
                default:
                    return piece.HasActiveEffect(effectId);
            }
        }

        /// <summary>
        /// 次に同じマスへ来たときの判断材料を駒へ書き込む。
        /// <see cref="CellEffectLifetime.EveryStop"/>は何も残さない。残すと回数が頭打ちになる。
        /// </summary>
        private static PieceState RecordApplication(
            PieceState piece, string effectId, CellEffectLifetime lifetime)
        {
            switch (lifetime)
            {
                case CellEffectLifetime.PermanentOncePerPiece:
                    return piece.WithPermanentEffectApplied(effectId);
                case CellEffectLifetime.EveryStop:
                    return piece;
                default:
                    return piece.HasActiveEffect(effectId)
                        ? piece
                        : piece.WithActiveEffect(effectId);
            }
        }
    }
}
