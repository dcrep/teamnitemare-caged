using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// TODO: This needs an Interactable component, or to inherit from ISaveable
// otherwise it ignores state/quest progress (although it is stored in GameState)
// to save/restore state and trigger onTasksComplete in the quest

[RequireComponent(typeof(QuestComponent))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FragmentConnection : MonoBehaviour
{
    [field: SerializeField] public FragmentConnection fragmentToConnectTo { get; private set; }
    public bool isConnected = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float maxMoveDistance = 20f;
    [SerializeField] private float snapDistance = 0.05f;

    private bool isMoving = false;
    private bool approachAlongX;
    private Vector3 originPosition;
    private Vector3 moveVelocity;
    private Collider ownCollider;
    private Collider targetCollider;

    private QuestComponent questComponent;
    //private static Quest sharedQuest;
    private static readonly string questName = "QuestConnect";
    private Quest quest = null;

    void Awake()
    {
        ownCollider = GetComponent<Collider>();
        questComponent = GetComponent<QuestComponent>();
        if (questComponent == null)
        {
            questComponent = gameObject.AddComponent<QuestComponent>();
        }

        quest = GameObject.Find(questName)?.GetComponent<Quest>();
        if (quest == null)
        {
            Debug.LogError("FC-No quest found with name: " + questName + ". Creating new quest.");
        }
    }
    // void Start()
    // {
    //     if (quest == null)
    //     {
    //         quest = QuestManager.Instance.FindQuest(questName);
            
    //         if (quest == null)
    //         {
    //             quest = Quest.CreateQuest(questName, SceneManager.GetActiveScene().name, questName);
    //         }
    //         quest.AddTaskObject(questComponent);
    //     }
    // }
    void Start()
    {
        //quest.AddTaskObject(questComponent);  // added in editor
        quest.GetTaskGroup().onTasksCompleted.AddListener(OnFragmentsConnected);
        //! Quests created on the fly are problematic because of newly generated unique IDs on every scene launch
        //! The only place this can be viable is in GLOBAL quests created outside of scenes, but even then
        //! I don't know that the uniqueID will be persistent across game sessions so its better to generate global
        //! uniqueID's and hardcode them, then manually assign the Quest data
        // if (sharedQuest == null)
        // {
        //     sharedQuest = QuestManager.Instance.FindQuestByName(questName)
        //                 ?? Quest.CreateQuest(questName, SceneManager.GetActiveScene().name, questName, 4);
        // }

        // quest = sharedQuest;

        //!! Problem: quest must add itself in Start(), can't ADdTaskObject until that happens so we need to
        // run a couroutine to wait until end of frame to add the task object to the quest
        // StartCoroutine(AddTaskObjectWhenReady());
        //quest.AddTaskObject(questComponent);
        //quest.GetTaskGroup().onTasksCompleted.AddListener(OnFragmentsConnected);
    }
    // IEnumerator AddTaskObjectWhenReady()
    // {
    //     while (quest == null || QuestManager.Instance.FindQuest(quest.questUniqueId.ID) == null)
    //     {
    //         yield return null; // wait for next frame
    //     }
    //     quest.AddTaskObject(questComponent);
    //     quest.GetTaskGroup().onTasksCompleted.AddListener(OnFragmentsConnected);
    // }
    private static void OnFragmentsConnected()
    {
        Debug.Log("FRAGMENT CallBACK");
    }

    private void Update()
    {
        if (!isMoving || isConnected || fragmentToConnectTo == null)
        {
            return;
        }

        if (Vector3.Distance(transform.position, originPosition) >= maxMoveDistance)
        {
            isMoving = false;
            return;
        }

        Vector3 destination = ComputeSideDestination();
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref moveVelocity, smoothTime, moveSpeed);

        if (Vector3.Distance(transform.position, destination) <= snapDistance)
        {
            SnapAndConnect();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isMoving || isConnected || fragmentToConnectTo == null)
        {
            return;
        }

        if (collision.gameObject == fragmentToConnectTo.gameObject)
        {
            SnapAndConnect();
        }
    }

    public void ConnectToFragment(FragmentConnection otherFragment)
    {
        if (otherFragment == null || isConnected)
        {
            return;
        }

        fragmentToConnectTo = otherFragment;
        targetCollider = fragmentToConnectTo.GetComponent<Collider>();
        originPosition = transform.position;
        moveVelocity = Vector3.zero;

        Bounds targetBounds = targetCollider != null
            ? targetCollider.bounds
            : new Bounds(fragmentToConnectTo.transform.position, Vector3.one);
        Vector3 myCenter = ownCollider != null ? ownCollider.bounds.center : transform.position;
        Vector3 toTarget = targetBounds.center - myCenter;
        approachAlongX = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.z);

        isMoving = true;
    }

    private void SnapAndConnect()
    {
        transform.position = ComputeSideDestination();
        moveVelocity = Vector3.zero;
        isMoving = false;
        isConnected = true;
        fragmentToConnectTo.isConnected = true;

        quest.CompleteTaskObject(questComponent);
        quest.CompleteTaskObject(fragmentToConnectTo.questComponent);
    }

    private Vector3 ComputeSideDestination()
    {
        Bounds targetBounds = targetCollider != null
            ? targetCollider.bounds
            : new Bounds(fragmentToConnectTo.transform.position, Vector3.one);
        Bounds myBounds = ownCollider != null
            ? ownCollider.bounds
            : new Bounds(transform.position, Vector3.one);

        Vector3 toTarget = targetBounds.center - myBounds.center;
        float destY = targetBounds.center.y;

        if (approachAlongX)
        {
            float sideX = toTarget.x > 0
                ? targetBounds.min.x - myBounds.extents.x
                : targetBounds.max.x + myBounds.extents.x;
            return new Vector3(sideX, destY, targetBounds.center.z);
        }
        else
        {
            float sideZ = toTarget.z > 0
                ? targetBounds.min.z - myBounds.extents.z
                : targetBounds.max.z + myBounds.extents.z;
            return new Vector3(targetBounds.center.x, destY, sideZ);
        }
    }
}
