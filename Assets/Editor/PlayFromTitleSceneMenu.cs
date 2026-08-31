using GCCC.BoardGame.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GCCC.BoardGame.EditorTools
{
    /// <summary>
    /// メニューから<see cref="BoardGameSceneNames.Title"/>を開いて再生を始める。
    /// タイトルからゲーム本体へのScene遷移を毎回通して確認するための入り口。
    /// </summary>
    /// <remarks>
    /// <see cref="EditorSceneManager.playModeStartScene"/>で常時差し替える方法は採らない。
    /// あの方法はTest Runnerが用意するSceneにも割り込み、テスト実行をハングさせる。
    /// ここでは人がメニューを選んだときだけ動く形にして、常駐する状態を持たせない。
    /// </remarks>
    internal static class PlayFromTitleSceneMenu
    {
        private const string MenuPath = "GCCC/タイトルから再生";

        // Scene名は BoardGameSceneNames を一次情報源として組み立てる。
        private static readonly string TitleScenePath =
            $"Assets/Scenes/{BoardGameSceneNames.Title}.unity";

        [MenuItem(MenuPath)]
        private static void PlayFromTitle()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem(MenuPath, isValidateFunction: true)]
        private static bool PlayFromTitleValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                   AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath) != null;
        }
    }
}
