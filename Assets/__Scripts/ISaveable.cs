using UnityEngine;

// This component (for monobehaviors inherited from this)
// is forced by Editor/SaveableValidator script for all ISaveable components without UniqueID
//[RequireComponent(typeof(UniqueID))]
public interface ISaveable
{
    /// <summary>
    /// Capture all data needed to restore this object's state.
    /// Return any serializable object (class, struct, primitive, etc.)
    /// </summary>
    object CaptureState();

    /// <summary>
    /// Restore this object's state from the previously captured data.
    /// </summary>
    void RestoreState(object state);
}
