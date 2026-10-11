using UnityEngine;
using UnityEngine.Rendering;

public static class DebugDraw
{

#if UNITY_EDITOR
    private static Material debugLineMaterial;
    private static GUIStyle debugLabelStyle;
#endif

    public static void InitializeDebugs_IFN()
    {
#if UNITY_EDITOR
        if (debugLineMaterial == null)
        {
            debugLineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            debugLineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            debugLineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            debugLineMaterial.SetInt("_Cull",     (int)CullMode.Off);
            debugLineMaterial.SetInt("_ZWrite", 0);
        }

        if (debugLabelStyle == null)
        {
            debugLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }
#endif
    }

    public static void BeginLines()
    {
#if UNITY_EDITOR
        debugLineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
#endif
    }

    public static void EndLines()
    {
#if UNITY_EDITOR
        GL.End();
#endif
    }

    public static void DebugLine(Camera cam, Vector3 a, Vector3 b, Color color)
    {
#if UNITY_EDITOR
        if (!WorldToGui(cam, a, out Vector2 pa) || !WorldToGui(cam, b, out Vector2 pb)) return;
        GL.Color(color);
        GL.Vertex3(pa.x, pa.y, 0f);
        GL.Vertex3(pb.x, pb.y, 0f);
#endif
    }

    public static void DebugCross(Camera cam, Vector3 p, float size, Color color)
    {
#if UNITY_EDITOR
        DebugLine(cam, p + Vector3.left * size,    p + Vector3.right * size,   color);
        DebugLine(cam, p + Vector3.down * size,    p + Vector3.up * size,      color);
        DebugLine(cam, p + Vector3.back * size,    p + Vector3.forward * size, color);
#endif
    }

    public static void DebugCircle(Camera cam, Vector3 center, float radius, Color color, int segments = 32)
    {
#if UNITY_EDITOR
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            DebugLine(cam, prev, next, color);
            prev = next;
        }
#endif
    }

    public static void DebugLabel(Camera cam, Vector3 worldPos, Color color, string text)
    {
#if UNITY_EDITOR
        if (!WorldToGui(cam, worldPos, out Vector2 p)) return;

        debugLabelStyle.normal.textColor = Color.black; // drop shadow
        GUI.Label(new Rect(p.x - 99f, p.y - 29f, 200f, 60f), text, debugLabelStyle);

        debugLabelStyle.normal.textColor = color;
        GUI.Label(new Rect(p.x - 100f, p.y - 30f, 200f, 60f), text, debugLabelStyle);
#endif
    }

    private static bool WorldToGui(Camera cam, Vector3 world, out Vector2 gui)
    {
#if UNITY_EDITOR
        Vector3 s = cam.WorldToScreenPoint(world);
        gui = new Vector2(s.x, cam.pixelHeight - s.y);
        return s.z > 0f; // false when behind the camera
#else
        gui = default;
        return false;
#endif
    }
}
