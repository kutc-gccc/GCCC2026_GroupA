using System;
using System.Collections.Generic;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Rules.Movement
{
    public sealed class DirectionalMovementRule : IMovementRule
    {
        private readonly IMoveDirectionResolver directionResolver;

        private static readonly DirectionStep[] Steps =
        {
            new DirectionStep(MoveDirections.North, 0, 1),
            new DirectionStep(MoveDirections.NorthEast, 1, 1),
            new DirectionStep(MoveDirections.East, 1, 0),
            new DirectionStep(MoveDirections.SouthEast, 1, -1),
            new DirectionStep(MoveDirections.South, 0, -1),
            new DirectionStep(MoveDirections.SouthWest, -1, -1),
            new DirectionStep(MoveDirections.West, -1, 0),
            new DirectionStep(MoveDirections.NorthWest, -1, 1)
        };

        public DirectionalMovementRule()
            : this(new ProfileMoveDirectionResolver(
                new[] { PowerMovementProfile.CreateStandard() }))
        {
        }

        public DirectionalMovementRule(IMoveDirectionResolver directionResolver)
        {
            this.directionResolver = directionResolver ??
                throw new ArgumentNullException(nameof(directionResolver));
        }

        public IReadOnlyList<GridPosition> GetLegalDestinations(
            GameSnapshot snapshot,
            PieceState piece)
        {
            if (snapshot.IsGameOver)
            {
                return Array.Empty<GridPosition>();
            }

            MoveDirections effectiveDirections = directionResolver.Resolve(piece);
            List<GridPosition> legalDestinations = new List<GridPosition>(8);
            foreach (DirectionStep step in Steps)
            {
                if ((effectiveDirections & step.Direction) == 0)
                {
                    continue;
                }

                GridPosition destination = piece.Position + step.Offset;
                if (!snapshot.IsInside(destination) || IsOwnTerritory(snapshot, piece, destination))
                {
                    continue;
                }

                if (!snapshot.TryGetPiece(destination, out PieceState occupied) ||
                    occupied.Owner != piece.Owner)
                {
                    legalDestinations.Add(destination);
                }
            }

            return legalDestinations;
        }

        private static bool IsOwnTerritory(
            GameSnapshot snapshot,
            PieceState piece,
            GridPosition destination)
        {
            return snapshot.TryGetCell(destination, out CellDefinition cell) &&
                   cell.TerritoryOwner == piece.Owner;
        }

        private readonly struct DirectionStep
        {
            public DirectionStep(MoveDirections direction, int columnOffset, int rowOffset)
            {
                Direction = direction;
                Offset = new GridPosition(columnOffset, rowOffset);
            }

            public MoveDirections Direction { get; }

            public GridPosition Offset { get; }
        }
    }
}
