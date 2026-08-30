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
using GCCC.BoardGame.Core.Rules.Random;
using GCCC.BoardGame.Core.Rules.Turn;

namespace GCCC.BoardGame.Core
{
    public sealed class GameSession
    {
        private readonly GameDefinition definition;
        private readonly IMovementRule movementRule;
        private readonly ICombatResolver combatResolver;
        private readonly IFusionResolver fusionResolver;
        private readonly IRandomSource randomSource;
        private readonly TurnResolver turnResolver;
        private readonly Dictionary<string, ICellEffectHandler> cellEffectHandlers;
        private readonly Dictionary<Type, IGameCommandHandler> commandHandlers;
        private readonly Dictionary<GridPosition, CellDefinition> cellsByPosition =
            new Dictionary<GridPosition, CellDefinition>();
        private readonly Dictionary<PieceId, PieceState> piecesById =
            new Dictionary<PieceId, PieceState>();
        private readonly Dictionary<GridPosition, PieceId> pieceIdsByPosition =
            new Dictionary<GridPosition, PieceId>();
        private readonly Dictionary<PlayerId, List<ReservePieceState>> reservesByPlayer =
            new Dictionary<PlayerId, List<ReservePieceState>>();

        private PlayerId currentPlayer;
        private PlayerId? winner;
        private bool isDraw;
        private int nextPieceId;

        public GameSession(
            GameDefinition definition,
            IMovementRule movementRule = null,
            ICombatResolver combatResolver = null,
            IFusionResolver fusionResolver = null,
            IEnumerable<ICellEffectHandler> cellEffectHandlers = null,
            IRandomSource randomSource = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.randomSource = randomSource ?? new SystemRandomSource();
            this.movementRule = movementRule ?? new DirectionalMovementRule(
                new ProfileMoveDirectionResolver(definition.MovementProfiles));
            this.combatResolver = combatResolver ?? new SimultaneousCombatResolver();
            this.fusionResolver = fusionResolver ??
                new AdjacentFusionResolver(this.randomSource);
            turnResolver = new TurnResolver();
            this.cellEffectHandlers = (cellEffectHandlers ??
                    Array.Empty<ICellEffectHandler>())
                .ToDictionary(handler => handler.EffectId, StringComparer.Ordinal);

            commandHandlers = new IGameCommandHandler[]
            {
                new MovePieceCommandHandler(),
                new FusePiecesCommandHandler(),
                new RandomizePowerCommandHandler(),
                new DeployReservePieceCommandHandler()
            }.ToDictionary(handler => handler.CommandType);

            ValidateAndLoadCells();
            ValidateCellEffectHandlers();
            Reset();
        }

        public GameSnapshot Snapshot => new GameSnapshot(
            definition.Columns,
            definition.Rows,
            piecesById.Values,
            cellsByPosition.Values,
            currentPlayer,
            winner,
            isDraw,
            effectDefinitions: definition.CellEffectDefinitions,
            players: CreatePlayerStates(),
            maxPiecesPerPlayer: definition.MaxPiecesPerPlayer,
            reserveDeploymentDepth: definition.ReserveDeploymentDepth);

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
            return winner.HasValue || isDraw || player != currentPlayer
                ? Array.Empty<GameCommand>()
                : BuildLegalCommands(player);
        }

