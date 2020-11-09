//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zinnia.Cast;
using Zinnia.Pointer;
using System;

namespace Virgis
{

    /// <summary>
    /// Main Script for controlling the UI behaviour and the movement of the Camera
    /// </summary>
    public class FlyingCam : MonoBehaviour
    {
        public GameObject trackingSpace; // reference to the active tracking space
        public Camera MainCamera; // the main camera for ray tracing

        [Header("Constants - Desktop")]                                 //unity controls and constants input - keyboard
        public float DesktopAcceleration; // controls how fast you speed up
        public float XAxisSensitivity; // control mouse sensitivity
        public float YAxisSensitivity;

        [Space]

        [Range(0, 89)] public float MaxXAngle = 60f; // for mouse input - controls the max angle allowe din vertical axis to prevent roll-over

        [Space]

        [Header("Constants - Occulus Rift")]
        public float VrAcceleration; // controls how fast you speed up horizontallyly
        public float PanSensitvity; // controls sensitivity to Pan Control
        public float ZoomSensitivity; // controls sensitivity for Zoom Control
        public float SlideMod; // controls sensitivity to @slide@ control on selected marker
        public float AxisSensitivity; // minimum change to be registered as an axis change


        private bool editSelected = false; // edit state 
        private float selectedDistance; // distance to the selected marker``
        private float _rotationX; // used when clamping vertical rotation
        private Transform currentPointerHit; // current marker selected by pointer
        private Transform currentSelected; // current marker in selected state
        private Vector3 lastHitPosition; // position of the last recorded hit
        private bool addVertexState; // current state of the button to add vertex
        private bool delVertexState; // current state of the button to remove vertex
        private Vector3 axis; // axis represented by the line between the two controllers
        private bool AxisEdit = false; // Whether we are in AxisEdit mode
        private Vector3 from; // caches the last position indicated by the user to which to move the selected component
        private AppState appState;
        private Rigidbody _thisRigidbody;

        private List<SelectionType> SELECT_SELECTION_TYPES = new List<SelectionType>() { SelectionType.SELECT, SelectionType.SELECTALL };


        private void Start()
        {
            appState = AppState.instance;
            appState.trackingSpace = trackingSpace;
            _thisRigidbody = GetComponent<Rigidbody>();
            _thisRigidbody.detectCollisions = false;
            appState.ButtonStatus.Event.Subscribe(select);
            appState.ButtonStatus.Event.Subscribe(unSelect);
            appState.mainCamera = MainCamera;
            appState.Project.Event.Subscribe(Project => onProjectLoad());
        }

