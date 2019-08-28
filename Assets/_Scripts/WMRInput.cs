using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

/// <summary>
/// Input handler for Steam VR
/// </summary>
public class WMRInput : MonoBehaviour
{
    public SteamVR_LaserPointer m_Laser;
    public SteamVR_ActionSet m_ActionSet;

    public SteamVR_Action_Boolean m_TrackPadRightPress;
    public SteamVR_Action_Boolean m_TrackPadLeftPress;
    public SteamVR_Action_Boolean m_TriggerPress;
    public Transform drawingHandLocation;

    DrawingManager manager;

    private void Awake()
    {
        m_TriggerPress = SteamVR_Actions._default.Paint;

        m_TrackPadRightPress[SteamVR_Input_Sources.RightHand].onStateDown += TrackpadSinglePress;
        m_TrackPadLeftPress[SteamVR_Input_Sources.LeftHand].onStateDown += TrackpadSinglePress;
        m_TriggerPress[SteamVR_Input_Sources.RightHand].onState += TriggerPressAndHold;
    }

    private void Start()
    {
        m_ActionSet.Activate(SteamVR_Input_Sources.Any, 0, true);
        manager = FindObjectOfType<DrawingManager>();
    }

    private void OnDestroy()
    {
        m_TrackPadRightPress[SteamVR_Input_Sources.RightHand].onStateDown -= TrackpadSinglePress;
        m_TrackPadLeftPress[SteamVR_Input_Sources.LeftHand].onStateDown -= TrackpadSinglePress;
        m_TriggerPress[SteamVR_Input_Sources.RightHand].onState -= TriggerPressAndHold;
    }

    private void Update()
    {
        manager.StickToController(drawingHandLocation.transform.position);
    }

    private void TrackpadSinglePress(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        manager.HandleTrackPadPress(drawingHandLocation.position, source == SteamVR_Input_Sources.LeftHand);
        // Currently this is linked to SteamVR's laser pointer script, will probably need to abstract it to an interface or a generic laser at some point
        if (source == SteamVR_Input_Sources.LeftHand) m_Laser.enabled = !m_Laser.enabled;
    }

    private void TriggerPressAndHold(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
    {
        manager.Draw(drawingHandLocation.transform.position, action.state, action.lastState);
    }

    private void AxisTest(SteamVR_Action_Vector2 action, SteamVR_Input_Sources source, Vector2 axis, Vector2 delta)
    {

    }
}
