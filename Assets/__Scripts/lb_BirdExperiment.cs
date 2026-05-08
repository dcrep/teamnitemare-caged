using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lb_BirdExperiment : MonoBehaviour
{
    public enum BirdType
    {
        robin,
        blueJay,
        cardinal,
        chickadee,
        sparrow,
        goldFinch,
        crow
    }

    public enum AnimationParameterType
    {
        Trigger,
        Bool,
        Int,
        Float
    }

    [System.Serializable]
    public class AnimationStep
    {
        public AnimationParameterType parameterType = AnimationParameterType.Trigger;
        public string parameterName = "sing";
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public float holdTimeSeconds = 2.0f;

        public void Apply(Animator animator)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }

            switch (parameterType)
            {
                case AnimationParameterType.Trigger:
                    animator.SetTrigger(parameterName);
                    break;
                case AnimationParameterType.Bool:
                    animator.SetBool(parameterName, boolValue);
                    break;
                case AnimationParameterType.Int:
                    animator.SetInteger(parameterName, intValue);
                    break;
                case AnimationParameterType.Float:
                    animator.SetFloat(parameterName, floatValue);
                    break;
            }
        }
    }

    [Header("Spawn")]
    public bool spawnOnStart = true;
    public bool spawnAtSpecificLocation = true;
    public Transform spawnPoint;
    public Vector3 spawnPosition = Vector3.zero;
    public Vector3 spawnEulerRotation = Vector3.zero;
    public bool highQuality = true;
    public BirdType birdType = BirdType.robin;

    [Header("Animation")]
    public bool playSequenceOnStart = true;
    public bool loopSequence = true;
    public bool disableDefaultBirdBehaviour = true;
    [Range(0.0f, 1.0f)] public float stepSkipChance = 0.0f;
    public Vector2 holdTimeRandomOffset = Vector2.zero;
    public List<AnimationStep> animationSequence = new List<AnimationStep>();

    [Header("Flee")]
    public bool canFlee = true;
    public bool resumeSequenceAfterFlee = true;
    public bool resumeSequenceAfterFleeReturn = true;
    public bool useFleeHotkey = false;
    public KeyCode fleeKey = KeyCode.F;
    public float fleeDistanceMin = 8.0f;
    public float fleeDistanceMax = 20.0f;
    public float fleeHeight = 6.0f;
    public float fleeDuration = 2.4f;
    public float fleeReturnPause = 0.4f;
    public float preFlightDelay = 0.2f;
    public bool waitForFlyAnimationState = true;
    public float maxFlyStateWait = 0.5f;

    [Header("Direct Flight")]
    public bool resumeSequenceAfterFlyToLocation = true;
    public float flyToLocationDuration = 1.0f;

    [Header("Debug")]
    public bool enableMovementDebugLogs = false;
    [Min(0.1f)] public float movementDebugLogInterval = 0.5f;

    [Header("Feather Emitter")]
    public bool enableFeatherEmitter = true;
    public bool spawnFeatherEmitterOnFlee = false;
    public string featherEmitterResourceName = "featherEmitter";
    public GameObject featherEmitterPrefab;
    public float featherEmitterLifetime = 4.5f;
    public bool featherEmitterUseBirdPosition = true;
    public Vector3 featherEmitterPositionOffset = Vector3.zero;
    public Vector3 featherEmitterRotationOffset = Vector3.zero;

    [Header("Audio")]
    public bool playAudio = true;
    public bool playSongAudioOnSingTrigger = true;
    public bool playFlyAwayAudioOnTakeoff = true;
    public string singTriggerParameterName = "sing";
    public AudioClip song1;
    public AudioClip song2;
    public AudioClip flyAway1;
    public AudioClip flyAway2;
    public bool playSongAtBirdPosition = true;
    public Vector3 songPositionOffset = Vector3.zero;
    public bool playFlyAwayAtBirdPosition = true;
    public Vector3 flyAwayPositionOffset = Vector3.zero;
    public float positionalAudioMinDistance = 1.0f;
    public float positionalAudioMaxDistance = 200.0f;
    [Min(0.0f)] public float songVolume = 1.0f;
    [Min(0.0f)] public float flyAwayVolume = 0.1f;
    [Min(0.0f)] public float songVolumeBoost = 2.0f;
    [Min(0.0f)] public float flyAwayVolumeBoost = 2.0f;
    public bool useGlobalAudioThrottling = true;
    [Range(0.0f, 1.0f)] public float songPlayChance = 1.0f;
    [Range(0.0f, 1.0f)] public float flyAwayPlayChance = 1.0f;
    public float minTimeBetweenAnyBirdAudio = 0.08f;
    public float minTimeBetweenSongAudio = 0.35f;
    public float minTimeBetweenFlyAwayAudio = 0.2f;

    GameObject spawnedBird;
    Animator birdAnimator;
    AudioSource birdAudioSource;
    Coroutine sequenceCoroutine;
    Coroutine fleeCoroutine;
    Coroutine flyCoroutine;
    readonly List<GameObject> activeFeatherEmitters = new List<GameObject>();

    const string FlyingBoolName = "flying";
    const string LandingBoolName = "landing";
    const float MinFleeDuration = 0.05f;
    static readonly int FlyAnimationHash = Animator.StringToHash("Base Layer.fly");
    static float nextAnyBirdAudioTime;
    static float nextSongAudioTime;
    static float nextFlyAwayAudioTime;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnBird();
        }

        if (playSequenceOnStart)
        {
            StartAnimationSequence();
        }
    }

    void Update()
    {
        if (useFleeHotkey && Input.GetKeyDown(fleeKey))
        {
            FleeAndReturn();
        }
    }

    public GameObject SpawnBird()
    {
        RemoveSpawnedBird();

        string resourceName = GetBirdResourceName();
        GameObject birdPrefab = Resources.Load<GameObject>(resourceName);
        if (birdPrefab == null)
        {
            Debug.LogError("lb_BirdExperiment could not load bird prefab from Resources: " + resourceName);
            return null;
        }

        Vector3 spawnPos;
        Quaternion spawnRot;
        if (spawnAtSpecificLocation)
        {
            if (spawnPoint != null)
            {
                spawnPos = spawnPoint.position;
                spawnRot = spawnPoint.rotation;
            }
            else
            {
                spawnPos = spawnPosition;
                spawnRot = Quaternion.Euler(spawnEulerRotation);
            }
        }
        else
        {
            spawnPos = transform.position;
            spawnRot = transform.rotation;
        }

        spawnedBird = Instantiate(birdPrefab, spawnPos, spawnRot, transform);
        birdAnimator = spawnedBird.GetComponent<Animator>();
        birdAudioSource = spawnedBird.GetComponent<AudioSource>();
        ConfigureAnimationEventReceiver();

        if (disableDefaultBirdBehaviour)
        {
            Rigidbody birdBody = spawnedBird.GetComponent<Rigidbody>();
            if (birdBody != null)
            {
                birdBody.linearVelocity = Vector3.zero;
                birdBody.isKinematic = true;
            }
        }

        return spawnedBird;
    }

    void ConfigureAnimationEventReceiver()
    {
        if (spawnedBird == null)
        {
            return;
        }

        lb_BirdAnimationEventReceiver eventReceiver = spawnedBird.GetComponent<lb_BirdAnimationEventReceiver>();
        if (eventReceiver == null)
        {
            eventReceiver = spawnedBird.AddComponent<lb_BirdAnimationEventReceiver>();
        }

        eventReceiver.Configure(
            birdAnimator,
            birdAudioSource,
            playAudio && playSongAudioOnSingTrigger,
            song1,
            song2,
            songVolume * songVolumeBoost,
            playSongAtBirdPosition,
            songPositionOffset);
    }

    public void StartAnimationSequence()
    {
        if (spawnedBird == null)
        {
            SpawnBird();
        }

        if (birdAnimator == null)
        {
            Debug.LogWarning("lb_BirdExperiment cannot play sequence because the spawned bird has no Animator.");
            return;
        }

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceCoroutine = StartCoroutine(PlayAnimationSequence());
    }

    public void StopAnimationSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    public void Flee()
    {
        if (!canFlee || spawnedBird == null)
        {
            return;
        }

        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
        }

        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }

        fleeCoroutine = StartCoroutine(FleeRoutine());
    }

    public void FleeAndReturn()
    {
        if (!canFlee || spawnedBird == null)
        {
            return;
        }

        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
        }

        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }

        fleeCoroutine = StartCoroutine(FleeAndReturnRoutine());
    }

    public void FleeTowardLocation(Vector3 worldPosition)
    {
        if (!canFlee || spawnedBird == null)
        {
            if (enableMovementDebugLogs)
            {
                Debug.Log($"{name} ignored FleeTowardLocation. canFlee={canFlee}, spawnedBirdExists={spawnedBird != null}", this);
            }
            return;
        }

        if (enableMovementDebugLogs)
        {
            Debug.Log($"{name} FleeTowardLocation from {spawnedBird.transform.position} to {worldPosition}", this);
        }

        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
        }

        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }

        fleeCoroutine = StartCoroutine(FleeTowardLocationRoutine(worldPosition));
    }

    public void FleeTowardTransform(Transform target)
    {
        if (target == null)
        {
            if (enableMovementDebugLogs)
            {
                Debug.LogWarning($"{name} received null target in FleeTowardTransform.", this);
            }
            return;
        }

        if (enableMovementDebugLogs)
        {
            Debug.Log($"{name} FleeTowardTransform target={target.name}, targetPosition={target.position}", this);
        }

        FleeTowardLocation(target.position);
    }

    public void FlyToLocation(Vector3 worldPosition)
    {
        if (spawnedBird == null)
        {
            SpawnBird();
        }

        if (spawnedBird == null)
        {
            return;
        }

        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
            fleeCoroutine = null;
        }

        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
        }

        flyCoroutine = StartCoroutine(FlyToLocationRoutine(worldPosition));
    }

    public void FlyToTransform(Transform target)
    {
        if (target == null)
        {
            return;
        }

        FlyToLocation(target.position);
    }

    public void SpawnBirdAt(Vector3 worldPosition)
    {
        spawnAtSpecificLocation = true;
        spawnPoint = null;
        spawnPosition = worldPosition;
        SpawnBird();
    }

    public void SpawnBirdAtAttachedTransform()
    {
        spawnAtSpecificLocation = false;
        SpawnBird();
    }

    public GameObject SpawnFeatherEmitterAtBird()
    {
        if (spawnedBird == null)
        {
            return null;
        }

        return SpawnFeatherEmitterAt(spawnedBird.transform.position, spawnedBird.transform.rotation);
    }

    public GameObject SpawnFeatherEmitterAt(Vector3 worldPosition)
    {
        return SpawnFeatherEmitterAt(worldPosition, Quaternion.identity);
    }

    public GameObject SpawnFeatherEmitterAt(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!enableFeatherEmitter)
        {
            return null;
        }

        GameObject sourcePrefab = ResolveFeatherEmitterPrefab();
        if (sourcePrefab == null)
        {
            Debug.LogWarning("lb_BirdExperiment could not find a feather emitter prefab.");
            return null;
        }

        Vector3 basePosition = featherEmitterUseBirdPosition && spawnedBird != null
            ? spawnedBird.transform.position
            : worldPosition;
        Quaternion baseRotation = featherEmitterUseBirdPosition && spawnedBird != null
            ? spawnedBird.transform.rotation
            : worldRotation;

        Vector3 finalPosition = basePosition + baseRotation * featherEmitterPositionOffset;
        Quaternion finalRotation = baseRotation * Quaternion.Euler(featherEmitterRotationOffset);

        GameObject emitter = Instantiate(sourcePrefab, finalPosition, finalRotation, transform);
        activeFeatherEmitters.Add(emitter);

        float safeLifetime = Mathf.Max(0.05f, featherEmitterLifetime);
        StartCoroutine(DisableAndCleanupFeatherEmitter(emitter, safeLifetime));
        return emitter;
    }

    IEnumerator PlayAnimationSequence()
    {
        if (animationSequence == null || animationSequence.Count == 0)
        {
            Debug.LogWarning("lb_BirdExperiment has no animation steps configured.");
            sequenceCoroutine = null;
            yield break;
        }

        int lastPlayedStepIndex = -1;

        do
        {
            for (int i = 0; i < animationSequence.Count; i++)
            {
                int stepIndex = Random.Range(0, animationSequence.Count);
                if (animationSequence.Count > 1 && stepIndex == lastPlayedStepIndex)
                {
                    stepIndex = (stepIndex + Random.Range(1, animationSequence.Count)) % animationSequence.Count;
                }

                AnimationStep step = animationSequence[stepIndex];
                if (step == null)
                {
                    continue;
                }

                if (stepSkipChance > 0.0f && Random.value < stepSkipChance)
                {
                    continue;
                }

                step.Apply(birdAnimator);
                TryPlaySongAudioForStep(step);
                lastPlayedStepIndex = stepIndex;

                float holdTime = GetRandomizedHoldTime(step.holdTimeSeconds);
                if (holdTime > 0.0f)
                {
                    yield return new WaitForSeconds(holdTime);
                }
                else
                {
                    yield return null;
                }
            }
        }
        while (loopSequence);

        sequenceCoroutine = null;
    }

    IEnumerator FleeRoutine()
    {
        StopAnimationSequence();

        Vector3 startPos = spawnedBird.transform.position;
        Vector3 fleeTarget = startPos + BuildRandomFleeOffset();
        yield return MoveBirdTo(startPos, fleeTarget, fleeDuration, spawnFeatherEmitterOnFlee);

        if (spawnedBird != null)
        {
            Destroy(spawnedBird);
            spawnedBird = null;
            birdAnimator = null;
            birdAudioSource = null;
        }

        fleeCoroutine = null;
    }

    IEnumerator FleeAndReturnRoutine()
    {
        StopAnimationSequence();

        Vector3 startPos = spawnedBird.transform.position;
        Quaternion startRot = spawnedBird.transform.rotation;
        Vector3 fleeTarget = startPos + BuildRandomFleeOffset();

        yield return MoveBirdTo(startPos, fleeTarget, fleeDuration, spawnFeatherEmitterOnFlee);

        if (fleeReturnPause > 0.0f)
        {
            yield return new WaitForSeconds(fleeReturnPause);
        }

        yield return MoveBirdTo(fleeTarget, startPos, fleeDuration);

        spawnedBird.transform.rotation = startRot;
        SetFleeAnimationState(false);
        fleeCoroutine = null;

        if (resumeSequenceAfterFleeReturn)
        {
            StartAnimationSequence();
        }
    }

    IEnumerator FleeTowardLocationRoutine(Vector3 targetPosition)
    {
        StopAnimationSequence();

        Vector3 startPos = spawnedBird.transform.position;
        yield return MoveBirdTo(startPos, targetPosition, fleeDuration, spawnFeatherEmitterOnFlee);

        if (spawnedBird != null)
        {
            Destroy(spawnedBird);
            spawnedBird = null;
            birdAnimator = null;
            birdAudioSource = null;
        }

        fleeCoroutine = null;
    }

    IEnumerator FlyToLocationRoutine(Vector3 targetPosition)
    {
        StopAnimationSequence();

        Vector3 startPos = spawnedBird.transform.position;
        yield return MoveBirdTo(startPos, targetPosition, flyToLocationDuration);

        SetFleeAnimationState(false);
        flyCoroutine = null;

        if (resumeSequenceAfterFlyToLocation)
        {
            StartAnimationSequence();
        }
    }

    void OnDisable()
    {
        StopAnimationSequence();
        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
            fleeCoroutine = null;
        }
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }

        ClearFeatherEmitters();
    }

    void RemoveSpawnedBird()
    {
        StopAnimationSequence();
        if (fleeCoroutine != null)
        {
            StopCoroutine(fleeCoroutine);
            fleeCoroutine = null;
        }
        if (flyCoroutine != null)
        {
            StopCoroutine(flyCoroutine);
            flyCoroutine = null;
        }

        if (spawnedBird != null)
        {
            Destroy(spawnedBird);
            spawnedBird = null;
            birdAnimator = null;
            birdAudioSource = null;
        }
    }

    void TryPlaySongAudioForStep(AnimationStep step)
    {
        if (!playAudio || !playSongAudioOnSingTrigger || step == null)
        {
            return;
        }

        if (step.parameterType != AnimationParameterType.Trigger)
        {
            return;
        }

        if (step.parameterName != singTriggerParameterName)
        {
            return;
        }

        PlayRandomSongClip();
    }

    void PlayRandomSongClip()
    {
        if (!CanPlayBirdAudio(false))
        {
            return;
        }

        AudioClip clip = GetRandomClip(song1, song2);
        PlayClip(clip, songVolume * songVolumeBoost, true);
    }

    void PlayRandomFlyAwayClip()
    {
        if (!CanPlayBirdAudio(true))
        {
            return;
        }

        AudioClip clip = GetRandomClip(flyAway1, flyAway2);
        PlayClip(clip, flyAwayVolume * flyAwayVolumeBoost, false);
    }

    bool CanPlayBirdAudio(bool isFlyAway)
    {
        if (!playAudio)
        {
            return false;
        }

        float playChance = isFlyAway ? flyAwayPlayChance : songPlayChance;
        if (playChance < 1.0f && Random.value > Mathf.Clamp01(playChance))
        {
            return false;
        }

        if (!useGlobalAudioThrottling)
        {
            return true;
        }

        float now = Time.time;
        if (now < nextAnyBirdAudioTime)
        {
            return false;
        }

        if (isFlyAway)
        {
            if (now < nextFlyAwayAudioTime)
            {
                return false;
            }

            nextFlyAwayAudioTime = now + Mathf.Max(0.0f, minTimeBetweenFlyAwayAudio);
        }
        else
        {
            if (now < nextSongAudioTime)
            {
                return false;
            }

            nextSongAudioTime = now + Mathf.Max(0.0f, minTimeBetweenSongAudio);
        }

        nextAnyBirdAudioTime = now + Mathf.Max(0.0f, minTimeBetweenAnyBirdAudio);
        return true;
    }

    AudioClip GetRandomClip(AudioClip clipA, AudioClip clipB)
    {
        if (clipA == null && clipB == null)
        {
            return null;
        }

        if (clipA == null)
        {
            return clipB;
        }

        if (clipB == null)
        {
            return clipA;
        }

        return Random.value < 0.5f ? clipA : clipB;
    }

    void PlayClip(AudioClip clip, float volume, bool isSong)
    {
        if (!playAudio || clip == null)
        {
            return;
        }

        bool playAtBird = isSong ? playSongAtBirdPosition : playFlyAwayAtBirdPosition;
        Vector3 offset = isSong ? songPositionOffset : flyAwayPositionOffset;
        if (playAtBird && spawnedBird != null)
        {
            PlayPositionalClip(clip, spawnedBird.transform.position + offset, volume);
            return;
        }

        if (birdAudioSource == null)
        {
            return;
        }

        birdAudioSource.PlayOneShot(clip, Mathf.Max(0.0f, volume));
    }

    void PlayPositionalClip(AudioClip clip, Vector3 worldPosition, float volume)
    {
        if (clip == null)
        {
            return;
        }

        GameObject tempAudioObject = new GameObject("lb_birdPositionalAudio");
        tempAudioObject.transform.position = worldPosition;

        AudioSource source = tempAudioObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1.0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.01f, positionalAudioMinDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.01f, positionalAudioMaxDistance);
        source.clip = clip;
        source.volume = Mathf.Max(0.0f, volume);
        source.Play();

        Destroy(tempAudioObject, clip.length + 0.1f);
    }

    void ClearFeatherEmitters()
    {
        for (int i = activeFeatherEmitters.Count - 1; i >= 0; i--)
        {
            if (activeFeatherEmitters[i] != null)
            {
                Destroy(activeFeatherEmitters[i]);
            }
        }

        activeFeatherEmitters.Clear();
    }

    string GetBirdResourceName()
    {
        string baseName;
        switch (birdType)
        {
            case BirdType.robin:
                baseName = "lb_robin";
                break;
            case BirdType.blueJay:
                baseName = "lb_blueJay";
                break;
            case BirdType.cardinal:
                baseName = "lb_cardinal";
                break;
            case BirdType.chickadee:
                baseName = "lb_chickadee";
                break;
            case BirdType.sparrow:
                baseName = "lb_sparrow";
                break;
            case BirdType.goldFinch:
                baseName = "lb_goldFinch";
                break;
            case BirdType.crow:
                baseName = "lb_crow";
                break;
            default:
                baseName = "lb_robin";
                break;
        }

        return highQuality ? baseName + "HQ" : baseName;
    }

    float GetRandomizedHoldTime(float baseHoldTime)
    {
        float offset = Random.Range(holdTimeRandomOffset.x, holdTimeRandomOffset.y);
        return Mathf.Max(0.0f, baseHoldTime + offset);
    }

    Vector3 BuildRandomFleeOffset()
    {
        Vector2 horizontal = Random.insideUnitCircle.normalized;
        if (horizontal == Vector2.zero)
        {
            horizontal = Vector2.right;
        }

        float distance = Random.Range(fleeDistanceMin, fleeDistanceMax);
        Vector3 horizontalOffset = new Vector3(horizontal.x, 0.0f, horizontal.y) * distance;
        Vector3 verticalOffset = Vector3.up * Mathf.Max(0.0f, fleeHeight);
        return horizontalOffset + verticalOffset;
    }

    void SetFleeAnimationState(bool isFleeing)
    {
        if (birdAnimator == null)
        {
            return;
        }

        birdAnimator.SetBool(FlyingBoolName, isFleeing);
        birdAnimator.SetBool(LandingBoolName, false);
    }

    IEnumerator MoveBirdTo(Vector3 from, Vector3 to, float duration, bool spawnEmitterAtTakeoff = false)
    {
        if (spawnedBird == null)
        {
            yield break;
        }

        if (enableMovementDebugLogs)
        {
            float requestedDistance = Vector3.Distance(from, to);
            Debug.Log($"{name} MoveBirdTo started. from={from}, to={to}, distance={requestedDistance:F2}, duration={duration:F2}", this);
        }

        Rigidbody birdBody = spawnedBird.GetComponent<Rigidbody>();
        if (birdBody != null)
        {
            birdBody.linearVelocity = Vector3.zero;
            birdBody.isKinematic = true;
        }

        SetFleeAnimationState(true);

        if (preFlightDelay > 0.0f)
        {
            yield return new WaitForSeconds(preFlightDelay);
        }

        if (waitForFlyAnimationState)
        {
            yield return WaitForFlyAnimationState();
        }

        if (playFlyAwayAudioOnTakeoff)
        {
            PlayRandomFlyAwayClip();
        }

        if (spawnEmitterAtTakeoff)
        {
            SpawnFeatherEmitterAtExactPosition(spawnedBird.transform.position, spawnedBird.transform.rotation);
        }

        float safeDuration = Mathf.Max(MinFleeDuration, duration);
        float t = 0.0f;
        float nextProgressLog = Mathf.Max(0.1f, movementDebugLogInterval);
        while (t < safeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / safeDuration);
            Vector3 nextPos = Vector3.Lerp(from, to, normalized);
            Vector3 facingDir = nextPos - spawnedBird.transform.position;
            if (facingDir.sqrMagnitude > 0.0001f)
            {
                spawnedBird.transform.rotation = Quaternion.LookRotation(facingDir.normalized);
            }
            spawnedBird.transform.position = nextPos;

            if (enableMovementDebugLogs && t >= nextProgressLog)
            {
                Debug.Log($"{name} MoveBirdTo progress={normalized:P0}, currentPosition={spawnedBird.transform.position}", this);
                nextProgressLog += Mathf.Max(0.1f, movementDebugLogInterval);
            }

            yield return null;
        }

        if (enableMovementDebugLogs)
        {
            Debug.Log($"{name} MoveBirdTo complete. finalPosition={spawnedBird.transform.position}", this);
        }
    }

    IEnumerator WaitForFlyAnimationState()
    {
        if (birdAnimator == null)
        {
            yield break;
        }

        float elapsed = 0.0f;
        float timeout = Mathf.Max(0.0f, maxFlyStateWait);
        while (elapsed < timeout)
        {
            if (birdAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == FlyAnimationHash)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    GameObject ResolveFeatherEmitterPrefab()
    {
        if (featherEmitterPrefab != null)
        {
            return featherEmitterPrefab;
        }

        if (string.IsNullOrWhiteSpace(featherEmitterResourceName))
        {
            return null;
        }

        return Resources.Load<GameObject>(featherEmitterResourceName);
    }

    GameObject SpawnFeatherEmitterAtExactPosition(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!enableFeatherEmitter)
        {
            return null;
        }

        GameObject sourcePrefab = ResolveFeatherEmitterPrefab();
        if (sourcePrefab == null)
        {
            return null;
        }

        Vector3 finalPosition = worldPosition + worldRotation * featherEmitterPositionOffset;
        Quaternion finalRotation = worldRotation * Quaternion.Euler(featherEmitterRotationOffset);
        GameObject emitter = Instantiate(sourcePrefab, finalPosition, finalRotation, transform);
        activeFeatherEmitters.Add(emitter);

        float safeLifetime = Mathf.Max(0.05f, featherEmitterLifetime);
        StartCoroutine(DisableAndCleanupFeatherEmitter(emitter, safeLifetime));
        return emitter;
    }

    IEnumerator DisableAndCleanupFeatherEmitter(GameObject emitter, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (emitter != null)
        {
            emitter.SetActive(false);
            activeFeatherEmitters.Remove(emitter);
            Destroy(emitter);
        }
    }
}
