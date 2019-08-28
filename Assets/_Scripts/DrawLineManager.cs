using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Draw straight lines using Unity's Line Renderer.
/// Might consider making these straight lines use the line Mesh as well... 
/// they use more data over the network but their behaviour is a little bit more controlable.
/// </summary>
public class DrawLineManager : MonoBehaviour
{
    #region Variables
    // Line prefab assigned in the inspector
    [Header("Prefab")]
    public GameObject m_LinePrefab;

    LineRenderer activeLine;
    int activeLineId;
    public bool draggingPoint;
    #endregion

    #region Drawing
    /// <summary>
    /// Erase points on the line.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    public void Erase(Vector3 position, float radius)
    {
        var remaining = new List<Vector3>();
        // Loop through all our lines and find any that are in our eraser radius. If they are, erase them.
        foreach (var line in DrawingManager.m_AllLines)
        {
            if (line.Value.useRenderer == 0)
                continue;
            foreach (var point in line.Value.Points)
            {
                if (Vector3.Distance(point, position) > radius)
                    remaining.Add(point);
            }
            line.Value.Points.Clear();
            line.Value.Points.AddRange(remaining);
            line.Value.Renderer.positionCount = line.Value.Points.Count;
            for (int i = 0; i < line.Value.Renderer.positionCount; i++)
                line.Value.Renderer.SetPosition(i, line.Value.Points[i]);
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
    public void HandleDrawButtonPress(Vector3 position, bool draw, Color color, float width)
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
            if (draggingPoint)
                CompleteLine(position);
        }
    }

    /// <summary>
    /// Draw a line that has been added from a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void AddNetworkLine(Line line)
    {
        var renderer = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
        var localLine = new Line(renderer, line.LineID);
        DrawingManager.m_AllLines[localLine.LineID] = localLine;

        // Debug.Log("[AddNetworkLine] Line ID is: " + line.LineID);
        // Debug.Log("[AddNetworkLine] LocalLine ID is: " + localLine.LineID);

        renderer.startColor = renderer.endColor = line.m_Color;
        renderer.startWidth = renderer.endWidth = line.m_Width;
        renderer.positionCount = line.Points.Count;
        for (int i = 0; i < line.Points.Count; i++)
            renderer.SetPosition(i, line.Points[i]);
    }

    /// <summary>
    /// Erase a line that has been erased from a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void RemoveNetworkLine(Line line)
    {
        if (DrawingManager.m_AllLines.ContainsKey(line.LineID))
        {
            Destroy(DrawingManager.m_AllLines[line.LineID].Renderer.gameObject);
            DrawingManager.m_AllLines.Remove(line.LineID);
        }
    }

    /// <summary>
    /// Edit a line that has been edited from a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void UpdateNetworkLine(Line line)
    {
        //Debug.Log("[UpdateNetworkLine] Looking for line with ID: " + line.LineID);

        if (DrawingManager.m_AllLines.ContainsKey(line.LineID))
        {
            //Debug.Log("[UpdateNetworkLine] Found line with ID: " + line.LineID);
            var edit = DrawingManager.m_AllLines[line.LineID];
            edit.Renderer.positionCount = line.Points.Count;
            edit.Renderer.startColor = edit.Renderer.endColor = line.m_Color;
            edit.Renderer.startWidth = edit.Renderer.endWidth = line.m_Width;
            for (int i = 0; i < edit.Renderer.positionCount; i++)
                edit.Renderer.SetPosition(i, line.Points[i]);
        }
        else
            return; //Debug.Log("[UpdateNetworkLine] NO Line FOUND!");
    }

    /// <summary>
    /// Start drawing a new line.
    /// </summary>
    /// <param name="position">Line starting position</param>
    public void StartNewLine(Vector3 position, Color color, float width)
    {
        var renderer = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
        var line = new Line(renderer, DrawingManager.GetNextLineID());

        // Debug.Log("Current LineID is: " + line.LineID);
        // Debug.Log("Current m_NumberLines is: " + DrawingManager.GetCurrentLineID());

        DrawingManager.m_AllLines[line.LineID] = line;
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
        var getLine = DrawingManager.m_AllLines[activeLineId];
        activeLine.SetPosition(getLine.Points.Count, position);
        getLine.AddPoint(position);
    }

    /// <summary>
    /// Finish drawing the current line.
    /// </summary>
    /// <param name="position">Position of final point.</param>
    public void CompleteLine(Vector3 position)
    {
        var getLine = DrawingManager.m_AllLines[activeLineId];
        activeLine.SetPosition(getLine.Points.Count-1, position);
        getLine.AddPoint(position);
        activeLine = null;
        activeLineId = 0;
        draggingPoint = false;
    }
    #endregion
}
