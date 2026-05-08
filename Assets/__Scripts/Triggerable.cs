using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class Triggerable : MonoBehaviour, ISaveable
{
    public bool isActive = true;
    [SerializeField] public GameObject playerOrNullForAll = null;
    [SerializeField] public UnityEngine.Events.UnityEvent onTrigger;
    [SerializeField] public UnityEngine.Events.UnityEvent onTriggerExit;

    //!
    [SerializeField] GameObject billBoardObject = null;
    [SerializeField] Vector3 billBoardOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] TMP_Text billBoardTextObject = null;
    [SerializeField] Vector3 billBoardTextOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] string billBoardInteractText = "Interact";
    [SerializeField] bool showBillboardOnStart = true;

    //[SerializeField] InkleStoryComponent inkStoryComponentOrNull = null;

    //!!
    [SerializeField] bool hideBillboardOnInteract = true;
    [SerializeField] bool showBillboardOnInteractExit = true;
    [SerializeField] bool triggerOnlyOnce = false;
    
    public int triggeredCount = 0;
    bool isActiveInScene = false;
    bool bDataRestored = false;
    GameObject objectTriggeredBy = null;

    void Awake()
    {
        isActiveInScene = gameObject.activeInHierarchy;
    }
    void Start()
    {

        // SetBillboardVisibility(showBillboardOnStart);

        if (bDataRestored)
        {
            gameObject.SetActive(isActiveInScene);
        }
    }

    public void SetIsActive(bool active)
    {
        isActive = active;
    }

    [ContextMenu("Manual Trigger")]
    public void ManualTrigger()
    {
        if (isActive)
        {
            onTrigger.Invoke();
            triggeredCount++;
        }
    }

    public void AddTriggerListener(UnityEngine.Events.UnityAction action)
    {
        onTrigger.AddListener(action);
    }
    public void RemoveTriggerListener(UnityEngine.Events.UnityAction action)
    {
        onTrigger.RemoveListener(action);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            if (playerOrNullForAll == null || other.gameObject == playerOrNullForAll)  
            {
                objectTriggeredBy = other.gameObject;
                onTrigger.Invoke();
                triggeredCount++;
                // if (hideBillboardOnInteract)
                // {
                //     SetBillboardVisibility(false);
                // }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (isActive && objectTriggeredBy == other.gameObject)
        {
            if (playerOrNullForAll == null || other.gameObject == playerOrNullForAll)
            {
                onTriggerExit.Invoke();
                // if (showBillboardOnInteractExit)
                // {
                //     SetBillboardVisibility(true);
                // }
                //inkStoryComponentOrNull?.ExitTriggerArea();
            }            
        }
    }
#region ISaveable implementation
    private class TriggerableData
    {
        public bool isActive;
        public bool isActiveInScene;
        public int triggeredCount;
    }

    public object CaptureState()
    {
        var data = new TriggerableData
        {
            isActive = this.isActive,
            isActiveInScene = this.isActiveInScene,
            triggeredCount = this.triggeredCount
        };
        return data;
    }
    public void RestoreState(object state)
    {
        if (state is TriggerableData data)
        {
            this.isActive = data.isActive;
            this.isActiveInScene = data.isActiveInScene;
            this.triggeredCount = data.triggeredCount;
            // not running onTrigger here since it is location-based
        }
        //gameObject.SetActive(isActiveInScene);
    }
#endregion ISaveable implementation
}
