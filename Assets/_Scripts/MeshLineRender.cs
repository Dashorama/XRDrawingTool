﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshLineRender : MonoBehaviour
{
    const float m_DistanceThreshold = 0.025f;
    public Material penMaterial;
    Mesh m_Mesh;
    MeshRenderer m_Renderer;
    
    float m_LineSize;
    bool firstQuad = true;
    int currentLineID;

    public List<int> m_ContainedLineIds = new List<int>();

    void OnEnable()
    {
        m_Mesh = GetComponent<MeshFilter>().mesh;
        m_Renderer = GetComponent<MeshRenderer>();
        m_Renderer.material = new Material(penMaterial);
    }

    public void SetColor(Color color)
    {
        m_Renderer.material.color = color;
    }

    public Color GetColor()
    {
        return m_Renderer.material.color;
    }

    public void SetWidth(float width)
    {
        m_LineSize = width;
    }

    public float GetWidth() { return m_LineSize; }

    public void EraseMesh(Vector3 position, float radius)
    {
        radius *= 0.45f;
        int[] triangles = m_Mesh.triangles;
        Vector3[] vertices = m_Mesh.vertices;
        Vector2[] uv = m_Mesh.uv;
        Vector3[] normals = m_Mesh.normals;
        List<Vector3> vertList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<Vector3> normalsList = new List<Vector3>();
        List<int> trianglesList = new List<int>();

        // Make lists for easy resizing
        for (int i = 0; i < vertices.Length; i++)
        {
            vertList.Add(vertices[i]);
            uvList.Add(uv[i]);
            normalsList.Add(normals[i]);
        }

        for (int triCount = 0; triCount < triangles.Length; triCount += 3)
        {
            // Find if all the vertices of a triangle is within the eraser tool
            if ((Vector3.Distance(position, transform.TransformPoint(vertices[triangles[triCount]])) > radius) &&
                (Vector3.Distance(position, transform.TransformPoint(vertices[triangles[triCount+1]])) > radius) &&
                (Vector3.Distance(position, transform.TransformPoint(vertices[triangles[triCount+2]])) > radius))
            {
                trianglesList.Add(triangles[triCount]);
                trianglesList.Add(triangles[triCount + 1]);
                trianglesList.Add(triangles[triCount + 2]);
            }
        }

        // May need to optimize this eventually, ToArray is relatively costly
        triangles = trianglesList.ToArray();
        vertices = vertList.ToArray();
        uv = uvList.ToArray();
        normals = normalsList.ToArray();
        m_Mesh.triangles = triangles;
        m_Mesh.vertices = vertices;
        m_Mesh.uv = uv;
        m_Mesh.normals = normals;
    }

    public Line NewLine()
    {
        //Debug.Log("New line was called!");
        var line = new Line();
        currentLineID = line.LineID;
        m_ContainedLineIds.Add(currentLineID);
        DrawingManager.m_AllLines.Add(line);
        line.m_Color = GetColor();
        line.m_Width = m_LineSize;
        firstQuad = true;
        //Debug.Log("Added a line with ID: " + line.LineID);
        //Debug.Log("Current line ID is: " + currentLineID);
        var curLine = DrawingManager.m_AllLines.Find(x => x.LineID == currentLineID);
        //Debug.Log("Line exists in DrawingManager?: " + (curLine != null));
        return line;
    }

    public void AddPoint(Vector3 point)
    {
        //Debug.Log("Attempting to Add a point. Current Line ID is: "+currentLineID);
        var curLine = DrawingManager.m_AllLines.Find(x => x.LineID == currentLineID);
        if (curLine == null)
            curLine = NewLine();

        //Debug.Log("Adding a point to line with id: " + curLine.LineID);
        //Debug.Log("First Quad is: " + firstQuad);

        if (curLine.Points.Count < 1)
        {
            AddLine(m_Mesh, MakeQuad(point, point, m_LineSize, firstQuad));
            curLine.AddPoint(point);
            firstQuad = false;
        }
        else
        {
            if (Vector3.Distance(curLine.LastPoint, point) >= m_DistanceThreshold)
            {
                AddLine(m_Mesh, MakeQuad((firstQuad)?point:curLine.LastPoint, point, m_LineSize, firstQuad));
                curLine.AddPoint(point);
                firstQuad = false;
            }
        }
    }

    public void DrawLineFromNetwork(Line line)
    {
        var lineExists = DrawingManager.m_AllLines.Find(x => x.LineID == line.LineID);
        if (lineExists != null)
        {
            for (int i = 0; i < line.Points.Count; i++)
            {
                if (i >= lineExists.Points.Count)
                {
                    lineExists.AddPoint(line.Points[i]);
                    if (Vector3.Distance(line.Points[i], line.Points[i - 1]) > (3f * m_DistanceThreshold))
                        AddLine(m_Mesh, MakeQuad(line.Points[i], line.Points[i], line.m_Width, true));
                    else
                        AddLine(m_Mesh, MakeQuad(line.Points[i-1], line.Points[i], line.m_Width, false));
                }
                else if (line.Points[i] == lineExists.Points[i])
                    continue;
            }
        }
        else
        {
            DrawingManager.m_AllLines.Add(line);
            m_ContainedLineIds.Add(line.LineID);
            for (int i = 0; i < line.Points.Count; i++)
            {
                if (i == 0)
                    AddLine(m_Mesh, MakeQuad(line.Points[i], line.Points[i], line.m_Width, true));
                else
                    AddLine(m_Mesh, MakeQuad(line.Points[i-1], line.Points[i], line.m_Width, false));
            }
        }
    }

    private Vector3[] MakeQuad(Vector3 start, Vector3 end, float width, bool all)
    {
        //Debug.Log("Making a quad at start: " + start + " and end: " + end);
        width *= 0.5f;
        Vector3[] quad;
        if (all)
            quad = new Vector3[4];
        else
            quad = new Vector3[2];

        Vector3 n = Vector3.Cross(start, end);
        Vector3 length = Vector3.Cross(n, end - start);
        length.Normalize();

        if (all)
        {
            quad[0] = transform.InverseTransformPoint(start + length * width);
            quad[1] = transform.InverseTransformPoint(start + length * -width);
            quad[2] = transform.InverseTransformPoint(end + length * width);
            quad[3] = transform.InverseTransformPoint(end + length * -width);
        }
        else
        {
            quad[0] = transform.InverseTransformPoint(start + length * width);
            quad[1] = transform.InverseTransformPoint(start + length * -width);
        }
        return quad;
    }

    private void AddLine(Mesh m, Vector3[] quad)
    {
        //Debug.Log("Adding a line to mesh, " + m.name);
        int v1 = m.vertices.Length;
        Vector3[] vs = m.vertices;
        vs = ResizeVertices(vs, 2 * quad.Length);

        for (int i = 0; i < 2*quad.Length; i+=2)
        {
            vs[v1 + i] = quad[i / 2];
            vs[v1 + i + 1] = quad[i / 2];
        }

        Vector2[] uvs = m.uv;
        uvs = ResizeUVs(uvs, 2 * quad.Length);

        if (quad.Length == 4)
        {
            uvs[v1] = Vector2.zero;
            uvs[v1 + 1] = Vector2.zero;
            uvs[v1 + 2] = Vector2.right;
            uvs[v1 + 3] = Vector2.right;
            uvs[v1 + 4] = Vector2.up;
            uvs[v1 + 5] = Vector2.up;
            uvs[v1 + 6] = Vector2.one;
            uvs[v1 + 7] = Vector2.one;
        }
        else
        {
            if (v1 % 8 == 0)
            {
                uvs[v1] = Vector2.zero;
                uvs[v1 + 1] = Vector2.zero;
                uvs[v1 + 2] = Vector2.right;
                uvs[v1 + 3] = Vector2.right;
            } else
            {
                uvs[v1] = Vector2.up;
                uvs[v1 + 1] = Vector2.up;
                uvs[v1 + 2] = Vector2.one;
                uvs[v1 + 3] = Vector2.one;
            }
        }

        int t1 = m.triangles.Length;
        int[] ts = m.triangles;
        ts = ResizeTriangle(ts, 12);

        if (quad.Length == 2)
            v1 -= 4;

        // Front facing quad
        ts[t1] = v1;
        ts[t1+1] = v1+2;
        ts[t1+2] = v1+4;

        ts[t1+3] = v1+2;
        ts[t1+4] = v1+6;
        ts[t1+5] = v1+4;

        // Back facing quad
        ts[t1+6] = v1+5;
        ts[t1+7] = v1+3;
        ts[t1+8] = v1+1;

        ts[t1+9] = v1+5;
        ts[t1+10] = v1+7;
        ts[t1+11] = v1+3;

        m.vertices = vs;
        m.uv = uvs;
        m.triangles = ts;
        m.RecalculateBounds();
        m.RecalculateNormals();
    }

    private int[] ResizeTriangle(int[] triangles, int amount)
    {
        int[] newTriangles = new int[triangles.Length + amount];
        for (int i = 0; i < triangles.Length; i++)
            newTriangles[i] = triangles[i];
        return newTriangles;
    }

    private Vector2[] ResizeUVs(Vector2[] uvs, int amount)
    {
        Vector2[] newUVs = new Vector2[uvs.Length + amount];
        for (int i = 0; i < uvs.Length; i++)
            newUVs[i] = uvs[i];
        return newUVs;
    }

    private Vector3[] ResizeVertices(Vector3[] vertices, int amount)
    {
        Vector3[] newVerts = new Vector3[vertices.Length + amount];
        for (int i = 0; i < vertices.Length; i++)
            newVerts[i] = vertices[i];
        return newVerts;
    }
}
