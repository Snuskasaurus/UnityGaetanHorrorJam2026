using UnityEngine;

// Kind of ground, used to pick the footstep sounds
public enum Surface
{
    Stone,
    Water,
    Metal,
    Wood,
    Dirt
}

// Put it on a ground object (or one of its parents) to tell which footstep sounds to play on it
public class SurfaceType : MonoBehaviour
{
    [SerializeField]
    private Surface surface = Surface.Stone;

    public Surface Surface => surface;
}
