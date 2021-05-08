using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

using Unity.VectorGraphics;
using UniRx;
using Unity.Collections;
using Unity.Jobs;

public class RuntimeSvg : MonoBehaviour
{
    [SerializeField] string assetpath;
    [SerializeField] ViewportOptions viewPrtOptions;
    [SerializeField] SVGParser.SceneInfo sceneInfo;
    public Vector2[] path;
    public Mesh mesh;

    // Start is called before the first frame update
    void OnEnable()
    {
        using (var stream = new StreamReader(assetpath))
            sceneInfo = SVGParser.ImportSVG(stream, viewPrtOptions);

        var tessellateOptions = new VectorUtils.TessellationOptions { StepDistance = 10, MaxCordDeviation = 10f, MaxTanAngleDeviation = 10, SamplingStepSize = 0f };
        var pathProps = new PathProperties { Corners = PathCorner.Tipped, Head = PathEnding.Chop, Tail = PathEnding.Chop, Stroke = new Stroke { Color = Color.white } };
        var indeces = new ushort[0];
        var bezierContour = sceneInfo.Scene.Root.Children[0].Shapes[0].Contours[0];
        var closed = bezierContour.Closed;
        VectorUtils.TessellatePath(bezierContour, pathProps, tessellateOptions, out path, out indeces);
        var center = new Vector2(path.Average(v2 => v2.x), path.Average(v2 => v2.y));
        path = path.Select(p => { p = p - center; p.y *= -1; return p; }).Distinct().ToArray();
        GenerateMesh(path, closed);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void GenerateMesh(Vector2[] path, bool closed)
    {
        mesh = new Mesh();
        var vertsUvs = path.Select((p, idx) =>
        {
            var v3 = (Vector3)p;
            v3.z = 100;
            var uv = new Vector2(idx / (closed ? path.Length : path.Length - 1f), 1f);
            return (vert: v3, uv: uv);
        });
        var vertList = new List<Vector3> { Vector3.zero };
        vertList.AddRange(vertsUvs.Select(vu => vu.vert));
        var uvList = new List<Vector2> { Vector2.zero };
        uvList.AddRange(vertsUvs.Select(vu => vu.uv));
        var indeces = Enumerable.Range(0, path.Length).SelectMany(i =>
        {
            if (i == path.Length - 1 && !closed)
                return new[] { -1 };
            else
                return new[] { 0, i + 1, (i + 1) % path.Length + 1 };
        }).Where(i => -1 < i).ToList();
        mesh.SetVertices(vertList);
        mesh.SetUVs(0, uvList);
        mesh.SetIndices(indeces, MeshTopology.Triangles, 0);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        for (var i = 0; i < path.Length - 1; i++)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(path[i], path[i + 1]);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(path[i], 1f);
            Gizmos.DrawSphere(path[i + 1], 1f);
        }
    }
}
