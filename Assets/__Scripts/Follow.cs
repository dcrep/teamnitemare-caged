using UnityEngine;

public class Follow : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float maxSpeed = 12f;
    public AudioClip getSound;
    public float volume = 1.0f;

    [SerializeField] private GameObject followBox;

    [SerializeField] private Animator animator;
    [SerializeField] private Transform target;

    [SerializeField] private BirdFollowTrigger followTrigger;

    [SerializeField] private GameObject visualCue;

    public static int numberOfFollowers = 0;


    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //followBox = GameObject.FindGameObjectWithTag("FollowBox");
        AudioSource.PlayClipAtPoint(getSound, transform.position, volume);
        numberOfFollowers++;
        if (followTrigger != null)
        {
            followTrigger.OnNewBirbFollowing();
        }
        ChirpAttract attract = GetComponent<ChirpAttract>();
        if (attract != null)
        {
            attract.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (followBox != null)
        {
            float distance = Vector3.Distance(transform.position, followBox.transform.position);
            float currentSpeed = Mathf.Min(maxSpeed, distance * speed);
            transform.position = Vector3.MoveTowards(transform.position, followBox.transform.position, currentSpeed * Time.deltaTime);
        }

        // Animaton
        if (animator != null)
        {
            animator.SetBool("Bounce", false);
            animator.SetBool("Fly", true);
        }
        //animator.SetBool("Idle_A", false);
        if (target != null)
        {
            transform.LookAt(target);
        }

        if (visualCue != null)
        {
            visualCue.SetActive(false);
        }
    }
}
