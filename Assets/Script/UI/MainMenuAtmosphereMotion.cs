using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuAtmosphereMotion : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float fogOpacity = 0.18f;
    [SerializeField, Min(0f)] private float fogSpeed = 0.012f;
    [SerializeField, Range(0f, 0.1f)] private float lightVariation = 0.045f;

    private Volume volume;
    private VolumeProfile runtimeProfile;
    private Vignette vignette;
    private FilmGrain grain;
    private ColorAdjustments colors;
    private RawImage fog;
    private Texture2D fogTexture;
    private float baseExposure;
    private float baseVignette;

    private void Awake()
    {
        var camera = Camera.main;
        if (camera == null) return;
        var data = camera.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        camera.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);

        foreach (var candidate in FindObjectsByType<Volume>())
        {
            if (candidate.gameObject.scene != gameObject.scene || !candidate.isGlobal || candidate.sharedProfile == null) continue;
            if (volume == null || candidate.priority > volume.priority) volume = candidate;
        }
        if (volume != null)
        {
            data.volumeLayerMask = data.volumeLayerMask.value | (1 << volume.gameObject.layer);
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            foreach (var component in volume.sharedProfile.components)
                runtimeProfile.components.Add(Instantiate(component));
            volume.profile = runtimeProfile;
            if (!runtimeProfile.TryGet(out vignette)) vignette = runtimeProfile.Add<Vignette>(true);
            if (!runtimeProfile.TryGet(out grain)) grain = runtimeProfile.Add<FilmGrain>(true);
            if (!runtimeProfile.TryGet(out colors)) colors = runtimeProfile.Add<ColorAdjustments>(true);
            vignette.active = grain.active = colors.active = true;
            baseVignette = Mathf.Clamp(vignette.intensity.value, 0.12f, 0.24f);
            baseExposure = Mathf.Max(colors.postExposure.value, 0.2f);
            colors.contrast.Override(Mathf.Max(colors.contrast.value, -15f));
            colors.colorFilter.Override(new Color(0.9f, 0.94f, 1f));
            colors.saturation.Override(Mathf.Max(colors.saturation.value, -35f));
            if (runtimeProfile.TryGet<WhiteBalance>(out var balance)) balance.tint.Override(10f);
            if (runtimeProfile.TryGet<DepthOfField>(out var depth)) depth.active = false;
            if (runtimeProfile.TryGet<MotionBlur>(out var blur)) blur.active = false;
        }

        foreach (var canvas in FindObjectsByType<Canvas>())
        {
            if (canvas.gameObject.scene != gameObject.scene || !canvas.isRootCanvas) continue;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Camera-space UI participates in URP post-processing while retaining its anchors.
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(1f, camera.nearClipPlane + 0.1f);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
            }
            var panel = canvas.transform.Find("MainMenuPanel");
            if (panel != null && fog == null)
            {
                var picture = panel.GetComponent<Image>();
                if (picture != null)
                {
                    var color = picture.color;
                    color.a = Mathf.Max(color.a, 0.85f);
                    picture.color = color;
                }
                CreateFog(panel);
            }
        }
    }

    private void CreateFog(Transform parent)
    {
        fogTexture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        fogTexture.name = "Main Menu Drifting Fog";
        fogTexture.wrapMode = TextureWrapMode.Repeat;
        fogTexture.filterMode = FilterMode.Bilinear;
        var pixels = new Color[128 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 128; x++)
        {
            float noise = Mathf.PerlinNoise(x * 0.035f, y * 0.05f) * 0.7f
                + Mathf.PerlinNoise(x * 0.09f + 13f, y * 0.1f + 9f) * 0.3f;
            float alpha = Mathf.Pow(noise, 2.5f) * Mathf.Sin(Mathf.PI * y / 63f);
            pixels[y * 128 + x] = new Color(0.7f, 0.75f, 0.82f, alpha);
        }
        fogTexture.SetPixels(pixels);
        fogTexture.Apply(false, true);
        var child = new GameObject("Drifting Menu Fog", typeof(RectTransform), typeof(RawImage));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        child.transform.SetAsFirstSibling();
        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        fog = child.GetComponent<RawImage>();
        fog.texture = fogTexture;
        fog.raycastTarget = false;
        fog.color = new Color(1f, 1f, 1f, fogOpacity);
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float breath = Mathf.Sin(t * 0.65f);
        if (colors != null) colors.postExposure.Override(baseExposure + breath * lightVariation);
        if (vignette != null)
        {
            vignette.intensity.Override(baseVignette + Mathf.Sin(t * 0.45f) * 0.03f);
            vignette.center.Override(new Vector2(0.5f + Mathf.Sin(t * 0.19f) * 0.012f, 0.5f + Mathf.Cos(t * 0.23f) * 0.008f));
        }
        if (grain != null) grain.intensity.Override(0.3f + Mathf.PerlinNoise(t * 0.35f, 2f) * 0.1f);
        if (fog != null) fog.uvRect = new Rect(t * fogSpeed, Mathf.Sin(t * 0.12f) * 0.04f, 1f, 1f);
    }

    private void OnDestroy()
    {
        if (volume != null && runtimeProfile != null) volume.profile = null;
        if (runtimeProfile != null)
        {
            foreach (var component in runtimeProfile.components) Destroy(component);
            Destroy(runtimeProfile);
        }
        if (fog != null) Destroy(fog.gameObject);
        if (fogTexture != null) Destroy(fogTexture);
    }
}
