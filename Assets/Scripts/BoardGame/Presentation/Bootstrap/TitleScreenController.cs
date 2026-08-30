using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Bootstrap
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        private Button startButton;

        private void Awake()
        {
            startButton = GetComponentInChildren<Button>(true);
            if (startButton == null)
            {
                Debug.LogError("[TitleScreenController] ゲーム開始ボタンが見つかりません。");
                return;
            }

            startButton.onClick.AddListener(StartGame);
        }

        public void StartGame()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }
        }
    }
}
