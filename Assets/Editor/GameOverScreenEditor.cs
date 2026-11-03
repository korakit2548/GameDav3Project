using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameOverScreenEditor
{
    private const string PrefabPath = "Assets/Resources/GameOver/GameOverScreen.prefab";

    [MenuItem("Tools/Game UI/Edit Game Over Screen")]
    public static void Open()
    {
        AssetDatabase.OpenAsset(EnsurePrefab());
    }

    public static GameObject EnsurePrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;
        GameObject owner = null;
        GameObject screen = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            owner = new GameObject("Game Over Template Builder");
            var sequence = owner.AddComponent<GameOverSequence>();
            var type = typeof(GameOverSequence);
            type.GetMethod("CreateDefaultScreen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sequence, null);
            screen = (GameObject)type.GetField("screen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sequence);
            var view = screen.AddComponent<GameOverScreenView>();
            var settings = new SerializedObject(view);
            var background = screen.transform.Find("Game Over Fade").GetComponent<Image>();
            var content = screen.transform.Find("Game Over Menu").GetComponent<CanvasGroup>();
            settings.FindProperty("fadeImage").objectReferenceValue = background;
            settings.FindProperty("menuGroup").objectReferenceValue = content;
            settings.FindProperty("restartButton").objectReferenceValue = content.transform.Find("Restart Button").GetComponent<Button>();
            settings.FindProperty("mainMenuButton").objectReferenceValue = content.transform.Find("Main Menu Button").GetComponent<Button>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            // Keep the design visible in Prefab Mode. Runtime applies its own fade.
            background.color = Color.black;
            content.alpha = 1f;
            content.interactable = true;
            content.blocksRaycasts = true;
            var result = PrefabUtility.SaveAsPrefabAsset(screen, PrefabPath);
            if (result == null || !result.GetComponent<GameOverScreenView>().IsConfigured) throw new InvalidOperationException("Game Over prefab references are incomplete");
            return result;
        }
        finally
        {
            if (screen != null) Object.DestroyImmediate(screen);
            if (owner != null) Object.DestroyImmediate(owner);
        }
    }
}
