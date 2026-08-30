using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GCCC.BoardGame.Core.Commands;

namespace GCCC.BoardGame.Core.Model
{
    public sealed class GameSnapshot
    {
        private readonly IReadOnlyDictionary<PieceId, PieceState> piecesById;
        private readonly IReadOnlyDictionary<GridPosition, PieceState> piecesByPosition;
        private readonly IReadOnlyDictionary<GridPosition, CellDefinition> cellsByPosition;
        private readonly IReadOnlyDictionary<string, CellEffectDefinition>
            effectDefinitionsById;
        private readonly IReadOnlyDictionary<PlayerId, PlayerState> playersById;

        public GameSnapshot(
            int columns,
            int rows,
            IEnumerable<PieceState> pieces,
            IEnumerable<CellDefinition> cells,
            PlayerId currentPlayer,
            PlayerId? winner,
            bool isDraw,
            IReadOnlyList<GameCommand> legalCommands = null,
            IEnumerable<CellEffectDefinition> effectDefinitions = null,
            IEnumerable<PlayerState> players = null,
            int maxPiecesPerPlayer = GameDefinition.StandardMaxPiecesPerPlayer,
            int reserveDeploymentDepth = GameDefinition.StandardReserveDeploymentDepth)
        {
            Columns = columns;
            Rows = rows;
            CurrentPlayer = currentPlayer;
            Winner = winner;
            IsDraw = isDraw;
            MaxPiecesPerPlayer = maxPiecesPerPlayer;
            ReserveDeploymentDepth = reserveDeploymentDepth;
            LegalCommands = new ReadOnlyCollection<GameCommand>(
                (legalCommands ?? Array.Empty<GameCommand>()).ToArray());

            PieceState[] pieceCopies = pieces
                .Select(CopyPiece)
                .ToArray();
            Pieces = new ReadOnlyCollection<PieceState>(pieceCopies);
            piecesById = new ReadOnlyDictionary<PieceId, PieceState>(
                pieceCopies.ToDictionary(piece => piece.Id));
            piecesByPosition = new ReadOnlyDictionary<GridPosition, PieceState>(
                pieceCopies.ToDictionary(piece => piece.Position));

            CellDefinition[] cellCopies = cells
                .Select(cell => new CellDefinition(
                    cell.Position, cell.TerritoryOwner, cell.EffectIds))
                .ToArray();
            Cells = new ReadOnlyCollection<CellDefinition>(cellCopies);
            cellsByPosition = new ReadOnlyDictionary<GridPosition, CellDefinition>(
                cellCopies.ToDictionary(cell => cell.Position));

            CellEffectDefinition[] effectCopies = (effectDefinitions ??
                    Array.Empty<CellEffectDefinition>())
                .Select(effect => new CellEffectDefinition(
                    effect.EffectId, effect.Lifetime))
                .ToArray();
            CellEffectDefinitions =
                new ReadOnlyCollection<CellEffectDefinition>(effectCopies);
            effectDefinitionsById =
                new ReadOnlyDictionary<string, CellEffectDefinition>(
                    effectCopies.ToDictionary(
                        effect => effect.EffectId,
                        StringComparer.Ordinal));

            PlayerState[] playerCopies = (players ??
                    new[]
                    {
                        new PlayerState(PlayerId.Player1),
                        new PlayerState(PlayerId.Player2)
                    })
                .Select(player => new PlayerState(player.Player, player.ReservePieces))
                .ToArray();
            Players = new ReadOnlyCollection<PlayerState>(playerCopies);
            playersById = new ReadOnlyDictionary<PlayerId, PlayerState>(
                playerCopies.ToDictionary(player => player.Player));
        }

        public int Columns { get; }

        public int Rows { get; }

        public IReadOnlyList<PieceState> Pieces { get; }

        public IReadOnlyList<CellDefinition> Cells { get; }

        public IReadOnlyList<CellEffectDefinition> CellEffectDefinitions { get; }

        public IReadOnlyList<PlayerState> Players { get; }

        public PlayerId CurrentPlayer { get; }

        public int MaxPiecesPerPlayer { get; }

        public int ReserveDeploymentDepth { get; }

        public PlayerId? Winner { get; }

        public bool IsDraw { get; }

        public IReadOnlyList<GameCommand> LegalCommands { get; }

        public bool IsGameOver => Winner.HasValue || IsDraw;

        public bool IsInside(GridPosition position)
        {
            return position.Column >= 0 && position.Column < Columns &&
                   position.Row >= 0 && position.Row < Rows;
        }

        public bool TryGetPiece(PieceId id, out PieceState piece)
        {
            return piecesById.TryGetValue(id, out piece);
        }

        public bool TryGetPiece(GridPosition position, out PieceState piece)
        {
            return piecesByPosition.TryGetValue(position, out piece);
        }

        public bool TryGetCell(GridPosition position, out CellDefinition cell)
        {
            return cellsByPosition.TryGetValue(position, out cell);
        }

        public bool TryGetCellEffectDefinition(
            string effectId,
            out CellEffectDefinition definition)
        {
            return effectDefinitionsById.TryGetValue(effectId, out definition);
        }

        public PlayerState GetPlayer(PlayerId player)
        {
            return playersById[player];
        }

        public int GetPieceCount(PlayerId player)
        {
            return Pieces.Count(piece => piece.Owner == player);
        }

        public int GetOwnedPieceCount(PlayerId player)
        {
            return GetPieceCount(player) + GetPlayer(player).ReservePieces.Count;
        }

        public GameSnapshot WithLegalCommands(IReadOnlyList<GameCommand> legalCommands)
        {
            return new GameSnapshot(
                Columns,
                Rows,
                Pieces,
                Cells,
                CurrentPlayer,
                Winner,
                IsDraw,
                legalCommands,
                CellEffectDefinitions,
                Players,
                MaxPiecesPerPlayer,
                ReserveDeploymentDepth);
        }

        private static PieceState CopyPiece(PieceState piece)
        {
            return new PieceState(
                piece.Id,
                piece.Owner,
                piece.Position,
                piece.CombatPower,
                piece.MovementProfileId,
                piece.AppliedPermanentEffectIds,
                piece.ActiveCellEffects);
        }
    }
}
