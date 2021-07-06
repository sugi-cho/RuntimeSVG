using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Unity.VectorGraphics;

[RequireComponent(typeof(SpriteRenderer))]
public class CsvToTex : MonoBehaviour
{
    [SerializeField] string path;
    [SerializeField] Texture2D tex;

    private void Start()
    {
        BuildSprite();
    }

    [ContextMenu("build")]
    private void BuildSprite()
    {
        var renderer = GetComponent<SpriteRenderer>();

        SVGParser.SceneInfo sceneInfo;
        using (var stream = new StreamReader(path))
            sceneInfo = SVGParser.ImportSVG(stream, ViewportOptions.DontPreserve);

        var options = MakeLineOptions();
        var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, options);

        var sprite = VectorUtils.BuildSprite(geoms, 100f, VectorUtils.Alignment.Center, Vector2.zero, 64, true);
        renderer.sprite = sprite;
        tex = VectorUtils.RenderSpriteToTexture2D(sprite, 512, 512, renderer.material);
    }
    protected VectorUtils.TessellationOptions MakeLineOptions(float stepDistance = float.MaxValue)
    {
        var options = new VectorUtils.TessellationOptions()
        {
            StepDistance = stepDistance,
            MaxCordDeviation = 0.05f,
            MaxTanAngleDeviation = 0.05f,
            SamplingStepSize = 0.01f
        };

        return options;
    }
}
