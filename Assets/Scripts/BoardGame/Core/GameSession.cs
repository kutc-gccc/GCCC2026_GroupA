using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using GCCC.BoardGame.Core.Rules.Combat;
using GCCC.BoardGame.Core.Rules.Fusion;
using GCCC.BoardGame.Core.Rules.Movement;
using GCCC.BoardGame.Core.Rules.Turn;

namespace GCCC.BoardGame.Core
{
    public sealed class GameSession
    {
        private readonly GameDefinition definition;
        private readonly IMovementRule movementRule;
        private readonly ICombatResolver combatResolver;
        private readonly IFusionResolver fusionResolver;
        private readonly TurnResolver turnResolver;
        private readonly Dictionary<string, ICellEffectHandler> cellEffectHandlers;
        private readonly Dictionary<Type, IGameCommandHandler> commandHandlers;
        private readonly Dictionary<GridPosition, CellDefinition> cellsByPosition =
            new Dictionary<GridPosition, CellDefinition>();
        private readonly Dictionary<PieceId, PieceState> piecesById =
            new Dictionary<PieceId, PieceState>();
        private readonly Dictionary<GridPosition, PieceId> pieceIdsByPosition =
            new Dictionary<GridPosition, PieceId>();

        private PlayerId currentPlayer;
        private PlayerId? winner;
        private bool isDraw;

        public GameSession(
            GameDefinition definition,
            IMovementRule movementRule = null,
            ICombatResolver combatResolver = null,
            IFusionResolver fusionResolver = null,
            IEnumerable<ICellEffectHandler> cellEffectHandlers = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.movementRule = movementRule ?? new DirectionalMovementRule(
                new ProfileMoveDirectionResolver(definition.MovementProfiles));
            this.combatResolver = combatResolver ?? new SimultaneousCombatResolver();
            this.fusionResolver = fusionResolver ?? new DisabledFusionResolver();
            turnResolver = new TurnResolver();
            this.cellEffectHandlers = (cellEffectHandlers ?? Array.Empty<ICellEffectHandler>())
                .ToDictionary(handler => handler.EffectId, StringComparer.Ordinal);

            commandHandlers = new IGameCommandHandler[]
            {
                new MovePieceCommandHandler(),
                new FusePiecesCommandHandler(),
                new RandomizePowerCommandHandler()
            }.ToDictionary(handler => handler.CommandType);

            ValidateAndLoadCells();
            Reset();
        }

        public GameSnapshot Snapshot => new GameSnapshot(
            definition.Columns,
            definition.Rows,
            piecesById.Values,
            cellsByPosition.Values,
            currentPlayer,
            winner,
            isDraw);

        public CommandResult Execute(GameCommand command)
        {
            if (command == null)
            {
                return CommandResult.Failed(CommandFailureReason.InvalidCommand);
            }

            if (winner.HasValue || isDraw)
            {
                return CommandResult.Failed(CommandFailureReason.GameOver);
            }

            if (command.Player != currentPlayer)
            {
                return CommandResult.Failed(CommandFailureReason.NotPlayersTurn);
            }

            return commandHandlers.TryGetValue(command.GetType(), out IGameCommandHandler handler)
                ? handler.Execute(this, command)
                : CommandResult.Failed(CommandFailureReason.InvalidCommand);
        }

        public IReadOnlyList<GameCommand> GetLegalCommands(PlayerId player)
        {
            if (winner.HasValue || isDraw || player != currentPlayer)
            {
                return Array.Empty<GameCommand>();
            }

            GameSnapshot snapshot = Snapshot;
            List<GameCommand> commands = new List<GameCommand>();
            foreach (PieceState piece in snapshot.Pieces)
            {
                if (piece.Owner != player)
                {
                    continue;
                }

                // 移動コマンドの追加
                foreach (GridPosition destination in
                         movementRule.GetLegalDestinations(snapshot, piece))
                {
                    commands.Add(new MovePieceCommand(player, piece.Id, destination));
                }

                // 戦闘力ランダムコマンドの追加
                commands.Add(new RandomizePowerCommand(player, piece.Id));
            }

            if (fusionResolver.IsEnabled)
            {
                foreach (FusionPair pair in fusionResolver.GetLegalFusions(snapshot, player))
                {
                    commands.Add(new FusePiecesCommand(
                        player, pair.FirstPieceId, pair.SecondPieceId));
                }
            }

            return commands;
        }

