using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector2 moveInput;

    [SerializeField]
    private Animator animator;
    private bool isRunning;
    private bool isWalking;

    [SerializeField]
    private InteractHitbox interactHitbox;

    [SerializeField]
    private float NormalSpeed = 5;

    [SerializeField]
    private float RunSpeed = 5;
    [SerializeField, Range(0.1f, 1f)]
    private float carrySpeedMultiplier = 0.36f;
    private float currentSpeed;

    [SerializeField]
    float RotationSpeed = 1;

    [SerializeField]
    float maxSanity = 100;
    private float currentSanity;
    public event System.Action<float> SanityChanged;
    [SerializeField, Min(0f)] private float sanityDamagePerHit = 3f;

    [SerializeField]
    private float sanityRegenAmount = 0.2f;
    [SerializeField]
    private float sanityRegenInterval = 3f;

    private float sanityRegenTimer;

    [SerializeField]
    private GameObject HurtUI;

    [SerializeField]
    private PlayerCam playerCam;
    private bool isGameOver;


    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    [SerializeField]
    private float bigShakeForce = 1f;

    [SerializeField]
    private float smallShakeForce = 0.2f;

    private Camera MainCamera;

    private IInteractable currentInteract;
    private IHoldInteractable currentHoldInteract;
    private bool isHoldingInteract;
    private bool interactionButtonHeld;

    [SerializeField]
    private CleaningTool currentTool = CleaningTool.sponge;

    private PlayerCarrySystem playerCarrySystem;
    private PlayerFootsteps footstepEffects;
    private DamageScreenFeedback damageFeedback;
    public void Awake()
    {
        MainCamera = Camera.main;

        Rb = GetComponent<Rigidbody>();
        damageFeedback = GetComponent<DamageScreenFeedback>();
        if (damageFeedback == null)
            damageFeedback = gameObject.AddComponent<DamageScreenFeedback>();
        damageFeedback.Initialize(HurtUI);
        currentSpeed = NormalSpeed;
        currentSanity = maxSanity;
        Debug.Log("Current Tool: " + currentTool);

        playerCarrySystem = GetComponent<PlayerCarrySystem>();
        var animations = GameplayAnimationSet.Load();
        if (animations != null && animator != null)
        {
            animator.runtimeAnimatorController = animations.controller;
            var cleaning = GetComponent<CleaningController>();
            if (cleaning == null) cleaning = gameObject.AddComponent<CleaningController>();
            cleaning.Configure(animator, animations);
            if (playerCarrySystem != null) playerCarrySystem.Configure(animator, animations);
        }
        footstepEffects = GetComponent<PlayerFootsteps>();
        if (footstepEffects == null)
            footstepEffects = gameObject.AddComponent<PlayerFootsteps>();
    }

    private void OnEnable()
    {
        if (interactHitbox != null) interactHitbox.RefreshTool();
    }

    private void OnDisable()
    {
        if (footstepEffects != null) footstepEffects.SetMoving(false);
        interactionButtonHeld = false;
        StopCurrentHold();
        currentHoldInteract = null;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (isGameOver) return;
        moveInput = context.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

    }
    public void OnRun(InputAction.CallbackContext context)
    {
        if (isGameOver) return;
        bool hasItem = playerCarrySystem != null && playerCarrySystem.HasItem();

        if (context.performed)
        {
            if (!hasItem)
            {
                currentSpeed = RunSpeed;
                isRunning = true;
            }
            else
            {
                currentSpeed = NormalSpeed;
                isRunning = false;
            }
        }
        else if (context.canceled)
        {
            currentSpeed = NormalSpeed;
            isRunning = false;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (isGameOver) return;
        ValidateHoldTarget();
        if (context.canceled)
        {
            interactionButtonHeld = false;
            StopCurrentHold();
            return;
        }
        if (!context.started && !context.performed) return;
        interactionButtonHeld = true;
        if (currentHoldInteract != null)
        {
            BeginCurrentHold();
            return;
        }
        if (context.performed && currentInteract != null)
        {
            currentInteract.Interact();
            currentInteract = null;
        }
    }

    private void BeginCurrentHold()
    {
        ValidateHoldTarget();
        if (currentHoldInteract == null || isHoldingInteract) return;
        isHoldingInteract = true;
        currentHoldInteract.BeginHold();
        if (Rb != null) Rb.linearVelocity = new Vector3(0f, Rb.linearVelocity.y, 0f);
    }

    private void StopCurrentHold()
    {
        ValidateHoldTarget();
        if (currentHoldInteract != null) currentHoldInteract.EndHold();
        isHoldingInteract = false;
    }

    private void ValidateHoldTarget()
    {
        if (currentHoldInteract is UnityEngine.Object target && target == null)
            currentHoldInteract = null;
        if (currentHoldInteract == null) isHoldingInteract = false;
    }

    public void OnChangeTool(InputAction.CallbackContext context)
    {
        if (isGameOver) return;
        if (!context.performed)
            return;

        StopCurrentHold();
        currentHoldInteract = null;
        if (currentTool == CleaningTool.sponge)
        {
            currentTool = CleaningTool.Broom;
        }
        else
        {
            currentTool = CleaningTool.sponge;
        }
        Debug.Log("Current Tool: " + currentTool);

        if (interactHitbox != null)
        {
            interactHitbox.RefreshTool();
        }
    }

    public CleaningTool GetCurrentTool()
    {
        return currentTool;
    }

    public void Update()
    {
        if (isGameOver) return;
        ValidateHoldTarget();
        UpdateMovementSpeed();

        Vector3 cameraForward = MainCamera.transform.forward;
        Vector3 cameraRight = MainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

        if (isHoldingInteract)
        {
            moveDirection = Vector3.zero;
            Rb.linearVelocity = new Vector3(
                0f,
                Rb.linearVelocity.y,
                0f
            );
        }
        else
        {
            Rb.linearVelocity = new Vector3(
                moveDirection.x * currentSpeed,
                Rb.linearVelocity.y,
                moveDirection.z * currentSpeed
            );
        }

        Rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, Rb.linearVelocity.y, moveDirection.z * currentSpeed);

        footstepEffects.SetMoving(moveDirection.sqrMagnitude > 0.01f &&
            !isHoldingInteract && !isGameOver && currentSanity > 0f);
        footstepEffects.SetRunning(isRunning);

        Animation();

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            Rb.MoveRotation(
                Quaternion.Slerp(
                    Rb.rotation,
                    targetRotation,
                    RotationSpeed *
                    Time.fixedDeltaTime
                )
            );
        }

        if (isHoldingInteract && currentHoldInteract != null)
        {
            currentHoldInteract.UpdateHold(Time.deltaTime);
        }

        RegenerateSanity();

        if (currentSanity <= 0f) FinishGameOver();
    }

    public void SetInteractable(IInteractable interactable)
    {
        currentInteract = interactable;
    }

    public void RemoveInteractable(IInteractable interactable)
    {
        if (currentInteract == interactable)
        {
            currentInteract = null;
        }
    }

    public void SetHoldInteractable(IHoldInteractable interactable)
    {
        ValidateHoldTarget();
        if (!ReferenceEquals(currentHoldInteract, interactable))
        {
            StopCurrentHold();
            currentHoldInteract = interactable;
        }
        if (interactionButtonHeld) BeginCurrentHold();
    }

    public void RemoveHoldInteractable(IHoldInteractable interactable)
    {
        if (ReferenceEquals(currentHoldInteract, interactable))
        {
            StopCurrentHold();
            currentHoldInteract = null;
        }
    }
    public void TakeSanityDamage(float damage)
    {
        if (isGameOver || damage <= 0f)
            return;

        float previousSanity = currentSanity;
        currentSanity -= Mathf.Max(0f, sanityDamagePerHit);

        damageFeedback.ShowDamage();
        currentSanity = Mathf.Max(0f, currentSanity);
        SanityChanged?.Invoke(currentSanity - previousSanity);

        if (currentSanity <= 0f)
        {
            currentSanity = 0f;
            FinishGameOver();
            return;
        }
        Debug.Log("Sanity: " + currentSanity + " / " + maxSanity);
    }
    private void FinishGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        currentSanity = 0f;
        interactionButtonHeld = false;
        StopCurrentHold();
        if (footstepEffects != null) footstepEffects.SetMoving(false);
        if (Rb != null) Rb.linearVelocity = Vector3.zero;
        if (playerCam == null) playerCam = FindAnyObjectByType<PlayerCam>();
        if (playerCam != null) playerCam.GameOver();
        else
        {
            var sequence = GetComponent<GameOverSequence>();
            if (sequence == null) sequence = gameObject.AddComponent<GameOverSequence>();
            sequence.Show("MainMenu");
        }
    }

    public void BigShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(bigShakeForce);
            damageFeedback.PlayShakeSound(true);
        }
    }
    public void SmallShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(smallShakeForce);
            damageFeedback.PlayShakeSound(false);
        }
    }
    public float GetSanity()
    {
        return currentSanity;
    }
    public float GetMaxSanity()
    {
        return maxSanity;
    }
    private void UpdateMovementSpeed()
    {
        bool hasItem = playerCarrySystem != null && playerCarrySystem.HasItem();

        if (hasItem)
        {
            currentSpeed = NormalSpeed * carrySpeedMultiplier;
            isRunning = false;
        }
        else
        {
            currentSpeed = isRunning ? RunSpeed : NormalSpeed;
        }
    }
    private void Animation()
    {

        if (isHoldingInteract)
        {
            animator.SetBool("IsIdle", true);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsCarrying", false);
            return;
        }


        bool hasItem = playerCarrySystem != null &&
                       playerCarrySystem.HasItem();

        if (hasItem)
        {
            animator.SetBool("IsCarrying", true);


            animator.SetBool("IsWalking", isWalking);


            animator.SetBool("IsIdle", false);
            animator.SetBool("IsRunning", false);

            return;
        }


        animator.SetBool("IsCarrying", false);

        if (isRunning && isWalking)
        {
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", false);
        }
        else if (isWalking)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsIdle", false);
        }
        else
        {
            animator.SetBool("IsIdle", true);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsWalking", false);
        }
    }

    private void RegenerateSanity()
    {
        if (currentSanity >= maxSanity || isGameOver)
            return;

        sanityRegenTimer += Time.deltaTime;

        if (sanityRegenTimer >= sanityRegenInterval)
        {
            float previousSanity = currentSanity;
            currentSanity += sanityRegenAmount;

            currentSanity = Mathf.Min(currentSanity, maxSanity);
            SanityChanged?.Invoke(currentSanity - previousSanity);

            sanityRegenTimer = 0f;

            Debug.Log("Sanity: " + currentSanity + " / " + maxSanity);
        }
    }
}
