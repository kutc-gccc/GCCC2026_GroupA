using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GCCC.BoardGame.Core.Events;
using GCCC.BoardGame.Core.Model;
using GCCC.BoardGame.Presentation;
using GCCC.BoardGame.Presentation.Audio;
using GCCC.BoardGame.Presentation.Config;
using GCCC.BoardGame.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GCCC.BoardGame.Tests
{
    public sealed class PresentationRefactoringTests
    {
        private GameObject root;
        private RuntimeSpriteFactory sprites;

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            sprites?.Dispose();
        }

        /// <summary>
        /// 合体してできた駒は、駒を少し大きくし、数字を白く縁取った黒にして見分けられるようにする。
        /// 数字そのものの見た目の大きさは変えない。
        /// </summary>
        [UnityTest]
        public IEnumerator FusedPieceGrowsAndOutlinesItsPowerInWhite()
        {
            root = new GameObject("Fused Piece Label Test");
            sprites = new RuntimeSpriteFactory();

            PieceView view = new GameObject("Piece").AddComponent<PieceView>();
            view.transform.SetParent(root.transform, false);
            view.Initialize(CreateLabelTestPiece(false), sprites.TriangleSprite, 6, 10);

            TextMesh label = view.transform.Find("Combat Power").GetComponent<TextMesh>();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.color, Is.EqualTo(Color.white));
            float normalPieceScale = view.transform.localScale.x;
            float normalApparentSize = label.characterSize * normalPieceScale;
            Assert.That(OutlineMeshes(label).Any(outline => outline.gameObject.activeSelf),
                Is.False, "通常の駒には縁取りを出さない。");

            view.Render(CreateLabelTestPiece(true), sprites.TriangleSprite);

            Assert.That(view.transform.localScale.x, Is.GreaterThan(normalPieceScale),
                "合体してできた駒は少し大きくする。");
            Assert.That(label.color, Is.EqualTo(Color.black),
                "合体してできた駒は数字を黒くする。");
            Assert.That(label.characterSize * view.transform.localScale.x,
                Is.EqualTo(normalApparentSize).Within(0.0001f),
                "駒を大きくしても、数字の見た目の大きさは変えない。");

            TextMesh[] outlines = OutlineMeshes(label);
            Assert.That(outlines.Length, Is.GreaterThanOrEqualTo(4),
                "黒い数字を読めるようにする白い縁取りを敷く。");
            Assert.That(outlines.All(outline => outline.gameObject.activeSelf), Is.True);
            Assert.That(outlines.All(outline => outline.color == Color.white), Is.True);
            Assert.That(outlines.All(outline => outline.text == label.text), Is.True);
            Assert.That(
                outlines.All(outline => outline.transform.localPosition.magnitude > 0f),
                Is.True, "縁取りは数字からずらして置く。");

            view.Render(CreateLabelTestPiece(false), sprites.TriangleSprite);
            Assert.That(OutlineMeshes(label).Any(outline => outline.gameObject.activeSelf),
                Is.False, "合体していない状態へ戻したら縁取りは消す。");
            yield return null;
        }

        private static TextMesh[] OutlineMeshes(TextMesh label)
        {
            return label.GetComponentsInChildren<TextMesh>(true)
                .Where(mesh => mesh != label)
                .ToArray();
        }

        private static PieceState CreateLabelTestPiece(bool hasFused)
        {
            return new PieceState(
                new PieceId(1),
                PlayerId.Player1,
                new GridPosition(0, 1),
                3,
                PowerMovementProfile.StandardId,
                null,
                null,
                hasFused);
        }

        [UnityTest]
        public IEnumerator TerritoryBordersFollowSnapshotCellsAtArbitraryRows()
        {
            GameSnapshot snapshot = CreateTerritorySnapshot(4, 6, 2, 4);
            root = new GameObject("Territory Border Test");
            sprites = new RuntimeSpriteFactory();
            BoardView board = root.AddComponent<BoardView>();
            board.Initialize(Camera.main, sprites, snapshot);
            yield return null;

            Transform player1 = root.transform.Find("Player 1 Territory Border");
            Transform player2 = root.transform.Find("Player 2 Territory Border");
            Assert.That(player1, Is.Not.Null);
            Assert.That(player2, Is.Not.Null);
            Assert.That(player1.childCount, Is.EqualTo(10));
            Assert.That(player2.childCount, Is.EqualTo(10));
            Assert.That(player1.Find("Top (0, 2)"), Is.Not.Null);
            Assert.That(player2.Find("Bottom (0, 4)"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PieceViewShowsEffectiveCombatPower()
        {
            root = new GameObject("Effective Power Piece Test");
            sprites = new RuntimeSpriteFactory();
            PieceView view = root.AddComponent<PieceView>();
            PieceState piece = new PieceState(
                new PieceId(1),
                PlayerId.Player1,
                new GridPosition(1, 1),
                2,
                PowerMovementProfile.StandardId,
                activeCellEffects: new[]
                {
                    new ActiveCellEffectState("temporary", 3)
                });
            view.Initialize(piece, sprites.CircleSprite, 4, 6);
            yield return null;

            Assert.That(
                view.transform.Find("Combat Power").GetComponent<TextMesh>().text,
                Is.EqualTo("5"));
        }

        [UnityTest]
        public IEnumerator ReconcileKeepsUnchangedPieceViewInstances()
        {
            root = new GameObject("Piece Reconcile Test");
            sprites = new RuntimeSpriteFactory();
            GameSnapshot before = CreatePieceSnapshot(
                new PieceState(
                    new PieceId(1), PlayerId.Player1, new GridPosition(0, 1), 1,
                    PowerMovementProfile.StandardId),
                new PieceState(
                    new PieceId(2), PlayerId.Player2, new GridPosition(3, 4), 1,
                    PowerMovementProfile.StandardId));
            PieceViewManager manager = root.AddComponent<PieceViewManager>();
            manager.Initialize(sprites.CircleSprite, sprites.SquareSprite, before);
            PieceView unchangedBefore = FindPieceView(manager, new PieceId(2));

            GameSnapshot after = CreatePieceSnapshot(
                new PieceState(
                    new PieceId(1), PlayerId.Player1, new GridPosition(0, 2), 1,
                    PowerMovementProfile.StandardId),
                before.Pieces.Single(piece => piece.Id == new PieceId(2)));
            manager.ApplyEvents(Array.Empty<GameEvent>(), after);
            yield return null;

            Assert.That(FindPieceView(manager, new PieceId(2)),
                Is.SameAs(unchangedBefore));
        }

        [Test]
        public void InteractionStateFactoriesCannotRepresentConflictingModes()
        {
            InteractionState selected = InteractionState.PieceSelected(new PieceId(1));
            InteractionState fusion = InteractionState.Fusion(new PieceId(2));
            InteractionState reserve =
                InteractionState.ReserveDeployment(new PieceId(3));

            Assert.That(selected.Mode, Is.EqualTo(InteractionMode.PieceSelected));
            Assert.That(selected.SelectedReservePieceId, Is.Null);
            Assert.That(fusion.Mode, Is.EqualTo(InteractionMode.Fusion));
            Assert.That(fusion.SelectedReservePieceId, Is.Null);
            Assert.That(reserve.Mode, Is.EqualTo(InteractionMode.ReserveDeployment));
            Assert.That(reserve.SelectedPieceId, Is.Null);
        }

        [Test]
        public void AudioResolverPreservesEventPlaybackOrder()
        {
            GameEvent[] events =
            {
                new CombatResolved(
                    new PieceId(1), new PieceId(2), 2, 1, 1, 0),
                new PieceDestroyed(new PieceId(2), new GridPosition(1, 1)),
                new PieceMoved(
                    new PieceId(1), new GridPosition(0, 1), new GridPosition(1, 1)),
                new FusionAttemptFailed(new PieceId(3), new PieceId(4)),
                new GameEnded(PlayerId.Player1, false)
            };

            Assert.That(GameEventAudioResolver.Resolve(events), Is.EqualTo(new[]
            {
                AudioCue.Battle,
                AudioCue.PieceDestroyed,
                AudioCue.Move,
                AudioCue.FusionFailed,
                AudioCue.GameEnded
            }));
        }

        [TestCase(ValidationCase.MinimumRows, "at least four")]
        [TestCase(ValidationCase.NullMovementProfiles, "non-null movement profiles")]
        [TestCase(ValidationCase.MissingInitialProfile, "is not registered")]
        [TestCase(ValidationCase.NullCellEntry, "must not contain null entries")]
        [TestCase(ValidationCase.DuplicateCell, "must be unique")]
        [TestCase(ValidationCase.OutOfRangeCell, "inside the board")]
        public void ConfigRejectsInvalidSerializedData(
            ValidationCase validationCase,
            string expectedMessage)
        {
            BoardGameConfig config = ScriptableObject.CreateInstance<BoardGameConfig>();
            try
            {
                ConfigureInvalidCase(config, validationCase);
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => config.CreateDefinition());
                Assert.That(exception.Message, Does.Contain(expectedMessage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static void ConfigureInvalidCase(
            BoardGameConfig config,
            ValidationCase validationCase)
        {
            switch (validationCase)
            {
                case ValidationCase.MinimumRows:
                    SetField(config, "rows", 3);
                    return;
                case ValidationCase.NullMovementProfiles:
                    SetField(config, "movementProfiles", null);
                    return;
                case ValidationCase.MissingInitialProfile:
                    SetField(config, "initialMovementProfileId", "missing-profile");
                    return;
                case ValidationCase.NullCellEntry:
                    SetField(config, "cellEffects", CreateCellEffectList(
                        new object[] { null }));
                    return;
                case ValidationCase.DuplicateCell:
                    object duplicate = CreateCellEffectEntry(new Vector2Int(1, 1));
                    SetField(config, "cellEffects", CreateCellEffectList(
                        duplicate,
                        CreateCellEffectEntry(new Vector2Int(1, 1))));
                    return;
                case ValidationCase.OutOfRangeCell:
                    SetField(config, "cellEffects", CreateCellEffectList(
                        CreateCellEffectEntry(new Vector2Int(99, 99))));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(validationCase));
            }
        }

        private static object CreateCellEffectEntry(Vector2Int position)
        {
            Type entryType = typeof(BoardGameConfig).GetNestedType(
                "CellEffectEntry", BindingFlags.NonPublic);
            object entry = Activator.CreateInstance(entryType, true);
            SetField(entry, "position", position);
            return entry;
        }

        private static object CreateCellEffectList(params object[] entries)
        {
            Type entryType = typeof(BoardGameConfig).GetNestedType(
                "CellEffectEntry", BindingFlags.NonPublic);
            IList list = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(entryType));
            foreach (object entry in entries)
            {
                list.Add(entry);
            }

            return list;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(target, value);
        }

        private static GameSnapshot CreateTerritorySnapshot(
            int columns,
            int rows,
            int player1Row,
            int player2Row)
        {
            List<CellDefinition> cells = new List<CellDefinition>();
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    PlayerId? owner = row == player1Row
                        ? PlayerId.Player1
                        : row == player2Row ? PlayerId.Player2 : (PlayerId?)null;
                    cells.Add(new CellDefinition(
                        new GridPosition(column, row), owner));
                }
            }

            return new GameSnapshot(
                columns, rows, Array.Empty<PieceState>(), cells,
                PlayerId.Player1, null, false);
        }

        private static GameSnapshot CreatePieceSnapshot(params PieceState[] pieces)
        {
            GameSnapshot board = CreateTerritorySnapshot(4, 6, 0, 5);
            return new GameSnapshot(
                board.Columns,
                board.Rows,
                pieces,
                board.Cells,
                PlayerId.Player1,
                null,
                false);
        }

        private static PieceView FindPieceView(
            PieceViewManager manager,
            PieceId id)
        {
            return manager.GetComponentsInChildren<PieceView>(true)
                .Single(view => view.PieceId == id);
        }

        public enum ValidationCase
        {
            MinimumRows,
            NullMovementProfiles,
            MissingInitialProfile,
            NullCellEntry,
            DuplicateCell,
            OutOfRangeCell
        }
    }
}
