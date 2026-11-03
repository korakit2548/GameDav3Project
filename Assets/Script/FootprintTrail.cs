using UnityEngine;
using UnityEngine.Rendering;

public sealed class FootprintTrail : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock properties;
    private Renderer[] marks;
    private float[] ages;
    private Mesh mesh;
    private Color color;
    private Vector2 size;
    private float lifetime;
    private float fadeDuration;
    private int nextMark;

    public void Initialize(Material material, int capacity, float duration, float fading,
        Color printColor, Vector2 printSize)
    {
        capacity = Mathf.Clamp(capacity, 2, 128);
        properties = new MaterialPropertyBlock();
        lifetime = Mathf.Max(0.1f, duration);
        fadeDuration = Mathf.Clamp(fading, 0.01f, lifetime);
        color = printColor;
        size = printSize;
        marks = new Renderer[capacity];
        ages = new float[capacity];
        mesh = new Mesh { name = "Shoe Print Quad" };
        mesh.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        for (int i = 0; i < capacity; i++)
        {
            var mark = new GameObject("Shoe Print", typeof(MeshFilter), typeof(MeshRenderer));
            mark.transform.SetParent(transform, false);
            mark.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = mark.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            marks[i] = renderer;
            mark.SetActive(false);
        }
    }

    public void Stamp(Vector3 position, Quaternion rotation, bool leftFoot)
    {
        if (marks == null) return;
        Renderer mark = marks[nextMark];
        mark.transform.SetPositionAndRotation(position, rotation);
        mark.transform.localScale = new Vector3(leftFoot ? -size.x : size.x, size.y, 1f);
        ages[nextMark] = 0f;
        SetColor(mark, color);
        mark.gameObject.SetActive(true);
        nextMark = (nextMark + 1) % marks.Length;
    }

    private void Update() { FadeFootprints(Time.deltaTime); }

    private void FadeFootprints(float deltaTime)
    {
        if (marks == null || deltaTime <= 0f) return;
        for (int i = 0; i < marks.Length; i++)
        {
            if (!marks[i].gameObject.activeSelf) continue;
            ages[i] += deltaTime;
            if (ages[i] >= lifetime)
            {
                marks[i].gameObject.SetActive(false);
                continue;
            }
            Color fading = color;
            fading.a *= Mathf.Clamp01((lifetime - ages[i]) / fadeDuration);
            SetColor(marks[i], fading);
        }
    }

    private void SetColor(Renderer mark, Color value)
    {
        properties.SetColor(BaseColorId, value);
        properties.SetColor(ColorId, value);
        mark.SetPropertyBlock(properties);
    }

    private void OnDestroy()
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh);
        else DestroyImmediate(mesh);
    }
}
