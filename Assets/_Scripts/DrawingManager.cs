using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// The top level manager for drawing of lines. This manager takes inputs from the local client controllers,
/// or from the Network through PUN and passes them to the appropriate managers for each type of drawing tool.
/// </summary>
public class DrawingManager : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Enum
    public enum DrawTools
    {
        Line,
        Pen,
        Eraser
    }
    #endregion

    #region Variables
    // Unique Player ID - Generated from the Launcher class
    public static int PlayerID = 0;

    // Keep a running tally of the number of lines drawn, increment each time a new one is created.
    static int m_NumberLines = 0;
    public static int GetNextLineID() { return m_NumberLines++; }
    public static int GetCurrentLineID() { return m_NumberLines; }

    // Magic number eraser with -- May want to make this variable at some point in the future.
    public const float eraserRadius = 0.1f;


    [Header("UI Links")]
    public GameObject UI;
    public ColorPickerTriangle colorPicker;

    [Header("GameObject Links")]
    public SphereCollider eraser;

    [Header("Debug")]
    public TextMeshProUGUI debug;
    public TextMeshProUGUI debugMock;

    // Dictionary of all availble lines, key = LineID, value = Line
    public static Dictionary<int, Line> m_AllLines;

    private DrawTools drawTool;
    DrawLineManager lineDraw;
    DrawPenManager penDraw;
    bool uiEnabled = true;
    float currentWidth = 0.06f;
    #endregion

    #region Monobehavior

    // Get all the references.
    private void Awake()
    {
        lineDraw = GetComponent<DrawLineManager>();
        penDraw = GetComponent<DrawPenManager>();
        m_AllLines = new Dictionary<int, Line>();
    }

    #endregion

    #region PUN Serialization
    // Register our custom Line class with PUN 2
    public static void RegisterCustomTypes()
    {
        var result = PhotonPeer.RegisterType(typeof(Line), (byte)'L', SerializeLine, DeserializeLine);
        //Debug.Log("Registering Custom Line Class:" + ((result) ? "Success" : "Failure"));
    }

    // Byte[] size of Vector 3
    public static readonly byte[] memVector3 = new byte[3 * 4];

    // Byte[] size of non vector Data in Line class 
    // 4 bytes for LineID, a 4 float vector for RGBA color, a float for width, and a short at 0 or 1 for line or pen respectively
    public static readonly int memLine = 4 + (4 * 4) + 4 + 2;

    /// <summary>
    /// Serializer for the Line class for PUN 2 
    /// </summary>
    /// <param name="outStream"></param>
    /// <param name="customobject"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Deserializer for the Line class for PUN 2
    /// </summary>
    /// <param name="inStream"></param>
    /// <param name="length"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Keep the line number and the line Dictionary sync'd between all active users.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="info"></param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // If we are writing, send the current value for m_NumberLines and each Line in the m_AllLines Dictionary
            stream.SendNext(m_NumberLines);
            foreach (var line in m_AllLines)
                stream.SendNext(line.Value);
        }
        else
        {
            // If we are reading, pull the current value for m_NumberLines, keep the greater value.
            int receive = (int)stream.ReceiveNext();

            if (receive > m_NumberLines)
                m_NumberLines = receive;
            // Loop through the total number of lines on the network
            for (int i = 4; i < stream.Count; i++)
            {
                var line = (Line)stream.ReceiveNext();

                //Debug.Log("[OnPhotonSerializeView] Reading data for line with ID: " + line.LineID);
                //Debug.Log("[OnPhotonSerializeView] Dictionary Entry Found?: " + m_AllLines.ContainsKey(line.LineID));
               
                // If we find a line that doesn't exist locally, let's draw it.
                if (!m_AllLines.ContainsKey(line.LineID))
                    DrawNetworkLine(line);
                else if (m_AllLines[line.LineID].Points.Count == line.Points.Count)
                    continue;
                else
                {
                    // If we find a line that exists but has fewer points than on the server, let's update the line.
                    // If it has more points, we need to erase, if it has less we need to draw.
                    if (m_AllLines[line.LineID].Points.Count < line.Points.Count)
                        EditNetworkLine(line);
                    else
                        EraseNetworkLine(line);
                }
            }
        }
    }
    #endregion

    #region Draw Handlers
    /// <summary>
    /// Passes the draw function to the correct manager depending on which tool is active.
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="buttonCurValue">State of the trigger for the current frame</param>
    /// <param name="buttonlastVal">State of the trigger for the previous frame</param>
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

    /// <summary>
    /// Erase a line.
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="buttonCurValue">State of the trigger for the current frame</param>
    /// <param name="buttonlastVal">State of the trigger for the previous frame</param>
    private void Erase(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        if (!buttonCurValue || uiEnabled)
            return;

        position += eraser.transform.localPosition;

        lineDraw.Erase(position, eraserRadius);
        penDraw.Erase(position, eraserRadius);
    }

    /// <summary>
    /// Draw function when the Line tool is active
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="buttonCurValue">State of the trigger for the current frame</param>
    /// <param name="buttonlastVal">State of the trigger for the previous frame</param>
    private void HandleLineTool(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        // Don't allow drawing if the UI is open
        if (uiEnabled)
            return;

        if (buttonCurValue == true && buttonlastVal == false)
            lineDraw.HandleDrawButtonPress(position, true, colorPicker.TheColor, currentWidth);
    }

    /// <summary>
    /// UI Toggle Function if the 'toggleUI' is true.
    /// End drawing of currently dragged line if the 'toggleUI' is false and user is currently using the Line tool. 
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="toggleUI">Toggle UI on/off if True, End Line drag if false & currently dragging a line</param>
    public void HandleTrackPadPress(Vector3 position, bool toggleUI)
    {
        if (toggleUI)
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

    /// <summary>
    /// Draw a free-form line.
    /// </summary>
    /// <param name="position">Position of the controller in world space</param>
    /// <param name="buttonCurValue">State of the trigger for the current frame</param>
    /// <param name="buttonlastVal">State of the trigger for the previous frame</param>
    void HandlePenTool(Vector3 position, bool buttonCurValue, bool buttonlastVal)
    {
        // Don't allow drawing if the UI is open
        if (uiEnabled)
            return;

        //Debug.Log("Current state is: " + buttonCurValue);
        //Debug.Log("Last state is: " + buttonlastVal);
        
        // If we haven't pushed the trigger last frame, don't connect the meshes from the last point
        if (buttonCurValue == true & buttonlastVal == false)
            penDraw.HandleReleased();
        else if (buttonCurValue == true & buttonlastVal == true)
            penDraw.HandleDrawButtonPress(position, colorPicker.TheColor, currentWidth);
    }

    /// <summary>
    /// Pass the position of the controller to the Line Drawing Manager so the next point sticks to the controller
    /// until the user clicks the end line button.
    /// </summary>
    /// <param name="position"></param>
    public void StickToController(Vector3 position)
    {
        if (uiEnabled)
            return;

        if (lineDraw.draggingPoint)
            lineDraw.StickToController(position);
    }

    /// <summary>
    /// Select the currently active tool. Passed by index to work with Unity button events in the Editor.
    /// </summary>
    /// <param name="index">Enum index. 0 - Line, 1 - Pen, 2 - Eraser.</param>
    public void SetTool(int index)
    {
        drawTool = (DrawTools)index;
        if (drawTool == DrawTools.Eraser)
            eraser.gameObject.SetActive(true);
        else
            eraser.gameObject.SetActive(false);
    }

    /// <summary>
    /// Select currently active tool.
    /// </summary>
    /// <param name="tool">DrawingManger.DrawTool</param>
    public void SetTool(DrawTools tool)
    {
        drawTool = tool;
        if (drawTool == DrawTools.Eraser)
            eraser.gameObject.SetActive(true);
        else
            eraser.gameObject.SetActive(false);
    }

    /// <summary>
    /// Set current line width.
    /// </summary>
    /// <param name="width">Width</param>
    public void SetLineWidth(float width)
    {
        currentWidth = width;
    }

    /// <summary>
    /// Draw a line that has been drawn by a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void DrawNetworkLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.AddNetworkLine(line);
        else
            penDraw.AddNetworkLine(line);
    }

    /// <summary>
    /// Erase a line, or points from a line that has been drawn by a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void EraseNetworkLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.RemoveNetworkLine(line);
        else
            penDraw.EraseNetworkLine(line);
    }

    /// <summary>
    /// Draw more points for a line that has been updated by a remote client.
    /// </summary>
    /// <param name="line"></param>
    public void EditNetworkLine(Line line)
    {
        if (line.useRenderer > 0)
            lineDraw.UpdateNetworkLine(line);
        else
            penDraw.EditNetworkLine(line);
    }
    #endregion
}
