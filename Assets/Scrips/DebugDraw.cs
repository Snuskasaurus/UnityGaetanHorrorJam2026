using UnityEngine;

public static class DebugDraw
{
    public static void Circle(Vector3 center, float radius, Color color, int segments = 32, float duration = 0f)
    {
#if UNITY_EDITOR
        float step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Debug.DrawLine(prev, next, color, duration);
            prev = next;
        }
#endif
    }
}