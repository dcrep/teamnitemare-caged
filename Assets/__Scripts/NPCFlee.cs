using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCFlee : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float maxSpeed = 12f;
    public AudioClip getSound;
    public float volume = 1.0f;

    [SerializeField] private GameObject followBox;

    [SerializeField] private Animator animator;
    [SerializeField] private Transform target;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        AudioSource.PlayClipAtPoint(getSound, transform.position, volume);
    }

    // Update is called once per frame
    void Update()
    {
        if (followBox != null)
        {
            float distance = Vector3.Distance(transform.position, followBox.transform.position);
            float currentSpeed = Mathf.Min(maxSpeed, distance * speed);
            transform.position = Vector3.MoveTowards(transform.position, followBox.transform.position, currentSpeed * Time.deltaTime);

            // Animaton
            if (animator != null)
            {
                //animator.SetBool("Bounce", false);
                //animator.SetBool("Fly", true);
                //Invoke(nameof(ResetAnimation), 5f);
            }
            //animator.SetBool("Idle_A", false);
            if (target != null)
            {
                transform.LookAt(target);
            }
        }
    }

   // void ResetAnimation()
   // {
   //     animator.SetBool("Bounce", true);
   //     animator.SetBool("Fly", false);
   // }


}
