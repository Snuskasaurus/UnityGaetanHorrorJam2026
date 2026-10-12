using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class DebugDraw
{
#if UNITY_EDITOR
    private static Material debugLineMaterial;
    private static GUIStyle debugLabelStyle;

    // ---- Scene view replay buffers ----
    private struct LineCmd  { public Vector3 a, b; public Color color; }
    private struct LabelCmd { public Vector3 pos; public Color color; public string text; }

    private static readonly List<LineCmd>  sceneLines  = new List<LineCmd>(512);
    private static readonly List<LabelCmd> sceneLabels = new List<LabelCmd>(64);
    private static int lastRecordedFrame = -1;
    private static GUIStyle sceneLabelStyle;

    /// <summary>If true, scene view lines are drawn on top of geometry (like the game view overlay).</summary>
    public static bool SceneAlwaysOnTop = true;

    [InitializeOnLoadMethod]
    private static void RegisterSceneView()
    {
        SceneView.duringSceneGui -= OnSceneGUI; // safe with domain reload disabled
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged += _ => { sceneLines.Clear(); sceneLabels.Clear(); };
    }

    // Only record on Repaint (OnGUI also runs for Layout/Input events) and reset once per rendered frame.
    private static bool ShouldRecord()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.Repaint) return false;

        if (lastRecordedFrame != Time.renderedFrameCount)
        {
            lastRecordedFrame = Time.renderedFrameCount;
            sceneLines.Clear();
            sceneLabels.Clear();
        }
        return true;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (sceneLines.Count == 0 && sceneLabels.Count == 0) return;

        var prevZTest = Handles.zTest;
        var prevColor = Handles.color;
        Handles.zTest = SceneAlwaysOnTop ? CompareFunction.Always : CompareFunction.LessEqual;

        for (int i = 0; i < sceneLines.Count; i++)
        {
            LineCmd l = sceneLines[i];
            Handles.color = l.color;
            Handles.DrawLine(l.a, l.b);
        }

        if (sceneLabelStyle == null)
        {
            sceneLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
        }

        Handles.BeginGUI();
        for (int i = 0; i < sceneLabels.Count; i++)
        {
            LabelCmd lb = sceneLabels[i];
            Vector3 world = lb.pos;

            // skip labels behind the scene camera
            Vector3 vp = sceneView.camera.WorldToViewportPoint(world);
            if (vp.z <= 0f) continue;

            Vector2 p = HandleUtility.WorldToGUIPoint(world);

            sceneLabelStyle.normal.textColor = Color.black; // drop shadow
            GUI.Label(new Rect(p.x - 99f, p.y - 29f, 200f, 60f), lb.text, sceneLabelStyle);

            sceneLabelStyle.normal.textColor = lb.color;
            GUI.Label(new Rect(p.x - 100f, p.y - 30f, 200f, 60f), lb.text, sceneLabelStyle);
        }
        Handles.EndGUI();

        Handles.zTest = prevZTest;
        Handles.color = prevColor;
    }
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
        // Record for the scene view (before the behind-camera early-out: the scene camera is different)
        if (ShouldRecord())
            sceneLines.Add(new LineCmd { a = a, b = b, color = color });

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
        if (ShouldRecord())
            sceneLabels.Add(new LabelCmd { pos = worldPos, color = color, text = text });

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