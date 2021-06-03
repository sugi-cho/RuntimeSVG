using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

using Unity.VectorGraphics;

public class RuntimeSvg : MonoBehaviour
{
    [SerializeField] string assetpath;
    [SerializeField] ViewportOptions viewPrtOptions;
    VectorUtils.TessellationOptions tessellateOptions;
    PathProperties pathProperties;

    public Mesh mesh;

    // Start is called before the first frame update
    void OnEnable()
    {
        SVGParser.SceneInfo sceneInfo;
        using (var stream = new StreamReader(assetpath))
            sceneInfo = SVGParser.ImportSVG(stream, viewPrtOptions);

        // Automatically compute sensible tessellation options from the
        // vector scene's bouding box and target resolution
        // from package SVGImporter.cs
        float stepDist;
        float samplingStepDist = 100f;
        float maxCord;
        float maxTangent;
        ComputeTessellationOptions(sceneInfo, 400, 1f, out stepDist, out maxCord, out maxTangent);
        tessellateOptions = new VectorUtils.TessellationOptions();
        tessellateOptions.MaxCordDeviation = maxCord;
        tessellateOptions.MaxTanAngleDeviation = maxTangent;
        tessellateOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
        tessellateOptions.StepDistance = 1f;

        pathProperties = new PathProperties { Corners = PathCorner.Tipped, Head = PathEnding.Chop, Tail = PathEnding.Chop, Stroke = new Stroke { Color = Color.white } };

        var beziers = GetAllBeziers(sceneInfo.Scene.Root);
        var pathes = new List<Vector2[]>();
        foreach (var b in beziers)
        {
            var path = TessellateBezierPath(b);
            pathes.Add(path);
        }
        GenerateMesh(pathes);
        GetComponent<MeshFilter>().mesh = mesh;
    }

    List<BezierContour> GetAllBeziers(SceneNode node)
    {

        var beziers = new List<BezierContour>();
        if (node.Shapes != null)
            beziers.AddRange(node.Shapes.SelectMany(s => s.Contours));
        if (node.Children != null)
            beziers.AddRange(node.Children.SelectMany(n => GetAllBeziers(n)));
        return beziers;
    }

    Vector2[] TessellateBezierPath(BezierContour countour)
    {
        Vector2[] path;
        ushort[] indeces;
        VectorUtils.TessellatePath(countour, pathProperties, tessellateOptions, out path, out indeces);
        var closed = countour.Closed;
        var center = new Vector2(path.Average(v2 => v2.x), path.Average(v2 => v2.y));
        if (closed)
            path = path.Concat(new[] { path[0] }).ToArray();
        path = path.Select(p =>
        {
            p = p - center;
            p *= 50f / center.y;
            p.y *= -1;
            return p;
        }).Distinct().ToArray();
        return path;
    }
    private void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
    {
        // These tessellation options were found by trial and error to find values that made
        // visual sense with a variety of SVG assets.

        // "Pixels per Unit" doesn't make sense for UI Toolkit since it will be displayed in
        // a pixels space.  We adjust the magic values below accordingly.
        var ppu = 100f;

        var bbox = VectorUtils.ApproximateSceneNodeBounds(sceneInfo.Scene.Root);
        float maxDim = Mathf.Max(bbox.width, bbox.height) / ppu;

        // The scene ratio gives a rough estimate of coverage % of the vector scene on the screen.
        // Higher values should result in a more dense tessellation.
        float sceneRatio = maxDim / (targetResolution * multiplier);

        stepDist = float.MaxValue; // No need for uniform step distance

        maxCord = Mathf.Max(0.01f, 2.0f * sceneRatio);
        maxTangent = Mathf.Max(0.1f, 3.0f * sceneRatio);
    }

    void GenerateMesh(List<Vector2[]> pathes)
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        var vertList = new List<Vector3> { Vector3.zero };
        var uvList = new List<Vector2> { Vector2.zero };
        var idxList = new List<int>();

        var strokeCount = pathes.Sum(p => p.Length - 1);

        void AddPath(Vector2[] path)
        {
            var idx0 = vertList.Count;
            var x0 = uvList.Last().x;
            var x1 = x0 + (path.Length - 1f) / strokeCount;
            vertList.AddRange(path.Select(v2 => new Vector3(v2.x, v2.y, 100f)));
            uvList.AddRange(
                Enumerable.Range(0, path.Length)
                .Select(idx => new Vector2(Mathf.Lerp(x0, x1, idx / (path.Length - 1f)), 1f))
            );
            idxList.AddRange(
                Enumerable.Range(0, path.Length - 1)
                .SelectMany(i => new[] { 0, idx0 + i + 1, idx0 + i })
            );
        }
        pathes.ForEach(p => AddPath(p));

        mesh.SetVertices(vertList);
        mesh.SetUVs(0, uvList);
        mesh.SetIndices(idxList, MeshTopology.Triangles, 0);
    }
}
