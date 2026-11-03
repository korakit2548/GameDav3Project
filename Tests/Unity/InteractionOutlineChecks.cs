using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class InteractionOutlineChecks
{
    private static readonly StringBuilder Report = new StringBuilder();
    private static int failures;
    public static void Run()
    {
        try { Behavior(); Proximity(); RemainingTargets(); RepeatedPress(); PromptPosition(); StainSide(); }
        catch (Exception error) { failures++; Report.AppendLine("ERROR: " + error); }
        File.WriteAllText(Environment.GetEnvironmentVariable("OUTLINE_CHECK_REPORT"), Report.ToString());
        Debug.Log(Report.ToString());
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Behavior()
    {
        var root = new GameObject("Interactable fixture");
        var first = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        first.transform.SetParent(root.transform, false);
        second.transform.SetParent(root.transform, false);
        var source = new Material(Shader.Find("Unlit/Color"));
        source.color = Color.gray;
        first.GetComponent<Renderer>().sharedMaterial = source;
        second.GetComponent<Renderer>().sharedMaterial = source;
        var label = new GameObject("E prompt");
        label.transform.SetParent(root.transform, false);
        var effect = root.AddComponent<InteractHighlight>();
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(effect, label);
        Invoke(effect, "Awake");
        Check("Outline and E prompt start hidden", !label.activeSelf && root.GetComponentsInChildren<Renderer>().Length == 2);
        effect.SetHighlight(true);
        var overlays = root.GetComponentsInChildren<Renderer>().Where(x => x.gameObject != first && x.gameObject != second).ToArray();
        Check("Interactable meshes receive an edge overlay", overlays.Length == 4);
        Check("Interaction outlines use a thin one-pixel stroke", overlays.Where(x => x.name == "Interaction Outline Edge").All(x => x.sharedMaterial.GetFloat("_OutlineWidth") <= 1f));
        Check("Highlight preserves every original material and color", first.GetComponent<Renderer>().sharedMaterial == source && second.GetComponent<Renderer>().sharedMaterial == source && source.color == Color.gray);
        Check("Outline appears together with the E prompt", label.activeSelf && overlays.All(x => x.enabled));
        Check("Outline adds no interaction colliders", root.GetComponentsInChildren<Collider>(true).Length == 2);
        Check("Both parts of a multi mesh object are outlined", overlays.Any(x => x.transform.parent == first.transform) && overlays.Any(x => x.transform.parent == second.transform));
        effect.SetHighlight(false);
        Check("Leaving interaction range hides prompt and edge overlays", !label.activeSelf && root.GetComponentsInChildren<Renderer>().Length == 2);
        effect.SetHighlight(true);
        Check("Reentering interaction range reuses overlays", root.GetComponentsInChildren<Renderer>(true).Length == 6);
        Invoke(effect, "OnDisable");
        Check("Disabling the object removes its interaction indication", !label.activeSelf && root.GetComponentsInChildren<Renderer>().Length == 2);
        Invoke(effect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(source);

        var stain = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var transparent = new Material(Shader.Find("Unlit/Transparent"));
        transparent.renderQueue = 3000;
        stain.GetComponent<Renderer>().sharedMaterial = transparent;
        var stainEffect = stain.AddComponent<InteractHighlight>();
        Invoke(stainEffect, "Awake");
        stainEffect.SetHighlight(true);
        var edge = stain.GetComponentsInChildren<Renderer>().FirstOrDefault(x => x.gameObject != stain);
        Check("Transparent stains use the texture contour instead of a rectangle", edge != null && edge.sharedMaterial.HasProperty("_Mode") && edge.sharedMaterial.GetFloat("_Mode") == 2f);
        Invoke(stainEffect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(stain);
        UnityEngine.Object.DestroyImmediate(transparent);

        var mixed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mixedMesh = UnityEngine.Object.Instantiate(mixed.GetComponent<MeshFilter>().sharedMesh);
        mixedMesh.subMeshCount = 2;
        mixedMesh.SetTriangles(new[] { 0, 1, 2 }, 0);
        mixedMesh.SetTriangles(new[] { 3, 4, 5 }, 1);
        mixed.GetComponent<MeshFilter>().sharedMesh = mixedMesh;
        var solidPart = new Material(Shader.Find("Unlit/Color"));
        var alphaPart = new Material(Shader.Find("Unlit/Transparent")) { renderQueue = 3000 };
        alphaPart.mainTexture = Texture2D.blackTexture;
        mixed.GetComponent<Renderer>().sharedMaterials = new[] { solidPart, alphaPart };
        var mixedEffect = mixed.AddComponent<InteractHighlight>();
        Invoke(mixedEffect, "Awake");
        mixedEffect.SetHighlight(true);
        var mixedEdge = mixed.GetComponentsInChildren<Renderer>().FirstOrDefault(x => x.name == "Interaction Outline Edge");
        Check("Transparent parts retain their alpha contour inside a mixed material mesh", mixedEdge != null && mixedEdge.sharedMaterials[1].GetFloat("_Mode") == 2f && mixedEdge.sharedMaterials[1].GetTexture("_BaseMap") == Texture2D.blackTexture);
        Invoke(mixedEffect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(mixed);
        UnityEngine.Object.DestroyImmediate(mixedMesh);
        UnityEngine.Object.DestroyImmediate(solidPart);
        UnityEngine.Object.DestroyImmediate(alphaPart);

        var empty = new GameObject("Prompt without renderer");
        var emptyLabel = new GameObject("E prompt");
        emptyLabel.transform.SetParent(empty.transform, false);
        var emptyEffect = empty.AddComponent<InteractHighlight>();
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(emptyEffect, emptyLabel);
        Invoke(emptyEffect, "Awake");
        emptyEffect.SetHighlight(true);
        Check("An interactable without a mesh still shows its E prompt", emptyLabel.activeSelf);
        Invoke(emptyEffect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(empty);
    }

    private static void Proximity()
    {
        var playerRoot = new GameObject("Interaction player fixture");
        var player = playerRoot.AddComponent<PlayerController>();
        playerRoot.transform.position = Vector3.back;
        var hitboxRoot = new GameObject("Existing E trigger");
        hitboxRoot.transform.SetParent(playerRoot.transform, false);
        var hitbox = hitboxRoot.AddComponent<InteractHitbox>();
        Invoke(hitbox, "Awake");
        foreach (string tag in new[] { "Trash", "Dirty", "CursedTrash" })
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = tag + " interaction fixture";
            target.tag = tag;
            var hold = target.AddComponent<OutlineHoldFixture>();
            var effect = target.AddComponent<InteractHighlight>();
            var label = new GameObject("E prompt");
            label.transform.SetParent(target.transform, false);
            typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(effect, label);
            Invoke(effect, "Awake");
            var collider = target.GetComponent<Collider>();
            player.tool = tag == "Dirty" ? CleaningTool.Broom : CleaningTool.sponge;
            Trigger(hitbox, "OnTriggerEnter", collider);
            Check(tag + " shows its outline on approach with the other tool", HasVisibleEdge(target) && !label.activeSelf && player.currentHold == null);
            player.tool = tag == "Dirty" ? CleaningTool.sponge : CleaningTool.Broom;
            hitbox.RefreshTool();
            Check(tag + " retains the outline and enables E with the correct tool", HasVisibleEdge(target) && label.activeSelf && player.currentHold == hold);
            player.tool = tag == "Dirty" ? CleaningTool.Broom : CleaningTool.sponge;
            hitbox.RefreshTool();
            Check(tag + " keeps its outline when changing away from the correct tool", HasVisibleEdge(target) && !label.activeSelf && player.currentHold == null);
            Trigger(hitbox, "OnTriggerExit", collider);
            Check(tag + " hides its outline after leaving the E trigger", !HasVisibleEdge(target) && !label.activeSelf && player.currentHold == null);
            Invoke(effect, "OnDestroy");
            UnityEngine.Object.DestroyImmediate(target);
        }
        UnityEngine.Object.DestroyImmediate(playerRoot);
    }

    private static bool HasVisibleEdge(GameObject target)
    {
        return target.GetComponentsInChildren<Renderer>().Any(x => x.name == "Interaction Outline Edge" && x.enabled);
    }
    private static void RemainingTargets()
    {
        var playerRoot = new GameObject("Nearby target selection fixture");
        var player = playerRoot.AddComponent<PlayerController>();
        playerRoot.transform.position = Vector3.back;
        player.tool = CleaningTool.sponge;
        var hitbox = playerRoot.AddComponent<InteractHitbox>();
        Invoke(hitbox, "Awake");
        var first = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var second = GameObject.CreatePrimitive(PrimitiveType.Cube);
        first.tag = second.tag = "Dirty";
        second.transform.position = Vector3.right * 0.25f;
        var firstHold = first.AddComponent<OutlineHoldFixture>();
        var secondHold = second.AddComponent<OutlineHoldFixture>();
        var firstEffect = first.AddComponent<InteractHighlight>();
        var secondEffect = second.AddComponent<InteractHighlight>();
        var firstLabel = new GameObject("First E");
        var secondLabel = new GameObject("Second E");
        firstLabel.transform.SetParent(first.transform, false);
        secondLabel.transform.SetParent(second.transform, false);
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(firstEffect, firstLabel);
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(secondEffect, secondLabel);
        Invoke(firstEffect, "Awake"); Invoke(secondEffect, "Awake");
        var firstCollider = first.GetComponent<Collider>();
        var secondCollider = second.GetComponent<Collider>();
        Trigger(hitbox, "OnTriggerEnter", firstCollider);
        Trigger(hitbox, "OnTriggerEnter", secondCollider);
        Check("Only the selected nearby object displays E", firstLabel.activeSelf != secondLabel.activeSelf && player.currentHold != null);
        Trigger(hitbox, "OnTriggerExit", secondCollider);
        Check("An E-visible wall remains usable when another wall leaves range", player.currentHold == firstHold && firstLabel.activeSelf);
        var child = new GameObject("Untagged child collider");
        child.transform.SetParent(first.transform, false);
        var childCollider = child.AddComponent<BoxCollider>();
        Trigger(hitbox, "OnTriggerEnter", childCollider);
        Check("Child colliders use the wall owner's cleaning tag", player.currentHold == firstHold && firstLabel.activeSelf);
        Trigger(hitbox, "OnTriggerExit", firstCollider);
        Check("A wall stays usable while one of its colliders remains nearby", player.currentHold == firstHold && firstLabel.activeSelf);
        Trigger(hitbox, "OnTriggerExit", childCollider);
        Check("The prompt clears after every collider leaves range", player.currentHold == null && !firstLabel.activeSelf);
        Trigger(hitbox, "OnTriggerEnter", firstCollider);
        Trigger(hitbox, "OnTriggerEnter", secondCollider);
        bool firstWasSelected = player.currentHold == firstHold;
        (firstWasSelected ? first : second).SetActive(false);
        Invoke(hitbox, "FixedUpdate");
        Check("A remaining wall is selected when the previous target is disabled", player.currentHold == (firstWasSelected ? secondHold : firstHold) && (firstWasSelected ? secondLabel : firstLabel).activeSelf);
        Invoke(firstEffect, "OnDestroy"); Invoke(secondEffect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(first);
        UnityEngine.Object.DestroyImmediate(second);
        UnityEngine.Object.DestroyImmediate(playerRoot);
    }
    private static void PromptPosition()
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.transform.position = new Vector3(4, 2, 7);
        var canvas = new GameObject("World E canvas", typeof(RectTransform));
        canvas.transform.SetParent(target.transform, false);
        var anchor = new GameObject("E and progress anchor", typeof(RectTransform), typeof(Billboard));
        anchor.transform.SetParent(canvas.transform, false);
        anchor.transform.localPosition = new Vector3(5, 20, 0);
        var effect = target.AddComponent<InteractHighlight>();
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(effect, canvas);
        Invoke(effect, "Awake");
        effect.SetHighlight(true);
        Vector3 expected = target.GetComponent<Renderer>().bounds.center;
        bool centered;
        if (Camera.main != null)
        {
            var actualViewport = Camera.main.WorldToViewportPoint(anchor.transform.position);
            var expectedViewport = Camera.main.WorldToViewportPoint(expected);
            centered = Vector2.Distance(actualViewport, expectedViewport) < 0.001f;
        }
        else centered = Vector3.Distance(anchor.transform.position, expected) < 0.001f;
        Check("The world E icon is centered on its interactable geometry", centered);
        Invoke(effect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(target);
    }
    private static void RepeatedPress()
    {
        var playerRoot = new GameObject("Repeated press fixture");
        var player = playerRoot.AddComponent<PlayerController>();
        var hitbox = playerRoot.AddComponent<InteractHitbox>();
        Invoke(hitbox, "Awake");
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var press = target.AddComponent<OutlinePressFixture>();
        Trigger(hitbox, "OnTriggerEnter", target.GetComponent<Collider>());
        player.currentInteract.Interact();
        player.currentInteract = null;
        Invoke(hitbox, "FixedUpdate");
        Check("A reusable E interaction stays selected after its first press", press.presses == 1 && player.currentInteract == press);
        Trigger(hitbox, "OnTriggerExit", target.GetComponent<Collider>());
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(playerRoot);
    }
    private static void StainSide()
    {
        var playerRoot = new GameObject("Painted side fixture");
        var player = playerRoot.AddComponent<PlayerController>();
        player.tool = CleaningTool.sponge;
        playerRoot.transform.position = Vector3.back;
        var hitbox = playerRoot.AddComponent<InteractHitbox>();
        Invoke(hitbox, "Awake");
        var target = GameObject.CreatePrimitive(PrimitiveType.Quad);
        target.tag = "Dirty";
        var hold = target.AddComponent<OutlineHoldFixture>();
        var effect = target.AddComponent<InteractHighlight>();
        var label = new GameObject("E prompt");
        label.transform.SetParent(target.transform, false);
        typeof(InteractHighlight).GetField("highlightLabel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(effect, label);
        Invoke(effect, "Awake");
        Trigger(hitbox, "OnTriggerEnter", target.GetComponent<Collider>());
        Check("E is available on the painted face of a wall stain", player.currentHold == hold && label.activeSelf);
        playerRoot.transform.position = Vector3.forward;
        Invoke(hitbox, "FixedUpdate");
        Check("The opposite face has no usable E or outline", player.currentHold == null && !label.activeSelf && !HasVisibleEdge(target));
        playerRoot.transform.position = Vector3.back;
        Invoke(hitbox, "FixedUpdate");
        Check("Returning to the painted face restores E without changing tools", player.currentHold == hold && label.activeSelf);
        Invoke(effect, "OnDestroy");
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(playerRoot);
    }
    private static void Trigger(object hitbox, string method, Collider collider)
    {
        hitbox.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hitbox, new object[] { collider });
    }
    private static void Invoke(object target, string method)
    {
        var info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info != null) info.Invoke(target, null);
    }
    private static void Check(string name, bool result) { Report.AppendLine((result ? "PASS: " : "FAIL: ") + name); if (!result) failures++; }
}