        public void Reset()
        {
            piecesById.Clear();
            pieceIdsByPosition.Clear();
            currentPlayer = definition.FirstPlayer;
            winner = null;
            isDraw = false;

            foreach (InitialPieceDefinition initialPiece in definition.InitialPieces)
            {
                PieceState piece = initialPiece.CreateState();
                ValidateInitialPiece(piece);
                AddPiece(piece);

                if (IsOpponentTerritory(piece.Owner, piece.Position))
                {
                    winner = piece.Owner;
                }
            }

            if (!winner.HasValue)
            {
                TurnResolution initialTurn = turnResolver.ResolveInitialTurn(
                    definition.FirstPlayer, HasAnyLegalAction);
                currentPlayer = initialTurn.CurrentPlayer;
                isDraw = initialTurn.IsDraw;
            }
        }

        internal CommandResult ExecuteMove(MovePieceCommand command)
        {
            if (!piecesById.TryGetValue(command.PieceId, out PieceState attacker))
            {
                return CommandResult.Failed(CommandFailureReason.PieceNotFound);
            }

            if (attacker.Owner != command.Player)
            {
                return CommandResult.Failed(CommandFailureReason.NotPieceOwner);
            }

            GameSnapshot beforeMove = Snapshot;
            if (!movementRule.GetLegalDestinations(beforeMove, attacker)
                .Contains(command.Destination))
            {
                return CommandResult.Failed(CommandFailureReason.IllegalMove);
            }

            List<GameEvent> events = new List<GameEvent>();
            PieceState occupyingPiece = TryGetPiece(command.Destination);
            PieceState survivingAttacker = occupyingPiece == null
                ? ResolveUnoccupiedMove(attacker, command.Destination, events)
                : ResolveCombatMove(attacker, occupyingPiece, command.Destination, events);

            if (survivingAttacker != null)
            {
                survivingAttacker = ApplyCellEffects(survivingAttacker, events);
                if (IsOpponentTerritory(survivingAttacker.Owner, survivingAttacker.Position))
                {
                    winner = survivingAttacker.Owner;
                    events.Add(new GameEnded(winner, false));
                    return CommandResult.Succeeded(events);
                }
            }

            ResolveNextTurn(command.Player, events);
            return CommandResult.Succeeded(events);
        }

        internal CommandResult ExecuteFusion(FusePiecesCommand command)
        {
            if (!fusionResolver.IsEnabled)
            {
                return CommandResult.Failed(CommandFailureReason.FusionDisabled);
            }

            if (!piecesById.TryGetValue(command.FirstPieceId, out PieceState first) ||
                !piecesById.TryGetValue(command.SecondPieceId, out PieceState second))
            {
                return CommandResult.Failed(CommandFailureReason.PieceNotFound);
            }

            if (first.Owner != command.Player || second.Owner != command.Player)
            {
                return CommandResult.Failed(CommandFailureReason.NotPieceOwner);
            }

            if (!fusionResolver.TryResolve(first, second, out FusionResolution resolution))
            {
                return CommandResult.Failed(CommandFailureReason.IllegalMove);
            }

            if (resolution?.ResultingPiece == null ||
                !definition.TryGetMovementProfile(
                    resolution.ResultingPiece.MovementProfileId,
                    out _))
            {
                throw new InvalidOperationException(
                    "Fusion must return a piece with a registered movement profile.");
            }

            RemovePiece(first.Id);
            RemovePiece(second.Id);
            AddPiece(resolution.ResultingPiece);

            List<GameEvent> events = new List<GameEvent>
            {
                new PiecesFused(first.Id, second.Id, resolution.ResultingPiece.Id)
            };
            ResolveNextTurn(command.Player, events);
            return CommandResult.Succeeded(events);
        }

