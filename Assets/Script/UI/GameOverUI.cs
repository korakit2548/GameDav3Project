using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverPanel;

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        var playerCam = FindAnyObjectByType<PlayerCam>();
        if (playerCam != null) playerCam.GameOver();
        else
        {
            var sequence = GetComponent<GameOverSequence>();
            if (sequence == null) sequence = gameObject.AddComponent<GameOverSequence>();
            sequence.Show("MainMenu");
        }
    }
}
