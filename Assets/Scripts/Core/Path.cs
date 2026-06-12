using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Path : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();

    private static readonly Regex TrailingNumberRegex = new Regex(@"(\d+)$", RegexOptions.Compiled);

    private void Awake()
    {
        RebuildFromChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildFromChildren();
    }
#endif

    public void RebuildFromChildren()
    {
        waypoints.Clear();

        var children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (IsWaypointTransform(child))
            {
                children.Add(child);
            }
        }

        // Only real Waypoint0, Waypoint1, Waypoint2... children are allowed into the route.
        // Visual helper children such as RouteBank, RouteFoam and GroundBackground must not
        // become path targets, otherwise boats will drive to invisible render helper objects.
        children.Sort(CompareWaypointTransforms);
        waypoints.AddRange(children);

        if (waypoints.Count == 0 && Application.isPlaying && gameObject.scene.IsValid() && gameObject.scene.isLoaded)
        {
            Debug.LogWarning($"[Path] {name} does not contain any waypoint children named Waypoint0, Waypoint1, etc.", this);
        }
    }

    public Transform GetWaypoint(int index)
    {
        return index >= 0 && index < waypoints.Count ? waypoints[index] : null;
    }

    public int Count => waypoints.Count;

    private static bool IsWaypointTransform(Transform transformToCheck)
    {
        if (!transformToCheck)
        {
            return false;
        }

        string childName = transformToCheck.name;
        return childName.StartsWith("Waypoint") && TryGetTrailingNumber(childName, out _);
    }

    private static int CompareWaypointTransforms(Transform left, Transform right)
    {
        bool leftHasNumber = TryGetTrailingNumber(left ? left.name : string.Empty, out int leftNumber);
        bool rightHasNumber = TryGetTrailingNumber(right ? right.name : string.Empty, out int rightNumber);

        if (leftHasNumber && rightHasNumber && leftNumber != rightNumber)
        {
            return leftNumber.CompareTo(rightNumber);
        }

        if (leftHasNumber != rightHasNumber)
        {
            return leftHasNumber ? -1 : 1;
        }

        int siblingCompare = (left ? left.GetSiblingIndex() : 0).CompareTo(right ? right.GetSiblingIndex() : 0);
        return siblingCompare != 0 ? siblingCompare : string.CompareOrdinal(left ? left.name : string.Empty, right ? right.name : string.Empty);
    }

    private static bool TryGetTrailingNumber(string value, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Match match = TrailingNumberRegex.Match(value);
        return match.Success && int.TryParse(match.Groups[1].Value, out number);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        var orderedWaypoints = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (IsWaypointTransform(child))
            {
                orderedWaypoints.Add(child);
            }
        }

        orderedWaypoints.Sort(CompareWaypointTransforms);

        for (int i = 0; i < orderedWaypoints.Count; i++)
        {
            var waypoint = orderedWaypoints[i];
            if (waypoint)
            {
                Gizmos.DrawSphere(waypoint.position, 0.1f);
            }

            if (i + 1 < orderedWaypoints.Count)
            {
                var next = orderedWaypoints[i + 1];
                if (waypoint && next)
                {
                    Gizmos.DrawLine(waypoint.position, next.position);
                }
            }
        }
    }
}