        internal CommandResult ExecuteRandomizePower(RandomizePowerCommand command)
        {
            if (!piecesById.TryGetValue(command.PieceId, out PieceState piece))
            {
                return CommandResult.Failed(CommandFailureReason.PieceNotFound);
            }

            if (piece.Owner != command.Player)
            {
                return CommandResult.Failed(CommandFailureReason.NotPieceOwner);
            }

            int previousPower = piece.CombatPower;

            // System.Random を使用して1〜3の値をランダム取得（第2引数は「未満」）
            Random random = new Random();
            int newPower = random.Next(1, 4);

            PieceState updatedPiece = piece.WithCombatPower(newPower);
            piecesById[piece.Id] = updatedPiece;

            List<GameEvent> events = new List<GameEvent>
            {
                new RandomizePowerEvent(piece.Id, previousPower, newPower),
                new PiecePowerChanged(piece.Id, previousPower, newPower)
            };

            ResolveNextTurn(command.Player, events);
            return CommandResult.Succeeded(events);
        }

        private PieceState ResolveUnoccupiedMove(
            PieceState piece,
            GridPosition destination,
            ICollection<GameEvent> events)
        {
            GridPosition from = piece.Position;
            PieceState movedPiece = piece.WithPosition(destination);
            RemovePiece(piece.Id);
            AddPiece(movedPiece);
            events.Add(new PieceMoved(piece.Id, from, destination));
            return movedPiece;
        }

        private PieceState ResolveCombatMove(
            PieceState attacker,
            PieceState defender,
            GridPosition destination,
            ICollection<GameEvent> events)
        {
            CombatResolution combat = combatResolver.Resolve(attacker, defender);
            events.Add(new CombatResolved(
                attacker.Id,
                defender.Id,
                attacker.CombatPower,
                defender.CombatPower,
                combat.AttackerRemainingPower,
                combat.DefenderRemainingPower));

            RemovePiece(attacker.Id);

            if (combat.AttackerRemainingPower > 0)
            {
                RemovePiece(defender.Id);
                events.Add(new PieceDestroyed(defender.Id, defender.Position));

                PieceState survivingAttacker = new PieceState(
                    attacker.Id,
                    attacker.Owner,
                    destination,
                    combat.AttackerRemainingPower,
                    attacker.MovementProfileId);
                AddPiece(survivingAttacker);
                events.Add(new PiecePowerChanged(
                    attacker.Id, attacker.CombatPower, combat.AttackerRemainingPower));
                events.Add(new PieceMoved(attacker.Id, attacker.Position, destination));
                return survivingAttacker;
            }

            events.Add(new PieceDestroyed(attacker.Id, attacker.Position));
            if (combat.DefenderRemainingPower > 0)
            {
                PieceState survivingDefender = defender.WithCombatPower(
                    combat.DefenderRemainingPower);
                piecesById[defender.Id] = survivingDefender;
                events.Add(new PiecePowerChanged(
                    defender.Id, defender.CombatPower, combat.DefenderRemainingPower));
                return null;
            }

            RemovePiece(defender.Id);
            events.Add(new PieceDestroyed(defender.Id, defender.Position));
            return null;
        }

        private PieceState ApplyCellEffects(
            PieceState piece,
            ICollection<GameEvent> events)
        {
            if (!cellsByPosition.TryGetValue(piece.Position, out CellDefinition cell))
            {
                return piece;
            }

            PieceState currentPiece = piece;
            foreach (string effectId in cell.EffectIds)
            {
                if (!cellEffectHandlers.TryGetValue(effectId, out ICellEffectHandler handler))
                {
                    throw new InvalidOperationException($"No cell effect handler is registered for '{effectId}'.");
                }

                CellEffectResult result = handler.Apply(
                    new CellEffectContext(Snapshot, currentPiece, cell));
                ValidateCellEffectResult(currentPiece, result.Piece);

                events.Add(new CellEffectTriggered(effectId, currentPiece.Id, cell.Position));
                if (currentPiece.CombatPower != result.Piece.CombatPower)
                {
                    events.Add(new PiecePowerChanged(
                        currentPiece.Id,
                        currentPiece.CombatPower,
                        result.Piece.CombatPower));
                }

                foreach (GameEvent additionalEvent in result.Events)
                {
                    events.Add(additionalEvent);
                }

                currentPiece = result.Piece;
                piecesById[currentPiece.Id] = currentPiece;
            }

            return currentPiece;
        }

