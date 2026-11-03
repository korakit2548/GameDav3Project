
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    private void Awake()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects()) MenuButtonHoverAudio.Bind(root);
        if (GetComponent<MainMenuAtmosphereMotion>() == null)
            gameObject.AddComponent<MainMenuAtmosphereMotion>();
        if (GetComponent<MainMenuWind>() == null)
            gameObject.AddComponent<MainMenuWind>();
    }

    private void OnEnable() { ShowCursor(); }

    private void LateUpdate() { ShowCursor(); }

    private static void ShowCursor()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    public void Play()
    {
        MenuFeedbackAudio.PlayClick();
        PrepareSceneChange();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenTutorial()
    {
        MenuFeedbackAudio.PlayClick();
        PrepareSceneChange();
        Time.timeScale = 1f;
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void Quit()
    {
        Debug.Log("Quit Game");
        MenuFeedbackAudio.QuitAfterClick();
    }

    private static void PrepareSceneChange()
    {
#if UNITY_EDITOR
        // Release the Volume inspector before its scene is unloaded (Unity 6.5).
        var selected = UnityEditor.Selection.activeGameObject;
        if (selected != null && selected.GetComponent<UnityEngine.Rendering.Volume>() != null)
        {
            UnityEditor.Selection.activeObject = null;
            UnityEditor.ActiveEditorTracker.sharedTracker.ForceRebuild();
        }
#endif
    }
}
