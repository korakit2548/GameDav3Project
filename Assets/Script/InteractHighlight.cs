using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractHighlight : MonoBehaviour
{
    [SerializeField, ColorUsage(true, true)]
    private Color outlineColor = new Color(3f, 2.7f, 1.8f, 1f);

    [SerializeField, Range(0.5f, 6f)]
    private float outlineWidthPixels = 1f;

    [SerializeField]
    private GameObject highlightLabel;

    private readonly List<GameObject> overlays = new List<GameObject>();
    private readonly List<Material> outlineMaterials = new List<Material>();
    private readonly List<MeshRenderer> targetRenderers = new List<MeshRenderer>();
    private Transform promptAnchor;
    private bool initialized;

    private void Awake()
    {
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        SetHighlight(active, active);
    }

    public void SetHighlight(bool active, bool showLabel)
    {
        if (active && !initialized) CreateOutlines();
        foreach (var overlay in overlays)
            if (overlay != null) overlay.SetActive(active);
        if (showLabel) CenterPrompt();
        if (highlightLabel != null) highlightLabel.SetActive(showLabel);
    }

    private void LateUpdate()
    {
        if (highlightLabel != null && highlightLabel.activeInHierarchy) CenterPrompt();
    }

    private void CenterPrompt()
    {
        if (highlightLabel == null) return;
        if (promptAnchor == null)
        {
            var billboard = highlightLabel.GetComponentInChildren<Billboard>(true);
            promptAnchor = billboard != null ? billboard.transform : highlightLabel.transform;
        }
        Bounds bounds = default;
        bool found = false;
        foreach (var renderer in targetRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!found) bounds = renderer.bounds;
            else bounds.Encapsulate(renderer.bounds);
            found = true;
        }
        if (!found) return;
        Vector3 position = bounds.center;
        if (Camera.main != null)
        {
            Vector3 toCamera = Camera.main.transform.position - position;
            float distance = toCamera.magnitude;
            if (distance > 0.001f)
            {
                Vector3 direction = toCamera / distance;
                Vector3 absolute = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
                float front = Mathf.Min(Vector3.Dot(bounds.extents, absolute) + 0.02f, distance * 0.5f);
                position += direction * front;
            }
        }
        promptAnchor.position = position;
    }

    private void CreateOutlines()
    {
        initialized = true;
        var shader = Resources.Load<Shader>("InteractionOutline/InteractionOutline");
        if (shader == null) return;

        // Snapshot the original renderers before adding children. UI labels are excluded.
        foreach (var source in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (highlightLabel != null && source.transform.IsChildOf(highlightLabel.transform)) continue;
            var filter = source.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            targetRenderers.Add(source);
            var originals = source.sharedMaterials;
            bool transparent = originals.Length > 0;
            foreach (var original in originals)
                transparent &= original != null && original.renderQueue >= 3000;
            if (!transparent) CreateOverlay(source, filter.sharedMesh, originals, shader, 0);
            CreateOverlay(source, filter.sharedMesh, originals, shader, transparent ? 2 : 1);
        }
    }

    private void CreateOverlay(MeshRenderer source, Mesh mesh, Material[] originals, Shader shader, int mode)
    {
        var overlay = new GameObject(mode == 0 ? "Interaction Outline Mask" : "Interaction Outline Edge");
        overlay.layer = source.gameObject.layer;
        overlay.transform.SetParent(source.transform, false);
        overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = overlay.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.enabled = source.enabled;
        var materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++)
        {
            Bounds bounds = mesh.GetSubMesh(i).bounds;
            if (bounds.size.sqrMagnitude < 0.000001f) bounds = mesh.bounds;
            Vector3 size = bounds.size;
            bool planar = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) < Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * 0.02f;
            var original = originals.Length > 0 ? originals[Mathf.Min(i, originals.Length - 1)] : null;
            bool transparentPart = original != null && original.renderQueue >= 3000;
            int edgeMode = mode == 1 && transparentPart ? 2 : mode;
            var material = new Material(shader) { name = "Interaction edge", hideFlags = HideFlags.DontSave };
            material.SetFloat("_Mode", edgeMode);
            material.SetColor("_OutlineColor", outlineColor);
            material.SetFloat("_OutlineWidth", outlineWidthPixels);
            material.SetFloat("_Planar", planar ? 1 : 0);
            material.SetVector("_MeshCenter", bounds.center);
            material.SetFloat("_ColorMask", mode == 0 ? 0 : 15);
            material.SetFloat("_StencilComp", (float)(edgeMode == 1 ? CompareFunction.NotEqual : CompareFunction.Always));
            material.SetFloat("_StencilPass", (float)(mode == 0 ? StencilOp.Replace : StencilOp.Keep));
            material.SetFloat("_StencilWriteMask", mode == 0 ? 128 : 0);
            material.SetFloat("_Cull", (float)(edgeMode == 1 && !planar ? CullMode.Front : CullMode.Off));
            material.renderQueue = mode == 0 ? 3100 : 3101;
            bool alphaClip = transparentPart || original != null && original.HasProperty("_AlphaClip") && original.GetFloat("_AlphaClip") > 0.5f;
            if (alphaClip && original != null)
            {
                string textureProperty = original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                if (original.HasProperty(textureProperty))
                {
                    material.SetTexture("_BaseMap", original.GetTexture(textureProperty));
                    material.SetTextureScale("_BaseMap", original.GetTextureScale(textureProperty));
                    material.SetTextureOffset("_BaseMap", original.GetTextureOffset(textureProperty));
                }
            }
            materials[i] = material;
            outlineMaterials.Add(material);
        }
        renderer.sharedMaterials = materials;
        overlays.Add(overlay);
    }

    private void OnDisable() { SetHighlight(false); }

    private void OnDestroy()
    {
        foreach (var material in outlineMaterials)
        {
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
        outlineMaterials.Clear();
    }
}
