using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    public FPController controller;          // drag FPController (or leave null; we'll auto-get it)

    [Header("Step Sounds")]
    public AudioClip[] stepClips;
    [Tooltip("Distance (meters) between steps while walking.")]
    public float walkStepDistance = 2.1f;
    [Tooltip("Multiplier for step distance while sprinting (smaller = more frequent).")]
    public float sprintDistanceMul = 0.8f;   // 0.8 -> slightly faster than walk
    [Tooltip("Multiplier for step distance while crouched (bigger = less frequent).")]
    public float crouchDistanceMul = 1.4f;

    [Header("Jump / Land")]
    public AudioClip[] jumpClips;
    public AudioClip[] landClips;
    public float minLandVelocity = 3f;

    [Header("Tuning")]
    [Tooltip("Minimum horizontal speed to consider the player 'moving'.")]
    public float minMoveSpeed = 0.2f;
    [Tooltip("Random pitch range for variation.")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    CharacterController cc;
    AudioSource src;

    // distance accumulator
    Vector3 lastPos;
    float distAccum;
    bool wasGrounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        src = GetComponent<AudioSource>();
        if (!controller) controller = GetComponent<FPController>();

        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D for FPS

        lastPos = transform.position;
    }

    void Update()
    {
        if (controller == null) return;

        // ----- Landing detection -----
        if (!wasGrounded && cc.isGrounded)
        {
            if (landClips != null && landClips.Length > 0 && Mathf.Abs(cc.velocity.y) > minLandVelocity)
                PlayOne(landClips);
            // small reset so we don't immediately step on landing
            distAccum = 0f;
            lastPos = transform.position;
        }
        wasGrounded = cc.isGrounded;

        // need to be grounded to make footsteps
        if (!cc.isGrounded || stepClips == null || stepClips.Length == 0) { lastPos = transform.position; return; }

        // accumulate *horizontal* distance since last frame
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos; delta.y = 0f;
        lastPos = pos;

        // if moving fast enough, add to accumulator
        Vector3 horizVel = cc.velocity; horizVel.y = 0f;
        float speed = horizVel.magnitude;
        if (speed < minMoveSpeed) { distAccum = 0f; return; } // standing still

        distAccum += delta.magnitude;

        // current step distance based on stance
        float stepDist = walkStepDistance;
        bool sprinting = controller.sprintAction && controller.sprintAction.action.IsPressed();
        bool crouching = controller.crouchAction && controller.crouchAction.action.IsPressed();
        if (sprinting) stepDist *= sprintDistanceMul;
        if (crouching) stepDist *= crouchDistanceMul;

        // time to step?
        if (distAccum >= stepDist)
        {
            distAccum -= stepDist; // keep remainder to stay consistent across frames
            PlayOne(stepClips);
        }
    }

    // called from FPController on jump
    public void PlayJump()
    {
        if (jumpClips != null && jumpClips.Length > 0)
            PlayOne(jumpClips);
    }

    void PlayOne(AudioClip[] set)
    {
        if (set == null || set.Length == 0) return;
        int i = UnityEngine.Random.Range(0, set.Length);
        src.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        src.PlayOneShot(set[i]);
    }
}
