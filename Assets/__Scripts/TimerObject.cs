using System;
using UnityEngine;


public class TimerObject : MonoBehaviour, ISaveable
{
    [SerializeField] private bool isEnabled = true;
    public float timerDuration = 5f;
    public bool restartAfterExpire = false;
    public int triggerCount = 0;
    private bool bRestoredState = false;

    public UnityEngine.Events.UnityEvent onTimerExpire;

    void Start()
    {
        if (bRestoredState)
        {
            // if we restored state and triggerCount > 0, we assume the timer had already expired at least once, so we trigger the event immediately
            if (triggerCount > 0)
            {
                //! CHANGE: Unknown consequences on restore with timing of events etc
                //onTimerExpire?.Invoke();
                //! This is iffy as to timing..
                if (restartAfterExpire)
                {
                    //StartTimer();
                }
            }
        }
    }

    public void StartTimer()
    {
        if (!isEnabled)
            return;
        Invoke(nameof(TimerExpired), timerDuration);
    }

    public void CancelTimer()
    {
        CancelInvoke(nameof(TimerExpired));
    }

    public void SetIsEnabled(bool value)
    {
        isEnabled = value;
        if (!isEnabled)
        {
            CancelTimer();
        }
    }

    public bool ResetTimer()
    {
        if (!isEnabled)
            return false;
        CancelTimer();
        StartTimer();
        return true;
    }

    public bool SetTimerDuration(float duration)
    {
        if (duration <= 0f)
            return false;
        // safeguard if actively running
        CancelTimer();
        timerDuration = duration;
        return true;
    }


    [ContextMenu("Manual Timer Trigger")]
    public void ManualTimerTrigger()
    {
        TriggerImmediately();
    }
    public void TriggerImmediately()
    {
        if (!isEnabled)
            return;
        CancelTimer();
        TimerExpired();
    }

    private void TimerExpired()
    {
        Debug.Log("Timer expired! on object: " + gameObject.name);
        onTimerExpire?.Invoke();
        triggerCount++;

        if (restartAfterExpire)
        {
            StartTimer();
        }
    }

    public void AddTimerExpiredListener(UnityEngine.Events.UnityAction action)
    {
        onTimerExpire.AddListener(action);
    }
    public void RemoveTimerExpiredListener(UnityEngine.Events.UnityAction action)
    {
        onTimerExpire.RemoveListener(action);
    }


#region ISaveable implementation
    private class TimerObjectData
    {
        public bool isEnabled;
        public float timerDuration;
        public bool restartAfterExpire;
        public int triggerCount;
    }
    public object CaptureState()
    {
        var data = new TimerObjectData
        {
            isEnabled = this.isEnabled,
            timerDuration = this.timerDuration,
            restartAfterExpire = this.restartAfterExpire,
            triggerCount = this.triggerCount
        };
        return data;
    }
    public void RestoreState(object state)
    {
        if (state is TimerObjectData data)
        {
            this.isEnabled = data.isEnabled;
            this.timerDuration = data.timerDuration;
            this.restartAfterExpire = data.restartAfterExpire;
            this.triggerCount = data.triggerCount;

            bRestoredState = true;
        }
    }
#endregion ISaveable implementation
}
