using UnityEngine;

public class Common : MonoBehaviour
{
    public static T FindTypeInScene<T>() where T : Object
    {
        T objectFound = FindObjectOfType<T>();
        if (objectFound == null)
        {
            Debug.LogError($"{typeof(T).Name} failed to be found in the scene.");
        }
        return objectFound;
    }

    public static T FindTypeInScene<T>(string objectName) where T : Object
    {
        T objectFound = FindObjectOfType<T>();
        if (objectFound == null)
        {
            Debug.LogError($"Failed to find {objectName} ({typeof(T).Name}) in the scene.");
        }
        return objectFound;
    }
}
