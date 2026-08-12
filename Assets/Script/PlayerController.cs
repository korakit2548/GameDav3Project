using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody Rb;
    private Vector2 moveInput;
    [SerializeField]
    private float NormalSpeed = 5;
    [SerializeField]
    private float RunSpeed = 5;
    [SerializeField]
    float RotationSpeed = 1;

    private float currentSpeed;

    private Camera MainCamera;

    private IInteractable currentInteract;
    private IHoldInteractable currentHoldInteract;

    public void Awake()
    {
        MainCamera = Camera.main;

        Rb = GetComponent<Rigidbody>();
        currentSpeed = NormalSpeed;
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


        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            Rb.MoveRotation(Quaternion.Slerp(Rb.rotation, targetRotation, RotationSpeed * Time.fixedDeltaTime));
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
}
