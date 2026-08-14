using GCCC.BoardGame.Presentation.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GCCC.BoardGame.Presentation.Input
{
    public sealed class BoardInputController : MonoBehaviour
    {
        private BoardView boardView;
        private GameHudView hudView;
        private GameCoordinator coordinator;

        public void Initialize(
            BoardView board,
            GameHudView hud,
            GameCoordinator gameCoordinator)
        {
            boardView = board;
            hudView = hud;
            coordinator = gameCoordinator;
        }

        private void Update()
        {
            if (coordinator == null || !TryGetPointerPress(out Vector2 screenPosition) ||
                hudView.IsPointerOverControl(screenPosition))
            {
                return;
            }

            if (boardView.TryScreenToCell(screenPosition, out Core.Model.GridPosition cell))
            {
                coordinator.HandleCellClick(cell);
            }
        }

        private static bool TryGetPointerPress(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }
    }
}
