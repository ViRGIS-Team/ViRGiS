using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

using Zinnia.Pointer;
using Zinnia.Cast;

public class PointerInteractor : XRBaseInteractor, IUIInteractable
{

    [SerializeField]
    bool m_EnableUIInteraction = true;
    /// <summary>Gets or sets whether this interactor is able to affect UI.</summary>
    /// 
    // Input Module for fast access to UI systems.
    XRUIInputModule m_InputModule;

    // Used by UpdateUIModel to retrieve the line points to pass along to Unity UI.
    static Vector3[] s_CachedLinePoints;

    /// <summary>The starting transform of any Raycasts.  Default value is the controller transform, or the attachTransform if it is not null.</summary>
    Transform m_StartTransform { get { return  attachTransform ?? transform; } }

    [SerializeField]
    LayerMask m_RaycastMask = -1;
    /// <summary>Gets or sets layer mask used for limiting raycast targets.</summary>
    public LayerMask raycastMask { get { return m_RaycastMask; } set { m_RaycastMask = value; } }

    public float movementDeadzone;

    int m_HitCount = 0;

    RaycastHit[] m_RaycastHits = new RaycastHit[1];

    Vector3[] m_LinePoints;

    bool isUISelectActive;
    DateTime _lastClick;
    bool isSelectRequested;
    bool isClick;



    public bool enableUIInteraction
    {
        get
        {
            return m_EnableUIInteraction;
        }
        set
        {
            
            if (m_EnableUIInteraction)
            {
                m_InputModule.RegisterInteractable(this);
            }
            else
            {
                m_InputModule.UnregisterInteractable(this);
            }
        }
    }

    void FindOrCreateXRUIInputModule()
    {
        var eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
            eventSystem = new GameObject("Event System", typeof(EventSystem)).GetComponent<EventSystem>();

        m_InputModule = eventSystem.GetComponent<XRUIInputModule>();
        if (m_InputModule == null)
            m_InputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    protected override void OnEnable()
    {
        _lastClick = DateTime.Now;
        
        base.OnEnable();

        if (m_EnableUIInteraction)
        {
            FindOrCreateXRUIInputModule();
            m_InputModule.RegisterInteractable(this);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // clear lines
        m_HitCount = 0;

        if (m_EnableUIInteraction)
        {
            m_InputModule.UnregisterInteractable(this);
        }
        m_InputModule = null;
    }

    public override void GetValidTargets(List<XRBaseInteractable> validTargets)
    {
    }
    /// <summary>
    /// Updates the current UI Model to match the state of the Interactor
    /// </summary>
    /// <param name="model">The model that will match this Interactor</param>
    public void UpdateUIModel(ref TrackedDeviceModel model)
    {
        model.position = m_StartTransform.position;
        model.orientation = m_StartTransform.rotation;
        model.select = isUISelectActive;
        if (isClick) {
            isUISelectActive = false;
            isClick = false;
        }


        int numPoints = 0;
        GetLinePoints(ref s_CachedLinePoints, ref numPoints);

        List<Vector3> raycastPoints = model.raycastPoints;
        raycastPoints.Clear();
        if (numPoints > 0 && s_CachedLinePoints != null)
        {
            raycastPoints.Capacity = raycastPoints.Count + numPoints;
            for (int i = 0; i < numPoints; i++)
                raycastPoints.Add(s_CachedLinePoints[i]);
        }
        model.raycastLayerMask = raycastMask;
    }

    /// <summary>
    /// Attempts to retrieve the current UI Model.  Returns false if not available.
    /// </summary>
    /// <param name="model"> The UI Model that matches that Interactor.</param>
    /// <returns></returns>
    public bool TryGetUIModel(out TrackedDeviceModel model)
    {
        if (m_InputModule != null)
        {
            if (m_InputModule.GetTrackedDeviceModel(this, out model))
                return true;
        }

        model = new TrackedDeviceModel(-1);
        return false;
    }

    /// <summary>
    /// This function will return the first raycast result, if any raycast results are available.
    /// </summary>
    /// <param name="raycastHit">the raycastHit result that will be filled in by this function</param>
    /// <returns>true if the raycastHit parameter contains a valid raycast result</returns>
    public bool GetCurrentRaycastHit(out RaycastHit raycastHit)
    {
        raycastHit = new RaycastHit();
        return false;
    }

    /// <summary> This function implements the ILineRenderable interface and returns the sample points of the line. </summary>
    public bool GetLinePoints(ref Vector3[] linePoints, ref int noPoints)
    {
        if (m_LinePoints != null && m_LinePoints.Length > 0) {
            linePoints = new Vector3[2];
            Array.Copy(m_LinePoints, linePoints, 2);
            noPoints = 2;
            return true;
        }
        return false;
    }

    /// <summary> This function implements the ILineRenderable interface, 
    /// if there is a raycast hit, it will return the world position and the normal vector
    /// of the hit point, and its position in linePoints. </summary>
    //
    //TODO Implemet this for full functionality 
    public bool TryGetHitInfo(ref Vector3 position, ref Vector3 normal, ref int positionInLine, ref bool isValidTarget)
    {
        return false;
    }

    // 
    // Connect this callback to the UnityEvent that shhould start the "Selected" state
    // e.g the VRTK ObjectPointer Activated event
    //
    public void Selected(ObjectPointer.EventData data)
    {
        isSelectRequested = true;
        _lastClick = DateTime.Now;
    }

    // 
    // Connect this callback to the UnityEvent that shhould stop the "Selected" state
    // e.g the VRTK ObjectPointer Deactivated event
    //
    public void UnSelected(ObjectPointer.EventData data)
    {
        isUISelectActive = isSelectRequested;
        if (isUISelectActive)
            isClick = true;
    }

    
    //
    // Connect this to the ObjectPointer/StraightCaster ResultsChanged event to get the results of the latest ray cast
    //
    public void receiveRay(PointsCast.EventData data)
    {
        if (isSelectRequested) {
            if (DateTime.Now - _lastClick > new TimeSpan(0, 0, 0, (int) movementDeadzone * 1000)) {
                isUISelectActive = true;
                isSelectRequested = false;
            }
            return;
        }
            m_LinePoints = data.Points.ToArray<Vector3>();
    }
}
