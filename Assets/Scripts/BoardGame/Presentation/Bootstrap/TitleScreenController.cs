using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GCCC.BoardGame.Presentation.Bootstrap
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject titlePage;
        [SerializeField] private GameObject howToPage;
        [SerializeField] private Button startButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button backButton;

        private bool bindingsValid;

        private void Awake()
        {
            bindingsValid = HasRequiredBindings();
            if (!bindingsValid)
            {
                Debug.LogError(
                    "[TitleScreenController] タイトル画面の必須UI参照が不足しています。",
                    this);
                return;
            }

            startButton.onClick.AddListener(StartGame);
            howToButton.onClick.AddListener(ShowHowToPage);
            backButton.onClick.AddListener(ShowTitlePage);
            SetPageVisibility(showHowTo: false);
        }

        private void Start()
        {
            if (bindingsValid)
            {
                SelectButton(startButton);
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene(BoardGameSceneNames.Game, LoadSceneMode.Single);
        }

        private void ShowHowToPage()
        {
            SetPageVisibility(showHowTo: true);
            SelectButton(backButton);
        }

        private void ShowTitlePage()
        {
            SetPageVisibility(showHowTo: false);
            SelectButton(howToButton);
        }

        private bool HasRequiredBindings()
        {
            return titlePage != null &&
                   howToPage != null &&
                   startButton != null &&
                   howToButton != null &&
                   backButton != null;
        }

        private void SetPageVisibility(bool showHowTo)
        {
            titlePage.SetActive(!showHowTo);
            howToPage.SetActive(showHowTo);
        }

        private static void SelectButton(Button button)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (!bindingsValid)
            {
                return;
            }

            startButton.onClick.RemoveListener(StartGame);
            howToButton.onClick.RemoveListener(ShowHowToPage);
            backButton.onClick.RemoveListener(ShowTitlePage);
        }
    }
}
