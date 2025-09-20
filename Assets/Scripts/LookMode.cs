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
    public AudioClip nvOnClip, nvOffClip, flOnClip, flOffClip; // nvOnClip WON'T play on first boot (we boot silently later too)
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [SerializeField] float toggleCooldown = 0.25f;

    [Header("Night Vision Boot (first time only)")]
    [Tooltip("Played only the FIRST time you turn NV on. After the clip finishes (or delay elapses), NV turns on.")]
    public AudioClip nvBootClip;               // optional 4s boot SFX
    [Tooltip("Used if no clip is assigned.")]
    public float nvBootDelaySeconds = 4f;

    // ---- internals ----
    bool nightVisionOn = false;
    bool flashLightOn = false;
    float nextNVToggleAllowed = 0f;
    float nextFLToggleAllowed = 0f;

    bool nvBooting = false;    // currently playing the boot SFX
    bool nvBootDone = false;   // we've already done the first boot this session

    AudioSource audioSrc;
    Camera cam;
    NightVisionScript nvUI;
    FlashLightScript flUI;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (!volume) volume = GetComponent<PostProcessVolume>();
        if (volume && standard) volume.profile = standard;

        if (nightVisionOverlay)
        {
            nightVisionOverlay.SetActive(false);
            nvUI = nightVisionOverlay.GetComponent<NightVisionScript>();
        }

        if (flashlightOverlay)
        {
            flashlightOverlay.SetActive(false);
            flUI = flashlightOverlay.GetComponent<FlashLightScript>();
        }

        if (!flashLight)
        {
            var go = GameObject.Find("FlashLight");
            if (go) flashLight = go.GetComponent<Light>();
        }
        if (flashLight) flashLight.enabled = false;

        audioSrc = GetComponent<AudioSource>();
        if (!audioSrc) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;

        // known startup state
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
            // ignore further presses during boot SFX
            if (!nvBooting)
            {
                if (nightVisionOn)
                {
                    // Turn OFF immediately
                    ForceNightVision(false, playSfx: true);
                }
                else
                {
                    // Turning ON
                    if (!nvBootDone)
                    {
                        // First-time boot flow: play SFX, wait, then enable NV once
                        StartCoroutine(NightVisionFirstBootRoutine());
                    }
                    else
                    {
                        // Subsequent turns on: no sound (as requested)
                        ForceNightVision(true, playSfx: false);
                    }
                }
            }

            nextNVToggleAllowed = Time.unscaledTime + toggleCooldown;
        }

        // Toggle Flashlight (F)
        if (kb.fKey.wasPressedThisFrame && Time.unscaledTime >= nextFLToggleAllowed)
        {
            // allow flashlight toggles even while NV booting (change to `if(!nvBooting && ... )` if you want to lock it)
            if (flashLightOn) ForceFlashlight(false, playSfx: true);
            else ForceFlashlight(true, playSfx: true);

            nextFLToggleAllowed = Time.unscaledTime + toggleCooldown;
        }

        // Auto shutoff on empty batteries
        if (nightVisionOn && nvUI != null && nvUI.batteryPower <= 0f)
            ForceNightVision(false, playSfx: true);

        if (flashLightOn && flUI != null && flUI.batteryPower <= 0f)
            ForceFlashlight(false, playSfx: true);
    }

    System.Collections.IEnumerator NightVisionFirstBootRoutine()
    {
        nvBooting = true;

        // (Optionally) turn the flashlight off during boot
        if (disableFlashlightWhenNVOn && flashLightOn)
            ForceFlashlight(false, playSfx: false);

        // Play boot SFX or wait a fixed delay
        float wait = nvBootClip ? nvBootClip.length : nvBootDelaySeconds;
        if (nvBootClip) { audioSrc.Stop(); audioSrc.PlayOneShot(nvBootClip, sfxVolume); }
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);

        // If battery ran out while waiting, just bail
        if (nvUI != null && nvUI.batteryPower <= 0f)
        {
            nvBooting = false;
            nvBootDone = true; // still count as booted so we don't replay on next attempt
            yield break;
        }

        // Finally enable NV (no extra 'on' sound)
        ForceNightVision(true, playSfx: false);

        nvBooting = false;
        nvBootDone = true; // boot only once per game session
    }

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
