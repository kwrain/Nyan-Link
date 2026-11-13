using UnityEngine.SceneManagement;
using UnityEngine;

namespace NineMood.Core
{
    public static class SceneLoader
    {
        public const string SceneGame = "Game";
        public const string SceneMainMenu = "Main";

        public static void LoadGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneGame); }
        public static void LoadMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene(SceneMainMenu); }
    }
}
