// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

using UnityEngine;

public struct PolygonAreaEffectorTriggerInfo {
  public Box box;
  public Vector2 force;
  public float timeInside;
  public PolygonAreaEffectorTriggerInfo(Box box, Vector2 force, float timeInside) {
    this.box = box;
    this.force = force;
    this.timeInside = timeInside;
  }
  public static PolygonAreaEffectorTriggerInfo Empty {
    get { return new PolygonAreaEffectorTriggerInfo(null, Vector2.zero, 0); }
  }
  public static bool operator !=(PolygonAreaEffectorTriggerInfo a, PolygonAreaEffectorTriggerInfo b) {
    return !(a == b);
  }
  public static bool operator ==(PolygonAreaEffectorTriggerInfo a, PolygonAreaEffectorTriggerInfo b) {
    return a.box == b.box && a.force == b.force && a.timeInside == b.timeInside;
  }

  public override bool Equals(object obj){return base.Equals(obj);}
  public override int GetHashCode(){return base.GetHashCode();}
}
