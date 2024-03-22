using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[AddComponentMenu("VFX/Property Binders/SplineBinder")]
[VFXBinder("Spline")]
public class VFXSplineBinder : VFXBinderBase
{
    public string Property { get => (string)m_Property; set => m_Property = value; }
    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    protected ExposedProperty m_Property = "Spline";
    ExposedProperty StartLUT;
    public SplineContainer Target = null;
    [SerializeField] Texture2D splineMap;
    [SerializeField] AnimationCurve curveStartLUT;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateTexture();
        UpdateSubProperty();
    }
    private void OnValidate()
    {
        UpdateTexture();
        UpdateSubProperty();
    }
    void UpdateTexture()
    {
        if (Target == null)
            return;

        var bezierCurves = Target.Splines.SelectMany(spline =>
        {
            var cs = spline.Knots.Select((knot, idx) =>
            {
                var next = spline.Knots.ElementAt((idx + 1) % spline.Knots.Count());
                var curve = new BezierCurve(knot, next);
                return curve;
            })
            .Where((c, idx) => idx < spline.Knots.Count() - 1 | spline.Closed)
            .Select(c => (curve: c, length: CurveUtility.CalculateLength(c)))
            .Where(c => 0 < c.length);
            return cs;
        }).ToList();

        var count = bezierCurves.Count;

        if (splineMap == null || splineMap.height != count)
            splineMap = new Texture2D(4, count, TextureFormat.RGBAFloat, false);

        var totalLength = bezierCurves.Sum(bc => bc.length);
        var curveStarts = Enumerable.Range(0, count)
            .Select(idx => bezierCurves.Where((bc, i) => i < idx).Sum(bc => bc.length) / totalLength)
            .ToList();
        var keyFrames = curveStarts.Select((start, idx) => new Keyframe(
            start,
            idx,
            0 < idx ? 1f / (start - curveStarts[idx - 1]) : 0,
            idx < count - 1 ? 1f / (curveStarts[idx + 1] - start) : 1f / (1f - start)
        )).ToList();
        keyFrames.Add(new Keyframe(1f, count, 1f / (1f - curveStarts.Last()), 0));
        curveStartLUT = new AnimationCurve(keyFrames.ToArray());

        var colors = new List<Color>();
        for (var i = 0; i < count; i++)
        {
            var bc = bezierCurves[i].curve;
            var length = bezierCurves[i].length;
            colors.Add(new Color(bc.P0.x, bc.P0.y, bc.P0.z, curveStarts[i]));
            colors.Add(new Color(bc.P1.x, bc.P1.y, bc.P1.z, length));
            colors.Add(new Color(bc.P2.x, bc.P2.y, bc.P2.z, totalLength));
            colors.Add(new Color(bc.P3.x, bc.P3.y, bc.P3.z, 1f / (length / totalLength)));
        }
        splineMap.name = Target.name + "_SplineMap";
        splineMap.filterMode = FilterMode.Point;
        splineMap.wrapMode = TextureWrapMode.Repeat;
        splineMap.SetPixels(colors.ToArray(), 0);
        splineMap.Apply();
    }
    void UpdateSubProperty()
    {
        StartLUT = m_Property + "_LUT";
    }

    public override bool IsValid(VisualEffect component)
    {
        return Target != null && component.HasTexture(m_Property) && component.HasAnimationCurve(StartLUT);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        if (Application.isEditor)
            UpdateTexture();

        component.SetTexture(m_Property, splineMap);
        component.SetAnimationCurve(StartLUT, curveStartLUT);
    }

    public override string ToString()
    {
        return string.Format("Spline : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
    }
}
