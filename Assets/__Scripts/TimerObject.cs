using System;
using UnityEngine;


public class TimerObject : MonoBehaviour, ISaveable
{
    [SerializeField] private bool isEnabled = true;
    public float timerDuration = 5f;
    public bool restartAfterExpire = false;
    public int triggerCount = 0;

    public UnityEngine.Events.UnityEvent onTimerExpire;

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

            if (triggerCount > 0)
            {
                onTimerExpire?.Invoke();
                //! This is iffy as to timing..
                if (restartAfterExpire)
                {
                    //StartTimer();
                }
            }
        }
    }
#endregion ISaveable implementation
}
