using UnityEngine;
// using UnityEngine.Splines;
// Splines can be used but requires the Splines package.
// This would allow for more complex curves, but for a simple arc, the code here is sufficient.

public class CurveDraw : MonoBehaviour
{
    public float radius = 1f;
    public int segments = 25;   // 50 // (smoother)
    public Color lineColor = Color.red;
    public float lineWidth = 0.05f;

    void Start()
    {
        GameObject curveObject = new GameObject("Curve");
        //curveObject.transform.SetParent(transform, false);
        LineRenderer lineRenderer = curveObject.AddComponent<LineRenderer>();
        int arcPoints = Mathf.Max(2, segments + 1);

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.positionCount = arcPoints;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // Assign a URP-compatible material so the mesh does not render as magenta.
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");

        if (shader != null)
        {
            Material runtimeMaterial = new Material(shader);
            runtimeMaterial.color = lineColor;
            lineRenderer.material = runtimeMaterial;
        }
        else
        {
            Debug.LogWarning("CurveDraw: No compatible shader found. Assign a material manually.");
        }

        for (int i = 0; i < arcPoints; i++)
        {
            float t = (float)i / (arcPoints - 1);
            float angle = Mathf.PI * t;
            lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}
