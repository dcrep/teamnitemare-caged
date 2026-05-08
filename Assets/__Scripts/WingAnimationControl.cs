using UnityEngine;

public class WingAnimationControl : MonoBehaviour
{
    private Animator wingAnimator = null;
    [SerializeField] private AlphaControllerForAnimationRenderer wingsAlphaControl;
    [SerializeField] private Light wingPointLightL;
    [SerializeField] private Light wingPointLightR;
    [SerializeField] private float glowTime = 1.5f;

    void Awake()
    {
        wingAnimator = GetComponentInChildren<Animator>();
        if (!wingAnimator)
            Debug.LogError("WingAnimationControl: No Animator found in children.");
    }
    void Start()
    {
        if (wingsAlphaControl)
        {
            wingsAlphaControl.SetFullyTransparent();
            wingsAlphaControl.SetEmissionIntensity(0f);
        }
    }

    public void SwitchAnimation(int index)
    {
        wingAnimator.SetInteger("Mode", index);
    }

    public void WingFlap()
    {
        if (wingsAlphaControl)
        {
            wingsAlphaControl.SetToAlpha(1f);
            wingsAlphaControl.SetEmissionIntensity(1f);
        }
        if (wingPointLightL)
        {
            wingPointLightL.enabled = true;
        }
        if (wingPointLightR)
        {
            wingPointLightR.enabled = true;
        }
        wingAnimator.SetTrigger("FlapTrigger");
        wingsAlphaControl.FadeOutToTransparent(1f);
        Invoke(nameof(WingLightReset), glowTime);
        //wingAnimator.SetInteger("Mode", 3);
        // Cancel any pending reset to idle to avoid conflicts.
        //CancelInvoke(nameof(ResetToIdle));        
        //Invoke(nameof(ResetToIdle), 1f);
    }
    void WingLightReset()
    {
        if (wingPointLightL)
        {
            wingPointLightL.enabled = false;
        }
        if (wingPointLightR)
        {
            wingPointLightR.enabled = false;
        }
    }
    public void ResetToIdle()
    {
        //wingAnimator.SetInteger("Mode", 0);
        wingAnimator.CrossFade("Idle", 0f);

    }

}
