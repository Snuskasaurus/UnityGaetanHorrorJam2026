using UnityEngine;

public class CrawlerExit : MonoBehaviour
{
    void Start()
    {
        CrawlerManager.RegisterCrawlerExitPosition(transform.position);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawIcon(transform.position, "CrawlerExit.png", true);
    }
}