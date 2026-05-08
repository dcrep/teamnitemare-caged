using UnityEngine;
using System.Collections;

public class AudioLoopWithDelay : MonoBehaviour
{
    public float Delay = 50.0f;

    //if you see this I wanted a short delay between loops because it would be cool to hear the wind with no background music for immersion but
    //i gave up!!!! :D (prioritiznig other things)
    void Start()
    {
        StartCoroutine(YourFunctionName());
    }

    
    void Update()
    {

    }

    IEnumerator YourFunctionName()
    {
        while (true)
        {
            DoSomething();
            yield return new WaitForSeconds(Delay);
        }
    }

    void DoSomething()
    {
            GetComponent<AudioSource>();
    }
}