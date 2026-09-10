using UnityEngine;

public class GameCompleteUI : MonoBehaviour
{
    [SerializeField]
    private GameObject completePanel;

    private bool isComplete;

    private void Start()
    {
        completePanel.SetActive(false);
    }

    public void ShowComplete()
    {

        isComplete = true;

        completePanel.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("GAME COMPLETE");
    }
}