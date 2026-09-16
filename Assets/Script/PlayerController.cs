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

    [SerializeField]
    private CleaningTool currentTool = CleaningTool.sponge;

    public void Awake()
    {
        MainCamera = Camera.main;

        Rb = GetComponent<Rigidbody>();
        HurtUI.SetActive(false);
        currentSpeed = NormalSpeed;
        currentSanity = maxSanity;
        gameOverTimer = gameOverDelay;
        Debug.Log("Current Tool: " + currentTool);
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
        if (context.performed)
        {
            currentSpeed = RunSpeed;
            isRunning = true;
        }
        else if (context.canceled)
        {
            currentSpeed = NormalSpeed;
            isRunning = false;
        }
    }
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (currentHoldInteract != null)
        {
            if (context.started)
            {
                currentHoldInteract.BeginHold();
            }
            else if (context.canceled)
            {
                currentHoldInteract.EndHold();
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
        Vector3 cameraForward = MainCamera.transform.forward;
        Vector3 cameraRight = MainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;

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

    private void Animation()
    {
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
}
