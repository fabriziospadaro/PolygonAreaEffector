// Author: Fabrizio Spadaro
// License Copyright 2020 (c) Fabrizio Spadaro
// https://twitter.com/SwordFab
// https://www.linkedin.com/in/fabrizio-spadaro-962790166/

using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(PolygonAreaEffector))]
public class PolygonAreaEffectorCustomInspector : Editor {
  private PolygonAreaEffector polygonAreaEffector;
  private Box selectedBox;
  private int selectedVertexID;
  bool takeInputControl;
  void OnEnable() {
    selectedVertexID = -1;
    polygonAreaEffector = this.target as PolygonAreaEffector;
    if(polygonAreaEffector.IsEmpty)
      polygonAreaEffector.GenerateDefaultBox();
    else {
      foreach(Box b in polygonAreaEffector.boxes) {
        b.BakeBox();
        b.ReconnectNodes(polygonAreaEffector.boxes);
      }
    }
  }

  public override void OnInspectorGUI() {
    if(GUILayout.Button("Destroy all boxes")) {
      Undo.RecordObject(polygonAreaEffector, "Destroy all boxes");
      polygonAreaEffector.DestroyArea();
      EditorUtility.SetDirty(polygonAreaEffector);
    }

    if(GUILayout.Button("Invert all directions")) {
      Undo.RecordObject(polygonAreaEffector, "Invert all directions");
      polygonAreaEffector.InvertDirection();
      EditorUtility.SetDirty(polygonAreaEffector);
    }

    if(GUILayout.Button("Log path info")) {
      Debug.Log("Lineaer Length: " + polygonAreaEffector.LinearLength);
      Debug.Log("Total Area: " + polygonAreaEffector.TotalArea);
    }
    if(selectedBox != null) {
      if(GUILayout.Button("Invert selected direction")) {
        Undo.RecordObject(polygonAreaEffector, "Invert selected direction");
        polygonAreaEffector.InvertBox(selectedBox);
        EditorUtility.SetDirty(polygonAreaEffector);
      }
      if(GUILayout.Button("Log selected box info")) {
        Debug.Log("Direction: " + selectedBox.direction);
        Debug.Log("Connected direction: " + selectedBox.connectionDirection);
        Debug.Log("Relative direction: " + selectedBox.RelativeDirection(selectedBox.origin));
        Debug.Log("Angle" + Mathf.Atan2(selectedBox.RelativeDirection(selectedBox.origin).y, selectedBox.RelativeDirection(selectedBox.origin).x) * Mathf.Rad2Deg);
      }
    }

    ShowEditorProperty("mask");
    ShowEditorProperty("force");
    ShowEditorProperty("forceOverTimeIncrementer");
    ShowEditorProperty("forceType");
    ShowEditorProperty("bakeBodies");
  }

  public float moveHandleSize = 0;
  public void OnSceneGUI() {
    if(!polygonAreaEffector.IsEmpty) {
      foreach(Box box in polygonAreaEffector.boxes) {
        box.BakeBox();
        Vector2 lastPoint = box.points[box.points.Length - 1];
        bool isSelected = box == selectedBox;
        Handles.color = Color.green;
        DrawArrow(box.origin, box.RelativeDirection(box.origin));
        foreach(Vector2 p in box.points) {
          Handles.color = isSelected ? Color.yellow : Color.white;
          Handles.DrawLine(p, lastPoint);
          moveHandleSize = HandleUtility.GetHandleSize(polygonAreaEffector.transform.position) * 0.2f;
          if(isSelected)
            Handles.DrawWireDisc(p, Vector3.forward, moveHandleSize);
          lastPoint = p;
        }
        Handles.color = Color.white;
      }
      ProcessInputs();
    }
  }

  public void ShowEditorProperty(string name) {
    EditorGUIUtility.labelWidth = 0;
    EditorGUIUtility.fieldWidth = 0;
    SerializedProperty property = serializedObject.FindProperty(name);
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(property, true);
    if(EditorGUI.EndChangeCheck())
      serializedObject.ApplyModifiedProperties();
  }

  public void DrawArrow(Vector2 origin, Vector2 direction) {
    float size = HandleUtility.GetHandleSize(polygonAreaEffector.transform.position)*1.1f;
    Handles.DrawDottedLine(origin, origin + (direction / 2), size * 2);
    Handles.DrawWireCube(origin + (direction / 2), size * Vector3.one * 0.1f);
  }

