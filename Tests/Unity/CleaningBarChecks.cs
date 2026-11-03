using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CleaningBarChecks
{
    public static void Run()
    {
        var report = new StringBuilder();
        int failures = 0;
        Action<string, bool> check = (name, result) => { report.AppendLine((result ? "PASS: " : "FAIL: ") + name); if (!result) failures++; };
        try
        {
            var root = new GameObject("Wall progress fixture", typeof(RectTransform), typeof(Slider));
            ((RectTransform)root.transform).sizeDelta = new Vector2(100, 100);
            var fillRoot = new GameObject("Green fill", typeof(RectTransform), typeof(Image));
            fillRoot.transform.SetParent(root.transform, false);
            var image = fillRoot.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            image.fillMethod = Image.FillMethod.Radial360;
            image.color = Color.green;
            var fillRect = (RectTransform)fillRoot.transform;
            fillRect.sizeDelta = Vector2.zero;
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.SetValueWithoutNotify(0);
            var bar = root.AddComponent<CleaningBar>();
            typeof(CleaningBar).GetField("fillImage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(bar, image);
            bar.SetProgress(0.5f);
            Canvas.ForceUpdateCanvases();
            check("Wall fill uses the same Filled mode as trash", image.type == Image.Type.Filled);
            check("Half cleaning progress reaches the green image", Mathf.Approximately(image.fillAmount, 0.5f));
            check("The Slider stays synchronized with cleaning progress", Mathf.Approximately(slider.normalizedValue, 0.5f));
            check("The green fill retains drawable width", fillRect.rect.width > 1f);
            check("Green color and radial style are preserved", image.color == Color.green && image.fillMethod == Image.FillMethod.Radial360);
            root.SetActive(false);
            bar.SetProgress(0.75f);
            root.SetActive(true);
            check("Showing the E prompt preserves current progress", Mathf.Approximately(image.fillAmount, 0.75f));
            bar.SetProgress(0);
            check("Releasing E clears the green fill", Mathf.Approximately(image.fillAmount, 0) && Mathf.Approximately(slider.normalizedValue, 0));
            bar.SetProgress(2);
            check("Completed progress is clamped to full", Mathf.Approximately(image.fillAmount, 1) && Mathf.Approximately(slider.normalizedValue, 1));
            UnityEngine.Object.DestroyImmediate(root);
        }
        catch (Exception error) { failures++; report.AppendLine("ERROR: " + error); }
        File.WriteAllText(Environment.GetEnvironmentVariable("CLEANING_BAR_CHECK_REPORT"), report.ToString());
        Debug.Log(report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }
}
