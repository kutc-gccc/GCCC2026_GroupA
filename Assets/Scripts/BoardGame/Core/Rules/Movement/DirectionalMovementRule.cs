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

        /// <summary>
        /// 各方向の正反対。<see cref="Steps"/>の移動量から作るので、方向の定義を足しても
        /// 書き写す手間がなく、対応表だけが古くなることもない。
        /// </summary>
        private static readonly IReadOnlyDictionary<MoveDirections, MoveDirections>
            Opposites = CreateOpposites();

        /// <summary>
        /// プレイヤーごとの前進する行方向（+1か-1）。盤の陣地の位置から求める。
        /// 求め直すのは盤が変わったときだけでよいので、Cellsの参照が同じ間は使い回す。
        /// </summary>
        private readonly Dictionary<PlayerId, int> forwardRowSteps =
            new Dictionary<PlayerId, int>();

        private object cachedCells;

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

            MoveDirections effectiveDirections = OrientToOwner(
                snapshot, piece.Owner, directionResolver.Resolve(piece));
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

        /// <summary>
        /// 移動プロファイルの方向を、その駒の持ち主から見た向きへ直す。
        /// </summary>
        /// <remarks>
        /// プロファイルは行が増える向きへ攻めるプレイヤーの視点で書かれている。反対側から
        /// 攻めるプレイヤーは盤を180°回した位置から見ているので、方向も180°回して当てる。
        /// 回さないと、たとえば「前方の右斜めを失う」制限が、相手にとっては「後方の左斜めを
        /// 失う」制限になり、同じ戦闘力でも動ける形が左右逆になってしまう。
        /// </remarks>
        private MoveDirections OrientToOwner(
            GameSnapshot snapshot,
            PlayerId owner,
            MoveDirections directions)
        {
            if (GetForwardRowStep(snapshot, owner) >= 0)
            {
                return directions;
            }

            MoveDirections turned = MoveDirections.None;
            foreach (KeyValuePair<MoveDirections, MoveDirections> opposite in Opposites)
            {
                if ((directions & opposite.Key) != 0)
                {
                    turned |= opposite.Value;
                }
            }

            return turned;
        }

        /// <summary>
        /// そのプレイヤーが前進する行方向を返す。陣地が読み取れない盤では、
        /// プロファイルどおりに扱えるよう+1（回さない）を返す。
        /// </summary>
        private int GetForwardRowStep(GameSnapshot snapshot, PlayerId owner)
        {
            if (!ReferenceEquals(cachedCells, snapshot.Cells))
            {
                BuildForwardRowSteps(snapshot);
                cachedCells = snapshot.Cells;
            }

            return forwardRowSteps.TryGetValue(owner, out int step) ? step : 1;
        }

        private void BuildForwardRowSteps(GameSnapshot snapshot)
        {
            forwardRowSteps.Clear();

            Dictionary<PlayerId, int> territoryRows = new Dictionary<PlayerId, int>();
            foreach (CellDefinition cell in snapshot.Cells)
            {
                if (cell.TerritoryOwner.HasValue)
                {
                    territoryRows[cell.TerritoryOwner.Value] = cell.Position.Row;
                }
            }

            foreach (KeyValuePair<PlayerId, int> own in territoryRows)
            {
                foreach (KeyValuePair<PlayerId, int> other in territoryRows)
                {
                    if (other.Key == own.Key)
                    {
                        continue;
                    }

                    // 自陣から相手陣へ向かう向きが、そのプレイヤーの「前」。
                    int step = Math.Sign(other.Value - own.Value);
                    if (step != 0)
                    {
                        forwardRowSteps[own.Key] = step;
                    }
                }
            }
        }

        private static IReadOnlyDictionary<MoveDirections, MoveDirections> CreateOpposites()
        {
            Dictionary<MoveDirections, MoveDirections> opposites =
                new Dictionary<MoveDirections, MoveDirections>(Steps.Length);
            foreach (DirectionStep step in Steps)
            {
                foreach (DirectionStep candidate in Steps)
                {
                    if (candidate.Offset.Column == -step.Offset.Column &&
                        candidate.Offset.Row == -step.Offset.Row)
                    {
                        opposites[step.Direction] = candidate.Direction;
                        break;
                    }
                }
            }

            return opposites;
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
