using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class ScreenLockedAudioVisualPulse : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;

    [Header("Hollow Circles")]
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private float lineWidth = 0.03f;
    [SerializeField] private int circleSegments = 48;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private int circlesPerPulse = 5;

    [Header("Timing")]
    [SerializeField] private float pulseInterval = 1.0f;
    [SerializeField] private float circleLifetime = 0.45f;
    [SerializeField] private float circleStagger = 0.05f;

    [Header("Screen Lock")]
    [Header("Range")]
    [SerializeField] private float stopPulsingWithinDistance = 3.75f;
    [SerializeField] private float screenPadding = 0.08f;
    [SerializeField] private float screenDepth = 7f;
    [SerializeField] private bool flipWhenBehindCamera = true;

    [Header("Motion")]
    [SerializeField] private float startRadius = 0.15f;
    [SerializeField] private float endRadius = 1.2f;
    [SerializeField] private float driftDistance = 0f;
    [SerializeField] private float rotationOffsetDegrees = 0f;

    [Header("Rendering")]
    [SerializeField] private bool alwaysRenderOnTop = true;
    [SerializeField] private int overlayRenderQueue = 4000;

    private readonly List<LineRenderer> ringPool = new List<LineRenderer>();
    private readonly Dictionary<LineRenderer, Coroutine> running = new Dictionary<LineRenderer, Coroutine>();
    private readonly List<ChirpAttract> activeAttractions = new List<ChirpAttract>();
    private Coroutine pulseLoop;
    private int nextIndex;

    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        BuildPool();
    }

    private void OnEnable()
    {
        if (pulseLoop == null)
        {
            pulseLoop = StartCoroutine(PulseLoop());
        }
    }

    private void OnDisable()
    {
        if (pulseLoop != null)
        {
            StopCoroutine(pulseLoop);
            pulseLoop = null;
        }
    }

    private IEnumerator PulseLoop()
    {
        while (enabled)
        {
            EmitPulse();
            yield return new WaitForSeconds(Mathf.Max(0.01f, pulseInterval));
        }
    }

    private void BuildPool()
    {
        int count = Mathf.Max(1, poolSize);
        int segments = Mathf.Max(12, circleSegments);

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("PulseRing_" + i);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = segments;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 4;

            Material mat;
            if (ringMaterial != null)
            {
                mat = new Material(ringMaterial);
            }
            else
            {
                mat = new Material(Shader.Find("Sprites/Default"));
            }

            if (alwaysRenderOnTop)
            {
                ConfigureOverlayMaterial(mat);
            }

            lr.material = mat;
            lr.startColor = ringColor;
            lr.endColor = ringColor;
            go.SetActive(false);

            ringPool.Add(lr);
        }
    }

    private void ConfigureOverlayMaterial(Material mat)
    {
        if (mat == null)
        {
            return;
        }

        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetInt("_ZWrite", 0);
        }

        if (mat.HasProperty("_ZTest"))
        {
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
        }

        mat.renderQueue = Mathf.Max(3000, overlayRenderQueue);
    }

    private void EmitPulse()
    {
        if (cam == null)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                return;
            }
        }

        RefreshAttractions();

        ChirpAttract closest = GetClosestAttraction();
        if (closest == null || ringPool.Count == 0)
        {
            return;
        }

        float distanceToTarget = Vector3.Distance(cam.transform.position, closest.transform.position);
        if (distanceToTarget <= Mathf.Max(0f, stopPulsingWithinDistance))
        {
            return;
        }

        Transform pulseTarget = closest.transform;

        int count = Mathf.Clamp(circlesPerPulse, 1, ringPool.Count);
        Vector3 pulseDirection = cam.transform.right;

        closest.PlaySound();

        for (int i = 0; i < count; i++)
        {
            LineRenderer lr = GetNextRing();
            if (lr == null)
            {
                break;
            }

            if (running.TryGetValue(lr, out Coroutine active) && active != null)
            {
                StopCoroutine(active);
            }

            float delay = i * circleStagger;
            Coroutine c = StartCoroutine(AnimateCircle(lr, pulseTarget, pulseDirection, delay));
            running[lr] = c;
        }
    }

    private void RefreshAttractions()
    {
        activeAttractions.Clear();
        activeAttractions.AddRange(FindObjectsByType<ChirpAttract>(FindObjectsSortMode.None));
    }

    private ChirpAttract GetClosestAttraction()
    {
        ChirpAttract best = null;
        float bestSqr = float.MaxValue;
        Vector3 camPos = cam.transform.position;

        for (int i = 0; i < activeAttractions.Count; i++)
        {
            ChirpAttract attraction = activeAttractions[i];
            if (attraction == null || !attraction.isActiveAndEnabled || !attraction.gameObject.activeInHierarchy)
            {
                continue;
            }

            float sqr = (attraction.transform.position - camPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = attraction;
            }
        }

        return best;
    }

    private Vector3 GetScreenLockedCenter(Vector3 worldPosition)
    {
        Vector3 viewport = cam.WorldToViewportPoint(worldPosition);

        if (flipWhenBehindCamera && viewport.z < 0f)
        {
            viewport.x = 1f - viewport.x;
            viewport.y = 1f - viewport.y;
        }

        float x = Mathf.Clamp(viewport.x, screenPadding, 1f - screenPadding);
        float y = Mathf.Clamp(viewport.y, screenPadding, 1f - screenPadding);
        return cam.ViewportToWorldPoint(new Vector3(x, y, screenDepth));
    }

    private LineRenderer GetNextRing()
    {
        if (ringPool.Count == 0)
        {
            return null;
        }

        LineRenderer lr = ringPool[nextIndex];
        nextIndex = (nextIndex + 1) % ringPool.Count;
        return lr;
    }

    private void DrawCircle(LineRenderer lr, Vector3 center, float radius)
    {
        int segments = lr.positionCount;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        Quaternion rotation = Quaternion.AngleAxis(rotationOffsetDegrees, cam.transform.forward);
        right = rotation * right;
        up = rotation * up;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;
            Vector3 point = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            lr.SetPosition(i, point);
        }
    }

    private IEnumerator AnimateCircle(LineRenderer lr, Transform pulseTarget, Vector3 driftDir, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (lr == null || pulseTarget == null)
        {
            yield break;
        }

        GameObject go = lr.gameObject;
        go.SetActive(true);

        float elapsed = 0f;
        while (elapsed < circleLifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / circleLifetime);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Vector3 center = GetScreenLockedCenter(pulseTarget.position) + driftDir * (driftDistance * eased);
            float radius = Mathf.Lerp(startRadius, endRadius, eased);
            DrawCircle(lr, center, radius);

            float alpha = 1f - eased;
            Color c = ringColor;
            c.a *= alpha;
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        go.SetActive(false);
        running[lr] = null;
    }
}
