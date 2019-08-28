using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// The DrawingManager that handles drawing of freeform lines.  Each line that has a different color needs its own GameObject
/// which this class keeps track of.  It also instantiates new mesh lines as needed.
/// </summary>
public class DrawPenManager : MonoBehaviour
{
    #region Variables
    // The Mesh Line prefab. Assigned in the inspector.
    [Header("Prefab")]
    public GameObject m_LinePrefab;

    // A list of all Mesh Line gameobjects that have been insantiated.
    List<MeshLineRender> m_LineGameObjects;
    // The currently active Mesh Line that is being drawn.
    MeshLineRender activeLine;
    bool triggerReleased = false;
    #endregion

    #region Monobehavior
    // Make a new list when the session starts
    private void OnEnable()
    {
        m_LineGameObjects = new List<MeshLineRender>();
    }
    #endregion

    #region Drawing
    /// <summary>
    /// Handle the drawing of Mesh Lines added by a remote client.
    /// </summary>
    /// <param name="line"></param>
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

    /// <summary>
    /// Update an existing line with new data from a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void EditNetworkLine(Line line)
    {
        var edit = m_LineGameObjects.Find(x => x.m_ContainedLineIds.Contains(line.LineID));
        edit.DrawLineFromNetwork(line);
    }

    /// <summary>
    /// Draw a line instigated by the owner of this client.
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="color">Color of the line</param>
    /// <param name="width">Width of the line</param>
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

    /// <summary>
    /// When the user releases the trigger we toggle the 'triggerReleased' bool.
    /// This tells the Line Mesh when it should connect previous points or not.
    /// </summary>
    public void HandleReleased()
    {
        //Debug.Log("Handle was released!");
        if (activeLine != null)
            triggerReleased = true;
    }

    /// <summary>
    /// Erase points from a line at the position, within radius distance in world space.
    /// </summary>
    /// <param name="position">Position of the controller.</param>
    /// <param name="radius">Width of the eraser.</param>
    public void Erase(Vector3 position, float radius)
    {
        var remove = new List<Vector3>();
        foreach (var line in DrawingManager.m_AllLines)
        {
            foreach (var point in line.Value.Points)
            {
                if (Vector3.Distance(point, position) < radius)
                {
                    foreach (var mesh in m_LineGameObjects)
                        mesh.EraseMesh(point, radius);
                    remove.Add(point);
                }
            }
            foreach (var point in remove)
                line.Value.RemovePoint(point);
            remove.Clear();
        }
    }

    /// <summary>
    /// Erase points updated from a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void EraseNetworkLine(Line line)
    {
        if (!DrawingManager.m_AllLines.ContainsKey(line.LineID))
            return;
        var match = DrawingManager.m_AllLines[line.LineID];
        var missing = new List<Vector3>();
        foreach (var point in match.Points)
        {
            if (!line.Points.Contains(point))
                missing.Add(point);
        }
        foreach (var point in missing)
            match.RemovePoint(point);
        foreach (var position in missing)
        {
            foreach (var mesh in m_LineGameObjects)
                mesh.EraseMesh(position, DrawingManager.eraserRadius);
        }
    }

    /// <summary>
    /// Create a new MeshLine gameobject and add it the list of Mesh Line GameObjects.
    /// </summary>
    /// <param name="color">Color of the new line.</param>
    /// <returns>A new MeshLineRender instance</returns>
    private MeshLineRender NewLine(Color color)
    {
        var retVal = Instantiate(m_LinePrefab, Vector3.zero, Quaternion.identity).GetComponent<MeshLineRender>();
        retVal.SetColor(color);
        m_LineGameObjects.Add(retVal);
        return retVal;
    }
    #endregion
}
