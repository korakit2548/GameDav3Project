
using UnityEngine;
using UnityEngine.InputSystem;

public class Fullscreen : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void ToggleFullscreen(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                Screen.SetResolution(
                    Screen.currentResolution.width,
                    Screen.currentResolution.height,
                    FullScreenMode.FullScreenWindow
                );
            }
            else
            {
                Screen.SetResolution(
                    1280,
                    720,
                    FullScreenMode.Windowed
                );
            }
        }
    }
}