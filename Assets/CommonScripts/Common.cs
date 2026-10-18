using UnityEngine;

[System.Serializable]
public struct FloatRange
{
    public float min;
    public float max;

    public FloatRange(float min, float max)
    {
        this.min = min; this.max = max;
    }

    public static float Random(FloatRange range)
    {
        return UnityEngine.Random.Range(range.min, range.max);
    }

    public static float Lerp(FloatRange range, float t)
    {
        return Mathf.Lerp(range.min, range.max, t);
    }

    public static float Clamp(FloatRange range, float v)
    {
        return Mathf.Clamp(v, range.min, range.max);
    }
}

public class Common : MonoBehaviour
{
    public static T FindTypeInScene<T>() where T : Object
    {
        T objectFound = FindAnyObjectByType<T>();
        if (objectFound == null)
        {
            Debug.LogError($"{typeof(T).Name} failed to be found in the scene.");
        }
        return objectFound;
    }

    public static T FindTypeInScene<T>(string objectName) where T : Object
    {
        T objectFound = FindAnyObjectByType<T>();
        if (objectFound == null)
        {
            Debug.LogError($"Failed to find {objectName} ({typeof(T).Name}) in the scene.");
        }
        return objectFound;
    }

    public static float GetCurrentDate()
    {
        return Time.fixedTime;
    }

    public static bool IsBeforeDate(float Date)
    {
        return Time.fixedTime < Date;
    }

    public static bool HasReachedDate(float Date)
    {
        return Time.fixedTime >= Date;
    }
}
