using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data holder for our Lines
/// </summary>
[System.Serializable]
public class Line
{
    public int LineID;
    public List<Vector3> Points;

    public LineRenderer Renderer;
    public Color m_Color;
    public float m_Width;
    public short useRenderer;
    public Vector3 LastPoint { get { return Points[Points.Count-1]; } }

    /// <summary>
    /// Empty Constructor
    /// </summary>
    public Line()
    {
        Points = new List<Vector3>();
        m_Color = Color.magenta;
        m_Width = 0.05f;
        useRenderer = 0;
    }

    /// <summary>
    /// Mesh Line Constructor
    /// </summary>
    public Line(int ID)
    {
        Points = new List<Vector3>();
        LineID = ID;
        m_Color = Color.magenta;
        m_Width = 0.05f;
        useRenderer = 0;
    }

    /// <summary>
    /// Line Renderer constructor
    /// </summary>
    /// <param name="renderer"></param>
    public Line(LineRenderer renderer, int ID)
    {
        Points = new List<Vector3>();
        LineID = ID;
        Renderer = renderer;
        m_Color = Color.magenta;
        m_Width = 0.05f;
        useRenderer = 1;
    }

    public void AddPoint(Vector3 point)
    {
        Points.Add(point);
    }

    public void AddPoint(float x, float y, float z)
    {
        Points.Add(new Vector3(x, y, z));
    }

    public void RemovePoint(Vector3 point)
    {
        Points.Remove(point);
    }
}
    