        public void Reset()
        {
            piecesById.Clear();
            pieceIdsByPosition.Clear();
            reservesByPlayer.Clear();
            reservesByPlayer[PlayerId.Player1] = new List<ReservePieceState>();
            reservesByPlayer[PlayerId.Player2] = new List<ReservePieceState>();
            currentPlayer = definition.FirstPlayer;
            winner = null;
            isDraw = false;
            nextPieceId = definition.InitialPieces.Count == 0
                ? 1
                : definition.InitialPieces.Max(piece => piece.Id.Value) + 1;

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

            if (!movementRule.GetLegalDestinations(Snapshot, attacker)
                .Contains(command.Destination))
            {
                return CommandResult.Failed(CommandFailureReason.IllegalMove);
            }

            List<GameEvent> events = new List<GameEvent>();
            attacker = ExpireActiveEffects(attacker, events);
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

            List<GameEvent> events = new List<GameEvent>();
            if (!resolution.IsSuccessful)
            {
                events.Add(new FusionAttemptFailed(first.Id, second.Id));
                ResolveNextTurn(command.Player, events);
                return CommandResult.Succeeded(events);
            }

            if (resolution.ResultingPiece == null ||
                !definition.TryGetMovementProfile(
                    resolution.ResultingPiece.MovementProfileId, out _))
            {
                throw new InvalidOperationException(
                    "Fusion must return a piece with a registered movement profile.");
            }

            RemovePiece(first.Id);
            RemovePiece(second.Id);
            AddPiece(resolution.ResultingPiece);

            events.Add(new PiecesFused(
                first.Id, second.Id, resolution.ResultingPiece.Id, resolution.Bonus));
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

            if (!CanRandomizePower(piece))
            {
                return CommandResult.Failed(CommandFailureReason.IllegalMove);
            }

            int previousPower = piece.EffectiveCombatPower;
            int newPower = randomSource.NextInt(1, 4);
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

        internal CommandResult ExecuteDeployReservePiece(
            DeployReservePieceCommand command)
        {
            ReservePieceState reservePiece = reservesByPlayer[command.Player]
                .FirstOrDefault(piece => piece.Id == command.ReservePieceId);
            if (reservePiece == null)
            {
                bool belongsToOpponent = reservesByPlayer
                    .Where(pair => pair.Key != command.Player)
                    .SelectMany(pair => pair.Value)
                    .Any(piece => piece.Id == command.ReservePieceId);
                return CommandResult.Failed(belongsToOpponent
                    ? CommandFailureReason.NotPieceOwner
                    : CommandFailureReason.ReservePieceNotFound);
            }

            if (GetBoardPieceCount(command.Player) >= definition.MaxPiecesPerPlayer)
            {
                return CommandResult.Failed(CommandFailureReason.PieceLimitReached);
            }

            if (!GetLegalReserveDeploymentPositions(command.Player)
                .Contains(command.Destination))
            {
                return CommandResult.Failed(
                    CommandFailureReason.InvalidDeploymentPosition);
            }

            reservesByPlayer[command.Player].Remove(reservePiece);
            PieceState deployedPiece = new PieceState(
                reservePiece.Id,
                reservePiece.Owner,
                command.Destination,
                reservePiece.CombatPower,
                reservePiece.MovementProfileId);
            AddPiece(deployedPiece);

            List<GameEvent> events = new List<GameEvent>
            {
                new ReservePieceDeployed(
                    deployedPiece.Id,
                    deployedPiece.Owner,
                    deployedPiece.Position)
            };
            ApplyCellEffects(deployedPiece, events);
            ResolveNextTurn(command.Player, events);
            return CommandResult.Succeeded(events);
        }

        private IReadOnlyList<GameCommand> BuildLegalCommands(PlayerId player)
        {
            GameSnapshot snapshot = Snapshot;
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

                if (CanRandomizePower(piece))
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

            if (GetBoardPieceCount(player) < definition.MaxPiecesPerPlayer)
            {
                GridPosition[] deploymentPositions =
                    GetLegalReserveDeploymentPositions(player).ToArray();
                foreach (ReservePieceState reservePiece in reservesByPlayer[player])
                {
                    foreach (GridPosition destination in deploymentPositions)
                    {
                        commands.Add(new DeployReservePieceCommand(
                            player, reservePiece.Id, destination));
                    }
                }
            }

            return commands;
        }

        private bool CanRandomizePower(PieceState piece)
        {
            IEnumerable<string> effectIds = piece.AppliedPermanentEffectIds
                .Concat(piece.ActiveCellEffects.Select(effect => effect.EffectId));
            foreach (string effectId in effectIds)
            {
                if (cellEffectHandlers.TryGetValue(
                    effectId, out ICellEffectHandler handler) &&
                    handler.BlocksPowerRandomization)
                {
                    return false;
                }
            }

            return true;
        }

        private PieceState ExpireActiveEffects(
            PieceState piece,
            ICollection<GameEvent> events)
        {
            if (piece.ActiveCellEffects.Count == 0)
            {
                return piece;
            }

            int previousPower = piece.EffectiveCombatPower;
            foreach (ActiveCellEffectState effect in piece.ActiveCellEffects)
            {
                events.Add(new CellEffectExpired(
                    effect.EffectId, piece.Id, piece.Position));
            }

            PieceState updated = piece.WithoutActiveEffects();
            piecesById[piece.Id] = updated;
            if (previousPower != updated.EffectiveCombatPower)
            {
                events.Add(new PiecePowerChanged(
                    piece.Id, previousPower, updated.EffectiveCombatPower));
            }

            return updated;
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
            int attackerPowerBefore = attacker.EffectiveCombatPower;
            int defenderPowerBefore = defender.EffectiveCombatPower;
            CombatResolution combat = combatResolver.Resolve(attacker, defender);
            if (combat.DamageToAttacker < 0 || combat.DamageToDefender < 0)
            {
                throw new InvalidOperationException(
                    "Combat damage must not be negative.");
            }

            PieceState survivingAttacker =
                attacker.ApplyDamage(combat.DamageToAttacker);
            PieceState survivingDefender =
                defender.ApplyDamage(combat.DamageToDefender);
            if (survivingAttacker != null && survivingDefender != null)
            {
                throw new InvalidOperationException(
                    "Combat cannot leave two pieces on the same cell.");
            }

            events.Add(new CombatResolved(
                attacker.Id,
                defender.Id,
                attackerPowerBefore,
                defenderPowerBefore,
                survivingAttacker?.EffectiveCombatPower ?? 0,
                survivingDefender?.EffectiveCombatPower ?? 0));

            RemovePiece(attacker.Id);
            if (survivingAttacker != null)
            {
                RemovePiece(defender.Id);
                events.Add(new PieceDestroyed(defender.Id, defender.Position));

                PieceState movedAttacker = survivingAttacker.WithPosition(destination);
                AddPiece(movedAttacker);
                if (attackerPowerBefore != movedAttacker.EffectiveCombatPower)
                {
                    events.Add(new PiecePowerChanged(
                        attacker.Id,
                        attackerPowerBefore,
                        movedAttacker.EffectiveCombatPower));
                }

                events.Add(new PieceMoved(attacker.Id, attacker.Position, destination));
                return movedAttacker;
            }

            events.Add(new PieceDestroyed(attacker.Id, attacker.Position));
            if (survivingDefender != null)
            {
                piecesById[defender.Id] = survivingDefender;
                if (defenderPowerBefore != survivingDefender.EffectiveCombatPower)
                {
                    events.Add(new PiecePowerChanged(
                        defender.Id,
                        defenderPowerBefore,
                        survivingDefender.EffectiveCombatPower));
                }

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
                CellEffectDefinition effectDefinition =
                    definition.CellEffectDefinitions.First(
                        effect => string.Equals(
                            effect.EffectId, effectId, StringComparison.Ordinal));
                bool alreadyApplied =
                    effectDefinition.Lifetime ==
                    CellEffectLifetime.PermanentOncePerPiece
                        ? currentPiece.HasAppliedPermanentEffect(effectId)
                        : currentPiece.HasActiveEffect(effectId);
                if (alreadyApplied)
                {
                    continue;
                }

                ICellEffectHandler handler = cellEffectHandlers[effectId];
                int previousPower = currentPiece.EffectiveCombatPower;
                CellEffectResult result = handler.Apply(
                    new CellEffectContext(
                        Snapshot, currentPiece, cell, effectDefinition));
                ValidateCellEffectResult(currentPiece, result.Piece);

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
                    AddReservePiece(grant, events);
                }

                foreach (GameEvent additionalEvent in result.Events)
                {
                    events.Add(additionalEvent);
                }

                currentPiece = updatedPiece;
                piecesById[currentPiece.Id] = currentPiece;
            }

            return currentPiece;
        }

