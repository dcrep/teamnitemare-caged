using System.Collections;
using UnityEngine;

public class AnimationDelay : MonoBehaviour
{

    [SerializeField] GameObject animation;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine (AnimateDelay());
    }
    private IEnumerator AnimateDelay()
    {
        yield return new WaitForSeconds(25);
        animation.SetActive (true);
    }
}
