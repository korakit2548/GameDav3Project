using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerFootsteps : MonoBehaviour
{
    [Header("Step Timing")]
    [SerializeField, Min(0.1f)] private float stepDistance = 1.4f;
    [SerializeField, Min(0f)] private float minimumStepInterval = 0.18f;
    [SerializeField, Min(0f)] private float carryingStepInterval = 0.65f;
    [SerializeField, Min(1f)] private float teleportDistance = 3f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.65f;
    [SerializeField, Range(0.5f, 1f)] private float carryingPitchMultiplier = 0.9f;

    [Header("Faint Shoe Prints")]
    [SerializeField] private Material footprintMaterial;
    [SerializeField] private Color footprintColor = new Color(0.015f, 0.015f, 0.015f, 0.45f);
    [SerializeField] private Vector2 footprintSize = new Vector2(0.14f, 0.27f);
    [SerializeField, Min(0f)] private float footSeparation = 0.1f;
    [SerializeField, Min(0.1f)] private float footprintLifetime = 2f;
    [SerializeField, Min(0.01f)] private float footprintFadeDuration = 2f;
    [SerializeField, Range(2, 128)] private int maxFootprints = 64;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0f)] private float groundContactTolerance = 0.03f;

    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private readonly System.Random audioRandom = new System.Random();
    private Rigidbody body;
    private PlayerCarrySystem carrySystem;
    private Collider bodyCollider;
    private AudioSource audioSource;
    private FootprintTrail trail;
    private Vector3 previousPosition;
    private float travelledDistance;
    private float timeSinceStep;
    private bool isMoving;
    private bool nextLeftFoot = true;
    private int previousClip = -1;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        carrySystem = GetComponent<PlayerCarrySystem>();
        bodyCollider = GetComponent<Collider>();
        previousPosition = transform.position;
        if (footstepClips == null || footstepClips.Length == 0)
            footstepClips = Resources.LoadAll<AudioClip>("PlayerFootsteps/Audio");
        if (footprintMaterial == null)
            footprintMaterial = Resources.Load<Material>("PlayerFootsteps/Shoeprint");

        var audioObject = new GameObject("Footstep Audio");
        audioObject.transform.SetParent(transform, false);
        audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0.65f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 18f;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        travelledDistance = 0f;
    }

    public void SetMoving(bool moving) { isMoving = moving; }

    private void LateUpdate() { UpdateFootsteps(Time.deltaTime); }

    private void UpdateFootsteps(float deltaTime)
    {
        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;
        if (deltaTime <= 0f)
        {
            travelledDistance = 0f;
            return;
        }
        timeSinceStep += deltaTime;
        if (!isMoving || displacement.sqrMagnitude > teleportDistance * teleportDistance)
        {
            travelledDistance = 0f;
            return;
        }
        if (displacement.sqrMagnitude < 0.000001f) return;
        if (!TryGetGround(transform.position, out _))
        {
            travelledDistance = 0f;
            return;
        }
        travelledDistance += displacement.magnitude;
        float spacing = Mathf.Max(0.1f, stepDistance);
        float stepInterval = minimumStepInterval;
        if (carrySystem != null && carrySystem.HasItem())
            stepInterval = Mathf.Max(stepInterval, carryingStepInterval);
        if (travelledDistance < spacing || timeSinceStep < stepInterval) return;
        travelledDistance %= spacing;
        if (EmitStep())
        {
            nextLeftFoot = !nextLeftFoot;
            timeSinceStep = 0f;
        }
    }

    private bool EmitStep()
    {
        bool firstFoot = StampFoot(nextLeftFoot, 0f);
        bool secondFoot = StampFoot(!nextLeftFoot, 0.1f);
        if (!firstFoot && !secondFoot) return false;
        if (footstepClips != null && footstepClips.Length > 0)
        {
            int clipIndex = audioRandom.Next(footstepClips.Length);
            if (footstepClips.Length > 1 && clipIndex == previousClip)
                clipIndex = (clipIndex + 1) % footstepClips.Length;
            previousClip = clipIndex;
            audioSource.pitch = 0.94f + (float)audioRandom.NextDouble() * 0.12f;
            if (carrySystem != null && carrySystem.HasItem())
                audioSource.pitch *= carryingPitchMultiplier;
            if (footstepClips[clipIndex] != null)
                audioSource.PlayOneShot(footstepClips[clipIndex], footstepVolume);
        }
        return true;
    }

    private bool StampFoot(bool leftFoot, float trailingOffset)
    {
        float side = leftFoot ? -1f : 1f;
        Vector3 footPosition = transform.position + transform.right * (side * footSeparation)
            - transform.forward * (0.1f + trailingOffset);
        if (!TryGetGround(footPosition, out var ground)) return false;
        if (footprintMaterial != null)
        {
            if (trail == null)
            {
                var trailObject = new GameObject("Footprint Trail - " + name);
                SceneManager.MoveGameObjectToScene(trailObject, gameObject.scene);
                trail = trailObject.AddComponent<FootprintTrail>();
                trail.Initialize(footprintMaterial, maxFootprints, footprintLifetime,
                    footprintFadeDuration, footprintColor, footprintSize);
            }
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, ground.normal).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, ground.normal).normalized;
            trail.Stamp(ground.point + ground.normal * 0.008f,
                Quaternion.LookRotation(ground.normal, forward), leftFoot);
        }
        return true;
    }

    private bool TryGetGround(Vector3 position, out RaycastHit ground)
    {
        float footHeight = bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;
        Vector3 origin = new Vector3(position.x, footHeight + 0.2f, position.z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            0.2f + Mathf.Max(0f, groundContactTolerance),
            groundLayers, QueryTriggerInteraction.Ignore);
        ground = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.attachedRigidbody == body
                || Vector3.Dot(hit.normal, Vector3.up) < 0.55f || hit.distance >= nearest) continue;
            ground = hit;
            nearest = hit.distance;
        }
        return nearest < float.PositiveInfinity;
    }

    private void OnDisable()
    {
        isMoving = false;
        travelledDistance = 0f;
        if (audioSource != null) audioSource.Stop();
    }

    private void OnDestroy()
    {
        if (trail == null) return;
        if (Application.isPlaying) Destroy(trail.gameObject);
        else DestroyImmediate(trail.gameObject);
    }
}
