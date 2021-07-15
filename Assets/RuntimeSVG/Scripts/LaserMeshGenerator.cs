using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.VectorGraphics;

public class LaserMeshGenerator
{
#if UNITY_EDITOR
    [MenuItem("Assets/Generate/Laser Mesh")]
    public static void GenerateMesh()
    {
        foreach (var o in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(o);
            Debug.Log(path);

            if (Path.GetExtension(path).ToLower() == ".svg")
            {
                var mesh = GenerateMesh(path);
                AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath($"Assets/{Path.GetFileName(path)}.asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = mesh;
            }
        }
    }
    static Mesh GenerateMesh(string path)
    {
        SVGParser.SceneInfo sceneInfo;
        using (var stream = new StreamReader(path))
            sceneInfo = SVGParser.ImportSVG(stream, ViewportOptions.DontPreserve);
        return GenerateMesh(sceneInfo);
    }
#endif

    public static Mesh GenerateMesh(SVGParser.SceneInfo sceneInfo)
    {
        float stepDist;
        float samplingStepDist = 100f;
        float maxCord;
        float maxTangent;
        ComputeTessellationOptions(sceneInfo, 400, 1f, out stepDist, out maxCord, out maxTangent);
        var tessellateOptions = new VectorUtils.TessellationOptions();
        tessellateOptions.MaxCordDeviation = maxCord;
        tessellateOptions.MaxTanAngleDeviation = maxTangent;
        tessellateOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
        tessellateOptions.StepDistance = 1f;

        var pathProperties = new PathProperties { Corners = PathCorner.Tipped, Head = PathEnding.Chop, Tail = PathEnding.Chop, Stroke = new Stroke() };

        var shapes = GetAllShapes(sceneInfo.Scene.Root, sceneInfo.Scene.Root.Transform);
        var pathes = new List<Vector2[]>();
        foreach (var s in shapes)
            foreach (var bezier in s.Contours)
            {
                var p = TessellateBezierPath(bezier, pathProperties, tessellateOptions, s.FillTransform);
                pathes.Add(p);
            }

        var ps = pathes.SelectMany(p => p);
        var min = new Vector2(ps.Select(v => v.x).Min(), ps.Select(v => v.y).Min());
        var max = new Vector2(ps.Select(v => v.x).Max(), ps.Select(v => v.y).Max());
        var center = (min + max) / 2f;
        var size = max - min;

        pathes = pathes.Select(p =>
            p.Select(v =>
            {
                v -= center;
                v = v / size.y * 100f;
                v.y *= -1f;
                return v;
            }).ToArray()
        ).ToList();
        var mesh = GenerateMesh(pathes);
        return mesh;
    }

    static List<Shape> GetAllShapes(SceneNode node, Matrix2D worldTransform)
    {
        var shapes = new List<Shape>();
        if (node.Shapes != null)
            shapes.AddRange(node.Shapes.Select(s =>
            {
                s.FillTransform = worldTransform;
                return s;
            }));
        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                var transform = worldTransform * child.Transform;
                shapes.AddRange(GetAllShapes(child, transform));
            }
        }
        return shapes;
    }

    static Vector2[] TessellateBezierPath(BezierContour bezier, PathProperties pathProperties, VectorUtils.TessellationOptions tessellateOptions, Matrix2D transform)
    {
        Vector2[] path;
        ushort[] indeces;
        VectorUtils.TessellatePath(bezier, pathProperties, tessellateOptions, out path, out indeces);
        var closed = bezier.Closed;
        if (closed)
            path = path.Concat(new[] { path[0] }).ToArray();
        path = path.Select(p =>
        {
            p = transform * p;
            return p;
        }).Distinct().ToArray();
        return path;
    }
    private static void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
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

    static Mesh GenerateMesh(List<Vector2[]> pathes)
    {
        var mesh = new Mesh();
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

        return mesh;
    }


    [MenuItem("Assets/Generate/_Show Info")]
    public static void ShowInfo()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (Path.GetExtension(path).ToLower() == ".svg")
        {
            SVGParser.SceneInfo sceneInfo;
            using (var stream = new StreamReader(path))
                sceneInfo = SVGParser.ImportSVG(stream, ViewportOptions.DontPreserve);

            float stepDist;
            float samplingStepDist = 100f;
            float maxCord;
            float maxTangent;
            ComputeTessellationOptions(sceneInfo, 400, 1f, out stepDist, out maxCord, out maxTangent);
            var tessellateOptions = new VectorUtils.TessellationOptions();
            tessellateOptions.MaxCordDeviation = maxCord;
            tessellateOptions.MaxTanAngleDeviation = maxTangent;
            tessellateOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
            tessellateOptions.StepDistance = 1f;

            var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessellateOptions);

            foreach (var g in geoms)
            {
                Debug.Log(g.WorldTransform);
                foreach (var v in g.Vertices)
                    Debug.Log(v);
            }
        }
    }
}