  public void ProcessInputs() {
    if(selectedBox != null) {
      EditorGUI.BeginChangeCheck();
      Vector3 newCenter = Handles.PositionHandle(selectedBox.origin, Quaternion.identity);
      if(EditorGUI.EndChangeCheck()) {
        Undo.RecordObject(polygonAreaEffector, "Moving box origin");
        selectedBox.TranslatePoints(newCenter);
      }
    }

    Event e = Event.current;
    if(e.type == EventType.MouseEnterWindow)
      takeInputControl = true;
    if(e.type == EventType.MouseLeaveWindow)
      takeInputControl = false;
    if(!takeInputControl) return;

    Vector2 mousPos = InitMouse(e, GetHashCode(), EditorWindow.GetWindow<SceneView>());


    if(selectedBox != null && selectedBox.nodes != null && selectedBox.nodes.Length == 4) {
      if(e.type == EventType.KeyDown && e.keyCode == KeyCode.D) {
        Undo.RecordObject(polygonAreaEffector, "Removing Box");
        selectedBox.RemoveBoxAndConnectToChild(polygonAreaEffector.boxes);
        polygonAreaEffector.boxes.Remove(selectedBox);
        EditorUtility.SetDirty(polygonAreaEffector);
      }

      Handles.color = Color.red;
      if(selectedBox.nodes.Length > 0)
        DrawArrow(selectedBox.origin, selectedBox.RelativeDirection(mousPos));
      Handles.color = Color.white;
      Vector2[] handlePos = new Vector2[4];
      for(int k = 0; k < 4; k++)
        if(k + 1 < 4)
          handlePos[k] = MathUtility.MiddlePoint(selectedBox.points[k], selectedBox.points[k + 1]);
        else
          handlePos[k] = MathUtility.MiddlePoint(selectedBox.points[k], selectedBox.points[0]);
      Handles.color = Color.white;
      if(selectedBox.connectionDirection != Vector2.down && DrawCrossHandle(handlePos[0], 0.4f, e)) {
        CreateBox(Vector2.up);
        return;
      }
      if(selectedBox.connectionDirection != Vector2.left && DrawCrossHandle(handlePos[1], 0.4f, e)) {
        CreateBox(Vector2.right);
        return;
      }
      if(selectedBox.connectionDirection != Vector2.up && DrawCrossHandle(handlePos[2], 0.4f, e)) {
        CreateBox(Vector2.down);
        return;
      }
      if(selectedBox.connectionDirection != Vector2.right && DrawCrossHandle(handlePos[3], 0.4f, e)) {
        CreateBox(Vector2.left);
        return;
      }
      Handles.color = Color.white;
      int i = 0;
      foreach(Vector2 p in selectedBox.points) {
        if(Vector2.Distance(p, mousPos) < moveHandleSize && e.type == EventType.MouseDown && e.button == 0) {
          Undo.RecordObject(polygonAreaEffector, "Changeded Shape of vertex");
          selectedVertexID = i;
          return;
        }
        i++;
      }
    }
    if(selectedVertexID != -1 && e.type == EventType.MouseUp) {
      selectedVertexID = -1;
      EditorUtility.SetDirty(polygonAreaEffector);
    }

    if(e.type == EventType.MouseDown && e.button == 0)
      selectedBox = GetSelectedBox(mousPos);

    if(selectedVertexID != -1)
      selectedBox.nodes[selectedVertexID].point = mousPos;
  }

  void CreateBox(Vector2 direction) {
    Undo.RecordObject(polygonAreaEffector, "Creating Box");
    polygonAreaEffector.AddBox(selectedBox, direction);
    selectedBox = polygonAreaEffector.boxes[polygonAreaEffector.boxes.Count - 1];
    EditorUtility.SetDirty(polygonAreaEffector);
  }


  public Box GetSelectedBox(Vector2 mousePos) {
    foreach(Box b in polygonAreaEffector.boxes)
      b.ReconnectNodes(polygonAreaEffector.boxes);
    foreach(Box boxes in polygonAreaEffector.boxes) {
      if(boxes.InsidePolygon(mousePos)) {
        return boxes;
      }
    }
    return null;
  }

  bool DrawCrossHandle(Vector2 origin, float size, Event e) {
    size = HandleUtility.GetHandleSize(polygonAreaEffector.transform.position) * size;
    Vector2 mousPos = InitMouse(e, GetHashCode(), EditorWindow.GetWindow<SceneView>());

    Handles.DrawLine(origin + Vector2.left * (size / 2), origin + Vector2.right * (size / 2));
    Handles.DrawLine(origin + Vector2.up * (size / 2), origin + Vector2.down * (size / 2));
    Handles.DrawWireDisc(origin, Vector3.forward, size / 4);
    if(e.type == EventType.MouseDown && Vector2.Distance(origin, mousPos) < size / 4)
      return true;
    else
      return false;
  }

  Vector2 InitMouse(Event uiEvent, int hashCode, SceneView sceneView) {
    if(uiEvent.type == EventType.Layout)
      HandleUtility.AddDefaultControl(
        GUIUtility.GetControlID(hashCode, FocusType.Keyboard));

    Vector2 screenMousePos = uiEvent.mousePosition;
    Rect screenRect = sceneView.camera.pixelRect;
    Vector2 worldMousePos = HandleUtility.GUIPointToWorldRay(screenMousePos).origin;
    return worldMousePos;
  }

}