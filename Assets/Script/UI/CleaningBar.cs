using UnityEngine;
using UnityEngine.UI;

public class CleaningBar : MonoBehaviour
{
    [SerializeField]
    private Image fillImage;

    public void SetProgress(float progress)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = progress;
    }
}