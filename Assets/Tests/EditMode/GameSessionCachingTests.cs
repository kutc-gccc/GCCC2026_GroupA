using System.Linq;
using GCCC.BoardGame.Core;
using GCCC.BoardGame.Core.Commands;
using GCCC.BoardGame.Core.Model;
using NUnit.Framework;

namespace GCCC.BoardGame.Tests
{
    public sealed class GameSessionCachingTests
    {
        [Test]
        public void LegalCommandsAreSharedUntilStateChanges()
        {
            GameSession session = new GameSession(GameDefinition.CreateStandard());

            var first = session.GetLegalCommands(PlayerId.Player1);
            var second = session.GetLegalCommands(PlayerId.Player1);

            Assert.That(second, Is.SameAs(first));

            MovePieceCommand move = first
                .OfType<MovePieceCommand>()
                .First(command => command.PieceId == new PieceId(1));
            Assert.That(session.Execute(move).Success, Is.True);

            var afterMutation = session.GetLegalCommands(PlayerId.Player2);
            Assert.That(afterMutation, Is.Not.SameAs(first));
            Assert.That(session.GetLegalCommands(PlayerId.Player2),
                Is.SameAs(afterMutation));
        }

        [Test]
        public void WithLegalCommandsSharesImmutableSnapshotData()
        {
            GameSession session = new GameSession(GameDefinition.CreateStandard());
            GameSnapshot snapshot = session.Snapshot;

            GameSnapshot withCommands = snapshot.WithLegalCommands(
                session.GetLegalCommands(snapshot.CurrentPlayer));

            Assert.That(withCommands.Pieces, Is.SameAs(snapshot.Pieces));
            Assert.That(withCommands.Cells, Is.SameAs(snapshot.Cells));
            Assert.That(withCommands.Players, Is.SameAs(snapshot.Players));
            Assert.That(withCommands.CellEffectDefinitions,
                Is.SameAs(snapshot.CellEffectDefinitions));
        }
    }
}
