using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;
using UnityEngine.Splines;
using System.IO;

public class SVGToSpline : MonoBehaviour
{
    [SerializeField] string filePath;
    [SerializeField] int pixelsPerUnit = 100;
    [SerializeField] Vector2 svgSize = new Vector2(512, 512);

    [ContextMenu("convert")]
    void Convert()
    {
        SVGParser.SceneInfo sceneInfo;
        using (var stream = new StreamReader(filePath))
            sceneInfo = SVGParser.ImportSVG(stream, pixelsPerUnit: pixelsPerUnit);
        var root = sceneInfo.Scene.Root;
        AddNode(root, transform, Matrix2D.identity);
    }

    void AddNode(SceneNode node, Transform parent, Matrix2D baseTransform)
    {
        var newNode = new GameObject("Node").transform;
        newNode.parent = parent;
        var matrix = baseTransform * node.Transform;
        if (node.Shapes != null)
            if (0 < node.Shapes.Count)
            {
                var container = newNode.gameObject.AddComponent<SplineContainer>();
                container.RemoveSplineAt(0);
                node.Shapes.ForEach(shape =>
                {
                    foreach (var bezier in shape.Contours)
                    {
                        var spline = new Spline();
                        spline.Closed = bezier.Closed;
                        var segments = bezier.Segments;
                        for (var i = 0; i < segments.Length; i++)
                        {
                            segments[i].P0 = matrix * segments[i].P0 - svgSize * 0.5f / pixelsPerUnit;
                            segments[i].P1 = matrix * segments[i].P1 - svgSize * 0.5f / pixelsPerUnit;
                            segments[i].P2 = matrix * segments[i].P2 - svgSize * 0.5f / pixelsPerUnit;
                            segments[i].P0.y = -segments[i].P0.y;
                            segments[i].P1.y = -segments[i].P1.y;
                            segments[i].P2.y = -segments[i].P2.y;
                        }
                        for (var i = 0; i < segments.Length; i++)
                        {
                            var segment = segments[i];
                            var prev = segments[(segments.Length + i - 1) % segments.Length];
                            var pos = (Vector3)segment.P0;
                            var tIn = (Vector3)prev.P2 - pos;
                            var tOut = (Vector3)segment.P1 - pos;
                            var knot = new BezierKnot(pos, tIn, tOut);
                            spline.Add(knot);
                        }
                        container.AddSpline(spline);
                    }
                });
            }
        if (node.Children != null)
            node.Children.ForEach(child => AddNode(child, newNode, matrix));
    }
}
