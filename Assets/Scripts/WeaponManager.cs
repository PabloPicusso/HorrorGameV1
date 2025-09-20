using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;   // New Input System

public class WeaponManager : MonoBehaviour
{
    public enum weaponSelect { knife, cleaver, bat, axe, pistol, shotgun, sprayCan, bottle }

    [Header("Setup")]
    public weaponSelect chosenWeapon = weaponSelect.knife;
    public GameObject[] weapons;

    [Header("Animator")]
    public Animator anim;                         // arms / viewmodel animator
    public string weaponIdParam = "WeaponID";     // int
    public string weaponChangedParam = "WeaponChanged"; // bool (Capital W)
    public string attackTriggerParam = "Attack";  // <— used when clicking to attack
    public float changedResetDelay = 0.5f;

    [Header("Audio")]
    private AudioSource audioPlayer;              // <— added
    public AudioClip[] weaponSounds;              // <— assign per weapon index

    int weaponID = 0;

    void Start()
    {
        // Prefer child animator (common for FPS rigs)
        if (!anim) anim = GetComponentInChildren<Animator>();

        // AudioSource on the same GameObject
        audioPlayer = GetComponent<AudioSource>();

        // Clamp and activate the initial weapon
        weaponID = Mathf.Clamp((int)chosenWeapon, 0, (weapons?.Length ?? 1) - 1);
        ChangeWeapons();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // NEXT (X)
        if (kb.xKey.wasPressedThisFrame)
        {
            if (weaponID < weapons.Length - 1)
                weaponID++;
            ChangeWeapons();
        }

        // PREV (Z)
        if (kb.zKey.wasPressedThisFrame)
        {
            if (weaponID > 0)
                weaponID--;
            ChangeWeapons();
        }

        // --- ATTACK on left mouse click ---
        bool attackPressed = Mouse.current != null
            ? Mouse.current.leftButton.wasPressedThisFrame
            : Input.GetMouseButtonDown(0); // fallback if old Input is enabled

        if (attackPressed)
        {
            // Trigger the attack animation
            if (anim) anim.SetTrigger(attackTriggerParam);

            // Play the weapon's sound (clip per weapon index)
            if (audioPlayer &&
                weaponSounds != null &&
                weaponID >= 0 && weaponID < weaponSounds.Length &&
                weaponSounds[weaponID] != null)
            {
                audioPlayer.clip = weaponSounds[weaponID];
                audioPlayer.Play();
                // (You could use audioPlayer.PlayOneShot(weaponSounds[weaponID]) instead if you prefer.)
            }
        }
    }

    void ChangeWeapons()
    {
        if (weapons == null || weapons.Length == 0) return;

        // Disable all, enable selected
        foreach (var w in weapons) if (w) w.SetActive(false);
        if (weaponID >= 0 && weaponID < weapons.Length && weapons[weaponID])
            weapons[weaponID].SetActive(true);

        chosenWeapon = (weaponSelect)weaponID;

        // Drive Animator like in the course
        if (anim)
        {
            anim.SetInteger(weaponIdParam, weaponID);
            anim.SetBool(weaponChangedParam, true);
            StopAllCoroutines();
            StartCoroutine(WeaponReset());
        }

        // Adjust held-weapon local position (same values from your example)
        Move();
    }

    IEnumerator WeaponReset()
    {
        yield return new WaitForSeconds(changedResetDelay);
        if (anim) anim.SetBool(weaponChangedParam, false);
    }

    void Move()
    {
        switch (chosenWeapon)
        {
            case weaponSelect.knife:
            case weaponSelect.cleaver:
            case weaponSelect.bat:
            case weaponSelect.axe:
            case weaponSelect.pistol:
            case weaponSelect.sprayCan:
            case weaponSelect.bottle:
                transform.localPosition = new Vector3(0.02f, -0.193f, 0.66f);
                break;

            case weaponSelect.shotgun:
                transform.localPosition = new Vector3(0.02f, -0.193f, 0.46f);
                break;
        }
    }
}
