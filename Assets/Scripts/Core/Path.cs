using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    public List<Transform> waypoints = new List<Transform>();

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
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints.Add(transform.GetChild(i));
        }

        if (waypoints.Count == 0)
        {
            Debug.LogWarning($"[Path] {name} does not contain any waypoint children.", this);
        }
    }

    public Transform GetWaypoint(int index)
    {
        return index >= 0 && index < waypoints.Count ? waypoints[index] : null;
    }

    public int Count => waypoints.Count;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < transform.childCount; i++)
        {
            var waypoint = transform.GetChild(i);
            if (waypoint)
            {
                Gizmos.DrawSphere(waypoint.position, 0.1f);
            }

            if (i + 1 < transform.childCount)
            {
                var next = transform.GetChild(i + 1);
                Gizmos.DrawLine(waypoint.position, next.position);
            }
        }
    }
}
