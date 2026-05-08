using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class AudioVisualPulse : MonoBehaviour
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
	[SerializeField] private float attractionSoundInterval = 2.0f;
	[SerializeField] private float silenceWhenVeryCloseDistance = 1.25f;
	[SerializeField] private float circleLifetime = 0.45f;
	[SerializeField] private float circleStagger = 0.05f;

	[Header("Facing")]
	[SerializeField] private float facingThreshold = 0.45f;
	[SerializeField] private LayerMask occlusionMask = ~0;
	[SerializeField] private Transform losIgnoreRoot;

	[Header("Screen Spawn")]
	[SerializeField] private float sideInset = 0.08f;
	[SerializeField] private float verticalClamp = 0.1f;
	[SerializeField] private float spawnDepth = 7f;

	[Header("Motion")]
	[SerializeField] private float startRadius = 0.15f;
	[SerializeField] private float endRadius = 1.2f;
	[SerializeField] private float driftDistance = 0.8f;
	[SerializeField] private float rotationOffsetDegrees = 0f;

	[Header("Rendering")]
	[SerializeField] private bool alwaysRenderOnTop = true;
	[SerializeField] private int overlayRenderQueue = 4000;

	[Header("Birb Sphere Pulse")]
	[SerializeField] private bool pulseBirbSphereEmission = true;
	// [SerializeField] private float birbSpherePulseLowIntensity = 0.25f;
	// [SerializeField] private float birbSpherePulseHighIntensity = 2.5f;

	private readonly List<LineRenderer> ringPool = new List<LineRenderer>();
	private readonly Dictionary<LineRenderer, Coroutine> running = new Dictionary<LineRenderer, Coroutine>();
	private List<ChirpAttract> activeAttractions = new List<ChirpAttract>();
	private int nextIndex;
	private float nextAttractionSoundTime;
	private bool wasFacingTarget;
	private Coroutine pulseLoop;
	private bool disableInCurrentScene;

	bool IsFractureScene()
	{
		UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
		return !string.IsNullOrEmpty(activeScene.name) && activeScene.name.IndexOf("Fracture", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	void Awake()
	{
		disableInCurrentScene = IsFractureScene();

		if (disableInCurrentScene)
		{
			enabled = false;
			return;
		}

		if (cam == null)
		{
			cam = Camera.main;
		}

		if (losIgnoreRoot == null && cam != null)
		{
			losIgnoreRoot = cam.transform.root;
		}

		BuildPool();
		RefreshAttractions();
		nextAttractionSoundTime = Time.time;
	}

	void OnEnable()
	{
		if (disableInCurrentScene)
		{
			enabled = false;
			return;
		}

		if (pulseLoop == null)
		{
			pulseLoop = StartCoroutine(PulseLoop());
		}
	}

	void OnDisable()
	{
		if (pulseLoop != null)
		{
			StopCoroutine(pulseLoop);
			pulseLoop = null;
		}
	}

	void FixedUpdate()
	{
		if (disableInCurrentScene || cam == null)
		{
			return;
		}

		RefreshAttractions();

		ChirpAttract closest = GetClosestAttraction();
		
		if (closest == null)
		{
			return;
		}

		Transform attractionTarget = GetAttractionTarget(closest.transform);
		if (attractionTarget == null)
		{
			return;
		}

		Vector3 targetPoint = attractionTarget.position;
		bool isFacingTarget = IsFacingTarget(targetPoint);
		bool hasClearLineOfSight = HasClearLineOfSight(attractionTarget, targetPoint);
		bool shouldHoldBright = isFacingTarget && hasClearLineOfSight;

		//Debug.Log("AudioVisualPulse.FixedUpdate: closest attraction is " + closest.name + ", shouldHoldBright=" + shouldHoldBright);

		if (shouldHoldBright)
		{
			wasFacingTarget = true;
			TrySetBirbSphereFullBrightness(attractionTarget);
			return;
		}

		if (wasFacingTarget)
		{
			wasFacingTarget = false;
			TrySetBirbSphereZeroBrightness(attractionTarget);
			nextAttractionSoundTime = Time.time;
		}
	}

	System.Collections.IEnumerator PulseLoop()
	{
		while (enabled)
		{
			RunPulseTick();
			yield return new WaitForSeconds(Mathf.Max(0.01f, pulseInterval));
		}
	}

	void RunPulseTick()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}

		if (cam == null || ringPool.Count == 0)
		{
			return;
		}

		RefreshAttractions();

		ChirpAttract closest = GetClosestAttraction();
		if (closest == null)
		{
			return;
		}

		Transform attractionTarget = GetAttractionTarget(closest.transform);
		if (attractionTarget == null)
		{
			return;
		}

		Vector3 targetPoint = attractionTarget.position;
		bool isFacingTarget = IsFacingTarget(targetPoint);
		bool hasClearLineOfSight = HasClearLineOfSight(attractionTarget, targetPoint);
		bool shouldPausePulses = isFacingTarget && hasClearLineOfSight;
		float distanceToTarget = Vector3.Distance(cam.transform.position, targetPoint);
		bool shouldSilenceAudio = distanceToTarget <= Mathf.Max(0f, silenceWhenVeryCloseDistance);

		if (shouldPausePulses)
		{
			wasFacingTarget = true;
			TrySetBirbSphereFullBrightness(attractionTarget);

			if (!shouldSilenceAudio && Time.time >= nextAttractionSoundTime)
			{
				closest.PlaySound();
				nextAttractionSoundTime = Time.time + Mathf.Max(0.05f, attractionSoundInterval);
			}

			return;
		}

		if (wasFacingTarget)
		{
			wasFacingTarget = false;
			nextAttractionSoundTime = Time.time;
		}

		if (Time.time >= nextAttractionSoundTime)
		{
			closest.PlaySound();
			nextAttractionSoundTime = Time.time + Mathf.Max(0.05f, attractionSoundInterval);
		}

		EmitPulse(attractionTarget);
	}

	void BuildPool()
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

	void ConfigureOverlayMaterial(Material mat)
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

	void RefreshAttractions()
	{
		activeAttractions = new List<ChirpAttract>(FindObjectsByType<ChirpAttract>(FindObjectsSortMode.None));
	}

	ChirpAttract GetClosestAttraction()
	{
		ChirpAttract best = null;
		float bestSqr = float.MaxValue;
		Vector3 camPos = cam.transform.position;

		for (int i = 0; i < activeAttractions.Count; i++)
		{
			ChirpAttract a = activeAttractions[i];
			if (a == null || !a.gameObject.activeInHierarchy)
			{
				continue;
			}

			Transform target = GetAttractionTarget(a.transform);
			float sqr = (target.position - camPos).sqrMagnitude;
			if (sqr < bestSqr)
			{
				bestSqr = sqr;
				best = a;
			}
		}

		return best;
	}

	Transform GetAttractionTarget(Transform attractionTransform)
	{
		if (attractionTransform == null)
		{
			return null;
		}

		if (attractionTransform.parent != null)
		{
			BirbSphere birbSphere = attractionTransform.parent.GetComponent<BirbSphere>();
			if (birbSphere != null)
			{
				return attractionTransform.parent;
			}
		}

		return attractionTransform;
	}

	bool IsFacingTarget(Vector3 targetWorld)
	{
		Vector3 toTarget = (targetWorld - cam.transform.position).normalized;
		float dot = Vector3.Dot(cam.transform.forward, toTarget);
		return dot > facingThreshold;
	}

	bool HasClearLineOfSight(Transform target, Vector3 targetWorld)
	{
		if (target == null)
		{
			return false;
		}

		Vector3 origin = cam.transform.position;
		Vector3 toTarget = targetWorld - origin;
		float distance = toTarget.magnitude;

		if (distance <= 0.0001f)
		{
			return true;
		}

		Vector3 dir = toTarget / distance;
		Ray ray = new Ray(origin + dir * 0.05f, dir);
		RaycastHit[] hits = Physics.RaycastAll(ray, distance, occlusionMask, QueryTriggerInteraction.Ignore);
		Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

		for (int i = 0; i < hits.Length; i++)
		{
			Collider hitCollider = hits[i].collider;
			if (hitCollider == null)
			{
				continue;
			}

			if (ShouldIgnoreAsInvisibleLosBlocker(hitCollider))
			{
				continue;
			}

			Transform hitTransform = hitCollider.transform;
			if (hitTransform == null)
			{
				return false;
			}

			if (IsIgnoredLosHit(hitTransform))
			{
				continue;
			}

			if (hitTransform == target || hitTransform.IsChildOf(target))
			{
				return true;
			}

			return false;
		}

		return true;
	}

	bool ShouldIgnoreAsInvisibleLosBlocker(Collider hitCollider)
	{
		if (hitCollider == null)
		{
			return false;
		}

		Renderer hitRenderer = hitCollider.GetComponent<Renderer>();
		if (hitRenderer == null)
		{
			return false;
		}

		return !hitRenderer.enabled;
	}

	bool IsIgnoredLosHit(Transform hitTransform)
	{
		if (hitTransform == null)
		{
			return false;
		}

		if (cam != null && (hitTransform == cam.transform || hitTransform.IsChildOf(cam.transform)))
		{
			return true;
		}

		if (hitTransform == transform || hitTransform.IsChildOf(transform))
		{
			return true;
		}

		if (losIgnoreRoot != null && (hitTransform == losIgnoreRoot || hitTransform.IsChildOf(losIgnoreRoot)))
		{
			return true;
		}

		return false;
	}

	void EmitPulse(Transform target)
	{
		int count = Mathf.Clamp(circlesPerPulse, 1, ringPool.Count);

		TryPulseBirbSphere(target, count);

		Vector3 toTargetWorld = (target.position - cam.transform.position).normalized;
		Vector3 toTargetLocal = cam.transform.InverseTransformDirection(toTargetWorld);

		Vector2 turnHint = new Vector2(toTargetLocal.x, toTargetLocal.y);
		if (turnHint.sqrMagnitude < 0.0001f)
		{
			// Fallback to screen-center offset if the local hint is too small.
			Vector3 vp = cam.WorldToViewportPoint(target.position);
			turnHint = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
		}

		turnHint = turnHint.sqrMagnitude > 0.0001f ? turnHint.normalized : Vector2.right;
		bool fromLeft = turnHint.x < 0f;

		float x = fromLeft ? sideInset : 1f - sideInset;
		float y = Mathf.Clamp(0.5f + Mathf.Clamp(turnHint.y, -1f, 1f) * 0.35f, verticalClamp, 1f - verticalClamp);
		Vector3 startCenter = cam.ViewportToWorldPoint(new Vector3(x, y, spawnDepth));

		Vector3 targetDir = cam.transform.right * turnHint.x + cam.transform.up * turnHint.y;
		if (targetDir.sqrMagnitude < 0.0001f)
		{
			targetDir = fromLeft ? cam.transform.right : -cam.transform.right;
		}

		targetDir.Normalize();
		targetDir = Quaternion.AngleAxis(rotationOffsetDegrees, cam.transform.forward) * targetDir;

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
			Coroutine c = StartCoroutine(AnimateCircle(lr, startCenter, targetDir, delay));
			running[lr] = c;
		}
	}

	void TryPulseBirbSphere(Transform target, int circleCount)
	{
		if (!pulseBirbSphereEmission || target == null)
		{
			return;
		}

		BirbSphere birbSphere = target.GetComponentInParent<BirbSphere>();
		if (birbSphere == null)
		{
			Debug.Log($"AudioVisualPulse: no BirbSphere found on {target.name} or its parents, so no emission pulse was triggered.");
			return;
		}

		float totalVisualPulseDuration = Mathf.Max(
			0.01f,
			Mathf.Max(0.01f, circleLifetime) + Mathf.Max(0f, (circleCount - 1) * circleStagger));

		Debug.Log($"AudioVisualPulse: pulsing BirbSphere '{birbSphere.name}' for {totalVisualPulseDuration:F2}s.");
		birbSphere.PulseEmission(totalVisualPulseDuration);
	}

	void TrySetBirbSphereFullBrightness(Transform target)
	{
		if (target == null)
		{
			return;
		}

		BirbSphere birbSphere = target.GetComponentInParent<BirbSphere>();
		if (birbSphere == null)
		{
			return;
		}

		birbSphere.SetFullBrightness();
	}

	void TrySetBirbSphereZeroBrightness(Transform target)
	{
		if (target == null)
		{
			return;
		}

		BirbSphere birbSphere = target.GetComponentInParent<BirbSphere>();
		if (birbSphere == null)
		{
			return;
		}

		birbSphere.SetZeroBrightness();
	}

	LineRenderer GetNextRing()
	{
		if (ringPool.Count == 0)
		{
			return null;
		}

		LineRenderer lr = ringPool[nextIndex];
		nextIndex = (nextIndex + 1) % ringPool.Count;
		return lr;
	}

	void DrawCircle(LineRenderer lr, Vector3 center, float radius)
	{
		int segments = lr.positionCount;
		Vector3 right = cam.transform.right;
		Vector3 up = cam.transform.up;

		for (int i = 0; i < segments; i++)
		{
			float t = (float)i / segments;
			float angle = t * Mathf.PI * 2f;
			Vector3 p = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
			lr.SetPosition(i, p);
		}
	}

	System.Collections.IEnumerator AnimateCircle(LineRenderer lr, Vector3 startCenter, Vector3 driftDir, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}

		GameObject go = lr.gameObject;
		go.SetActive(true);

		float elapsed = 0f;
		while (elapsed < circleLifetime)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / circleLifetime);
			float eased = Mathf.SmoothStep(0f, 1f, t);

			float radius = Mathf.Lerp(startRadius, endRadius, eased);
			Vector3 center = startCenter + driftDir * (driftDistance * eased);
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