        private void AddReservePiece(
            ReservePieceGrant grant,
            ICollection<GameEvent> events)
        {
            if (!definition.TryGetMovementProfile(
                grant.MovementProfileId, out _))
            {
                throw new InvalidOperationException(
                    "Reserve pieces must use a registered movement profile.");
            }

            if (GetOwnedPieceCount(grant.Owner) >= definition.MaxPiecesPerPlayer)
            {
                return;
            }

            ReservePieceState reservePiece = new ReservePieceState(
                new PieceId(nextPieceId++),
                grant.Owner,
                grant.CombatPower,
                grant.MovementProfileId);
            reservesByPlayer[grant.Owner].Add(reservePiece);
            events.Add(new ReservePieceAdded(reservePiece));
        }

        private IEnumerable<GridPosition> GetLegalReserveDeploymentPositions(
            PlayerId player)
        {
            int[] ownTerritoryRows = cellsByPosition.Values
                .Where(cell => cell.TerritoryOwner == player)
                .Select(cell => cell.Position.Row)
                .Distinct()
                .ToArray();
            int[] opponentTerritoryRows = cellsByPosition.Values
                .Where(cell => cell.TerritoryOwner.HasValue &&
                               cell.TerritoryOwner.Value != player)
                .Select(cell => cell.Position.Row)
                .Distinct()
                .ToArray();
            if (ownTerritoryRows.Length != 1 || opponentTerritoryRows.Length != 1)
            {
                yield break;
            }

            int direction = Math.Sign(opponentTerritoryRows[0] - ownTerritoryRows[0]);
            if (direction == 0)
            {
                yield break;
            }

            for (int distance = 1;
                 distance <= definition.ReserveDeploymentDepth;
                 distance++)
            {
                int row = ownTerritoryRows[0] + direction * distance;
                for (int column = 0; column < definition.Columns; column++)
                {
                    GridPosition position = new GridPosition(column, row);
                    if (IsInside(position) &&
                        !pieceIdsByPosition.ContainsKey(position) &&
                        !IsOpponentTerritory(player, position))
                    {
                        yield return position;
                    }
                }
            }
        }

