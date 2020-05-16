// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
[System.Serializable]
public class Box {
  //caprie perchè non vengono serializzate bene le reference
  public Box parent;
  public BoxNode[] nodes;
  public Vector2[] boundingBox;

  public Vector2 connectionDirection = Vector2.zero;
  public Vector2 direction;
  private Vector2 center;

  public Vector2[] nodePoints;
  public Box() {
    this.nodes = new BoxNode[4];
    this.connectionDirection = Vector2.zero;
  }
  public static Box Default(Vector2 origin, float size) {
    Box defaultBox = new Box();
    Vector2[] points = new Vector2[4] {
      origin + new Vector2(-size/2, size/2), origin + new Vector2(size/2,size/2),
      origin + new Vector2(size/2, -size/2),origin + new Vector2(-size/2, -size/2)
    };
    defaultBox.nodes = new BoxNode[4];
    for(int i = 0; i < 4; i++)
      defaultBox.nodes[i] = new BoxNode(points[i]);
    return defaultBox;
  }

  public void TranslatePoints(Vector2 center) {
    Vector2 diff = center - this.center;

    for(int i = 0; i < nodes.Length; i++) {
      Vector2 diff2Center = nodes[i].point - this.center;
      nodes[i].point = this.center + diff2Center + diff;
    }
    this.center = center;
  }

  void UpdatePointsIfNeeded() {
    if(nodePoints.Length == 0)
      nodePoints = points;
  }

  public void BakeBox() {
    nodePoints = points;
    boundingBox = BoundingBox;
  }

  public Vector2[] BoundingBox {
    get {
      center = origin;
      float maxX = nodePoints[0].x;
      float minX = nodePoints[0].x;
      float maxY = nodePoints[0].y;
      float minY = nodePoints[0].y;
      for(int i = 1; i < nodePoints.Length; i++) {
        maxX = Mathf.Max(nodePoints[i].x, maxX);
        minX = Mathf.Min(nodePoints[i].x, minX);
        maxY = Mathf.Max(nodePoints[i].y, maxY);
        minY = Mathf.Min(nodePoints[i].y, minY);
      }
      return new Vector2[] { new Vector2(maxX, maxY), new Vector2(minX, minY) };
    }
  }

  public Vector2 GetMidEdgePoint(int sign) {
    if(direction == Vector2.left * sign)
      return Vector2.Lerp(nodePoints[1], nodePoints[2], 0.5f);
    else if(direction == Vector2.right * sign)
      return Vector2.Lerp(nodePoints[0], nodePoints[3], 0.5f);
    else if(direction == Vector2.down * sign)
      return Vector2.Lerp(nodePoints[0], nodePoints[1], 0.5f);
    else if(direction == Vector2.up * sign)
      return Vector2.Lerp(nodePoints[2], nodePoints[3], 0.5f);
    return Vector2.zero;
  }
  public float GetEdgeLength(int sign)
  {
    if(direction == Vector2.left * sign)
      return Vector2.Distance(nodePoints[1], nodePoints[2]);
    else if(direction == Vector2.right * sign)
      return Vector2.Distance(nodePoints[0], nodePoints[3]);
    else if(direction == Vector2.down * sign)
      return Vector2.Distance(nodePoints[0], nodePoints[1]);
    else if(direction == Vector2.up * sign)
      return Vector2.Distance(nodePoints[2], nodePoints[3]);
    return 0;
  }
  public Vector2 RelativeDirection(Vector2 from) {
    UpdatePointsIfNeeded();
    Vector2 to;
    if(IsDirectionVertical) {
      if(direction.y > 0)
        to = MathUtility.MiddlePoint(nodePoints[0], nodePoints[1]);
      else
        to = MathUtility.MiddlePoint(nodePoints[3], nodePoints[2]);
    }
    else {
      if(direction.x < 0)
        to = MathUtility.MiddlePoint(nodePoints[0], nodePoints[3]);
      else
        to = MathUtility.MiddlePoint(nodePoints[1], nodePoints[2]);
    }
    to -= (center - to);
    Vector2 resultant = (to - from).normalized;
    return resultant;
  }

  public void ReconnectNodes(List<Box> boxes) {
    foreach(Box b in boxes)
      if(b != this)
        foreach(BoxNode n in b.nodes)
          for(int i = 0; i < nodes.Length; i++)
            if(n.point == nodes[i].point)
              nodes[i] = n;
  }
  public Box GetChildBox(List<Box> boxes) {
    foreach(Box b in boxes)
      if(b.parent == this)
        return b;
    return null;
  }
  //GIVEN NODE A - B - C
  //WHEN WE DESTROY NODE B WE GET THE SHARED NODES WITH C
  //quando distruggo il nodo
  List<BoxNode> GetChildSharedNodes(List<Box> boxes) {
    List<BoxNode> linkedNodes = new List<BoxNode>();
    Box child = GetChildBox(boxes);
    foreach(BoxNode n in child.nodes)
      for(int i = 0; i < nodes.Length; i++)
        if(n.point == nodes[i].point)
          linkedNodes.Add(n);
    return linkedNodes;
  }

  public void RemoveBoxAndConnectToChild(List<Box> boxes){
    if(GetChildBox(boxes) == null)
      return;
    List<BoxNode> parentSharedNode = parent.GetChildSharedNodes(boxes);
    List<BoxNode> childSharedNode = GetChildSharedNodes(boxes);
    //da rifar e
    for(int i = 0; i < parent.nodes.Length; i++) {
      for(int j = 0; j < parentSharedNode.Count; j++) {
        if(parent.nodes[i] == parentSharedNode[j]) {
          BoxNode closerNode = childSharedNode.OrderBy(p => Vector2.Distance(p.point, parent.nodes[i].point)).First();
          parent.nodes[i] = closerNode;
          childSharedNode.Remove(closerNode);
        }
      }
    }
    GetChildBox(boxes).parent = parent;
  }

  public bool InsidePolygon(Vector2 point) {
    return MathUtility.IsPointInPolygon(point, nodePoints);
  }

  public bool InsideBB(Vector2 p) {
    return p.x < boundingBox[0].x && p.x > boundingBox[1].x && p.y < boundingBox[0].y && p.y > boundingBox[1].y;
  }

  public Vector2 origin {
    get {
      Vector2 A = MathUtility.MiddlePoint(nodes[0].point, nodes[2].point);
      Vector2 B = MathUtility.MiddlePoint(nodes[1].point, nodes[3].point);
      return MathUtility.MiddlePoint(A, B);
    }
  }

  public bool IsEmpty {
    get { return nodes == null || nodes.Length == 0; }
  }

  public Vector2[] points {
    get { return new Vector2[4] { nodes[0].point, nodes[1].point, nodes[2].point, nodes[3].point }; }
  }

  public bool IsDirectionVertical {
    get { return (int)direction.y != 0; }
  }
}