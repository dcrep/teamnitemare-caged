using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerControllerBB : MonoBehaviour
{
    private CharacterController characterController;
    public Transform head;
    public Camera playerCamera;

    private Transform weapon;
    private Collider weaponCollider;
    //private ChainBarrier chainBarrier;

    bool attacking = false;
    bool retracting = false;

    public float moveSpeed = 5f;
    public float rotateSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -30f;

    public float swingSpeed = 250f;

    [SerializeField] private float cameraShakeDuration = 0.1f;
    [SerializeField] private float cameraShakeStrength = 0.15f;

    private Vector3 cameraOriginalPosition;
    private float rotationX;
    private float rotationY;
    private float verticalVelocity;

    private int hitCount = 0;

    public Vector3 newVelocity;

    [SerializeField] private GameObject dustAirEffectPrefab;
    [SerializeField] private GameObject explosion01EffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private float walkSoundDistance = 1.8f;
    [SerializeField] private float walkSoundVolume = 0.2f;
    [SerializeField] private AudioSource walkSoundSource;

    [SerializeField] AudioClip weaponSwingSound;
    [SerializeField] AudioClip chainHitSound;
    [SerializeField] AudioClip chainExplodeSound;
    [SerializeField] AudioClip chainSlideSound;

    [SerializeField] private List<GameObject> longChainsToBreak;
    [SerializeField] private GameObject invisibleBarrier;

    private MeleeWeapon meleeWeapon;
    private InputSystem_Actions playerControls;
    bool haveWeapon = false;
    private float walkSoundDistanceAccumulator;
    private int walkSoundIndex;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        rotationY = transform.localEulerAngles.y;
        rotationX = transform.localEulerAngles.x;
        // find child with component MeleeWeapon script
        meleeWeapon = GetComponentInChildren<MeleeWeapon>();
        weapon = meleeWeapon.transform;
        //set collider to isTrigger
        weaponCollider = weapon.GetComponent<Collider>();
        //weaponCollider.isTrigger = true;  // doesn't work to enable OnCollisionEnter calls
        // disable weapon at start
        weapon.gameObject.SetActive(false);

        if (walkSoundSource == null)
        {
            walkSoundSource = GetComponent<AudioSource>();
            if (walkSoundSource == null)
            {
                walkSoundSource = gameObject.AddComponent<AudioSource>();
            }
        }

        walkSoundSource.playOnAwake = false;
        walkSoundSource.loop = false;
        walkSoundSource.spatialBlend = 0f;
    }

    void OnEnable()
    {
        playerControls = new InputSystem_Actions();
        playerControls.Enable();
        playerControls.Player.Attack.performed += ctx => Attack();
        playerControls.Player.Jump.performed += ctx => Jump();
        MeleeWeapon.OnHit += MeleeHit;
    }

    void OnDisable()
    {
        MeleeWeapon.OnHit -= MeleeHit;
        playerControls.Player.Jump.performed -= ctx => Jump();
        playerControls.Player.Attack.performed -= ctx => Attack();
        playerControls.Disable();
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.AreLookControlsDisabled())
        {
            Vector2 lookInput = playerControls.Player.Look.ReadValue<Vector2>();
            Rotate(lookInput, playerControls.Player.Look.activeControl?.device is Mouse);
        }
        if (!GameManager.Instance.AreMoveControlsDisabled())
        {
            Vector2 moveInput = playerControls.Player.Move.ReadValue<Vector2>();
            Move(moveInput);
        }        
    }

    void FixedUpdate()
    {
        //rb.linearVelocity = transform.TransformDirection(newVelocity);
        if (attacking)
        {
            //if (weapon.rotation.eulerAngles.x >= 90f)
            if (weapon.localRotation.eulerAngles.z >= 60f)
            {
                attacking = false;
                retracting = true;
                weapon.localRotation = Quaternion.Euler(weapon.localRotation.eulerAngles.x, weapon.localRotation.eulerAngles.y, 60f);
                //weaponCollider.isTrigger = true;  // doesn't work to enable OnCollisionEnter calls
            }
            else
                weapon.Rotate(Vector3.forward * swingSpeed * Time.fixedDeltaTime, Space.Self);
        }
        else if (retracting)
        {
            //Debug.Log("Retracting, angle: " + weapon.rotation.eulerAngles.x);
            //if (weapon.rotation.eulerAngles.x <= 0f || weapon.rotation.eulerAngles.x >= 180f)
            if (weapon.localRotation.eulerAngles.z <= 0f || weapon.localRotation.eulerAngles.z >= 180f)
            {
                retracting = false;
                weapon.localRotation = Quaternion.Euler(weapon.localRotation.eulerAngles.x, weapon.localRotation.eulerAngles.y, 0);
            }
            else
                weapon.Rotate(Vector3.back * swingSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    void LateUpdate()
    {   
    }
    public void Move(Vector2 moveVector)
    {
        Vector3 move = transform.forward * moveVector.y + transform.right * moveVector.x;
        Vector3 horizontalMove = move * moveSpeed * Time.deltaTime;
        characterController.Move(horizontalMove);

        verticalVelocity += gravity * Time.deltaTime;
        characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);

        UpdateWalkSound(horizontalMove, true);  //characterController.isGrounded);
    }

    void UpdateWalkSound(Vector3 horizontalMove, bool wasGroundedThisFrame)
    {
        if (!wasGroundedThisFrame || walkSounds == null || walkSounds.Length == 0)
        {
            if (!wasGroundedThisFrame)
            {
                walkSoundDistanceAccumulator = 0f;
            }

            return;
        }

        float moveDistance = horizontalMove.magnitude;
        if (moveDistance <= 0.001f)
        {
            walkSoundDistanceAccumulator = 0f;
            return;
        }

        walkSoundDistanceAccumulator += moveDistance;

        float stepDistance = Mathf.Max(0.05f, walkSoundDistance);
        while (walkSoundDistanceAccumulator >= stepDistance)
        {
            walkSoundDistanceAccumulator -= stepDistance;
            AudioClip walkSound = walkSounds[walkSoundIndex % walkSounds.Length];
            walkSoundIndex++;

            if (walkSound == null)
            {
                continue;
            }

            if (walkSoundSource != null)
            {
                walkSoundSource.PlayOneShot(walkSound, walkSoundVolume);
            }
            else
            {
                AudioManager.PlayOneShot(walkSound, walkSoundVolume);
            }
        }
    }

    public void Rotate(Vector2 lookVector, bool isMouse)
    {
        // only multiply by deltaTime if using mouse input for smoother rotation, not for gamepad which already accounts for frame rate
        float deltaTime = isMouse ? Time.deltaTime : 1f;
        // x-axis of mouse controls pitch (looking up/down)
        rotationY += lookVector.x * rotateSpeed * deltaTime;
        // make sure to clamp the x rotation to prevent flipping over
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);        
        rotationX -= lookVector.y * rotateSpeed * deltaTime;
        //rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    public void Jump()
    {
        if (GameManager.Instance.AreMoveControlsDisabled())
        {
            return;
        }
        if (characterController.isGrounded)
        {
            verticalVelocity = jumpForce;
        }
    }

    public void Attack()
    {
        if (GameManager.Instance.AreControlsDisabled(DisabledControls.Attack))
        {
            return;
        }
        if (!haveWeapon)
        {
            //Debug.Log("PlayerControllerBB Attack: No weapon, cannot attack");
            return;
        }
        if (!attacking && !retracting)
        {
            attacking = true;
            // the following is needed for some reason now because no OnCollisionEnter
            // calls get registered after setting isTrigger to false on the weapon collider
            meleeWeapon.isSwinging = true;
            //weaponCollider.isTrigger = false;  // doesn't stop OnTriggerEnter calls currently
            // play weapon swing sound
            if (weaponSwingSound != null)
            {
                AudioSource.PlayClipAtPoint(weaponSwingSound, weapon.position);
            }
        }
        //Debug.Log("Attack triggered");
    }


    public void MeleeHit(GameObject hitObject, Vector3 hitPoint)
    {
        Debug.Log("PlayerController registered melee hit on: " + hitObject.name + " with tag: " + hitObject.tag);
        
        if (!retracting)
        {
            if (attacking)
            {
                if (hitObject.CompareTag("ChainLink"))
                {
                    Debug.Log("Chain hit: " + hitObject.name);

                    hitCount++;
                    if (hitCount > 3)
                        hitCount = 0;
                    
                    cameraShakeDuration = Mathf.Clamp(hitCount * 0.33f, 0.33f, 2f);
                    cameraShakeStrength = Mathf.Clamp(hitCount * 0.05f, 0.05f, 0.3f);
                    StartCoroutine(CameraShake());
                    Debug.Log("Hit chain");

                    if (hitCount == 3)
                    {
                        // add FX_explosion_01 to hitPoint, scale 25%
                        GameObject explosion = Instantiate(explosion01EffectPrefab, hitPoint, Quaternion.identity);
                        ApplyParticleScale(explosion, 0.5f);
                        ApplyParticleTransparency(explosion, 0.75f);
                        ApplyParticleDuration(explosion, 0.10f);
                        
                        // play chain explode sound
                        if (chainExplodeSound != null)
                        {
                            AudioSource.PlayClipAtPoint(chainExplodeSound, hitPoint);
                        }

                        // Old Brick barrier logic
                        // Trigger brick barrier destruction or other effects
                        // if (brickBarrier != null)
                        // {
                        //     brickBarrier.BrickExplode(hitObject, hitPoint);
                        // }
                        BreakChainApart(hitObject, hitPoint);
                        TearDownRemainingChains();

                        // also slow down time for 2 seconds
                        Time.timeScale = 0.25f;
                        StartCoroutine(ResetTimeScale());
                    }
                    else
                    {
                        // add FX_spash_dust_air to hitPoint scale 25%
                        GameObject dustAir = Instantiate(dustAirEffectPrefab, hitPoint, Quaternion.identity);
                        ApplyParticleScale(dustAir, 0.33f);
                        ApplyParticleTransparency(dustAir, 0.75f);

                        // play chain hit sound
                        if (chainHitSound != null)
                        {
                            AudioSource.PlayClipAtPoint(chainHitSound, hitPoint);
                        }
                        if (chainSlideSound != null)
                        {
                            AudioSource.PlayClipAtPoint(chainSlideSound, hitPoint);
                        }
                        BreakChainApart(hitObject, hitPoint);
                        // Trigger chain bash response
                        // if (chainBarrier != null)
                        // {
                        //     chainBarrier.ChainBashRespond(hitObject, hitPoint);
                        // }
                    }
                }
                attacking = false;
                meleeWeapon.isSwinging = false;
                retracting = true;
                // Ignore further hits during retraction/after attack finished
                //weaponCollider.isTrigger = true;  // doesn't work to enable OnCollisionEnter calls
            }
        }
    }

    void TearDownRemainingChains()
    {
        foreach (GameObject chainSegment in longChainsToBreak)
        {
            // add gravity to remaining chain segments so they fall down
            Rigidbody rb = chainSegment.AddComponent<Rigidbody>();
            rb.mass = 5f;
        }
        // after 5 seconds, remove rigidbodies to prevent too many physics objects
        StartCoroutine(RemoveLongChainPhysicsAfterTime(longChainsToBreak, 5f));

        // clear the list so it doesn't get processed again
        longChainsToBreak.Clear();
    }

    void BreakChainApart(GameObject chainLink, Vector3 hitPoint)
    {
        // the chainLink has a meshcollider and no rigidbody and is a child of a fragment parent object with another parent
        //  with multiple fragments,
        // so we need to first apply force to the chain link, get the parent of the parent and apply gravity to them
        Rigidbody rb = chainLink.AddComponent<Rigidbody>();
        rb.mass = 5f;
        Vector3 forceDirection = (chainLink.transform.position - hitPoint).normalized;
        rb.AddForce(forceDirection * 300f);
        Transform fragmentParent = chainLink.transform.parent.parent;
        foreach (Transform fragment in fragmentParent)
        {
            Rigidbody fragmentRb = fragment.gameObject.AddComponent<Rigidbody>();
            fragmentRb.mass = 5f;
        }
        // remove fragmentParent from longChainsToBreak list so it doesn't get torn down again later
        longChainsToBreak.Remove(fragmentParent.gameObject);

        // remove rigidbodies and colliders after 5 seconds to prevent too many physics objects
        StartCoroutine(RemovePhysicsAfterTime(chainLink, fragmentParent, 5f));
    }

    IEnumerator RemovePhysicsAfterTime(GameObject chainLink, Transform fragmentParent, float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(chainLink.GetComponent<Rigidbody>());
        foreach (Transform fragment in fragmentParent)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }
        }
    }
    IEnumerator RemoveLongChainPhysicsAfterTime(List<GameObject> chainFragments, float time)
    {
        yield return new WaitForSeconds(time);
        foreach (GameObject fragment in chainFragments)
        {
            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Destroy(rb);
            }
        }
    }

    // void OnCollisionEnter(Collision collision)
    // {
    //     Debug.Log("PlayerControllerBB OnCollisionEnter with: " + collision.gameObject.name + " with tag: " + collision.gameObject.tag);       
    // }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("PlayerControllerBB OnTriggerEnter with: " + other.gameObject.name);
        if (other.gameObject.CompareTag("MeleeSledge"))
        {
            // destroy object we hit
            Destroy(other.gameObject);
            // enable weapon
            weapon.gameObject.SetActive(true);
            haveWeapon = true;
            Debug.Log("PlayerControllerBB picked up weapon");
        }
    }