        private int GetBoardPieceCount(PlayerId player)
        {
            return piecesById.Values.Count(piece => piece.Owner == player);
        }

        private int GetOwnedPieceCount(PlayerId player)
        {
            return GetBoardPieceCount(player) + reservesByPlayer[player].Count;
        }

        private void ResolveNextTurn(
            PlayerId playerWhoActed,
            ICollection<GameEvent> events)
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
            events.Add(new TurnChanged(
                playerWhoActed, currentPlayer, turn.TurnWasPassed));
        }

        private bool HasAnyLegalAction(PlayerId player)
        {
            return BuildLegalCommands(player).Count > 0;
        }

        private IEnumerable<PlayerState> CreatePlayerStates()
        {
            yield return new PlayerState(
                PlayerId.Player1, reservesByPlayer[PlayerId.Player1]);
            yield return new PlayerState(
                PlayerId.Player2, reservesByPlayer[PlayerId.Player2]);
        }

        private void ValidateAndLoadCells()
        {
            foreach (CellDefinition cell in definition.Cells)
            {
                if (!IsInside(cell.Position) ||
                    !cellsByPosition.TryAdd(cell.Position, cell))
                {
                    throw new ArgumentException(
                        "The game definition contains an invalid cell.");
                }
            }

            if (cellsByPosition.Count != definition.Columns * definition.Rows)
            {
                throw new ArgumentException(
                    "The game definition must define every board cell.");
            }
        }

        private void ValidateCellEffectHandlers()
        {
            foreach (CellEffectDefinition effect in definition.CellEffectDefinitions)
            {
                if (!cellEffectHandlers.ContainsKey(effect.EffectId))
                {
                    throw new ArgumentException(
                        $"No cell effect handler is registered for '{effect.EffectId}'.");
                }
            }
        }

        private void ValidateInitialPiece(PieceState piece)
        {
            if (!IsInside(piece.Position) ||
                IsOwnTerritory(piece.Owner, piece.Position) ||
                piecesById.ContainsKey(piece.Id) ||
                pieceIdsByPosition.ContainsKey(piece.Position))
            {
                throw new ArgumentException(
                    "The game definition contains an invalid initial piece.");
            }
        }

        private void ValidateCellEffectResult(PieceState before, PieceState after)
        {
            if (after == null ||
                after.Id != before.Id ||
                after.Owner != before.Owner ||
                after.Position != before.Position ||
                !definition.TryGetMovementProfile(after.MovementProfileId, out _))
            {
                throw new InvalidOperationException(
                    "Cell effects may only change a piece's power, effects, or registered movement profile.");
            }
        }

        private bool IsInside(GridPosition position)
        {
            return position.Column >= 0 && position.Column < definition.Columns &&
                   position.Row >= 0 && position.Row < definition.Rows;
        }

        private bool IsOwnTerritory(PlayerId player, GridPosition position)
        {
            return cellsByPosition.TryGetValue(
                       position, out CellDefinition cell) &&
                   cell.TerritoryOwner == player;
        }

        private bool IsOpponentTerritory(PlayerId player, GridPosition position)
        {
            return cellsByPosition.TryGetValue(
                       position, out CellDefinition cell) &&
                   cell.TerritoryOwner.HasValue &&
                   cell.TerritoryOwner.Value != player;
        }

        private PieceState TryGetPiece(GridPosition position)
        {
            return pieceIdsByPosition.TryGetValue(position, out PieceId id)
                ? piecesById[id]
                : null;
        }

        private void AddPiece(PieceState piece)
        {
            if (!definition.TryGetMovementProfile(
                piece.MovementProfileId, out _))
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
}
