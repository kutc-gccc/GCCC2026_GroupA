using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Tests
{
    internal static class GameSessionTestBuilder
    {
        internal static GameSession CreateSession(
            PlayerId firstPlayer,
            params InitialPieceDefinition[] pieces)
        {
            return new GameSession(CreateDefinition(firstPlayer, null, pieces));
        }

        internal static GameDefinition CreateDefinition(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            return CreateDefinitionWithProfiles(
                firstPlayer,
                new[] { PowerMovementProfile.CreateStandard() },
                cellEffects,
                pieces);
        }

        internal static GameDefinition CreateDefinitionWithProfiles(
            PlayerId firstPlayer,
            IEnumerable<PowerMovementProfile> movementProfiles,
            IDictionary<GridPosition, string[]> cellEffects,
            params InitialPieceDefinition[] pieces)
        {
            List<CellDefinition> cells = CreateCells(cellEffects);
            CellEffectDefinition[] effectDefinitions = (cellEffects ??
                    new Dictionary<GridPosition, string[]>())
                .Values
                .SelectMany(effectIds => effectIds ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .Select(effectId => new CellEffectDefinition(
                    effectId, CellEffectLifetime.PermanentOncePerPiece))
                .ToArray();

            return new GameDefinition(
                6,
                10,
                cells,
                pieces,
                firstPlayer,
                movementProfiles,
                effectDefinitions);
        }

        internal static GameDefinition CreateDefinitionWithEffects(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            params InitialPieceDefinition[] pieces)
        {
            return CreateDefinitionWithEffectsAndLimits(
                firstPlayer,
                cellEffects,
                effectDefinitions,
                GameDefinition.StandardMaxPiecesPerPlayer,
                GameDefinition.StandardReserveDeploymentDepth,
                pieces);
        }

        internal static GameDefinition CreateDefinitionWithEffectsAndLimits(
            PlayerId firstPlayer,
            IDictionary<GridPosition, string[]> cellEffects,
            IEnumerable<CellEffectDefinition> effectDefinitions,
            int maxPiecesPerPlayer,
            int reserveDeploymentDepth,
            params InitialPieceDefinition[] pieces)
        {
            return new GameDefinition(
                6,
                10,
                CreateCells(cellEffects),
                pieces,
                firstPlayer,
                new[] { PowerMovementProfile.CreateStandard() },
                effectDefinitions,
                maxPiecesPerPlayer,
                reserveDeploymentDepth);
        }

        internal static InitialPieceDefinition InitialPiece(
            int id,
            int column,
            int row,
            PlayerId owner,
            int power = 1,
            string movementProfileId = PowerMovementProfile.StandardIdValue)
        {
            return new InitialPieceDefinition(
                new PieceId(id),
                owner,
                new GridPosition(column, row),
                power,
                new MovementProfileId(movementProfileId));
        }

        private static List<CellDefinition> CreateCells(
            IDictionary<GridPosition, string[]> cellEffects)
        {
            List<CellDefinition> cells = new List<CellDefinition>(60);
            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    PlayerId? territoryOwner = row == 0
                        ? PlayerId.Player1
                        : row == 9 ? PlayerId.Player2 : (PlayerId?)null;
                    string[] effects = null;
                    cellEffects?.TryGetValue(position, out effects);
                    cells.Add(new CellDefinition(position, territoryOwner, effects));
                }
            }

            return cells;
        }
    }
}
