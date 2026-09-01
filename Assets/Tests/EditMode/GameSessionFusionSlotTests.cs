using System.Collections.Generic;
using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Core.Rules.CellEffects;
using NUnit.Framework;

namespace GCCC.BoardGame.Tests
{
    /// <summary>
    /// 合体してできた駒が所有上限の枠を2つ使うこと。
    /// 合体で枠を空けてリザーブを稼ぐ動きを塞ぐための規則。
    /// </summary>
    public sealed partial class GameSessionTests
    {
        private const string SlotTestGrantEffectId = "reserve-piece-grant";

        [Test]
        public void FusedPieceCountsAsOnePieceButTakesTwoSlots()
        {
            GameSession custom = new GameSession(
                GameSessionTestBuilder.CreateDefinition(
                    PlayerId.Player1,
                    null,
                    InitialPiece(1, 0, 1, PlayerId.Player1),
                    InitialPiece(2, 1, 1, PlayerId.Player1),
                    InitialPiece(3, 0, 8, PlayerId.Player2)),
                randomSource: new FixedRandomSource(1, 0.5d));

            Assert.That(custom.Snapshot.GetOwnedSlotCount(PlayerId.Player1), Is.EqualTo(2));

            CommandResult result = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, new PieceId(1), new PieceId(2)));

            Assert.That(result.Events.OfType<PiecesFused>().Any(), Is.True,
                "この乱数では合体は成功する。");
            Assert.That(custom.Snapshot.GetPieceCount(PlayerId.Player1), Is.EqualTo(1),
                "盤上の駒の実数は1つに減る。");
            Assert.That(custom.Snapshot.GetOwnedSlotCount(PlayerId.Player1), Is.EqualTo(2),
                "合体しても所有上限の枠は空かない。");

            PieceState fused = GetPiece(custom.Snapshot, new GridPosition(0, 1));
            Assert.That(fused.HasFused, Is.True);
            Assert.That(fused.SlotCost, Is.EqualTo(2));
        }

        [Test]
        public void FusingDoesNotFreeASlotForAReserveGrant()
        {
            GameSession custom = CreateGrantSession(maxPiecesPerPlayer: 2);

            FuseThenPassToPlayer1(custom);
            CommandResult entry = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(0, 2)));

            Assert.That(entry.Success, Is.True);
            Assert.That(entry.Events.OfType<ReservePieceAdded>(), Is.Empty,
                "合体後も枠は埋まったままなので、リザーブは獲得できない。");
            Assert.That(custom.Snapshot.GetPlayer(PlayerId.Player1).ReservePieces, Is.Empty);
        }

        /// <summary>
        /// 獲得を一律に禁じているのではなく、枠が埋まっているから獲得できない、という規則であることを示す。
        /// </summary>
        [Test]
        public void FusedPieceStillGainsAReserveWhenASlotIsFree()
        {
            GameSession custom = CreateGrantSession(maxPiecesPerPlayer: 3);

            FuseThenPassToPlayer1(custom);
            CommandResult entry = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(0, 2)));

            Assert.That(entry.Events.OfType<ReservePieceAdded>().Count(), Is.EqualTo(1));
            Assert.That(custom.Snapshot.GetOwnedSlotCount(PlayerId.Player1), Is.EqualTo(3));
        }

        private static GameSession CreateGrantSession(int maxPiecesPerPlayer)
        {
            GameDefinition definition = CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(0, 2)] = new[] { SlotTestGrantEffectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        SlotTestGrantEffectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                maxPiecesPerPlayer,
                GameDefinition.StandardReserveDeploymentDepth,
                InitialPiece(1, 0, 1, PlayerId.Player1),
                InitialPiece(2, 1, 1, PlayerId.Player1),
                InitialPiece(3, 0, 8, PlayerId.Player2));

            return new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        SlotTestGrantEffectId, 1, PowerMovementProfile.StandardId)
                },
                randomSource: new FixedRandomSource(1, 0.5d));
        }

        /// <summary>合体して手番を1周させ、合体後の駒を動かせる状態にする。</summary>
        private static void FuseThenPassToPlayer1(GameSession custom)
        {
            CommandResult fusion = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, new PieceId(1), new PieceId(2)));
            Assert.That(fusion.Events.OfType<PiecesFused>().Any(), Is.True);

            custom.Execute(new RandomizePowerCommand(PlayerId.Player2, new PieceId(3)));
            Assert.That(custom.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));
        }
    }
}
