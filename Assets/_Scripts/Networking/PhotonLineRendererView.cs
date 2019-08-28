using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(LineRenderer))]
public class PhotonLineRendererView : MonoBehaviour, IPunObservable
{
    private float m_Distance;
    private float m_Angle;

    private PhotonView m_PhotonView;
    private int m_LineID;
    private Line m_Line;

    private List<Vector3> m_NetworkPoints;

    private Color m_StoredColor;
    private Color m_NetworkColor;

    private float m_StoredWidth;
    private float m_NetworkWidth;

    public bool m_SynchronizePoints = true;
    public bool m_SynchronizeColor = true;
    public bool m_SynchronizeWidth = true;

    LineRenderer m_Renderer;

    public void Awake()
    {
        m_PhotonView = GetComponent<PhotonView>();
        m_PhotonView.ObservedComponents.Add(this);
        m_Renderer = GetComponent<LineRenderer>();
        m_NetworkPoints = new List<Vector3>();
    }

    public void LoadLine(int lineID, Line line)
    {
        m_LineID = lineID;
        m_Line = line;
    }

    public void Update()
    {
        if (!this.m_PhotonView.IsMine)
        {
            m_Renderer.positionCount = m_NetworkPoints.Count;
            for (int i = 0; i < m_NetworkPoints.Count; i++)
                m_Renderer.SetPosition(i,m_NetworkPoints[i]);
            m_Renderer.startColor = m_Renderer.endColor = m_NetworkColor;
            m_Renderer.startWidth = m_Renderer.endWidth = m_NetworkWidth;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        Debug.Log("Serializing a Line Renderer View for ID: " + m_LineID);

        if (stream.IsWriting)
        {
            if (this.m_SynchronizePoints)
            {
                for (int i = 0; i < m_Renderer.positionCount; i++)
                    m_Line.Points[i] = m_Renderer.GetPosition(i);
                stream.SendNext(m_Line.Points);
            }

            if (this.m_SynchronizeColor)
            {
                m_StoredColor = m_Renderer.startColor;
                stream.SendNext(m_StoredColor);
            }

            if (this.m_SynchronizeWidth)
            {
                m_StoredWidth = m_Renderer.startWidth;
                stream.SendNext(m_StoredWidth);
            }
        }
        else
        {
            if (this.m_SynchronizePoints)
            {
                this.m_NetworkPoints = (List<Vector3>)stream.ReceiveNext();

                    m_Renderer.positionCount = m_NetworkPoints.Count;
                    for (int i = 0; i < m_NetworkPoints.Count; i++)
                        m_Renderer.SetPosition(i, m_NetworkPoints[i]);
            }

            if (this.m_SynchronizeColor)
            {
                this.m_NetworkColor = (Color)stream.ReceiveNext();
                m_Renderer.startColor = m_Renderer.endColor = m_NetworkColor;
            }

            if (this.m_SynchronizeWidth)
            {
                transform.localScale = (Vector3)stream.ReceiveNext();
                m_Renderer.startWidth = m_Renderer.endWidth = m_NetworkWidth;
            }
        }
    }
}

