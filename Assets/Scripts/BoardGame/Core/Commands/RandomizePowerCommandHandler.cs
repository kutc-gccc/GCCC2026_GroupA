using System;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Core.Commands
{
    public sealed class RandomizePowerCommandHandler : IGameCommandHandler
    {
        private static readonly Random Random = new Random();

        public Type CommandType => typeof(RandomizePowerCommand);

        public CommandResult Execute(GameSession session, GameCommand command)
        {
            if (command is RandomizePowerCommand randomizeCommand)
            {
                return Execute(session, randomizeCommand, null, null);
            }
            return CommandResult.Failed(CommandFailureReason.InvalidCommand);
        }

        public static CommandResult Execute(
            GameSession session,
            RandomizePowerCommand command,
            Action<PieceState> updatePieceState,
            Action advanceTurn)
        {
            var snapshot = session.Snapshot;

            // 1. ゲーム終了チェック
            if (snapshot.IsGameOver)
            {
                return CommandResult.Failed(CommandFailureReason.GameOver);
            }

            // 2. 手番チェック
            if (snapshot.CurrentPlayer != command.Player)
            {
                return CommandResult.Failed(CommandFailureReason.NotPlayersTurn);
            }

            // 3. 駒の存在チェック
            if (!snapshot.TryGetPiece(command.PieceId, out var piece))
            {
                return CommandResult.Failed(CommandFailureReason.PieceNotFound);
            }

            // 4. 駒の所有権チェック
            if (piece.Owner != command.Player)
            {
                return CommandResult.Failed(CommandFailureReason.NotPieceOwner);
            }

            // 5. 1〜3のランダムな値を決定して戦闘力を更新
            int previousPower = piece.CombatPower;
            int newPower = Random.Next(1, 4); // 1, 2, 3 のいずれか
            var updatedPiece = piece.WithCombatPower(newPower);

            // セッション内部の状態を更新
            updatePieceState?.Invoke(updatedPiece);

            // 発生したイベントを生成
            var events = new GameEvent[]
            {
                new RandomizePowerEvent(command.PieceId, previousPower, newPower),
                new PiecePowerChanged(command.PieceId, previousPower, newPower)
            };

            // 手番を次のプレイヤーへ（1ターン消費）
            advanceTurn?.Invoke();

            return CommandResult.Succeeded(events);
        }
    }
}