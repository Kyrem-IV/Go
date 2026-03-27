using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class StartScreen : MonoBehaviour
{
    public void StartClick()
    {
        SceneManager.LoadSceneAsync("GameScene");
    }

    public void RulesClick()
    {
        SceneManager.LoadSceneAsync("RulesScene");
    }

    public void BackClick()
    {
        SceneManager.LoadSceneAsync("StartScene");
    }

    public void ExitClick()
    {
        Application.Quit();
    }
}
