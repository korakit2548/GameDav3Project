using UnityEngine;

public class GameCompleteUI : MonoBehaviour
{
    [SerializeField]
    private GameObject completePanel;

    private bool isComplete;

    private void Start()
    {
        if (!isComplete && completePanel != null) completePanel.SetActive(false);
    }

    public void ShowComplete()
    {
        if (isComplete) return;
        isComplete = true;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (completePanel != null) completePanel.SetActive(true);

        // Objectives and the completion buttons share PlayerCam's gameplay state.
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var playerCam = root.GetComponentInChildren<PlayerCam>(true);
            if (playerCam == null) continue;
            playerCam.LevelComplete();
            return;
        }

        MenuFeedbackAudio.PlayLevelComplete();
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("GAME COMPLETE");
    }
}
