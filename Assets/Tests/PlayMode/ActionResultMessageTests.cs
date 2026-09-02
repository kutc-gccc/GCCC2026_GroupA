using System;
using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Players;
using GCCC.BoardGame.Core.Rules.CellEffects;
using GCCC.BoardGame.Presentation;
using NUnit.Framework;

namespace GCCC.BoardGame.Tests
{
    public sealed class ActionResultMessageTests
    {
        private static readonly PieceId First = new PieceId(1);
        private static readonly PieceId Second = new PieceId(2);
        private static readonly GridPosition Cell = new GridPosition(2, 3);

        [TestCase(2, 1, 1, 0, "攻撃側：残り1 ／ 防御側：消滅")]
        [TestCase(1, 3, 0, 2, "攻撃側：消滅 ／ 防御側：残り2")]
        [TestCase(1, 1, 0, 0, "相打ち：両方の駒が消滅")]
        public void CombatReportsActualOutcome(int a, int d, int afterA, int afterD, string expected)
        {
            string text = Build(new CombatResolved(First, Second, a, d, afterA, afterD));
            Assert.That(text, Does.StartWith("▲ P1：戦闘"));
            Assert.That(text, Does.Contain($"戦闘 {a}対{d}"));
            Assert.That(text, Does.Contain(expected));
        }

        [TestCase(1, 3)]
        [TestCase(3, 1)]
        [TestCase(2, 2)]
        public void RandomizationReportsChangeOnceIncludingUnchanged(int previous, int next)
        {
            string text = ActionResultMessageBuilder.Build(
                new RandomizePowerCommand(PlayerId.Player2, Second), Snapshot(), new GameEvent[]
                {
                    new RandomizePowerEvent(Second, previous, next),
                    new PiecePowerChanged(Second, previous, next)
                });
            Assert.That(text, Does.StartWith("▼ P2："));
            Assert.That(text.Split('→').Length, Is.EqualTo(2));
            Assert.That(text.Contains("変化なし・手番消費"), Is.EqualTo(previous == next));
        }

        [TestCase(CellEffectLifetime.WhileOccupied, "このマスにいる間")]
        [TestCase(CellEffectLifetime.PermanentOncePerPiece, "この駒につき1回")]
        [TestCase(CellEffectLifetime.EveryStop, "止まるたびに加算")]
        public void PowerEffectUsesValuesAndLifetimeNotEffectId(CellEffectLifetime lifetime, string note)
        {
            string text = ActionResultMessageBuilder.Build(
                new MovePieceCommand(PlayerId.Player1, First, Cell), Snapshot(lifetime),
                new GameEvent[]
                {
                    new CellEffectTriggered("custom", First, Cell),
                    new PiecePowerChanged(First, 2, 7),
                    new TurnChanged(PlayerId.Player1, PlayerId.Player2, false)
                });
            Assert.That(text, Does.Contain($"戦闘力＋5：2→7（{note}）"));
            Assert.That(text, Does.Not.Contain("custom"));
        }

        [Test]
        public void CombatAndPartialReserveGrantsAreBothShownWithoutDuplicates()
        {
            string text = Build(
                new CombatResolved(First, Second, 3, 1, 2, 0),
                new CellEffectTriggered("custom", First, Cell),
                new ReservePieceAdded(new ReservePieceState(new PieceId(3), PlayerId.Player1, 1, PowerMovementProfile.StandardId)),
                new ReservePieceGrantBlockedByLimit(PlayerId.Player1, 4, 4),
                new ReservePieceGrantBlockedByLimit(PlayerId.Player1, 4, 4));
            Assert.That(text, Does.Contain("攻撃側：残り2"));
            Assert.That(text, Does.Contain("リザーブ＋1（止まるたびに獲得）"));
            Assert.That(text, Does.Contain("リザーブ2個獲得なし：所持上限 4/4"));
            Assert.That(text.IndexOf("戦闘 3対1"), Is.LessThan(text.IndexOf("リザーブ＋1")));
        }

