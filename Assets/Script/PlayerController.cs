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
    private bool isIdle;
    private bool isWalking;

    [SerializeField]
    private InteractHitbox interactHitbox;

    [SerializeField]
    private float NormalSpeed = 5;

    [SerializeField]
    private float RunSpeed = 5;
    private float currentSpeed;

    [SerializeField]
    float RotationSpeed = 1;

    [SerializeField]
    float maxSanity = 100;
    private float currentSanity;

    [SerializeField]
    private float sanityRegenAmount = 1f;
    [SerializeField]
    private float sanityRegenInterval = 3f;

    private float sanityRegenTimer;

    [SerializeField] 
    private GameObject HurtUI;

    [SerializeField]
    private PlayerCam playerCam;
    private bool isGameOver;

    [SerializeField]
    private float gameOverDelay = 0.2f;
    private float gameOverTimer;

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

    [SerializeField]
    private CleaningTool currentTool = CleaningTool.sponge;

    private PlayerCarrySystem playerCarrySystem;
    public void Awake()
    {
        MainCamera = Camera.main;

        Rb = GetComponent<Rigidbody>();
        HurtUI.SetActive(false);
        currentSpeed = NormalSpeed;
        currentSanity = maxSanity;
        gameOverTimer = gameOverDelay;
        Debug.Log("Current Tool: " + currentTool);

        playerCarrySystem = GetComponent<PlayerCarrySystem>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            isWalking = true;
            isIdle = false;
        }
        else
        {
            isWalking = false;
            isIdle = true;
        }

    }
    public void OnRun(InputAction.CallbackContext context)
    {
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
        // ��Ǩ�ͺ��� Hold Interactable �١ Destroy ������������
        if (currentHoldInteract is UnityEngine.Object holdObject &&
            holdObject == null)
        {
            currentHoldInteract = null;
            isHoldingInteract = false;
        }

        if (currentHoldInteract != null)
        {
            if (context.started)
            {
                isHoldingInteract = true;
                currentHoldInteract.BeginHold();

                moveInput = Vector2.zero;
                Rb.linearVelocity = new Vector3(
                    0f,
                    Rb.linearVelocity.y,
                    0f
                );
            }
            else if (context.canceled)
            {
                currentHoldInteract.EndHold();
                isHoldingInteract = false;
            }

            return;
        }

        if (context.performed && currentInteract != null)
        {
            currentInteract.Interact();
            currentInteract = null;
        }
    }

    public void OnChangeTool(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (currentHoldInteract != null)
        {
            currentHoldInteract.EndHold();
            currentHoldInteract = null;
        }
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

        if (currentHoldInteract != null)
        {
            currentHoldInteract.UpdateHold(Time.deltaTime);
        }

        RegenerateSanity();

        if (currentSanity <= 0f && !isGameOver)
        {
            gameOverTimer -= Time.deltaTime;

            if (gameOverTimer <= 0f)
            {
                isGameOver = true;
                playerCam.GameOver();
            }
        }
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
        currentHoldInteract = interactable;
    }

    public void RemoveHoldInteractable(IHoldInteractable interactable)
    {
        if (currentHoldInteract == interactable)
        {
            currentHoldInteract = null;
        }
    }
    public void TakeSanityDamage(float damage)
    {
        if (isGameOver)
            return;

        currentSanity -= damage;

        StartCoroutine(ShowSanityDamageUI());

        if (currentSanity <= 0f)
        {
            currentSanity = 0f;
            return;
        }
        Debug.Log("Sanity: " + currentSanity + " / " + maxSanity);
    }
    public void BigShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(bigShakeForce);
        }
    }
    public void SmallShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(smallShakeForce);
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
    private IEnumerator ShowSanityDamageUI()
    {
        HurtUI.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        HurtUI.SetActive(false);
    }
    private void UpdateMovementSpeed()
    {
        bool hasItem = playerCarrySystem != null && playerCarrySystem.HasItem();

        if (hasItem)
        {
            currentSpeed = NormalSpeed;
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
            currentSanity += sanityRegenAmount;

            currentSanity = Mathf.Min(currentSanity, maxSanity);

            sanityRegenTimer = 0f;

            Debug.Log("Sanity: " + currentSanity + " / " + maxSanity);
        }
    }
}
