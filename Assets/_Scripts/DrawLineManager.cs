using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Photon.Pun;

public class DrawLineManager : MonoBehaviourPunCallbacks
{
    [Header("Prefab")]
    public GameObject m_LinePrefab;

    LineRenderer activeLine;
    int activeLineId;
    public bool draggingPoint;

    public void Update()
    {
        if (activeLine != null) 
        {
            //if (draggingPoint)
            //{
            //    activeLine.SetPosition(m_Lines[activeLine].Count, pos);
            //
            //    RaycastHit hit;
            //    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //    if (Physics.Raycast(ray, out hit))
            //    {
            //        var pos = hit.point;
            //        activeLine.SetPosition(m_Lines[activeLine].Count, pos);
            //    }
            //}

            //activeLine.startWidth = activeLine.endWidth = m_WidthSlider.value;
            //activeLine.numCornerVertices = (int)m_Roundness.value;
            //activeLine.startColor = activeLine.endColor = m_ColorPicker.color;
        }
    }

    /// <summary>
    /// Erase points on the line.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    public void Erase(Vector3 position, float radius)
    {
        var remaining = new List<Vector3>();
        foreach (var line in DrawingManager.m_AllLines)
        {
            if (line.useRenderer == 0)
                continue;
            foreach (var point in line.Points)
            {
                Debug.Log("Distance between Eraser and point is: " + Vector3.Distance(point, position));
                if (Vector3.Distance(point, position) > radius)
                    remaining.Add(point);
            }
            line.Points.Clear();
            line.Points.AddRange(remaining);
            line.Renderer.positionCount = line.Points.Count;
            for (int i = 0; i < line.Renderer.positionCount; i++)
                line.Renderer.SetPosition(i, line.Points[i]);
            remaining.Clear();
        }
    }

    /// <summary>
    /// Follow the position of the controller
    /// </summary>
    /// <param name="position">Position of the controller</param>
    public void StickToController(Vector3 position)
    {
        if (activeLine != null)
            activeLine.SetPosition(activeLine.positionCount-1, position);
    }

    /// <summary>
    /// Handle Trigger press inputs.
    /// </summary>
    /// <param name="position">Position of the controller</param>
    /// <param name="draw">True for right trigger (start new line or add point to line), false for left trigger (end line)</param>
    /// <param name="color">Set line color</param>
    /// <param name="width">Set line width</param>
    public void HandleDrawButtonPress(Vector3 position, bool draw, Color color, float width, bool downloadedLine = false)
    {
        if (draw)
        {
            if (activeLine == null)
                StartNewLine(position, color, width);
            if (draggingPoint)
                AddPointToLine(position);
        }
        else
        {
            if (activeLine == null)
                EditLine(position);
            if (draggingPoint)
                CompleteLine(position);
        }
    }

    public void AddNetworkLine(Line line)
    {
        var renderer = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
        line.Renderer = renderer;
        DrawingManager.m_AllLines.Add(line);
        renderer.startColor = renderer.endColor = line.m_Color;
        renderer.startWidth = renderer.endWidth = line.m_Width;
        renderer.positionCount = line.Points.Count;
        for (int i = 0; i < line.Points.Count; i++)
            renderer.SetPosition(i, line.Points[i]);
    }

    public void RemoveNetworkLine(Line line)
    {
        var erase = DrawingManager.m_AllLines.Find(x => x.LineID == line.LineID);
        if (erase != null)
        {
            DrawingManager.m_AllLines.Remove(erase);
            Destroy(erase.Renderer.gameObject);
        }
    }

    public void UpdateNetworkLine(Line line)
    {
        var edit = DrawingManager.m_AllLines.Find(x => x.LineID == line.LineID);
        if (edit != null)
        {
            edit.Renderer.positionCount = line.Points.Count;
            edit.Renderer.startColor = edit.Renderer.endColor = line.m_Color;
            edit.Renderer.startWidth = edit.Renderer.endWidth = line.m_Width;
            for (int i = 0; i < edit.Renderer.positionCount; i++)
                edit.Renderer.SetPosition(i, line.Points[i]);
        }
    }

    /// <summary>
    /// Start drawing a new line.
    /// </summary>
    /// <param name="position">Line starting position</param>
    public void StartNewLine(Vector3 position, Color color, float width)
    {
        //var renderer = PhotonNetwork.Instantiate(m_LinePrefab.name, Vector3.zero, Quaternion.identity, 0).GetComponent<LineRenderer>();
        var renderer = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
        var line = new Line(renderer);
        //renderer.GetComponent<PhotonLineRendererView>().LoadLine(line.LineID, line);
        DrawingManager.m_AllLines.Add(line);
        line.AddPoint(position);
        activeLineId = line.LineID;
        activeLine = renderer;
        activeLine.SetPosition(0, position);
        activeLine.startColor = activeLine.endColor = line.m_Color = color;
        activeLine.startWidth = activeLine.endWidth = line.m_Width = width;
        draggingPoint = true;
    }

    /// <summary>
    /// Append a new point to the currently drawing line
    /// </summary>
    /// <param name="position">Next point position</param>
    public void AddPointToLine(Vector3 position)
    {
        activeLine.positionCount++;
        var getLine = DrawingManager.m_AllLines.Find(x => x.LineID == activeLineId);
        activeLine.SetPosition(getLine.Points.Count, position);
        getLine.AddPoint(position);
    }

    /// <summary>
    /// Finish drawing the current line.
    /// </summary>
    /// <param name="position">Position of final point.</param>
    public void CompleteLine(Vector3 position)
    {
        var getLine = DrawingManager.m_AllLines.Find(x => x.LineID == activeLineId);
        activeLine.SetPosition(getLine.Points.Count-1, position);
        getLine.AddPoint(position);
        activeLine = null;
        activeLineId = 0;
        draggingPoint = false;
    }

    public void EditLine(Vector3 position)
    {
        // Need to sort this out
    }
}