        [Test]
        public void EffectBlocksDoNotMixPowerAndReserveOutcomes()
        {
            string text = Build(
                new CellEffectTriggered("custom", First, Cell),
                new PiecePowerChanged(First, 1, 3),
                new CellEffectTriggered("unknown", First, Cell),
                new CellEffectTriggered("custom", First, Cell),
                new ReservePieceGrantBlockedByLimit(PlayerId.Player1, 6, 6),
                new CellEffectAlreadyApplied("once", First, Cell));
            Assert.That(text, Does.Contain("戦闘力＋2：1→3"));
            Assert.That(text, Does.Contain("特殊マスの効果が発動"));
            Assert.That(text, Does.Contain("リザーブ獲得なし：所持上限 6/6"));
            Assert.That(text, Does.Contain("追加効果なし：この駒には適用済み"));
            Assert.That(text.Split('→').Length, Is.EqualTo(2));
        }

        [Test]
        public void ExpirationBeforeCombatExplainsThreeVersusOneDraw()
        {
            GameSnapshot snapshot = Snapshot(CellEffectLifetime.WhileOccupied, true);
            string text = ActionResultMessageBuilder.Build(
                new MovePieceCommand(PlayerId.Player1, First, Cell), snapshot,
                new GameEvent[]
                {
                    new CellEffectExpired("custom", First, new GridPosition(2, 2)),
                    new PiecePowerChanged(First, 3, 1),
                    new CombatResolved(First, Second, 1, 1, 0, 0),
                    new PieceDestroyed(First, new GridPosition(2, 2)),
                    new PieceDestroyed(Second, Cell)
                });
            Assert.That(text, Does.Contain("攻撃前に強化解除：3→1"));
            Assert.That(text, Does.Contain("戦闘 1対1"));
            Assert.That(text, Does.Contain("相打ち"));
            Assert.That(text, Does.Not.Contain("効果が発動"));
            Assert.That(text.IndexOf("強化解除"), Is.LessThan(text.IndexOf("戦闘 1対1")));
        }

        [TestCase(1, "合体成功！")]
        [TestCase(2, "大成功！")]
        public void FusionSuccessMessagesRemain(int bonus, string expected) =>
            Assert.That(Build(new PiecesFused(First, Second, First, bonus)), Does.Contain(expected));

        [Test]
        public void FusionFailureIsAnActionResult() =>
            Assert.That(Build(new FusionAttemptFailed(First, Second)), Does.Contain("合体失敗"));

        [Test]
        public void DeploymentCanTriggerAnEffectInTheSameResult()
        {
            string text = ActionResultMessageBuilder.Build(
                new DeployReservePieceCommand(PlayerId.Player1, First, Cell), Snapshot(),
                new GameEvent[]
                {
                    new ReservePieceDeployed(First, PlayerId.Player1, Cell),
                    new CellEffectTriggered("custom", First, Cell),
                    new PiecePowerChanged(First, 1, 3)
                });
            Assert.That(text, Does.Contain("リザーブを配置しました"));
            Assert.That(text, Does.Contain("戦闘力＋2：1→3"));
        }

