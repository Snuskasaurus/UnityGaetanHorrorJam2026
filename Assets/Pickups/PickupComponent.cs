using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PickupComponent : MonoBehaviour
{
    BoxCollider boxCollider;
    PickupManager manager;
    bool isHovered = false;

    static Texture2D texture;

    public void SetHovered(bool isHovered)
    {
        this.isHovered = isHovered;
    }

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (texture == null)
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
        }
    }

    void Start()
    {
        // Start runs after every Awake, so the manager is guaranteed to exist
        manager = FindAnyObjectByType<PickupManager>();
        if (manager == null)
        {
            Debug.LogError("Pickup component failed to find manager", this);
            return;
        }

        manager.RegisterPickupComponent(this);
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.UnregisterPickupComponent(this);
    }

    public bool IsInsideShape(Camera camera, Vector2 screenPoint)
    {
        return TryGetScreenRect(camera, out Rect rectangle) && rectangle.Contains(screenPoint);
    }

    bool TryGetScreenRect(Camera camera, out Rect rectangle)
    {
        Vector3 center = boxCollider.center;
        Vector3 extents = boxCollider.size * 0.5f;
        Transform t = boxCollider.transform;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        rectangle = default;

        for (int i = 0; i < 8; i++)
        {
            Vector3 local = center + new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z);

            Vector3 sp = camera.WorldToScreenPoint(t.TransformPoint(local));

            if (sp.z < 0f) return false;

            min = Vector2.Min(min, sp);
            max = Vector2.Max(max, sp);
        }

        rectangle = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    public void DrawRectangle(Camera camera)
    {
        if (!TryGetScreenRect(camera, out Rect screenRectangle))
            return;

        Rect rectangle = new Rect(screenRectangle.xMin, Screen.height - screenRectangle.yMax, screenRectangle.width, screenRectangle.height);

        Color previousColor = GUI.color;
        GUI.color = isHovered ? Color.green : Color.red;

        float thickness = 2f;

        var topRectangle = new Rect(rectangle.x, rectangle.y, rectangle.width, thickness);
        GUI.DrawTexture(topRectangle, texture);

        var botRectangle = new Rect(rectangle.x, rectangle.yMax - thickness, rectangle.width, thickness);
        GUI.DrawTexture(botRectangle, texture);

        var leftRectangle = new Rect(rectangle.x, rectangle.y, thickness, rectangle.height);
        GUI.DrawTexture(leftRectangle, texture);

        var rightRectangle = new Rect(rectangle.xMax - thickness, rectangle.y, thickness, rectangle.height);
        GUI.DrawTexture(rightRectangle, texture);

        GUI.color = previousColor;
    }
}