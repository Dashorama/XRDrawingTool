using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;

public class DrawingManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static int PlayerID = 0;

    static int m_NumberLines = 0;
    public static int GetNextLineID() { return m_NumberLines++; }

    public const float eraserRadius = 0.1f;

    public enum DrawTools
    {
        Line,
        Pen,
        Eraser
    }

    [Header("UI Links")]
    public GameObject UI;
    public ColorPickerTriangle colorPicker;

    [Header("GameObject Links")]
    public SphereCollider eraser;

    [Header("Debug")]
    public TextMeshProUGUI debug;
    public TextMeshProUGUI debugMock;

    public static List<Line> m_AllLines;
    public List<Line> debugLines;

    private DrawTools drawTool;
    DrawLineManager lineDraw;
    DrawPenManager penDraw;
    bool uiEnabled = true;
    float currentWidth = 0.06f;

    #region PunCallbacks

    #endregion

    private void Awake()
    {
        lineDraw = GetComponent<DrawLineManager>();
        penDraw = GetComponent<DrawPenManager>();
        m_AllLines = new List<Line>();
        //PhotonView pv = GetComponent<PhotonView>();
        //pv.ObservedComponents.Add(this);
        debugLines = m_AllLines;
    }

    public static void RegisterCustomTypes()
    {
        var result = PhotonPeer.RegisterType(typeof(Line), (byte)'L', SerializeLine, DeserializeLine);
        Debug.Log("Registering Custom Line Class:" + ((result) ? "Success" : "Failure"));
    }

    #region PUN Serialization
    public static readonly byte[] memVector3 = new byte[3 * 4];
    // 4 bytes for LineID, a 4 float vector for RGBA color, and a float for width
    public static readonly int memLine = 4 + (4 * 4) + 4 + 2;

    private static short SerializeLine(StreamBuffer outStream, object customobject)
    {
        Line line = (Line)customobject;
        //Debug.Log("[Serializer] Serializing a line with " + (3 * 4 * line.Points.Count + memLine) + " bytes.");
        byte[] bytes = new byte[3 * 4 * line.Points.Count + memLine];
        int index = 0;
        for (int i = 0; i < line.Points.Count; i++)
        {
            lock (bytes)
            {
                Protocol.Serialize(line.Points[i].x, bytes, ref index);
                Protocol.Serialize(line.Points[i].y, bytes, ref index);
                Protocol.Serialize(line.Points[i].z, bytes, ref index);
                outStream.Write(bytes, i * 3 * 4, 3 * 4);
            }
        }
        lock (bytes)
        {
            Protocol.Serialize(line.LineID, bytes, ref index);
            outStream.Write(bytes, bytes.Length-(4*4) - 4 - 4 - 2, 4);
            Protocol.Serialize(line.m_Color.r, bytes, ref index);
            Protocol.Serialize(line.m_Color.g, bytes, ref index);
            Protocol.Serialize(line.m_Color.b, bytes, ref index);
            Protocol.Serialize(line.m_Color.a, bytes, ref index);
            outStream.Write(bytes, bytes.Length-(4*4) - 4 - 2, 4 * 4);
            Protocol.Serialize(line.m_Width, bytes, ref index);
            outStream.Write(bytes, bytes.Length - 4 - 2, 4);
            Protocol.Serialize(line.useRenderer, bytes, ref index);
            outStream.Write(bytes, bytes.Length - 2, 2);
        }
        return (short)(3 * 4 * line.Points.Count + memLine);
    }

    private static object DeserializeLine(StreamBuffer inStream, short length)
    {
        Line line = new Line();

        var size = (length - memLine) / 12;
        //Debug.Log("[Deserializer] Serializing a line with " + size + " points.");
        byte[] bytes = new byte[length];
        int index = 0;
        for (int i = 0; i < size; i++)
        {
            lock (bytes)
            {
                inStream.Read(bytes,(i * 3 * 4), 3 * 4);
                Protocol.Deserialize(out float x, bytes, ref index);
                Protocol.Deserialize(out float y, bytes, ref index);
                Protocol.Deserialize(out float z, bytes, ref index);
                line.AddPoint(x, y, z);
            }
        }
        lock (bytes)
        {
            inStream.Read(bytes, length - (4 * 4) - 4 - 4 - 2, 4);
            Protocol.Deserialize(out line.LineID, bytes, ref index);
            inStream.Read(bytes, bytes.Length - (4 * 4) - 4 - 2, 4 * 4);
            Protocol.Deserialize(out float r, bytes, ref index);
            Protocol.Deserialize(out float g, bytes, ref index);
            Protocol.Deserialize(out float b, bytes, ref index);
            Protocol.Deserialize(out float a, bytes, ref index);
            line.m_Color = new Color(r, g, b, a);
            inStream.Read(bytes, length - 4 - 2, 4);
            Protocol.Deserialize(out line.m_Width, bytes, ref index);
            inStream.Read(bytes, length - 2, 2);
            Protocol.Deserialize(out line.useRenderer, bytes, ref index);
        }
        return line;
    }

    #endregion

    /// <summary>
    /// Passes the draw function to the correct manager depending on which tool is active.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="buttonCurValue"></param>
    /// <param name="buttonlastVal"></param>
    public void Draw(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        switch (drawTool)
        {
            case DrawTools.Line:
                HandleLineTool(position, buttonCurValue, buttonlastVal);
                break;
            case DrawTools.Pen:
                HandlePenTool(position, buttonCurValue, buttonlastVal);
                break;
            case DrawTools.Eraser:
                Erase(position, buttonCurValue, buttonlastVal);
                break;
        }
    }

    private void Erase(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        if (!buttonCurValue || uiEnabled)
            return;

        position += eraser.transform.localPosition;

        lineDraw.Erase(position, eraserRadius);
        penDraw.Erase(position, eraserRadius);
    }

    private void HandleLineTool(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        // Don't allow drawing if the UI is open
        if (uiEnabled)
            return;

        if (buttonCurValue == true && buttonlastVal == false)
            lineDraw.HandleDrawButtonPress(position, true, colorPicker.TheColor, currentWidth);
    }

    public void HandleTrackPadPress(Vector3 position, bool leftPress)
    {
        if (leftPress)
        {
            UI.SetActive(!UI.activeSelf);
            uiEnabled = UI.activeSelf;
            if (drawTool == DrawTools.Eraser)
                eraser.gameObject.SetActive(!uiEnabled);
        }
        else
        {
            // Don't allow drawing if the UI is open
            if (!uiEnabled)
            {
                if (lineDraw.draggingPoint)
                    lineDraw.HandleDrawButtonPress(position, false, colorPicker.TheColor, currentWidth);
            }
        }
    }

    void HandlePenTool(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        // Don't allow drawing if the UI is open
        if (uiEnabled)
            return;

        Debug.Log("Current state is: " + buttonCurValue);
        Debug.Log("Last state is: " + buttonlastVal);
        // If we haven't pushed the trigger last frame, don't connect the meshes from the last point
        if (buttonCurValue == true & buttonlastVal == false)
            penDraw.HandleReleased();
        else if (buttonCurValue == true & buttonlastVal == true)
            penDraw.HandleDrawButtonPress(position, colorPicker.TheColor, currentWidth);
    }

    public void StickToController(Vector3 position)
    {
        if (uiEnabled)
            return;

        if (lineDraw.draggingPoint)
            lineDraw.StickToController(position);
    }

    public void SetTool(int index)
    {
        drawTool = (DrawTools)index;
        if (drawTool == DrawTools.Eraser)
            eraser.gameObject.SetActive(true);
        else
            eraser.gameObject.SetActive(false);
    }

    public void SetLineWidth(float width)
    {
        currentWidth = width;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Debug.Log("Serializing the drawing manager!");
        //debugMock.text = "Serializing the drawing manager!";
        if (stream.IsWriting)
        {
            //Debug.Log("Sending data!");
            //debugMock.text = "SendingData!";
            stream.SendNext(m_NumberLines);
            foreach (var line in m_AllLines)
                stream.SendNext(line);
            //DebugAllLines();
        }
        else
        {
            //debugMock.text = "ReceivingData!";
            //Debug.Log("Receiving data!");

            m_NumberLines = (int)stream.ReceiveNext();
            //Debug.Log("Count is: " + m_NumberLines);
            for (int i = 0; i < m_NumberLines; i++)
            {
                var line = (Line)stream.ReceiveNext();
                var id = line.LineID;
                var match = m_AllLines.Find(x => x.LineID == id);
                if (match == null)
                {
                    DrawDownloadedLine(line);
                }
                else
                {
                    if (match.Points.Count <= line.Points.Count)
                        EditLine(line);
                    else
                        EraseLine(line);
                }
            }

            //debugMock.text = "PlayerID is: " + PlayerID.ToString() + "Writing number of lines: " + m_NumberLines.ToString();
            //DebugAllLines();
        }
    }

    public void AddLineToDebugList(Line line)
    {
        debugLines.Add(line);
    }

    public void DrawDownloadedLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.AddNetworkLine(line);
        else
            penDraw.AddNetworkLine(line);
    }

    public void EraseLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.RemoveNetworkLine(line);
        else
            penDraw.EraseNetworkLine(line);
    }

    public void EditLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.UpdateNetworkLine(line);
        else
            penDraw.EditNetworkLine(line);
    }
}
