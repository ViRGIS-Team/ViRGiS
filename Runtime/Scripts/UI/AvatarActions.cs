//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UniRx;

namespace Virgis
{

    /// <summary>
    /// Main Script for controlling the UI behaviour and the movement of the Camera
    /// </summary>
    public class AvatarActions : MonoBehaviour
    {
        [Header ("Avatar Objects")]
        public Transform MovementVector; // reference to the active tracking space
        public Camera MainCamera; // the main camera for ray tracing

        protected bool m_editSelected = false; // edit state 
        protected float m_selectedDistance; // distance to the selected marker``
        protected Transform m_currentPointerHit; // current marker selected by pointer
        protected Transform m_currentSelected; // current marker in selected state

        protected Vector3? m_from; // caches the last position indicated by the user to which to move the selected component
        protected AppState m_appState;
        private Rigidbody m_thisRigidbody;
        protected bool m_axisEdit = false; // Whether we are in AxisEdit mode
        protected Quaternion m_panTarget = Quaternion.identity;
        protected bool m_addVertexState; // current state of the button to add vertex
        protected bool m_delVertexState; // current state of the button to remove vertex
        protected bool m_lightEdit = false; // are we currently editing the lights

        private List<SelectionType> SELECT_SELECTION_TYPES = new List<SelectionType>() { SelectionType.SELECT, SelectionType.SELECTALL, SelectionType.MOVEAXIS };

        public void Start()
        {
            m_appState = AppState.instance;
            m_appState.trackingSpace = MovementVector;
            m_appState.mainCamera = MainCamera;
            Debug.Log("AppState awakens");
            m_thisRigidbody = GetComponent<Rigidbody>();
            m_thisRigidbody.detectCollisions = false;
            m_appState.ButtonStatus.Event.Subscribe(select);
            m_appState.ButtonStatus.Event.Subscribe(unSelect);
            m_appState.Project.Event.Subscribe(Project => onProjectLoad());
            StartCoroutine(Orient());
        }

        IEnumerator Orient()
        {
            while (true)
            {
                m_appState.Orientation.Set(m_appState.mainCamera.transform.forward);
                yield return new WaitForSeconds(2f);
            }
        }

        /// <summary>
        /// Tasks to be performed when a project is fully loaded
        /// </summary>
        public void onProjectLoad()
        {
            if (m_appState.project.Scale == null)
            {
                m_appState.project.Scale = new List<float>() { 1f, 1f, 1f, 1f };
            };
            m_appState.Zoom.Set(m_appState.project.Scale[0]);
            m_appState.currentView = 0;
            transform.position = m_appState.project.Cameras[m_appState.currentView].Coordinates.Vector3();
            while (m_appState.project.Cameras.Count < 4)
            {
                m_appState.project.Cameras.Add(m_appState.project.Cameras[0]);
            }
        }

        //
        // Internal methods common to both UIs
        //
        protected void Pan(float pan)
        {
            if (pan != 0)
            {
                m_panTarget *= Quaternion.AngleAxis(pan, Vector3.up);
            }
        }

        protected void Zoom(float zoom)
        {
            if (zoom != 0)
            {

                Scale(AppState.instance.Zoom.Get() * (1 - zoom));
            }
        }

        protected void Scale(float scale)
        {
            Vector3 here = m_appState.map.transform.InverseTransformPoint(transform.position);
            m_appState.Zoom.Set(scale);
            transform.position = m_appState.map.transform.TransformPoint(here);
        }

        protected void moveTo(Vector3 to)
        {
            if (!m_axisEdit)
            {
                MoveArgs args = new MoveArgs();
                args.translate = to - (m_from  ?? to);
                m_currentSelected?.SendMessage("MoveTo", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        protected virtual void select(ButtonStatus button)
        {
            if (
                button.activate &&
                SELECT_SELECTION_TYPES.Contains(m_appState.ButtonStatus.SelectionType) &&
                m_appState.InEditSession() &&
                m_currentPointerHit != null &&
                LayerIsEditable())
            {
                m_editSelected = true;
                m_currentSelected = m_currentPointerHit;
                m_currentSelected.SendMessage("Selected", m_appState.ButtonStatus.SelectionType, SendMessageOptions.DontRequireReceiver);
            }
            else if (button.activate &&
                     button.isLhGrip )
            {
                m_lightEdit = true;
            }
        }

        protected virtual void unSelect(ButtonStatus button)
        {
            if (!button.activate)
            {
                m_editSelected = false;
                m_currentSelected?.SendMessage("UnSelected", m_appState.ButtonStatus.SelectionType, SendMessageOptions.DontRequireReceiver);
                m_currentSelected = null;
                m_lightEdit = false;
                m_from = null;
            }
        }

        protected void MoveAxis(MoveArgs args)
        {
            if (m_axisEdit)
            {
                m_currentSelected?.SendMessage("MoveAxis", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        protected bool LayerIsEditable()
        {
            IVirgisLayer layer;
            if (m_currentSelected != null)
            {
                layer = m_currentSelected.GetComponentInParent<IVirgisLayer>();
            }
            else
            {
                layer = m_currentPointerHit?.GetComponentInParent<IVirgisLayer>();
            }
            return layer?.IsEditable() ?? false;
        }

        protected void MoveCamera(Vector3 force)
        {
            m_thisRigidbody.AddForce(m_appState.trackingSpace.rotation * force, ForceMode.Force);
        }

        protected void AddVertex(Vector3 pos)
        {
            if (m_appState.InEditSession() && m_currentPointerHit != null && LayerIsEditable())
            {
                m_currentPointerHit.SendMessage("AddVertex", pos, SendMessageOptions.DontRequireReceiver);
                m_addVertexState = false;
            }
        }

        protected void RemoveVertex()
        {
            if (m_appState.InEditSession() && m_currentSelected != null && LayerIsEditable())
            {
                m_currentSelected.SendMessage("Delete", SendMessageOptions.DontRequireReceiver);
                m_currentSelected = null;
                m_currentPointerHit = null;
            }
        }
    }

}