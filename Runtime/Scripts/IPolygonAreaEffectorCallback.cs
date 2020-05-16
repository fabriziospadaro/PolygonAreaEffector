// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

public interface IPolygonAreaEffectorCallback {
  void OnPolygonAreaEffectorTriggerEnter(PolygonAreaEffectorTriggerInfo info);
  void OnPolygonAreaEffectorTriggerExit();
  void OnPolygonAreaEffectorTriggerStay(PolygonAreaEffectorTriggerInfo info);
}
