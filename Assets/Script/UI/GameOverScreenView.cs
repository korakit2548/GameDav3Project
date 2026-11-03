using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverScreenView : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    public Image FadeImage => fadeImage;
    public CanvasGroup MenuGroup => menuGroup;
    public Button RestartButton => restartButton;
    public Button MainMenuButton => mainMenuButton;
    public bool IsConfigured => fadeImage != null && menuGroup != null && restartButton != null && mainMenuButton != null;
}
