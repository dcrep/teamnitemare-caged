using System;
using UnityEngine;

// This component is forced by Editor/SaveableValidator script for all ISaveable components without UniqueID
//[RequireComponent(typeof(UniqueID))]
public class SaveableTransformComponent : MonoBehaviour, ISaveable
{
    [Serializable]
    private struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
    public object CaptureState()
    {
        return new TransformData { position = this.transform.position, rotation = this.transform.rotation, scale = this.transform.localScale };
    }

    public void RestoreState(object state)
    {
        var data = (TransformData)state;
        this.transform.position = data.position;
        this.transform.rotation = data.rotation;
        this.transform.localScale = data.scale;
    }
}
