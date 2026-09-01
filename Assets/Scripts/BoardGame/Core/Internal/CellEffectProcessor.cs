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

                bool alreadyApplied = effectDefinition.Lifetime ==
                    CellEffectLifetime.PermanentOncePerPiece
                        ? currentPiece.HasAppliedPermanentEffect(effectId)
                        : currentPiece.HasActiveEffect(effectId);
                if (alreadyApplied)
                {
                    continue;
                }

                ICellEffectHandler handler = handlers[effectId];
                int previousPower = currentPiece.EffectiveCombatPower;
                CellEffectResult result = handler.Apply(
                    new CellEffectContext(
                        getSnapshot(), currentPiece, cell, effectDefinition));
                validateResult(currentPiece, result.Piece);

                PieceState updatedPiece = result.Piece;
                if (effectDefinition.Lifetime ==
                    CellEffectLifetime.PermanentOncePerPiece)
                {
                    updatedPiece = updatedPiece.WithPermanentEffectApplied(effectId);
                }
                else if (!updatedPiece.HasActiveEffect(effectId))
                {
                    updatedPiece = updatedPiece.WithActiveEffect(effectId);
                }

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
    }
}
