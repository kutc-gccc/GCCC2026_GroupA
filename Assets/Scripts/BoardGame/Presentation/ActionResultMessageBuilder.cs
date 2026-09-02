using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;

namespace GCCC.BoardGame.Presentation
{
    /// <summary>確定済みのイベントを、直前の1行動の説明にする。ルールの再計算はしない。</summary>
    internal static class ActionResultMessageBuilder
    {
        public static string Build(
            GameCommand command, GameSnapshot before, IReadOnlyList<GameEvent> events)
        {
            bool hasCombat = events.OfType<CombatResolved>().Any();
            string action = hasCombat ? "戦闘" : command switch
            {
                MovePieceCommand _ => "移動しました",
                DeployReservePieceCommand _ => "リザーブを配置しました",
                RandomizePowerCommand _ => "パワーランダム化",
                FusePiecesCommand _ => "合体",
                _ => "行動結果"
            };
            var lines = new List<string> { $"{PlayerLabel(command.Player)}：{action}" };
            AppendExpiredEffects(lines, before, events, hasCombat);

            for (int i = 0; i < events.Count; i++)
            {
                switch (events[i])
                {
                    case CombatResolved combat:
                        lines.Add($"戦闘 {combat.AttackerPowerBefore}対{combat.DefenderPowerBefore}");
                        lines.Add(combat.AttackerPowerAfter == 0 && combat.DefenderPowerAfter == 0
                            ? "相打ち：両方の駒が消滅"
                            : $"攻撃側：{Survivor(combat.AttackerPowerAfter)} ／ 防御側：{Survivor(combat.DefenderPowerAfter)}");
                        break;
                    case RandomizePowerEvent randomized:
                        lines.Add($"戦闘力 {randomized.PreviousPower}→{randomized.NewPower}" +
                            (randomized.PreviousPower == randomized.NewPower
                                ? "（変化なし・手番消費）" : string.Empty));
                        break;
                    case PiecesFused fused:
                        lines.Add(fused.Bonus >= 2
                            ? "大成功！ 戦闘力+2で合体しました"
                            : "合体成功！ 戦闘力+1で合体しました");
                        break;
                    case FusionAttemptFailed _:
                        lines.Add("合体失敗…　駒はそのまま残りました");
                        break;
                    case CellEffectAlreadyApplied _:
                        lines.Add("追加効果なし：この駒には適用済み");
                        break;
                    case CellEffectTriggered triggered:
                        int end = i + 1;
                        while (end < events.Count && !IsEffectBoundary(events[end])) end++;
                        AppendCellEffect(lines, before, triggered,
                            events.Skip(i + 1).Take(end - i - 1).ToArray(), command.Player);
                        i = end - 1;
                        break;
                }
            }

            return string.Join("\n", lines);
        }

        private static void AppendExpiredEffects(
            ICollection<string> lines, GameSnapshot before,
            IReadOnlyList<GameEvent> events, bool hasCombat)
        {
            foreach (PieceId id in events.OfType<CellEffectExpired>().Select(e => e.PieceId).Distinct())
            {
                if (!before.TryGetPiece(id, out PieceState piece)) continue;
                // Expiration outcomes occur before movement/combat and before arrival effects.
                PiecePowerChanged changed = events
                    .TakeWhile(e => !(e is CombatResolved) && !(e is PieceMoved) &&
                        !(e is CellEffectTriggered))
                    .OfType<PiecePowerChanged>().FirstOrDefault(e => e.PieceId == id);
                string prefix = hasCombat ? "攻撃前に強化解除" : "マスを離れて効果終了";
                lines.Add(changed == null ? prefix :
                    $"{prefix}：{piece.EffectiveCombatPower}→{changed.CurrentPower}");
            }
        }

        private static void AppendCellEffect(
            ICollection<string> lines, GameSnapshot before, CellEffectTriggered triggered,
            IReadOnlyList<GameEvent> outcomes, PlayerId actor)
        {
            CellEffectDefinition definition = before.CellEffectDefinitions
                .FirstOrDefault(e => e.EffectId == triggered.EffectId);
            int lineCount = lines.Count;
            foreach (PiecePowerChanged power in outcomes.OfType<PiecePowerChanged>()
                         .Where(e => e.PieceId == triggered.PieceId))
            {
                int delta = power.CurrentPower - power.PreviousPower;
                string change = delta >= 0 ? $"＋{delta}" : $"−{Math.Abs(delta)}";
                lines.Add($"戦闘力{change}：{power.PreviousPower}→{power.CurrentPower}" +
                    LifetimeNote(definition, false));
            }

            ReservePieceAdded[] added = outcomes.OfType<ReservePieceAdded>().ToArray();
            ReservePieceGrantBlockedByLimit[] blocked =
                outcomes.OfType<ReservePieceGrantBlockedByLimit>().ToArray();
            foreach (PlayerId owner in added.Select(e => e.Piece.Owner)
                         .Concat(blocked.Select(e => e.Owner)).Distinct())
            {
                string prefix = owner == actor ? string.Empty : $"{PlayerLabel(owner)}：";
                int count = added.Count(e => e.Piece.Owner == owner);
                if (count > 0)
                    lines.Add($"{prefix}リザーブ＋{count}" + LifetimeNote(definition, true));

                ReservePieceGrantBlockedByLimit[] misses = blocked.Where(e => e.Owner == owner).ToArray();
                if (misses.Length > 0)
                {
                    ReservePieceGrantBlockedByLimit limit = misses[misses.Length - 1];
                    string missed = count > 0 || misses.Length > 1
                        ? $"リザーブ{misses.Length}個獲得なし" : "リザーブ獲得なし";
                    lines.Add($"{prefix}{missed}：所持上限 {limit.OwnedPieceCount}/{limit.MaxPiecesPerPlayer}");
                }
            }

            if (lines.Count == lineCount) lines.Add("特殊マスの効果が発動");
        }

        private static bool IsEffectBoundary(GameEvent gameEvent) =>
            gameEvent is CellEffectTriggered || gameEvent is CellEffectAlreadyApplied ||
            gameEvent is TurnChanged || gameEvent is GameEnded;

        private static string LifetimeNote(CellEffectDefinition definition, bool reserve)
        {
            if (definition == null) return string.Empty;
            return definition.Lifetime switch
            {
                CellEffectLifetime.WhileOccupied => "（このマスにいる間）",
                CellEffectLifetime.PermanentOncePerPiece => "（この駒につき1回）",
                CellEffectLifetime.EveryStop => reserve
                    ? "（止まるたびに獲得）" : "（止まるたびに加算）",
                _ => string.Empty
            };
        }

        private static string Survivor(int power) => power > 0 ? $"残り{power}" : "消滅";
        private static string PlayerLabel(PlayerId player) =>
            player == PlayerId.Player1 ? "▲ P1" : "▼ P2";
    }
}
