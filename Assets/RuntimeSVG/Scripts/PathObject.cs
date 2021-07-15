using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;
using Unity.Mathematics;

public class PathObject : MonoBehaviour
{
    public List<Vector2> Path => path;
    [SerializeField] List<Vector2> path;

    private void OnDrawGizmos()
    {
        for (var i = 0; i < path.Count; i++)
        {
            var p0 = path[i];
            var p1 = path[(i + 1) % path.Count];

            p0 = transform.localToWorldMatrix * new float4(p0, 0, 1);
            p1 = transform.localToWorldMatrix * new float4(p1, 0, 1);

            Gizmos.DrawLine(p0, p1);
        }
    }
}
