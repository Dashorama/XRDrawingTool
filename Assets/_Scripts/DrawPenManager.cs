using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class DrawPenManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject m_LinePrefab;

    List<MeshLineRender> m_LineGameObjects;
    MeshLineRender activeLine;

    bool triggerReleased = false;

    private void OnEnable()
    {
        m_LineGameObjects = new List<MeshLineRender>();
    }

    public void AddNetworkLine(Line line)
    {
        var existing = m_LineGameObjects.Find(x => x.GetColor() == line.m_Color & x.GetWidth() == line.m_Width);
        if (existing != null)
            existing.DrawLineFromNetwork(line);
        else
        {
            var newLine = NewLine(line.m_Color);
            newLine.SetWidth(line.m_Width);
            newLine.DrawLineFromNetwork(line);
        }
    }

    public void EditNetworkLine(Line line)
    {
        var edit = m_LineGameObjects.Find(x => x.m_ContainedLineIds.Contains(line.LineID));
        edit.DrawLineFromNetwork(line);
    }

    public void HandleDrawButtonPress(Vector3 position, Color color, float width)
    {
        if (activeLine == null)
            activeLine = NewLine(color);
        else if (activeLine.GetColor() != color)
            activeLine = NewLine(color);
        else if (triggerReleased)
        {
            activeLine.NewLine();
            triggerReleased = false;    
        }

        activeLine.SetWidth(width);
        activeLine.AddPoint(position);
    }

    public void HandleReleased()
    {
        Debug.Log("Handle was released!");
        if (activeLine != null)
            triggerReleased = true;
    }

    public void Erase(Vector3 position, float radius)
    {
        var remove = new List<Vector3>();
        foreach (var line in DrawingManager.m_AllLines)
        {
            foreach (var point in line.Points)
            {
                if (Vector3.Distance(point, position) < radius)
                {
                    foreach (var mesh in m_LineGameObjects)
                        mesh.EraseMesh(point, radius);
                    remove.Add(point);
                }
            }
            foreach (var point in remove)
            {
                line.RemovePoint(point);
            }
            remove.Clear();
        }
    }

    public void EraseNetworkLine(Line line)
    {
        var match = DrawingManager.m_AllLines.Find(x => x.LineID == line.LineID);
        var missing = new List<Vector3>();
        foreach (var point in match.Points)
        {
            if (!line.Points.Contains(point))
                missing.Add(point);
        }
        foreach (var point in missing)
        {
            match.RemovePoint(point);
        }
        foreach (var position in missing)
        {
            foreach (var mesh in m_LineGameObjects)
                mesh.EraseMesh(position, DrawingManager.eraserRadius);
        }
    }

    private MeshLineRender NewLine(Color color)
    {
        var retVal = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<MeshLineRender>();
        retVal.SetColor(color);
        m_LineGameObjects.Add(retVal);
        return retVal;
    }
}
