// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class MathUtility {
  public static bool IsPointInPolygon(Vector2 p, Vector2[] collection) {
    int i, j;
    bool inside = false;
    for(i = 0, j = collection.Length - 1; i < collection.Length; j = i++) {
      if(((collection[i].y > p.y) != (collection[j].y > p.y)) &&
        (p.x < (collection[j].x - collection[i].x) * (p.y - collection[i].y) / (collection[j].y - collection[i].y) + collection[i].x))
        inside = !inside;
    }
    return inside;
  }
  public static Vector2 MiddlePoint(Vector2 A, Vector2 B) {
    return new Vector2(A.x + 0.5f * (B.x - A.x), A.y + 0.5f * (B.y - A.y));
  }

  public static Vector2[] BoundingBox(List<Vector2> points) {
    var x_query = from Vector2 p in points select p.x;
    float xmin = x_query.Min();
    float xmax = x_query.Max();

    var y_query = from Vector2 p in points select p.y;
    float ymin = y_query.Min();
    float ymax = y_query.Max();

    return new Vector2[] { new Vector2(xmax, ymax), new Vector2(xmin, ymin) };
  }

  public static Vector2[] BoundingBox(Vector2[] points) {
    var x_query = from Vector2 p in points select p.x;
    float xmin = x_query.Min();
    float xmax = x_query.Max();

    var y_query = from Vector2 p in points select p.y;
    float ymin = y_query.Min();
    float ymax = y_query.Max();

    return new Vector2[] { new Vector2(xmax, ymax), new Vector2(xmin, ymin) };
  }

  public static float PolygonArea(List<Vector2> points) {
    points.Add(points[0]);
    return Mathf.Abs(points.Take(points.Count - 1)
        .Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y))
        .Sum() / 2);
  }
}