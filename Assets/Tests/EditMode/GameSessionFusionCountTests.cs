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
    /// 合体してできた駒は1駒として数える。所有上限に対しても1つぶんなので、
    /// 合体すると空きが1つ生まれ、そこへリザーブを獲得できる。
    /// </summary>
    public sealed partial class GameSessionTests
    {
        private const string FusionGrantEffectId = "reserve-piece-grant";

        [Test]
        public void FusingTwoPiecesLeavesOnePiece()
        {
            GameSession custom = new GameSession(
                GameSessionTestBuilder.CreateDefinition(
                    PlayerId.Player1,
                    null,
                    InitialPiece(1, 0, 1, PlayerId.Player1),
                    InitialPiece(2, 1, 1, PlayerId.Player1),
                    InitialPiece(3, 0, 8, PlayerId.Player2)),
                randomSource: new FixedRandomSource(1, 0.5d));

            Assert.That(custom.Snapshot.GetOwnedPieceCount(PlayerId.Player1), Is.EqualTo(2));

            CommandResult result = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, new PieceId(1), new PieceId(2)));

            Assert.That(result.Events.OfType<PiecesFused>().Any(), Is.True,
                "この乱数では合体は成功する。");
            Assert.That(custom.Snapshot.GetPieceCount(PlayerId.Player1), Is.EqualTo(1));
            Assert.That(custom.Snapshot.GetOwnedPieceCount(PlayerId.Player1), Is.EqualTo(1),
                "合体してできた駒も1駒として数える。");

            PieceState fused = GetPiece(custom.Snapshot, new GridPosition(0, 1));
            Assert.That(fused.HasFused, Is.True);
        }

        /// <summary>
        /// 上限いっぱいでも、合体で1つ空くのでリザーブを獲得できる。
        /// </summary>
        [Test]
        public void FusingFreesRoomForAReserveGrant()
        {
            GameDefinition definition = CreateDefinitionWithEffectsAndLimits(
                PlayerId.Player1,
                new Dictionary<GridPosition, string[]>
                {
                    [new GridPosition(0, 2)] = new[] { FusionGrantEffectId }
                },
                new[]
                {
                    new CellEffectDefinition(
                        FusionGrantEffectId, CellEffectLifetime.PermanentOncePerPiece)
                },
                2,
                GameDefinition.StandardReserveDeploymentDepth,
                InitialPiece(1, 0, 1, PlayerId.Player1),
                InitialPiece(2, 1, 1, PlayerId.Player1),
                InitialPiece(3, 0, 8, PlayerId.Player2));

            GameSession custom = new GameSession(
                definition,
                cellEffectHandlers: new ICellEffectHandler[]
                {
                    new ReservePieceGrantCellEffectHandler(
                        FusionGrantEffectId, 1, PowerMovementProfile.StandardId)
                },
                randomSource: new FixedRandomSource(1, 0.5d));

            CommandResult fusion = custom.Execute(new FusePiecesCommand(
                PlayerId.Player1, new PieceId(1), new PieceId(2)));
            Assert.That(fusion.Events.OfType<PiecesFused>().Any(), Is.True);

            custom.Execute(new RandomizePowerCommand(PlayerId.Player2, new PieceId(3)));
            Assert.That(custom.Snapshot.CurrentPlayer, Is.EqualTo(PlayerId.Player1));

            CommandResult entry = custom.Execute(new MovePieceCommand(
                PlayerId.Player1, new PieceId(1), new GridPosition(0, 2)));

            Assert.That(entry.Success, Is.True);
            Assert.That(entry.Events.OfType<ReservePieceAdded>().Count(), Is.EqualTo(1),
                "合体で1つ空いたので獲得できる。");
            Assert.That(custom.Snapshot.GetOwnedPieceCount(PlayerId.Player1), Is.EqualTo(2));
        }
    }
}
