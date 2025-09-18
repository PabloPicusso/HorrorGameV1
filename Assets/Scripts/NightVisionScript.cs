using UnityEngine;
using UnityEngine.InputSystem;   // New Input System
using UnityEngine.UI;

public class NightVisionScript : MonoBehaviour
{
    // Drag in Inspector OR leave null and we'll Find() them in Start()
    public Camera cam;
    public Image zoomBar;
    public Image batteryChunks;

    [Header("Zoom")]
    public float minFOV = 10f;       // most zoomed-in
    public float maxFOV = 60f;       // most zoomed-out
    public float zoomStep = 5f;      // per scroll "tick"

    [Header("Battery")]
    [Range(0f, 1f)] public float batteryPower = 1.0f; // 1 = full, 0 = empty
    public float drainTime = 2f;                      // seconds between drains
    public float drainAmountPerTick = 0.25f;          // amount removed each tick

    [Header("UI on Enable")]
    public bool setBarToDefaultOnEnable = false;    // match the video if you want 0.6 on show
    [Range(0f, 1f)] public float defaultFillOnEnable = 0.6f;

    void Start()
    {
        // Auto-wire like the video (optional—drag in the Inspector if you prefer)
        if (!zoomBar)
            zoomBar = GameObject.Find("ZoomBar")?.GetComponent<Image>();
        if (!batteryChunks)
            batteryChunks = GameObject.Find("BatteryChunks")?.GetComponent<Image>();
        if (!cam)
        {
            var go = GameObject.Find("FirstPersonCharacter");
            cam = go ? go.GetComponent<Camera>() : Camera.main;
        }

        // Initialize UI
        UpdateZoomBarImmediate();
        if (batteryChunks) batteryChunks.fillAmount = Mathf.Clamp01(batteryPower);

        // Start the battery drain loop
        InvokeRepeating(nameof(BatteryDrain), drainTime, drainTime);
    }

    void OnEnable()
    {
        // Match the video’s behaviour (bar = 0.6) WITHOUT changing FOV
        if (setBarToDefaultOnEnable && zoomBar)
            zoomBar.fillAmount = Mathf.Clamp01(defaultFillOnEnable);
        else
            UpdateZoomBarImmediate();
    }

    void Update()
    {
        if (!cam) return;

        // New Input System scroll (fallback to old axis if project is set to Both)
        float scrollY = Mouse.current != null ? Mouse.current.scroll.ReadValue().y
                                              : Input.GetAxis("Mouse ScrollWheel");

        if (scrollY > 0.01f && cam.fieldOfView > minFOV)
            cam.fieldOfView = Mathf.Max(minFOV, cam.fieldOfView - zoomStep);
        else if (scrollY < -0.01f && cam.fieldOfView < maxFOV)
            cam.fieldOfView = Mathf.Min(maxFOV, cam.fieldOfView + zoomStep);

        // Update UI
        UpdateZoomBarImmediate();
        if (batteryChunks) batteryChunks.fillAmount = Mathf.Clamp01(batteryPower);
    }

    void BatteryDrain()
    {
        if (batteryPower > 0f)
            batteryPower = Mathf.Max(0f, batteryPower - drainAmountPerTick);
    }

    void UpdateZoomBarImmediate()
    {
        if (!zoomBar || !cam) return;
        // Keep the same mapping the video used (FOV / 100)
        zoomBar.fillAmount = Mathf.Clamp01(cam.fieldOfView / 100f);
    }
}
