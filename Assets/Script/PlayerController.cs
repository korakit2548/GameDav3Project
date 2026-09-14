using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
public class PlayerController : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector2 moveInput;
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
    private GameOverUI gameOverUI;
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

    [SerializeField]
    private CleaningTool currentTool = CleaningTool.Mop;


    public void Awake()
    {
        MainCamera = Camera.main;

        Rb = GetComponent<Rigidbody>();

        currentSpeed = NormalSpeed;
        currentSanity = maxSanity;
        Debug.Log("Current Tool: " + currentTool);
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            currentSpeed = RunSpeed;
        }
        else if (context.canceled)
        {
            currentSpeed = NormalSpeed;
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
        if (currentTool == CleaningTool.Mop)
        {
            currentTool = CleaningTool.Broom;
        }
        else
        {
            currentTool = CleaningTool.Mop;
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
        Vector3 cameraForward =
            MainCamera.transform.forward;

        Vector3 cameraRight =
            MainCamera.transform.right;

        cameraForward.y = 0f;

        cameraRight.y = 0f;

        cameraForward.Normalize();

        cameraRight.Normalize();


        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;


        Rb.linearVelocity =
            new Vector3(
                moveDirection.x * currentSpeed,
                Rb.linearVelocity.y,
                moveDirection.z * currentSpeed
            );


        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =Quaternion.LookRotation(moveDirection);

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

        if (currentSanity <= 0f)
        {
            currentSanity = 0f;
            GameOver();
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
    private void GameOver()
    {
        isGameOver = true;

        Rb.linearVelocity = Vector3.zero;

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver();
        }
    }
}
