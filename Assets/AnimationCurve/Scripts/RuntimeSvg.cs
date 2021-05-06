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
    [SerializeField] SVGParser.SceneInfo sceneInfo;
    public Vector2[] path;

    // Start is called before the first frame update
    void Start()
    {
        using (var stream = new StreamReader(assetpath))
            sceneInfo = SVGParser.ImportSVG(stream, viewPrtOptions);
        var children = sceneInfo.Scene.Root.Children;
        children.ForEach(c =>
        {
            c.Shapes.ForEach(s =>
            {
                foreach (var bezier in s.Contours)
                    foreach (var seg in bezier.Segments) { }
            });
        });

        var tessellateOptions = new VectorUtils.TessellationOptions { StepDistance = 10, MaxCordDeviation = 10f, MaxTanAngleDeviation = 10, SamplingStepSize = 0f };
        var pathProps = new PathProperties { Corners = PathCorner.Tipped, Head = PathEnding.Chop, Tail = PathEnding.Chop, Stroke = new Stroke { Color = Color.white } };
        var indeces = new ushort[0];
        VectorUtils.TessellatePath(sceneInfo.Scene.Root.Children[0].Shapes[0].Contours[0], pathProps, tessellateOptions, out path, out indeces);
        var center = new Vector2(path.Average(v2 => v2.x), path.Average(v2 => v2.y));
        path = path.Select(p => p - center).Distinct().ToArray();
    }

    // Update is called once per frame
    void Update()
    {

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
