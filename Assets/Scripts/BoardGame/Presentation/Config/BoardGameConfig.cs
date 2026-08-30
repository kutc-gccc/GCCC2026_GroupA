using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using UnityEngine;

namespace GCCC.BoardGame.Presentation.Config
{
    [CreateAssetMenu(menuName = "GCCC/Board Game Config", fileName = "BoardGameConfig")]
    public sealed class BoardGameConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int columns = 6;
        [SerializeField, Min(3)] private int rows = 10;
        [SerializeField] private PlayerId firstPlayer = PlayerId.Player1;
        [SerializeField] private int player1TerritoryRow;
        [SerializeField] private int player2TerritoryRow = 9;
        [SerializeField] private int player1StartRow = 1;
        [SerializeField] private int player2StartRow = 8;
        [SerializeField, Min(1)] private int initialCombatPower = 1;
        [SerializeField, Min(1)] private int maxPiecesPerPlayer =
            GameDefinition.StandardMaxPiecesPerPlayer;
        [SerializeField, Min(0)] private int reserveDeploymentDepth =
            GameDefinition.StandardReserveDeploymentDepth;
        [SerializeField] private string initialMovementProfileId =
            PowerMovementProfile.StandardIdValue;
        [SerializeField] private List<MovementProfileEntry> movementProfiles =
            CreateDefaultMovementProfiles();
        [SerializeField] private List<CellEffectEntry> cellEffects = new List<CellEffectEntry>();
        [SerializeField] private List<CellEffectConfig> cellEffectDefinitions =
            new List<CellEffectConfig>();

        public GameDefinition CreateDefinition()
        {
            ValidateRows();

            Dictionary<GridPosition, string[]> effectsByPosition =
                (cellEffects ?? new List<CellEffectEntry>())
                .ToDictionary(entry => entry.Position, entry => entry.EffectIds);
            PowerMovementProfile[] coreMovementProfiles = movementProfiles
                .Select(entry => entry.CreateProfile())
                .ToArray();
            MovementProfileId startingProfileId =
                new MovementProfileId(initialMovementProfileId);
            List<CellDefinition> cells = new List<CellDefinition>(columns * rows);
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == player1TerritoryRow
                        ? PlayerId.Player1
                        : row == player2TerritoryRow ? PlayerId.Player2 : (PlayerId?)null;
                    effectsByPosition.TryGetValue(position, out string[] effectIds);
                    cells.Add(new CellDefinition(position, territoryOwner, effectIds));
                }
            }

            List<InitialPieceDefinition> pieces = new List<InitialPieceDefinition>(columns * 2);
            int nextPieceId = 1;
            AddStartingRow(
                pieces,
                PlayerId.Player1,
                player1StartRow,
                startingProfileId,
                ref nextPieceId);
            AddStartingRow(
                pieces,
                PlayerId.Player2,
                player2StartRow,
                startingProfileId,
                ref nextPieceId);
            return new GameDefinition(
                columns,
                rows,
                cells,
                pieces,
                firstPlayer,
                coreMovementProfiles,
                (cellEffectDefinitions ?? new List<CellEffectConfig>())
                .Select(effect => effect.CreateDefinition()),
                maxPiecesPerPlayer,
                reserveDeploymentDepth);
        }

        public IReadOnlyList<ICellEffectHandler> CreateCellEffectHandlers()
        {
            return (cellEffectDefinitions ?? new List<CellEffectConfig>())
                .Select(effect => effect.CreateHandler())
                .ToArray();
        }

        private void AddStartingRow(
            ICollection<InitialPieceDefinition> pieces,
            PlayerId owner,
            int row,
            MovementProfileId movementProfileId,
            ref int nextPieceId)
        {
            for (int column = 0; column < columns; column++)
            {
                pieces.Add(new InitialPieceDefinition(
                    new PieceId(nextPieceId++),
                    owner,
                    new GridPosition(column, row),
                    initialCombatPower,
                    movementProfileId));
            }
        }

        private static List<MovementProfileEntry> CreateDefaultMovementProfiles()
        {
            return new List<MovementProfileEntry>
            {
                new MovementProfileEntry(
                    PowerMovementProfile.StandardIdValue,
                    new List<PowerMovementBandEntry>
                    {
                        new PowerMovementBandEntry(1, 1, MoveDirections.All),
                        new PowerMovementBandEntry(
                            2,
                            2,
                            MoveDirections.All & ~MoveDirections.NorthEast),
                        new PowerMovementBandEntry(
                            3,
                            3,
                            MoveDirections.All & ~MoveDirections.SouthEast),
                        new PowerMovementBandEntry(
                            4,
                            4,
                            MoveDirections.All & ~MoveDirections.NorthWest),
                        new PowerMovementBandEntry(
                            5,
                            5,
                            MoveDirections.All & ~MoveDirections.SouthWest),
                        new PowerMovementBandEntry(
                            6,
                            6,
                            MoveDirections.All & ~MoveDirections.West),
                        new PowerMovementBandEntry(
                            7,
                            7,
                            MoveDirections.All & ~MoveDirections.East),
                        new PowerMovementBandEntry(8, int.MaxValue, MoveDirections.All)
                    })
            };
        }

        private void ValidateRows()
        {
            int[] configuredRows =
            {
                player1TerritoryRow,
                player2TerritoryRow,
                player1StartRow,
                player2StartRow
            };
            if (configuredRows.Any(row => row < 0 || row >= rows) ||
                player1TerritoryRow == player2TerritoryRow ||
                player1StartRow == player1TerritoryRow ||
                player2StartRow == player2TerritoryRow ||
                player1StartRow == player2StartRow)
            {
                throw new InvalidOperationException(
                    "Territory and starting rows in BoardGameConfig are invalid.");
            }
        }

        [Serializable]
        private sealed class MovementProfileEntry
        {
            [SerializeField] private string profileId;
            [SerializeField] private List<PowerMovementBandEntry> bands;

            public MovementProfileEntry(
                string profileId,
                List<PowerMovementBandEntry> bands)
            {
                this.profileId = profileId;
                this.bands = bands;
            }

            public PowerMovementProfile CreateProfile()
            {
                return new PowerMovementProfile(
                    new MovementProfileId(profileId),
                    (bands ?? new List<PowerMovementBandEntry>())
                    .Select(entry => entry.CreateBand()));
            }
        }

        [Serializable]
        private sealed class PowerMovementBandEntry
        {
            [SerializeField, Min(1)] private int minCombatPower;
            [SerializeField, Min(1)] private int maxCombatPower;
            [SerializeField] private MoveDirections directions;

            public PowerMovementBandEntry(
                int minCombatPower,
                int maxCombatPower,
                MoveDirections directions)
            {
                this.minCombatPower = minCombatPower;
                this.maxCombatPower = maxCombatPower;
                this.directions = directions;
            }

            public PowerMovementBand CreateBand()
            {
                return new PowerMovementBand(
                    minCombatPower,
                    maxCombatPower,
                    directions);
            }
        }

        [Serializable]
        private sealed class CellEffectEntry
        {
            [SerializeField] private Vector2Int position;
            [SerializeField] private string[] effectIds = Array.Empty<string>();

            public GridPosition Position => new GridPosition(position.x, position.y);

            public string[] EffectIds => effectIds ?? Array.Empty<string>();
        }
    }
}
