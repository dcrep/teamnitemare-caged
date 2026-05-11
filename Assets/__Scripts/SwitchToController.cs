using UnityEngine;

public class SwitchToController : MonoBehaviour
{

    [SerializeField] private GameObject autoDialogue;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject cutsceneCamera;
    [SerializeField] private Animator birdFlee;
    [SerializeField] private GameObject bird;

    [SerializeField] private lb_BirdExperiment birb;
    [SerializeField] private Transform fleeTarget;

    [SerializeField] private UnityEngine.Events.UnityEvent onDialogueEnd;
    bool hasSwitched = false;
    bool hasFled = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!autoDialogue.activeInHierarchy)
        {
            //Invoke(nameof(BirdFlyingDelay), 3f);
            if (!hasFled)
            {
                hasFled = true;
                birb.FleeTowardTransform(fleeTarget);
            }
            


            //if (bird == null)
            //{
            if (!hasSwitched)
            {
                hasSwitched = true;
                Invoke(nameof(SwitchToPlayer), 8f);
            }
                
            //}
            
        }

    }

    void SwitchToPlayer()
    {
        // player.SetActive(true);
        // cutsceneCamera.SetActive(false);
        onDialogueEnd.Invoke();
        // disable this script
        this.enabled = false;
    }

    void BirdFlyingDelay()
    {
        birdFlee.SetBool("flying", true);
    }

}
