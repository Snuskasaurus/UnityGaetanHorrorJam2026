using System.Collections.Generic;
using UnityEngine;

// Fires an artillery shell around the player at random intervals, mostly aimed at the trenches near him.
// Put it once in the levels that need it.
public class ArtillerySpawner : MonoBehaviour
{
    // Tuning values, read from the artillery settings of GameSettings
    private ArtillerySettings Settings => GameSettings.Artillery;

    // References
    private Transform player;

    // Internal variables
    private float nextStrikeTime;
    private readonly List<Vector3> trenchPoints = new List<Vector3>();
    private readonly List<Vector3> candidates = new List<Vector3>();

    private void Start()
    {
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        CollectTrenchPoints();
        ScheduleNextStrike();
    }

    private void Update()
    {
        if (player != null && Time.time >= nextStrikeTime)
        {
            FireShell();
            ScheduleNextStrike();
        }
    }

    // Fires a shell right now, also usable from the component menu to test in play mode
    [ContextMenu("Fire Shell Now")]
    public void FireShell()
    {
        if (player == null || Settings.shellPrefab == null)
        {
            return;
        }

        bool isClose = Random.value < Settings.closeImpactChance;
        float minDistance = isClose ? Settings.closeMinDistance : Settings.minDistance;
        float maxDistance = isClose ? Settings.closeMaxDistance : Settings.maxDistance;

        Vector3 target;
        if (Random.value >= Settings.trenchImpactChance || !TryPickTrenchTarget(minDistance, maxDistance, out target))
        {
            float distance = Random.Range(minDistance, maxDistance);
            Vector2 direction = Random.insideUnitCircle.normalized;
            target = player.position + new Vector3(direction.x, 0f, direction.y) * distance;
        }

        // The shell lands on whatever is below the target: ground, trench, bunker roof
        Vector3 rayStart = target + Vector3.up * 500f;
        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1000f, Settings.groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        GameObject shell = Instantiate(Settings.shellPrefab);
        shell.GetComponent<ArtilleryShell>().Launch(hit.point);
    }

    // Picks a trench piece at the wanted distance from the player and aims around it
    private bool TryPickTrenchTarget(float minDistance, float maxDistance, out Vector3 target)
    {
        candidates.Clear();
        foreach (Vector3 point in trenchPoints)
        {
            float distance = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(player.position.x, player.position.z));
            if (distance >= minDistance && distance <= maxDistance)
            {
                candidates.Add(point);
            }
        }

        if (candidates.Count == 0)
        {
            target = Vector3.zero;
            return false;
        }

        Vector2 offset = Random.insideUnitCircle * Settings.trenchSpread;
        target = candidates[Random.Range(0, candidates.Count)] + new Vector3(offset.x, 0f, offset.y);
        return true;
    }

    // The trench pieces are found once by name, their centers are the possible targets
    private void CollectTrenchPoints()
    {
        trenchPoints.Clear();
        foreach (MeshRenderer meshRenderer in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            Bounds bounds = meshRenderer.bounds;
            if (bounds.size.x <= Settings.maxTrenchPieceSize && bounds.size.z <= Settings.maxTrenchPieceSize && IsTrenchPiece(meshRenderer.transform))
            {
                trenchPoints.Add(bounds.center);
            }
        }
    }

    // The renderer can be a child of the trench model, so its parents are checked too
    private bool IsTrenchPiece(Transform piece)
    {
        for (Transform current = piece; current != null; current = current.parent)
        {
            foreach (string prefix in Settings.trenchNamePrefixes)
            {
                if (!string.IsNullOrEmpty(prefix) && current.name.StartsWith(prefix))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void ScheduleNextStrike()
    {
        nextStrikeTime = Time.time + Random.Range(Settings.minInterval, Settings.maxInterval);
    }
}
