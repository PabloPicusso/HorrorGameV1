using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Camera))]
public class LookMode : MonoBehaviour
{
    [Header("Post-process")]
    public PostProcessVolume volume;
    public PostProcessProfile standard;
    public PostProcessProfile nightVision;

    [Header("Overlays / UI")]
    public GameObject nightVisionOverlay;      // has NightVisionScript
    public GameObject flashlightOverlay;       // has FlashLightScript

    [Header("Flashlight")]
    [Tooltip("Spot light object for the flashlight.")]
    public Light flashLight;                   // assign in Inspector; or we'll try to find "FlashLight"
    public bool disableFlashlightWhenNVOn = false;

    [Header("Camera")]
    public float defaultFOV = 60f;

    [Header("SFX (optional)")]
    public AudioClip nvOnClip, nvOffClip, flOnClip, flOffClip;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [SerializeField] float toggleCooldown = 0.25f;

    // ---- internals ----
    bool nightVisionOn = false;
    bool flashLightOn = false;
    float nextNVToggleAllowed = 0f;
    float nextFLToggleAllowed = 0f;

    AudioSource audioSrc;
    Camera cam;
    NightVisionScript nvUI;
    FlashLightScript flUI;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!volume) volume = GetComponent<PostProcessVolume>();
        if (volume && standard) volume.profile = standard;

        // Night vision overlay
        if (nightVisionOverlay)
        {
            nightVisionOverlay.SetActive(false);
            nvUI = nightVisionOverlay.GetComponent<NightVisionScript>();
        }

        // Flashlight overlay + script
        if (flashlightOverlay)
        {
            flashlightOverlay.SetActive(false);
            flUI = flashlightOverlay.GetComponent<FlashLightScript>();
        }

        // Flashlight Light reference
        if (!flashLight)
        {
            var go = GameObject.Find("FlashLight");    // must match your object name
            if (go) flashLight = go.GetComponent<Light>();
        }
        

        // Audio
        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        // Force known startup state (flashlight OFF, NV OFF)
        ForceNightVision(false, playSfx: false);
        ForceFlashlight(false, playSfx: false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Toggle Night Vision (N)
        if (kb.nKey.wasPressedThisFrame && Time.unscaledTime >= nextNVToggleAllowed)
        {
            ForceNightVision(!nightVisionOn, playSfx: true);
            nextNVToggleAllowed = Time.unscaledTime + toggleCooldown;
        }

        // Toggle Flashlight (F)
        if (kb.fKey.wasPressedThisFrame && Time.unscaledTime >= nextFLToggleAllowed)
        {
            ForceFlashlight(!flashLightOn, playSfx: true);
            nextFLToggleAllowed = Time.unscaledTime + toggleCooldown;
        }

        // Auto shutoff on empty batteries
        if (nightVisionOn && nvUI != null && nvUI.batteryPower <= 0f)
            ForceNightVision(false, playSfx: true);

        if (flashLightOn && flUI != null && flUI.batteryPower <= 0f)
            ForceFlashlight(false, playSfx: true);
    }

    // -------- Helpers --------
    void ForceNightVision(bool on, bool playSfx)
    {
        if (on && disableFlashlightWhenNVOn && flashLightOn)
            ForceFlashlight(false, playSfx: false);

        nightVisionOn = on;

        if (volume) volume.profile = on ? nightVision : standard;
        if (nightVisionOverlay) nightVisionOverlay.SetActive(on);
        if (!on && cam) cam.fieldOfView = defaultFOV;

        if (playSfx)
        {
            if (on && nvOnClip) { audioSrc.Stop(); audioSrc.PlayOneShot(nvOnClip, sfxVolume); }
            if (!on && nvOffClip) { audioSrc.Stop(); audioSrc.PlayOneShot(nvOffClip, sfxVolume); }
        }
    }

    void ForceFlashlight(bool on, bool playSfx)
    {
        flashLightOn = on;

        if (flashlightOverlay) flashlightOverlay.SetActive(on);
        if (flashLight) flashLight.enabled = on;

        // Start/stop drain if your FlashLightScript supports it
        if (flUI != null)
        {
            if (on) flUI.StartDrain();
            else flUI.StopDrain();
        }

        if (playSfx)
        {
            if (on && flOnClip) { audioSrc.Stop(); audioSrc.PlayOneShot(flOnClip, sfxVolume); }
            if (!on && flOffClip) { audioSrc.Stop(); audioSrc.PlayOneShot(flOffClip, sfxVolume); }
        }
    }
}
