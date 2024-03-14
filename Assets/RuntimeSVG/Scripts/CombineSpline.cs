using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class CombineSpline : MonoBehaviour
{
    [ContextMenu("combine")]
    void CombineSplines()
    {
        var container = GetComponent<SplineContainer>();
        if (container == null)
            container = gameObject.AddComponent<SplineContainer>();

        var childrens = GetComponentsInChildren<SplineContainer>()
            .Where(c => c!=container)
            .SelectMany(c => c.Splines)
            .ToList();
        while(0 < container.Splines.Count )
            container.RemoveSplineAt(0);
        childrens.ForEach(spline => container.AddSpline(spline));
    }
}
