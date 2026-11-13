using UnityEngine.SceneManagement;

namespace NineMood.Core
{
    public static class SceneLoader
    {
        public const string SceneGame = "Game";
        public const string SceneMainMenu = "Main";

        public static void LoadGame() => SceneManager.LoadScene(SceneGame);
        public static void LoadMainMenu() => SceneManager.LoadScene(SceneMainMenu);
    }
}