        private void ResolveNextTurn(PlayerId playerWhoActed, ICollection<GameEvent> events)
        {
            TurnResolution turn = turnResolver.ResolveAfterAction(
                playerWhoActed, HasAnyLegalAction);
            if (turn.IsDraw)
            {
                isDraw = true;
                events.Add(new GameEnded(null, true));
                return;
            }

            currentPlayer = turn.CurrentPlayer;
            events.Add(new TurnChanged(playerWhoActed, currentPlayer, turn.TurnWasPassed));
        }

        private bool HasAnyLegalAction(PlayerId player)
        {
            GameSnapshot snapshot = Snapshot;
            foreach (PieceState piece in snapshot.Pieces)
            {
                if (piece.Owner == player)
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidateAndLoadCells()
        {
            foreach (CellDefinition cell in definition.Cells)
            {
                if (!IsInside(cell.Position) || !cellsByPosition.TryAdd(cell.Position, cell))
                {
                    throw new ArgumentException("The game definition contains an invalid cell.");
                }
            }

            if (cellsByPosition.Count != definition.Columns * definition.Rows)
            {
                throw new ArgumentException("The game definition must define every board cell.");
            }
        }

        private void ValidateInitialPiece(PieceState piece)
        {
            if (!IsInside(piece.Position) ||
                IsOwnTerritory(piece.Owner, piece.Position) ||
                piecesById.ContainsKey(piece.Id) ||
                pieceIdsByPosition.ContainsKey(piece.Position))
            {
                throw new ArgumentException("The game definition contains an invalid initial piece.");
            }
        }

        private void ValidateCellEffectResult(PieceState before, PieceState after)
        {
            if (after == null || after.Id != before.Id || after.Owner != before.Owner ||
                after.Position != before.Position ||
                !definition.TryGetMovementProfile(after.MovementProfileId, out _))
            {
                throw new InvalidOperationException(
                    "Cell effects may only change a piece's combat power or registered movement profile.");
            }
        }

        private bool IsInside(GridPosition position)
        {
            return position.Column >= 0 && position.Column < definition.Columns &&
                   position.Row >= 0 && position.Row < definition.Rows;
        }

        private bool IsOwnTerritory(PlayerId player, GridPosition position)
        {
            return cellsByPosition.TryGetValue(position, out CellDefinition cell) &&
                   cell.TerritoryOwner == player;
        }

        private bool IsOpponentTerritory(PlayerId player, GridPosition position)
        {
            return cellsByPosition.TryGetValue(position, out CellDefinition cell) &&
                   cell.TerritoryOwner.HasValue && cell.TerritoryOwner.Value != player;
        }

        private PieceState TryGetPiece(GridPosition position)
        {
            return pieceIdsByPosition.TryGetValue(position, out PieceId id)
                ? piecesById[id]
                : null;
        }

        private void AddPiece(PieceState piece)
        {
            if (!definition.TryGetMovementProfile(piece.MovementProfileId, out _))
            {
                throw new InvalidOperationException(
                    $"Movement profile '{piece.MovementProfileId}' is not registered.");
            }

            piecesById.Add(piece.Id, piece);
            pieceIdsByPosition.Add(piece.Position, piece.Id);
        }

        private void RemovePiece(PieceId id)
        {
            if (!piecesById.TryGetValue(id, out PieceState piece))
            {
                return;
            }

            piecesById.Remove(id);
            pieceIdsByPosition.Remove(piece.Position);
        }
    }

    // --- コマンドハンドラーのインターフェース・実装クラス群 ---

    public interface IGameCommandHandler
    {
        Type CommandType { get; }
        CommandResult Execute(GameSession session, GameCommand command);
    }

    internal sealed class MovePieceCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(MovePieceCommand);
        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteMove((MovePieceCommand)command);
        }
    }

    internal sealed class FusePiecesCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(FusePiecesCommand);
        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteFusion((FusePiecesCommand)command);
        }
    }

    internal sealed class RandomizePowerCommandHandler : IGameCommandHandler
    {
        public Type CommandType => typeof(RandomizePowerCommand);
        public CommandResult Execute(GameSession session, GameCommand command)
        {
            return session.ExecuteRandomizePower((RandomizePowerCommand)command);
        }
    }
}