        private void Update()
        {

            if (appState && appState.ButtonStatus.isAxisEdit && editSelected)
            {
                Vector3 rh = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand);
                Vector3 lh = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand);
                Vector3 newAxis = rh - lh;
                if (AxisEdit)
                {
                    Vector3 AxisDelta = newAxis - axis;
                    if (AxisDelta.magnitude > AxisSensitivity)
                    {
                        MoveArgs args = new MoveArgs();
                        args.rotate = Quaternion.FromToRotation(axis, newAxis);
                        args.scale = newAxis.magnitude / axis.magnitude;
                        args.pos = lastHitPosition;
                        MoveAxis(args);
                        axis = newAxis;
                    }
                }
                else
                {
                    AxisEdit = true;
                    axis = newAxis;
                }
            }
            StartCoroutine("Orient");
        }

        IEnumerator Orient() {
            appState.Orientation.Set(appState.mainCamera.transform.forward);
            yield return new WaitForSeconds(2f);
        }


        /// <summary>
        /// Tasks tpo be perfomred when a project is fully loaded
        /// </summary>
        public void onProjectLoad() {
            transform.position = appState.project.Cameras[0].Coordinates.Vector3();

        }

        //
        // Methods for Input System based UIs
        //

        //
        // link this to an a 2D axis control for in plane movement
        //
        public void HandleMove(InputAction.CallbackContext context)
        {
            Vector3 force = Quaternion.Euler(90, 0, 0) * (context.ReadValue<Vector2>().normalized * DesktopAcceleration);
            MoveCamera(force);
        }

        // 
        // link this to a 1D axis control for vertical movement
        //
        public void HandleVertical(InputAction.CallbackContext context)
        {
            Vector3 force = context.ReadValue<Vector2>().normalized * DesktopAcceleration;
            MoveCamera(force);
        }


        //
        // Link this to a 2D axis control to control pan and zoom
        //
        public void HandlePanZoom(InputAction.CallbackContext context)
        {
            Vector2 pz_input = context.ReadValue<Vector2>().normalized;
            //transform.LookAt(Vector3.zero);
            float pan = pz_input.x;
            Pan(pan);
            float zoom = pz_input.y * 0.1f;
            Zoom(zoom);
        }

        //
        // Link this to the mouse delta control
        //
        public void HandleMouseRotation(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            if (!editSelected)
            {
                //mouse input
                var rotationHorizontal = XAxisSensitivity * input.x;
                var rotationVertical = YAxisSensitivity * input.y;

                //applying mouse rotation
                // always rotate Y in global world space to avoid gimbal lock
                transform.Rotate(Vector3.up * rotationHorizontal, Space.World);

                var rotationY = transform.localEulerAngles.y;

                _rotationX += rotationVertical;
                _rotationX = Mathf.Clamp(_rotationX, -MaxXAngle, MaxXAngle);

                transform.localEulerAngles = new Vector3(-_rotationX, rotationY, 0);
            }
            else
            {
                Ray ray = appState.mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                Vector3 to = ray.GetPoint(selectedDistance);

                if (currentPointerHit != null)
                {
                    moveTo(to);
                    from = to;
                }
            }
        }

        //
        // use this for processing any key inputs
        // 

        public async void HandleKeyInput(InputAction.CallbackContext context)
        {
            InputAction action = context.action;
            switch (action.name)
            {
                case "StartEdit":
                    appState.StartEditSession();
                    break;
                case "StopSaveEdit":
                    appState.StopSaveEditSession();
                    break;
                case "StopDiscardEdit":
                    appState.StopDiscardEditSession();
                    break;
                case "Exit":
                    Debug.Log("FlyingCam.Exit save before quit");
                    MapInitialize mi = appState.map.GetComponentInChildren<MapInitialize>();
                    await mi.Save();
                    Debug.Log("FlyingCam.Exit now quit");
                    Application.Quit();
                    break;
            }
        }

        //
        // Link this to the mouse click control
        //

        public void HandleMouseClick(InputAction.CallbackContext context)
        {
            InputAction action = context.action;
            if (action.phase == InputActionPhase.Canceled && appState.InEditSession())
            {
                UnClickHandler(action.name);
            }
            else if (action.phase == InputActionPhase.Started && !editSelected && appState.InEditSession())
            {
                ClickHandler(action.name);
            }
        }

        private void ClickHandler(string button)
        {
            RaycastHit hitInfo = new RaycastHit();
            Vector3 mousePos = Input.mousePosition;
            Ray ray = appState.mainCamera.ScreenPointToRay(mousePos);
            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1000f);
            bool hit = Physics.Raycast(ray, out hitInfo);
            if (hit)
            {
                currentPointerHit = hitInfo.transform;
                selectedDistance = hitInfo.distance;
                from = hitInfo.point;
                if (button == "Select")
                    appState.ButtonStatus.isRhTrigger = true;
                else
                    appState.ButtonStatus.isRhGrip = true;
            }
            else
            {
                editSelected = false;
            }
        }


        private void UnClickHandler(string button)
        {
            if (button == "Select")
                appState.ButtonStatus.isRhTrigger = false;
            else
                appState.ButtonStatus.isRhGrip = false;
        }


        //
        //
        // Methods for VRTK based integratin to the VR UI
        //


        //
        // Link this to a 2D control for in plane movement
        //
        public void Move(Vector2 axis)
        {
            if (axis != Vector2.zero)
            {
                Vector3 force = Quaternion.AngleAxis(90.0f, Vector3.right) * axis * VrAcceleration;
                MoveCamera(force);
            }
        }

        //
        // Link this to a 2d axis control for pan and zoom and move away
        //
        public void PanZoom(Vector2 axis)
        {
            Vector2 pz_input = axis;
            if (!editSelected)
            {
                float pan = pz_input.x * PanSensitvity;
                Pan(pan);
                float zoom = pz_input.y * ZoomSensitivity;
                Zoom(zoom);
            }
            else
            {
                selectedDistance += pz_input.y * AppState.instance.map.transform.TransformVector(Vector3.one * SlideMod).magnitude;
            }
        }


        //
        // link to boolean action for when the ray hits a collider
        //
        // e.g VRTK Pointer Entering event
        //
        public void PointerHit(ObjectPointer.EventData data)
        {
            if (!editSelected)
            {
                RaycastHit hitInfo = data.CurrentPointsCastData.HitData.Value;
                currentPointerHit = hitInfo.transform;
                selectedDistance = hitInfo.distance;
                from = hitInfo.point;
                select(appState.ButtonStatus);
                if (addVertexState)
                {
                    AddVertex(hitInfo.point);
                }
                if (delVertexState)
                {
                    RemoveVertex();
                }
                currentPointerHit?.SendMessage("Hover", SendMessageOptions.DontRequireReceiver);
            }

        }


        //
        // link to boolean action for when the ray stops hiting a collider
        //
        // e.g VRTK Pointer Exiting event
        //
        public void PointerUnhit(ObjectPointer.EventData data)
        {
            currentPointerHit?.SendMessage("UnHover", SendMessageOptions.DontRequireReceiver);
            currentPointerHit = null;
        }

        //
        // link to boolean action for first event type
        //
        // you can have as many as you like, you just have to clone this call and change button value
        //
        public void triggerPressed(bool thisEvent)
        {
            appState.ButtonStatus.isRhTrigger = true;
        }

        public void gripPressed(bool thisEvent)
        {
            appState.ButtonStatus.isRhGrip = true;
        }

        public void lhTriggerPressed(bool thisEvent)
        {
            appState.ButtonStatus.isLhTrigger = true;
        }

        public void lhGripPressed(bool thisEvent)
        {
            appState.ButtonStatus.isLhGrip = true;
        }

        public void addVertexPressed(bool thisEvent)
        {
            addVertexState = true;
            AddVertex(from);
        }

        public void delVertexPressed(bool thisEvent)
        {
            delVertexState = true;
            RemoveVertex();
        }

        //
        // call this when any event type ends
        //
        public void triggerReleased(bool thisEvent)
        {
            appState.ButtonStatus.isRhTrigger = false;

        }

        public void gripReleased(bool thisEvent)
        {
            appState.ButtonStatus.isRhGrip = false;
            AxisEdit = false;

        }

        public void lhTriggerReleased(bool thisEvent)
        {
            appState.ButtonStatus.isLhTrigger = false;
        }

        public void lhGripRelaesed(bool thisEvent)
        {
            appState.ButtonStatus.isLhGrip = false;
            AxisEdit = false;
        }

        public void addVertexReleased(bool thisEvent)
        {
            addVertexState = false;
        }

        public void delVertexReleased(bool thisEvent)
        {
            delVertexState = false;
        }

        //
        // this is used to get data on the movement of the pointer to allow move events to be sent to enitties
        //
        // link to the StraightCaster Changed event
        //
        public void receiveRay(PointsCast.EventData data)
        {
            lastHitPosition = data.Points[1];
            if (editSelected || currentPointerHit != null)
            {
                StartCoroutine(UpDateMove(data));
            }
        }

        IEnumerator UpDateMove(PointsCast.EventData data) {
            Vector3 dir = data.Points[1] - data.Points[0];
            Vector3 to = data.Points[0] + dir.normalized * selectedDistance;
            if (editSelected)
                moveTo(to);
            from = to;
            yield return new WaitForSeconds(0.2f);
        }


        //
        // link to boolean evcents to increase and decrease scale
        //
        public void ScaleUp(bool thisEvent)
        {
            if (thisEvent)
            {
                Scale(ZoomSensitivity);
            }
        }

        public void ScaleDown(bool thisEvent)
        {
            if (thisEvent)
            {
                Scale(-ZoomSensitivity);
            }
        }



        //
        // Internal methods common to both UIs
        //
        private void Pan(float pan)
        {
            if (pan != 0)
                transform.RotateAround(Vector3.zero, Vector3.down, pan);
        }

        private void Zoom(float zoom)
        {
            if (zoom != 0)
            {
                AppState.instance.Zoom.Set(AppState.instance.Zoom.Get() * (1 - zoom));
            }
        }

        private void Scale(float scale)
        {
            Vector3 here = AppState.instance.map.transform.InverseTransformPoint(transform.position);
            AppState.instance.Zoom.Set(AppState.instance.Zoom.Get() * (1 - scale));
            transform.position = AppState.instance.map.transform.TransformPoint(here);
        }

        private void moveTo(Vector3 to)
        {
            if (!AxisEdit)
            {
                MoveArgs args = new MoveArgs();
                args.translate = to - from;
                currentSelected?.SendMessage("MoveTo", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void select(ButtonStatus button)
        {
            if (
                appState.ButtonStatus.activate &&
                SELECT_SELECTION_TYPES.Contains(appState.ButtonStatus.SelectionType) &&
                appState.InEditSession() &&
                currentPointerHit != null &&
                LayerIsEditable()) {
                    editSelected = true;
                    currentSelected = currentPointerHit;
                    currentSelected.SendMessage("Selected", appState.ButtonStatus.SelectionType, SendMessageOptions.DontRequireReceiver);
            }
        }

        private void unSelect(ButtonStatus button)
        {
            if (!appState.ButtonStatus.activate) {
                editSelected = false;
                currentSelected?.SendMessage("UnSelected", appState.ButtonStatus.SelectionType, SendMessageOptions.DontRequireReceiver);
                currentSelected = null;
            }
        }

        private void MoveAxis(MoveArgs args)
        {
            if (AxisEdit)
            {
                currentSelected?.SendMessage("MoveAxis", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        private bool LayerIsEditable()
        {
            IVirgisLayer layer;
            if (currentSelected != null)
            {
                layer = currentSelected.GetComponentInParent<IVirgisLayer>();
            }
            else
            {
                layer = currentPointerHit?.GetComponentInParent<IVirgisLayer>();
            }
            return layer?.IsEditable() ?? false;
        }

        private void MoveCamera(Vector3 force)
        {
            _thisRigidbody.AddForce(appState.trackingSpace.transform.rotation * force, ForceMode.Force);
        }

        private void AddVertex(Vector3 pos)
        {
            if (appState.InEditSession() && currentPointerHit != null && LayerIsEditable())
            {
                currentPointerHit.SendMessage("AddVertex", pos, SendMessageOptions.DontRequireReceiver);
                addVertexState = false;
            }
        }

        private void RemoveVertex()
        {
            if (appState.InEditSession() && currentSelected != null && LayerIsEditable())
            {
                currentSelected.SendMessage("Delete", SendMessageOptions.DontRequireReceiver);
                currentSelected = null;
                currentPointerHit = null;
            }
        }
    }
}