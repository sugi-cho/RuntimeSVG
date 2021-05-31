using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.VectorGraphics;
using Simplifynet;

[RequireComponent(typeof(SpriteRenderer))]
public class DrawingPath : MonoBehaviour
{
    [SerializeField] List<Vector2> points;
    [SerializeField] List<Vector2> simplefied;

    SimplifyUtility utility;
    [SerializeField] BezierPathSegment[] segments;
    SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        points = new List<Vector2>();
        utility = new SimplifyUtility();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
        }
        if (Input.GetMouseButton(0))
        {
            points.Add(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            var ps = points.Select(p => new Point(p.x, p.y)).ToArray();
            simplefied = utility.Simplify(ps, 5, false).Select(s => new Vector2((float)s.X, (float)s.Y)).ToList();
            segments = Points2Segments(simplefied);

            var path = new Shape()
            {
                Contours = new BezierContour[] { new BezierContour() { Segments = segments, Closed = true } },
                PathProps = new PathProperties()
                {
                    Stroke = new Stroke() { Color = Color.blue, HalfThickness = 5f },
                    Corners = PathCorner.Round,
                }
            };
            path.Fill = new SolidFill() { Color = Color.red, Mode = FillMode.OddEven };
            var options = MakeLineOptions();
            var geo = BuildGeometry(path, options);
            var sprite = VectorUtils.BuildSprite(
                geo, 100f,
                VectorUtils.Alignment.Center,
                Vector2.zero, 128);
            var center = VectorUtils.Bounds(segments).center;
            transform.position = Camera.main.ScreenToWorldPoint(new Vector3(center.x, center.y, 10f));
            spriteRenderer.sprite = sprite;
        }
    }
    
    BezierPathSegment[] Points2Segments(List<Vector2> points)
    {
        var count = points.Count;
        var segments = new BezierPathSegment[count];
        for (var i = 0; i < count; i++)
        {
            var p = new[] {
                points[(count+i-1)%count],
                points[(count+i+0)%count],
                points[(count+i+1)%count],
                points[(count+i+2)%count],
            };

            var angle = Vector2.Angle(p[1] - p[0], p[2] - p[1]);
            var t = Mathf.InverseLerp(30f, 90f, angle);
            var val = Mathf.Lerp(6f, 36f, t);

            segments[i] = new BezierPathSegment
            {
                P0 = p[1],
                P1 = (-p[0] + val * p[1] + p[2]) / val,
                P2 = ( p[1] + val * p[2] - p[3]) / val,
            };
        }
        return segments;
    }

    // 
    protected List<VectorUtils.Geometry> BuildGeometry(Shape shape, VectorUtils.TessellationOptions options)
    {
        var node = new SceneNode()
        {
            Shapes = new List<Shape> { shape }
        };
        var scene = new Scene() { Root = node };
        var geom = VectorUtils.TessellateScene(scene, options);
        return geom;
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

    private void OnDrawGizmosSelected()
    {
        var ps = points.Distinct();
        foreach (var p in ps)
            Gizmos.DrawSphere(Camera.main.ScreenToWorldPoint(new Vector3(p.x, p.y, 10f)), 0.01f);
        for (var i = 0; i < simplefied.Count - 1; i++)
        {
            Gizmos.color = Color.white;
            var p0 = Camera.main.ScreenToWorldPoint(new Vector3(simplefied[i].x, simplefied[i].y, 10f));
            var p1 = Camera.main.ScreenToWorldPoint(new Vector3(simplefied[i + 1].x, simplefied[i + 1].y, 10f));
            Gizmos.DrawLine(p0, p1);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(p0, 0.1f);
            Gizmos.DrawSphere(p1, 0.1f);
        }
    }
}
