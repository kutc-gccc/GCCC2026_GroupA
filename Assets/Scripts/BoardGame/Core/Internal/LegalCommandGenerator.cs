using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.Fusion;
using GCCC.BoardGame.Core.Rules.Movement;

namespace GCCC.BoardGame.Core.Internal
{
    internal sealed class LegalCommandGenerator
    {
        private readonly IMovementRule movementRule;
        private readonly IFusionResolver fusionResolver;
        private readonly Func<PieceState, bool> canRandomizePower;
        private readonly Func<PlayerId, int> getBoardPieceCount;
        private readonly Func<PlayerId, IReadOnlyList<ReservePieceState>> getReserves;
        private readonly Func<PlayerId, IEnumerable<GridPosition>> getDeploymentPositions;
        private readonly int maxPiecesPerPlayer;

        public LegalCommandGenerator(
            IMovementRule movementRule,
            IFusionResolver fusionResolver,
            Func<PieceState, bool> canRandomizePower,
            Func<PlayerId, int> getBoardPieceCount,
            Func<PlayerId, IReadOnlyList<ReservePieceState>> getReserves,
            Func<PlayerId, IEnumerable<GridPosition>> getDeploymentPositions,
            int maxPiecesPerPlayer)
        {
            this.movementRule = movementRule;
            this.fusionResolver = fusionResolver;
            this.canRandomizePower = canRandomizePower;
            this.getBoardPieceCount = getBoardPieceCount;
            this.getReserves = getReserves;
            this.getDeploymentPositions = getDeploymentPositions;
            this.maxPiecesPerPlayer = maxPiecesPerPlayer;
        }

        public IReadOnlyList<GameCommand> Generate(
            GameSnapshot snapshot,
            PlayerId player)
        {
            List<GameCommand> commands = new List<GameCommand>();
            foreach (PieceState piece in snapshot.Pieces)
            {
                if (piece.Owner != player)
                {
                    continue;
                }

                foreach (GridPosition destination in
                         movementRule.GetLegalDestinations(snapshot, piece))
                {
                    commands.Add(new MovePieceCommand(player, piece.Id, destination));
                }

                if (canRandomizePower(piece))
                {
                    commands.Add(new RandomizePowerCommand(player, piece.Id));
                }
            }

            if (fusionResolver.IsEnabled)
            {
                foreach (FusionPair pair in fusionResolver.GetLegalFusions(snapshot, player))
                {
                    commands.Add(new FusePiecesCommand(
                        player, pair.FirstPieceId, pair.SecondPieceId));
                }
            }

            if (getBoardPieceCount(player) >= maxPiecesPerPlayer)
            {
                return commands;
            }

            GridPosition[] positions = new List<GridPosition>(
                getDeploymentPositions(player)).ToArray();
            foreach (ReservePieceState reservePiece in getReserves(player))
            {
                foreach (GridPosition destination in positions)
                {
                    commands.Add(new DeployReservePieceCommand(
                        player, reservePiece.Id, destination));
                }
            }

            return commands;
        }
    }
}