        [Test]
        public void CoordinatorKeepsLastResultAcrossSelectionInstructionsAndRejectedCommands()
        {
            GameSession session = CreateReserveSession();
            var hud = new RecordingHud();
            var player1 = new HumanPlayerAgent(PlayerId.Player1);
            var player2 = new HumanPlayerAgent(PlayerId.Player2);
            var coordinator = new GameCoordinator(session, new EmptyBoard(), new EmptyPieces(), hud, player1, player2);
            try
            {
                coordinator.HandleCellClick(new GridPosition(2, 2));
                coordinator.HandleCellClick(Cell);
                Assert.That(hud.Message, Does.Contain("リザーブ＋1"));
                player2.TrySubmit(new MovePieceCommand(PlayerId.Player2, Second, new GridPosition(5, 7)));
                string result = hud.Message;
                Assert.That(result, Does.StartWith("▼ P2："));
                coordinator.HandleCellClick(Cell);
                coordinator.HandleCellClick(Cell);
                Assert.That(hud.Message, Is.EqualTo(result));
                coordinator.ToggleReserveDeployMode();
                Assert.That(hud.Message, Does.Contain("マスを選んでください"));
                coordinator.ToggleReserveDeployMode();
                Assert.That(hud.Message, Is.EqualTo(result));
                coordinator.ToggleReserveDeployMode();
                coordinator.ToggleReservePieceSelection(coordinator.SelectedReservePieceId.Value);
                Assert.That(hud.Message, Is.EqualTo(result));
                GameSnapshot before = session.Snapshot;
                player1.TrySubmit(new MovePieceCommand(PlayerId.Player1, First, new GridPosition(99, 99)));
                Assert.That(session.Snapshot, Is.SameAs(before));
                Assert.That(hud.Message, Is.EqualTo(result));
                coordinator.HandleCellClick(Cell);
                coordinator.HandleCellClick(new GridPosition(2, 4));
                Assert.That(hud.Message, Does.StartWith("▲ P1：移動"));
                coordinator.Reset();
                Assert.That(hud.Message, Is.Empty);
            }
            finally { coordinator.Dispose(); }
        }

        private static string Build(params GameEvent[] events) => ActionResultMessageBuilder.Build(
            new MovePieceCommand(PlayerId.Player1, First, Cell), Snapshot(), events);

        private static GameSnapshot Snapshot(CellEffectLifetime lifetime = CellEffectLifetime.EveryStop, bool boosted = false) =>
            new GameSnapshot(6, 10, new[]
            {
                new PieceState(First, PlayerId.Player1, new GridPosition(2, 2), 1, PowerMovementProfile.StandardId,
                    activeCellEffects: boosted ? new[] { new ActiveCellEffectState("custom", 2) } : null)
            }, Array.Empty<CellDefinition>(), PlayerId.Player1, null, false,
                effectDefinitions: new[] { new CellEffectDefinition("custom", lifetime) });

        private static GameSession CreateReserveSession()
        {
            var standard = GameDefinition.CreateStandard();
            return new GameSession(new GameDefinition(6, 10,
                standard.Cells.Select(c => new CellDefinition(c.Position, c.TerritoryOwner,
                    c.Position == Cell ? new[] { "reserve" } : null)),
                new[]
                {
                    new InitialPieceDefinition(First, PlayerId.Player1, new GridPosition(2, 2), 1, PowerMovementProfile.StandardId),
                    new InitialPieceDefinition(Second, PlayerId.Player2, new GridPosition(5, 8), 1, PowerMovementProfile.StandardId)
                }, movementProfiles: standard.MovementProfiles,
                cellEffectDefinitions: new[] { new CellEffectDefinition("reserve", CellEffectLifetime.EveryStop) }),
                cellEffectHandlers: new[] { new ReservePieceGrantCellEffectHandler("reserve", 1, PowerMovementProfile.StandardId) });
        }

        private sealed class RecordingHud : IGameHud
        {
            public event Action OnRandomizePowerButtonClicked { add { } remove { } }
            public string Message { get; private set; }
            public void ShowMessage(string text) => Message = text;
            public void Render(GameSnapshot snapshot) { }
            public void SetFuseButtonInteractable(bool value) { }
            public void SetRandomizeButtonInteractable(bool value) { }
            public void SetReserveDeployButtonInteractable(bool value) { }
            public void SetFuseModeActive(bool value) { }
            public void SetReserveDeployModeActive(bool value) { }
            public void SetDeployableReservePieces(IEnumerable<PieceId> ids) { }
            public void SetSelectedReservePiece(PieceId? id) { }
        }
        private sealed class EmptyBoard : IBoardGameBoardView
        {
            public void ShowSelection(GridPosition? selected, IReadOnlyList<GridPosition> moves,
                IReadOnlyList<GridPosition> fusion, GameSnapshot snapshot) { }
        }
        private sealed class EmptyPieces : IPieceViewCollection
        {
            public void Rebuild(GameSnapshot snapshot) { }
            public void ApplyEvents(IReadOnlyList<GameEvent> events, GameSnapshot snapshot) { }
        }
    }
}
