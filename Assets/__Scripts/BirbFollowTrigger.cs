using UnityEngine;

public class BirdFollowTrigger : MonoBehaviour
{
    int requiredBirbs = 3;
    int totaBibsFollowing = 0;

    bool allBirbsCollected = false;

    public UnityEngine.Events.UnityEvent onAllBirbsCollected;

    public void OnNewBirbFollowing()
    {
        if (allBirbsCollected)
            return;

        totaBibsFollowing++;
        if (totaBibsFollowing >= requiredBirbs)
        {
            allBirbsCollected = true;
            onAllBirbsCollected.Invoke();
        }
    }
}
