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
    [SerializeField] VectorUtils.TessellationOptions tessellationOptions;
    [SerializeField] List<VectorUtils.Geometry> geometries;
    public Vector2[] vertices;

    // Start is called before the first frame update
    void Start()
    { 
        using (var stream = new StreamReader(assetpath))
            sceneInfo = SVGParser.ImportSVG(stream, viewPrtOptions);
        Debug.Log(sceneInfo.Scene.Root.Children.Count);
        var children = sceneInfo.Scene.Root.Children;
        children.ForEach(c =>
        {
            c.Shapes.ForEach(s =>
            {
                foreach (var bezier in s.Contours)
                    foreach (var seg in bezier.Segments) { }
            });
        });

        tessellationOptions = new VectorUtils.TessellationOptions { StepDistance = 10, MaxCordDeviation = 5f, MaxTanAngleDeviation = 10, SamplingStepSize = 0.5f };
        geometries = VectorUtils.TessellateScene(sceneInfo.Scene, tessellationOptions);
        Debug.Log(geometries.Count);

        var g = geometries[0];
        vertices = g.Indices.Select(idx => g.Vertices[idx]).ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
