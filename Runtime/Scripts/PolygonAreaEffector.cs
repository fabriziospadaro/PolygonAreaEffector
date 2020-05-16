// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PolygonAreaEffector : MonoBehaviour {
  public List<Box> boxes;
  public LayerMask mask;
  public float force;
  public float forceOverTimeIncrementer = 0;
  bool insideFlow;
  public bool bakeBodies = true;
  private List<Rigidbody2D> bakedBodiesList;

  public Dictionary<int, float> Info = new Dictionary<int, float>();
  public enum ForceType { XY, Y, X };
  public ForceType forceType = ForceType.XY;
  private void Awake() {
    foreach(Box b in boxes) b.BakeBox();
    foreach(Rigidbody2D r in FindBodiesWithLayer()) {
      int instanceId = r.gameObject.GetInstanceID();
      Info.Add(instanceId, 0);
    }
    if(bakeBodies)
      bakedBodiesList = FindBodiesWithLayer();
    boundingBox = BoundingBox;
  }

  private void FixedUpdate() {
    Process();
  }

  Vector2[] boundingBox;
  public Vector2[] BoundingBox {
    get {
      List<Vector2> _points = new List<Vector2>();

      for(int i = 0; i < boxes.Count; i++)
        for(int j = 0; j < 2; j++)
          _points.Add(boxes[i].boundingBox[j]);

      return MathUtility.BoundingBox(_points);
    }
  }
  public Vector2 Center {
    get {
      Vector2[] bb = BoundingBox;
      return new Vector2(bb[1].x + bb[0].x, bb[1].y + bb[0].y)/2;
    }
  }
  public bool BBVisible(Vector2 p){
    return p.x < boundingBox[0].x && p.x > boundingBox[1].x && p.y < boundingBox[0].y && p.y > boundingBox[1].y;
  }
  void Process() {
    foreach(Rigidbody2D r in bakeBodies ? bakedBodiesList : FindBodiesWithLayer()) {
      if(!InsideBB(r.position))
        continue;
      int instanceId = r.gameObject.GetInstanceID();
      if(!Info.ContainsKey(instanceId))
        Info.Add(instanceId, 0);

      insideFlow = false;
      Vector2 position = r.position;
      if(BBVisible(position)) {
        foreach(Box b in boxes) {
          if(b.InsideBB(position) && b.InsidePolygon(position)) {
            insideFlow = true;
            Vector2 processedForce = b.RelativeDirection(position) * Time.fixedDeltaTime * (force * 10 + (Info[instanceId] * 3 * forceOverTimeIncrementer * 10));
            switch(forceType) {
              case ForceType.X:
              processedForce.y = 0;
              break;
              case ForceType.Y:
              processedForce.x = 0;
              break;
            }
            string methodName = "OnPolygonAreaEffectorTriggerStay";
            if(Info[instanceId] == 0)
              methodName = "OnPolygonAreaEffectorTriggerEnter";
            r.SendMessage(methodName, new PolygonAreaEffectorTriggerInfo(b, processedForce, Info[instanceId]), SendMessageOptions.DontRequireReceiver);
            r.AddForce(processedForce, ForceMode2D.Impulse);
            break;
          }
        }
        if(insideFlow)
          Info[instanceId] += Time.deltaTime;
        else {
          if(Info[instanceId] != 0)
            r.SendMessage("OnPolygonAreaEffectorTriggerExit", SendMessageOptions.DontRequireReceiver);
          Info[instanceId] = 0;
        }
      }
    }
  }

  public void GenerateDefaultBox() {
    boxes = new List<Box>();
    boxes.Add(Box.Default(transform.position, 2));
  }

  public void AddBox(Box father, Vector2 direction) {
    direction.x = Mathf.RoundToInt(direction.x);
    direction.y = Mathf.RoundToInt(direction.y);
    Box box = new Box();
    father.connectionDirection = -direction;
    father.direction = direction;
    box.direction = direction;
    Vector2 realDirection = father.RelativeDirection(father.origin);
    if(direction == Vector2.right) {
      box.nodes[0] = father.nodes[1];
      box.nodes[3] = father.nodes[2];
      box.nodes[1] = new BoxNode(box.nodes[0].point + realDirection * 2);
      box.nodes[2] = new BoxNode(box.nodes[3].point + realDirection * 2);
    }
    else if(direction == Vector2.left) {
      box.nodes[1] = father.nodes[0];
      box.nodes[2] = father.nodes[3];
      box.nodes[3] = new BoxNode(box.nodes[2].point + realDirection * 2);
      box.nodes[0] = new BoxNode(box.nodes[1].point + realDirection * 2);
    }
    else if(direction == Vector2.up) {
      box.nodes[3] = father.nodes[0];
      box.nodes[2] = father.nodes[1];
      box.nodes[1] = new BoxNode(box.nodes[2].point + realDirection * 2);
      box.nodes[0] = new BoxNode(box.nodes[3].point + realDirection * 2);
    }
    else if(direction == Vector2.down) {
      box.nodes[0] = father.nodes[3];
      box.nodes[1] = father.nodes[2];
      box.nodes[2] = new BoxNode(box.nodes[1].point + realDirection * 2);
      box.nodes[3] = new BoxNode(box.nodes[0].point + realDirection * 2);
    }
    box.connectionDirection = direction;
    box.parent = father;
    boxes.Add(box);
  }

  public void DestroyArea() {
    GenerateDefaultBox();
  }

  public void InvertDirection() {
    foreach(Box b in boxes)
      b.direction *= -1;
  }
  public void InvertBox(Box box) {
    box.direction *= -1;
  }

  public bool Touching(GameObject go) {
    return Info.ContainsKey(go.GetInstanceID()) && Info[go.GetInstanceID()] > 0;
  }

  public float GetTimeInside(GameObject go) {
    return Info[go.GetInstanceID()];
  }

  public bool IsEmpty {
    get { return boxes == null; }
  }

  List<Rigidbody2D> FindBodiesWithLayer() {
    Rigidbody2D[] objs = FindObjectsOfType<Rigidbody2D>();
    List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    for(int i = 0; i < objs.Length; i++) {
      if(mask == (mask | (1 << objs[i].gameObject.layer)))
        bodies.Add(objs[i]);
    }
    return bodies;
  }

  public float TotalArea {
    get {
      return SignedPolygonArea();
    }
  }
  private float SignedPolygonArea() {
    float area = 0;
    foreach(Box b in boxes)
      area += MathUtility.PolygonArea(b.points.ToList());
    return area;
  }
  public float LinearLength {
    get {
      List<Vector2> points = new List<Vector2>();
      foreach(Box b in boxes)
        points.Add(b.GetMidEdgePoint(1));
      float lenght = 0;
      for(int i = 1; i < points.Count; i++) {
        lenght += Vector2.Distance(points[i - 1], points[i]);
      }
      return lenght + Vector2.Distance(points.Last(), boxes.Last().GetMidEdgePoint(-1));
    }
  }


  public bool InsideBB(Vector2 p) {
    return p.x < boundingBox[0].x && p.x > boundingBox[1].x && p.y < boundingBox[0].y && p.y > boundingBox[1].y;
  }

  private void OnDrawGizmos() {
    foreach(Box box in boxes) {
      Gizmos.color = Color.white;
      Vector2 lastPoint = box.points[box.points.Length - 1];
      Gizmos.color = Color.green;
      foreach(Vector2 p in box.points) {
        Gizmos.DrawLine(p, lastPoint);
        lastPoint = p;
      }
    }
  }
}