#region Camera Shake
    private IEnumerator CameraShake()
    {
        cameraOriginalPosition = playerCamera.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < cameraShakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / cameraShakeDuration;
            float intensity = (1f - t) * cameraShakeStrength; // Fade out the shake

            Vector3 randomShake = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * intensity;

            playerCamera.transform.localPosition = cameraOriginalPosition + randomShake;
            yield return null;
        }

        playerCamera.transform.localPosition = cameraOriginalPosition;
    }
#endregion Camera Shake

#region Time Scale Reset
    private IEnumerator ResetTimeScale()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 1f;
        // delete the barrier here too
        if (invisibleBarrier != null)
        {
            Destroy(invisibleBarrier);
        }
    }
#endregion Time Scale Reset

#region Particle FX Helpers
    private void ApplyParticleScale(GameObject fx, float scale)
    {
        if (fx == null) return;

        fx.transform.localScale *= scale;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Start Size
            main.startSizeMultiplier *= scale;

            // Size over Lifetime
            var sol = ps.sizeOverLifetime;
            if (sol.enabled)
                sol.sizeMultiplier *= scale;

            // Size by Speed
            var sbs = ps.sizeBySpeed;
            if (sbs.enabled)
                sbs.sizeMultiplier *= scale;

            // Shape (emission volume)
            var shape = ps.shape;
            shape.scale *= scale;
        }
    }

    private void ApplyParticleTransparency(GameObject fx, float alpha)
    {
        if (fx == null) return;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            Color startColor = main.startColor.color;
            startColor.a *= alpha; // Multiply existing alpha
            main.startColor = startColor;

            // If Color over Lifetime is enabled
            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                // Note: Cannot easily modify curves at runtime
                // Consider disabling or using Start Color only
            }
        }
    }
    private void ApplyParticleDuration(GameObject fx, float durationMultiplier)
    {
        if (fx == null) return;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            
            // Use Unscaled time so Time.timeScale doesn't affect particles
            main.simulationSpace = ParticleSystemSimulationSpace.World;
           // main.useUnscaledTime = true;
            
            main.duration *= durationMultiplier;
            main.startLifetimeMultiplier *= durationMultiplier;
            main.loop = false;
        }
    }
#endregion Particle FX Helpers
}