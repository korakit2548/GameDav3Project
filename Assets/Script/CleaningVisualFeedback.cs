using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class CleaningVisualFeedback : MonoBehaviour
{
    [Header("Cleaning Particles")]
    [SerializeField, Min(0f)] private float dustRate = 22f;
    [SerializeField, Min(0f)] private float wipeRate = 28f;

    private ParticleSystem particles;
    private Transform activeTool;
    private Transform surface;
    private Renderer[] toolRenderers;
    private Collider surfaceCollider;
    private Collider playerCollider;
    private CleaningAction action;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];

    public void Play(CleaningAction cleaningAction, Transform tool, Transform targetSurface)
    {
        Stop();
        if (tool == null) return;
        if (particles == null) CreateParticles();
        activeTool = tool;
        surface = targetSurface;
        surfaceCollider = surface != null ? surface.GetComponentInChildren<Collider>() : null;
        toolRenderers = tool.GetComponentsInChildren<Renderer>();
        playerCollider = GetComponent<Collider>();
        action = cleaningAction;

        bool sweeping = action == CleaningAction.Sweeping;
        var main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sweeping ? 0.45f : 0.25f, sweeping ? 0.85f : 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(sweeping ? 0.07f : 0.015f, sweeping ? 0.18f : 0.045f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, sweeping ? 0.18f : 0.08f);
        main.gravityModifier = sweeping ? -0.015f : 0.15f;
        main.startColor = sweeping
            ? new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.40f, 0.32f, 0.28f), new Color(0.65f, 0.60f, 0.5f, 0.35f))
            : new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.14f, 0.13f, 0.45f), new Color(0.82f, 0.87f, 0.86f, 0.65f));
        var shape = particles.shape;
        shape.radius = sweeping ? 0.1f : 0.035f;
        var emission = particles.emission;
        emission.rateOverTime = sweeping ? dustRate : wipeRate;
        UpdatePosition();
        particles.Play();
    }

    private void CreateParticles()
    {
        var child = new GameObject("Cleaning Particles");
        child.transform.SetParent(transform, false);
        particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = 64;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.5f));
        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Resources.Load<Material>("PlayerActions/SoftDust");
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
    }

    private void LateUpdate()
    {
        if (activeTool != null) UpdatePosition();
        else if (particles != null && particles.isEmitting) Stop();
    }

    private void UpdatePosition()
    {
        Vector3 point = activeTool.position;
        if (toolRenderers.Length > 0)
        {
            Bounds bounds = toolRenderers[0].bounds;
            for (int i = 1; i < toolRenderers.Length; i++) bounds.Encapsulate(toolRenderers[i].bounds);
            point = bounds.center;
        }
        if (action == CleaningAction.WipingWall)
        {
            if (surfaceCollider != null)
            {
                Vector3 onSurface = surfaceCollider.ClosestPoint(point);
                Vector3 normal = point - onSurface;
                if (normal.sqrMagnitude < 0.0001f) normal = transform.position - onSurface;
                point = onSurface + normal.normalized * 0.025f;
            }
            else if (surface != null) point = Vector3.Lerp(point, surface.position, 0.5f);
        }
        else
        {
            float floorHeight = playerCollider != null ? playerCollider.bounds.min.y : transform.position.y;
            point.y = floorHeight + 0.06f;
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 0.5f, Vector3.down, groundHits, 1f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.55f || hit.distance >= nearest) continue;
                nearest = hit.distance;
                point.y = hit.point.y + 0.04f;
            }
        }
        particles.transform.position = point;
    }

    public void Stop()
    {
        activeTool = null;
        if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnDisable()
    {
        Stop();
        if (particles != null) particles.Clear();
    }
}
