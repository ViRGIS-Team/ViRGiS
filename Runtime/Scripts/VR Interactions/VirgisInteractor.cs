using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;

namespace Virgis
{

    public class VirgisInteractor : XRDirectInteractor, IUIInteractor
    {
        XRUIInputModule m_InputModule;
        XRUIInputModule m_RegisteredInputModule;
        private XRNode m_ControllerNode = XRNode.RightHand;

        private AppState appState;

        [SerializeField]
        public bool m_EnableUIInteraction = true;
        /// <summary>
        /// Gets or sets whether this interactor is able to affect UI.
        /// </summary>
        public bool enableUIInteraction
        {
            get => m_EnableUIInteraction;
            set
            {
                if (m_EnableUIInteraction != value)
                {
                    m_EnableUIInteraction = value;
                    RegisterOrUnregisterXRUIInputModule();
                }
            }
        }

        /// <inheritdoc/>
        private void Update() {
            // get the model and check for gui active
            if (TryGetUIModel(out TrackedDeviceModel model)) {
                if (model.currentRaycast.isValid) {
                    if (m_ControllerNode == XRNode.LeftHand)
                        appState.lhguiActive = true;
                    else
                        appState.rhguiActive = true;
                } else {
                    if (m_ControllerNode == XRNode.LeftHand)
                        appState.lhguiActive = false;
                    else
                        appState.rhguiActive = false;
                }
            }
        }

        private new void Start() {
            base.Start();
            appState = AppState.instance;
            m_ControllerNode = GetComponent<VirgisUIController>().controllerNode;
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_EnableUIInteraction)
                RegisterWithXRUIInputModule();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();


            if (m_EnableUIInteraction)
                UnregisterFromXRUIInputModule();
        }

        void FindOrCreateXRUIInputModule()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            else
            {
                // Remove the Standalone Input Module if already implemented, since it will block the XRUIInputModule
                var standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneInputModule != null)
                    Destroy(standaloneInputModule);
            }

            m_InputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (m_InputModule == null)
                m_InputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        /// <summary>
        /// Register with the <see cref="XRUIInputModule"/> (if necessary).
        /// </summary>
        /// <seealso cref="UnregisterFromXRUIInputModule"/>
        void RegisterWithXRUIInputModule()
        {
            if (m_InputModule == null)
                FindOrCreateXRUIInputModule();

            if (m_RegisteredInputModule == m_InputModule)
                return;

            UnregisterFromXRUIInputModule();

            m_InputModule.RegisterInteractor(this);
            m_RegisteredInputModule = m_InputModule;
        }

        /// <summary>
        /// Unregister from the <see cref="XRUIInputModule"/> (if necessary).
        /// </summary>
        /// <seealso cref="RegisterWithXRUIInputModule"/>
        void UnregisterFromXRUIInputModule()
        {
            if (m_RegisteredInputModule != null)
                m_RegisteredInputModule.UnregisterInteractor(this);

            m_RegisteredInputModule = null;
        }

        /// <summary>
        /// Register with or unregister from the Input Module (if necessary).
        /// </summary>
        /// <remarks>
        /// If this behavior is not active and enabled, this function does nothing.
        /// </remarks>
        void RegisterOrUnregisterXRUIInputModule()
        {
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;

            if (m_EnableUIInteraction)
                RegisterWithXRUIInputModule();
            else
                UnregisterFromXRUIInputModule();
        }

        public virtual void UpdateUIModel(ref TrackedDeviceModel model)
        {
           
        }

        /// <inheritdoc />
        public bool TryGetUIModel(out TrackedDeviceModel model)
        {
            if (m_InputModule != null)
            {
                return m_InputModule.GetTrackedDeviceModel(this, out model);
            }

            model = new TrackedDeviceModel(-1);
            return false;
        }
    }
}
