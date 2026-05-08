using System.Collections.Generic;
using UnityEngine;

// [System.Serializable]
// public struct HitDecalInfo
// {
//     public float hitTime;
//     public GameObject decal;
// }

public class MeleeWeapon : MonoBehaviour
{
    //PlayerController playerController;
    //public GameObject decalPrefab; // Assign a prefab with DecalProjector component
    // [SerializeField] Material decalMaterial;

    //List<HitDecalInfo> hitDecals = new List<HitDecalInfo>();

    // Decals decalManager;
    //public float decalFadeTime = 10f;

    public float hitRecoverTime = 0.50f;
    // private float lastHitTime = 0f;

    public delegate void HitEvent(GameObject hitObject, Vector3 hitPoint);
    public static event HitEvent OnHit;

    public bool isSwinging = false; // Set this to true when the weapon is in the middle of a swing animation

    void Awake()
    {
        //playerController = FindFirstObjectByType<PlayerController>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // decalManager = FindFirstObjectByType<Decals>();
    }

    // Update is called once per frame
    void Update()
    {
        /*float curTime = Time.time;
        if (hitDecals.Count > 0)
        {
            for (int i = hitDecals.Count - 1; i > -1; i--)
            {
                if (curTime > hitDecals[i].hitTime + decalFadeTime)
                {
                    GameObject.Destroy(hitDecals[i].decal);
                    hitDecals.RemoveAt(i);
                }
            }
        }*/
        
    }


    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTrigger->Melee weapon hit: " + other.gameObject.name);
        if (isSwinging)
        {
            OnHit?.Invoke(other.gameObject, other.ClosestPoint(transform.position));
            //playerController.MeleeHit(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("OnCollision->Melee weapon collided with: " + collision.gameObject.name);
        OnHit?.Invoke(collision.gameObject, collision.contacts[0].point);
        //playerController.MeleeHit(collision.gameObject);

        //Debug.Log($"Time.time {Time.time}, lastHitTime: {lastHitTime}, hitRecoverTime: {hitRecoverTime}");
        //if (Time.time > lastHitTime + hitRecoverTime)
        //{
            // Spawn decal at hit point
            ContactPoint contact = collision.contacts[0];

            //Vector3 adjustedPoint = contact.point;
            //adjustedPoint.z -= 0.25f; // Slightly adjust backwards (weapon-specific? motion-specific?) (speed / 1000)
            
            // Use the contact normal and separation to get the actual surface point
            // The separation tells us how deep the contact is penetrating
            //Vector3 surfacePoint = contact.point + contact.normal * contact.separation;

            //decalManager.SpawnDecal(contact.point, contact.normal, collision.transform, contact.separation + 0.25f, decalMaterial);
/*
            //Vector3 surfacePoint = contact.point + contact.normal * contact.separation;
            Vector3 decalPosition = contact.point - contact.normal * 0.25f; // Offset INTO the surface
            GameObject decal = Instantiate(decalPrefab, decalPosition, Quaternion.LookRotation(contact.normal));
            decal.transform.SetParent(collision.transform);

            lastHitTime = Time.time;

            hitDecals.Add(new HitDecalInfo { hitTime = lastHitTime, decal = decal} );
*/          
        //}
    }
}
