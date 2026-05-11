using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI_q : MonoBehaviour
{
    public void StartGameButton()
    {
        SceneManager.LoadScene("Dorm");
    }

    public void OptionsButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Options);
    }

    public void ContinueButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Game);
    }

    public void QuitButton()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
