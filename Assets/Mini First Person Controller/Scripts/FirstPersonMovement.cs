using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Fall Fail")]
    [SerializeField] float failYThreshold = -25f;
    [SerializeField] float airborneDownwardAcceleration = 20f;
    [SerializeField] float maxAirborneDownwardSpeed = 20f;
    [SerializeField] float minAirborneDownwardSpeed = 1.5f;
    [SerializeField] float airborneGraceTime = 0.1f;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    bool dialogueManagerPresent = false;
    bool hasFailed = false;
    float lastGroundedTime;

    Rigidbody rb;
    GroundCheck groundCheck;
    /// <summary> Functions to override movement speed. Will use the last added override. </summary>
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();



    void Awake()
    {
        // Get the rigidbody on this.
        rb = GetComponent<Rigidbody>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        lastGroundedTime = Time.time;
        // Check if Dialogue Manager is present in the scene.
        dialogueManagerPresent = DialogueManager.GetInstance() != null;
    }

    void FixedUpdate()
    {
        if (!hasFailed && transform.position.y < failYThreshold)
        {
            Fail();
            return;
        }

        //Freeze player when dialogue is open
        if (dialogueManagerPresent && DialogueManager.GetInstance().dialogueIsPlaying)
        {
            return;
        }

        // Update IsRunning from input.
        IsRunning = canRun && Input.GetKey(runningKey);

        bool isGrounded = groundCheck == null || groundCheck.isGrounded;
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        bool shouldApplyAirborneRules = !isGrounded && Time.time - lastGroundedTime > airborneGraceTime;
        bool isFalling = rb.linearVelocity.y < -0.05f;
        bool shouldApplyFallingRules = shouldApplyAirborneRules && isFalling;

        // Get targetMovingSpeed.
        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        // Get targetVelocity from input.
        Vector2 targetVelocity =new Vector2( Input.GetAxis("Horizontal") * targetMovingSpeed, Input.GetAxis("Vertical") * targetMovingSpeed);

        // Prevent horizontal control while airborne.
        if (shouldApplyFallingRules)
        {
            targetVelocity = Vector2.zero;
        }

        Vector3 worldVelocity = transform.rotation * new Vector3(targetVelocity.x, 0f, targetVelocity.y);
        worldVelocity.y = rb.linearVelocity.y;

        // Always keep a slight downward bias when ungrounded to avoid hovering at y ~= 0.
        if (shouldApplyAirborneRules)
        {
            worldVelocity.y = Mathf.Min(worldVelocity.y, -minAirborneDownwardSpeed);
            worldVelocity.y -= airborneDownwardAcceleration * Time.fixedDeltaTime;
            worldVelocity.y = Mathf.Max(worldVelocity.y, -maxAirborneDownwardSpeed);
        }

        // Apply movement.
        rb.linearVelocity = worldVelocity;
    }

    void Fail()
    {
        hasFailed = true;
        Debug.Log("Player fell below fail threshold at Y = " + transform.position.y);

        GameManager.Instance.gameState.currentSceneScript.RestartScene();

        this.enabled = false;
    }
}