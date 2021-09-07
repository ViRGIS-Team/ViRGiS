using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace Virgis
{
    public class VirgisUIController : XRBaseController
    {
        bool m_Selected = false;

        [SerializeField]
        [Tooltip("The XRNode for this controller.")]
        XRNode m_ControllerNode = XRNode.RightHand;

        /// <summary>
        /// The <see cref="XRNode"/> for this controller.
        /// </summary>
        public XRNode controllerNode {
            get => m_ControllerNode;
            set => m_ControllerNode = value;
        }

        InputDevice m_InputDevice;
        /// <summary>
        /// (Read Only) The <see cref="InputDevice"/> being used to read data from.
        /// </summary>
        public InputDevice inputDevice => m_InputDevice.isValid ? m_InputDevice : m_InputDevice = InputDevices.GetDeviceAtXRNode(controllerNode);

        /// <inheritdoc />
        protected override void UpdateController()
        {
            GetControllerState(out XRControllerState controller);
            controller.position = transform.localPosition;
            controller.rotation = transform.localRotation;
            SetControllerState(controller);
            base.UpdateController();
        }

        public void SelectEvent(bool thisEvent) {
            m_Selected = thisEvent;
        }

        /// <inheritdoc />
        protected override void UpdateInput(XRControllerState controllerState)
        {
            controllerState.ResetFrameDependentStates();
            HandleInteractionAction( ref controllerState.uiPressInteractionState);
        }

        void HandleInteractionAction( ref InteractionState interactionState)
        {

            if (m_Selected)
            {
                if (!interactionState.active)
                {
                    interactionState.activatedThisFrame = true;
                    interactionState.active = true;
                }
            }
            else
            {
                if (interactionState.active)
                {
                    interactionState.deactivatedThisFrame = true;
                    interactionState.active = false;
                }
            }
        }

        /// <inheritdoc />
        public override bool SendHapticImpulse(float amplitude, float duration)
        {
            if (inputDevice.TryGetHapticCapabilities(out var capabilities) &&
                capabilities.supportsImpulse)
            {
                return inputDevice.SendHapticImpulse(0u, amplitude, duration);
            }
            return false;
        }

    }
}

