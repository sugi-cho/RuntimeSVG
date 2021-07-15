using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.VectorGraphics;

public class MergedPathes : MonoBehaviour
{
    [SerializeField] List<Vector2> mergedPath;
    [SerializeField] Line l1;
    [SerializeField] Line l2;

    [ContextMenu("merge")]
    void Merge()
    {
        var pathes = GetComponentsInChildren<PathObject>()
            .Select(po => po.Path.Select(p =>
                (Vector2)po.transform.TransformPoint(p)).ToArray())
            .ToList();

        mergedPath = new List<Vector2>(pathes[0]);
        mergedPath.AddRange(pathes[1]);
    }
    [System.Serializable]
    struct Line
    {
        public Vector2 start;
        public Vector2 end;
    }

    bool LineIntersection(Line l1, Line l2, out Vector2 point)
    {
        var a1 = l1.end.y - l1.start.y;
        var b1 = l1.start.x - l1.end.x;
        var c1 = a1 * l1.start.x + b1 * l1.start.y;

        var a2 = l2.end.y - l2.start.y;
        var b2 = l2.start.x - l2.end.x;
        var c2 = a2 * l2.start.x + b2 * l2.start.y;

        var delta = a1 * b2 - a2 * b1;

        var success = 0 != delta;
        point = success ? new Vector2((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta) : Vector2.zero;
        foreach (var l in new[] { l1, l2 })
        {
            var (min, max) = (Mathf.Min(l.start.x, l.end.x), Mathf.Max(l.start.x, l.end.x));
            success &= min < point.x && point.x < max;
        }
        return success;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (var i = 0; i < mergedPath.Count; i++)
        {
            var p0 = mergedPath[i];
            var p1 = mergedPath[(i + 1) % mergedPath.Count];
            Gizmos.DrawLine(p0, p1);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawLine(l1.start, l1.end);
        Gizmos.DrawLine(l2.start, l2.end);
        Vector2 point;
        if (LineIntersection(l1, l2, out point))
            Gizmos.DrawSphere(point, 0.1f);
    }
}
