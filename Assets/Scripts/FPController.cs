using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    // Input (assign from .inputactions)
    [Header("Input (assign from .inputactions)")]
    public InputActionReference moveAction;    // Player/Move   (Value/Vector2)
    public InputActionReference lookAction;    // Player/Look   (Value/Vector2)
    public InputActionReference jumpAction;    // Player/Jump   (Button)
    public InputActionReference sprintAction;  // Player/Sprint (Button)
    public InputActionReference crouchAction;  // Player/Crouch (Button)
    public InputActionReference zoomAction;    // Optional (Button, e.g. RMB)

    // Refs
    [Header("Refs")]
    public Camera playerCamera;
    [Tooltip("Optional. If empty, auto-grabs Footsteps on this object.")]
    public Footsteps footsteps;

    // Move
    [Header("Move")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.5f;
    public float jumpPower = 5f;
    public float gravity = -14f;

    // Look
    [Header("Look")]
    public float lookSensitivity = 120f;  // deg/sec
    public float pitchClamp = 80f;

    // Crouch
    [Header("Crouch (hold Ctrl)")]
    public float crouchHeight = 1.2f;
    public float standHeight = 1.8f;
    public float crouchLerp = 12f;      // height/center smoothing
    public float crouchSpeedMultiplier = 0.6f;

    // Zoom
    [Header("Zoom (optional)")]
    public float normalFOV = 60f;
    public float zoomFOV = 40f;
    public float zoomLerp = 10f;

    // Private
    CharacterController cc;
    float yVel;
    float pitch;
    float targetHeight;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
        if (!footsteps) footsteps = GetComponent<Footsteps>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerCamera) playerCamera.fieldOfView = normalFOV;

        // Init CC
        standHeight = Mathf.Max(standHeight, 1.2f);
        crouchHeight = Mathf.Clamp(crouchHeight, 0.9f, standHeight - 0.2f);
        cc.height = standHeight;
        cc.center = new Vector3(cc.center.x, standHeight * 0.5f, cc.center.z);
        targetHeight = standHeight;
    }

    void OnEnable()
    {
        if (moveAction) moveAction.action.Enable();
        if (lookAction) lookAction.action.Enable();
        if (jumpAction) jumpAction.action.Enable();
        if (sprintAction) sprintAction.action.Enable();
        if (crouchAction) crouchAction.action.Enable();
        if (zoomAction) zoomAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction) moveAction.action.Disable();
        if (lookAction) lookAction.action.Disable();
        if (jumpAction) jumpAction.action.Disable();
        if (sprintAction) sprintAction.action.Disable();
        if (crouchAction) crouchAction.action.Disable();
        if (zoomAction) zoomAction.action.Disable();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- Look ----
        Vector2 look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        transform.Rotate(0f, look.x * lookSensitivity * dt, 0f);
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity * dt, -pitchClamp, pitchClamp);
        if (playerCamera) playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // ---- Crouch (held) ----
        bool isCrouching =
            (crouchAction && crouchAction.action.IsPressed()) ||
            (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed));

        // ---- Move ----
        Vector2 move = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 wish = (transform.right * move.x + transform.forward * move.y).normalized;

        float speed = (sprintAction && sprintAction.action.IsPressed()) ? sprintSpeed : walkSpeed;
        if (isCrouching) speed *= crouchSpeedMultiplier;

        if (cc.isGrounded)
        {
            yVel = -1f;
            if (jumpAction && jumpAction.action.WasPressedThisFrame() && !isCrouching)
            {
                yVel = jumpPower;
                if (footsteps) footsteps.PlayJump();   // 🔊 jump SFX
            }
        }
        else
        {
            yVel += gravity * dt;
        }

        cc.Move((wish * speed + Vector3.up * yVel) * dt);

        // ---- Crouch height/center smoothing ----
        targetHeight = isCrouching ? crouchHeight : standHeight;
        float k = 1f - Mathf.Exp(-crouchLerp * dt);     // framerate-independent lerp
        cc.height = Mathf.Lerp(cc.height, targetHeight, k);
        Vector3 c = cc.center;
        c.y = Mathf.Lerp(c.y, cc.height * 0.5f, k);
        cc.center = c;

        // ---- Zoom ----
        if (playerCamera && zoomAction)
        {
            bool zooming = zoomAction.action.IsPressed();
            float targetFov = zooming ? zoomFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-zoomLerp * dt));
        }
    }
}
