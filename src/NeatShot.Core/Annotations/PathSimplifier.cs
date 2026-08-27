namespace NeatShot.Core.Annotations;

public static class PathSimplifier
{
    public static IReadOnlyList<ImagePoint> Simplify(IReadOnlyList<ImagePoint> points, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < 3)
        {
            return points;
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        Mark(points, 0, points.Count - 1, tolerance, keep);

        var result = new List<ImagePoint>();
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
            {
                result.Add(points[i]);
            }
        }

        return result;
    }

    public static IReadOnlyList<ImagePoint> Smooth(IReadOnlyList<ImagePoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Count < 3)
        {
            return points;
        }

        var result = new ImagePoint[points.Count];
        result[0] = points[0];
        result[^1] = points[^1];
        for (var i = 1; i < points.Count - 1; i++)
        {
            result[i] = new ImagePoint(
                (points[i - 1].X + points[i].X + points[i + 1].X) / 3,
                (points[i - 1].Y + points[i].Y + points[i + 1].Y) / 3);
        }

        return result;
    }

    private static void Mark(IReadOnlyList<ImagePoint> points, int first, int last, double tolerance, bool[] keep)
    {
        if (last - first < 2)
        {
            return;
        }

        var farthest = first;
        var farthestDistance = 0.0;
        for (var i = first + 1; i < last; i++)
        {
            var distance = points[i].DistanceToSegment(points[first], points[last]);
            if (distance > farthestDistance)
            {
                farthest = i;
                farthestDistance = distance;
            }
        }

        if (farthestDistance <= tolerance)
        {
            return;
        }

        keep[farthest] = true;
        Mark(points, first, farthest, tolerance, keep);
        Mark(points, farthest, last, tolerance, keep);
    }
}
