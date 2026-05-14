using System.Collections.Generic;
using UnityEngine;

public class ObjectsTracker : MonoBehaviour, ISaveable
{
    [Header("Track Object Active/Destroyed and Transforms")]
    [SerializeField] private List<GameObject> trackedBasicObjects = new List<GameObject>();
    [Header("Track RigidBody Objects")]
    [SerializeField] private List<Rigidbody> trackedRigidBodies = new List<Rigidbody>();

    //bool bDataRestored;

#region ISaveable implementation
    public class BasicObjectState
    {
        public int index;
        public bool isDestroyed;
        public bool isActive;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
    public class RigidBodyObjectState : BasicObjectState
    {
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }
    public class ObjectsTrackerState
    {
        public List<BasicObjectState> basicObjects;
        public List<RigidBodyObjectState> rigidBodies;
    }
    public object CaptureState()
    {
        List<BasicObjectState> basicObjectStates = new List<BasicObjectState>();
        for (int i = 0; i < trackedBasicObjects.Count; i++)
        {
            var obj = trackedBasicObjects[i];
            // Destroyed state objects will be null
            if (obj == null)
            {
                basicObjectStates.Add(new BasicObjectState
                {
                    index = i,
                    isDestroyed = true,
                    isActive = false,
                    position = Vector3.zero,
                    rotation = Quaternion.identity,
                    scale = Vector3.one
                });
                continue;
            }
            // else

                basicObjectStates.Add(new BasicObjectState
                {
                    index = i,
                    isDestroyed = false,
                    isActive = obj.activeSelf,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale
                });
        }

        List<RigidBodyObjectState> rigidBodyObjectStates = new List<RigidBodyObjectState>();
        for (int i = 0; i < trackedRigidBodies.Count; i++)
        {
            var rb = trackedRigidBodies[i];
            if (rb == null)
            {
                rigidBodyObjectStates.Add(new RigidBodyObjectState
                {
                    index = i,
                    isDestroyed = true,
                    isActive = false,
                    position = Vector3.zero,
                    rotation = Quaternion.identity,
                    scale = Vector3.one,
                    velocity = Vector3.zero,
                    angularVelocity = Vector3.zero
                });
                continue;
            }
            //else
                rigidBodyObjectStates.Add(new RigidBodyObjectState
                {
                    index = i,
                    isDestroyed = false,
                    isActive = rb.gameObject.activeSelf,
                    position = rb.transform.position,
                    rotation = rb.transform.rotation,
                    scale = rb.transform.localScale,
                    velocity = rb.linearVelocity,
                    angularVelocity = rb.angularVelocity
                });
        }

        return new ObjectsTrackerState { basicObjects = basicObjectStates, rigidBodies = rigidBodyObjectStates };
    }
    public void RestoreState(object state)
    {
        if (state is ObjectsTrackerState trackerState)
        {
            // Restore basic objects
            for (int i = 0; i < trackerState.basicObjects.Count; i++)
            {
                var obj = trackedBasicObjects[i];
                var stateObj = trackerState.basicObjects[i];
                if (obj != null)
                {
                    if (stateObj.isDestroyed)
                    {
                        Debug.Log("ObjT->RestoreState: Destroying tracked object " + obj.name);
                        obj.SetActive(false);
                        Destroy(obj);
                        continue;
                    }
                    else
                    {
                        Debug.Log("ObjT->RestoreState: Restoring tracked object " + obj.name + " active: " + stateObj.isActive);
                        obj.SetActive(stateObj.isActive);
                        obj.transform.position = stateObj.position;
                        obj.transform.rotation = stateObj.rotation;
                        obj.transform.localScale = stateObj.scale;
                    }
                }
            }

            // Restore rigid bodies
            for (int i = 0; i < trackerState.rigidBodies.Count; i++)
            {
                var rb = trackedRigidBodies[i];
                var stateRb = trackerState.rigidBodies[i];
                if (rb != null)
                {
                    if (stateRb.isDestroyed)
                    {
                        Debug.Log("ObjT->RestoreState: Destroying tracked rigid body game object " + rb.gameObject.name);
                        rb.gameObject.SetActive(false);
                        Destroy(rb.gameObject);
                        continue;
                    }
                    else
                    {
                        Debug.Log("ObjT->RestoreState: Restoring tracked rigid body game object " + rb.gameObject.name + " active: " + stateRb.isActive);
                        rb.gameObject.SetActive(stateRb.isActive);
                        rb.transform.position = stateRb.position;
                        rb.transform.rotation = stateRb.rotation;
                        rb.transform.localScale = stateRb.scale;
                        rb.linearVelocity = stateRb.velocity;
                        rb.angularVelocity = stateRb.angularVelocity;
                    }
                }
            }
        }
    }
        // capture 
#endregion ISaveable implementation

}
