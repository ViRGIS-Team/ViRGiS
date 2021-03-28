using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;

namespace Virgis
{
    public class VirgisUIController : XRBaseController
    {
        [SerializeField]
        [Tooltip("The XRNode for this controller.")]
        XRNode m_ControllerNode = XRNode.RightHand;

        /// <summary>
        /// The <see cref="XRNode"/> for this controller.
        /// </summary>
        public XRNode controllerNode
        {
            get => m_ControllerNode;
            set => m_ControllerNode = value;
        }

        [SerializeField]
        [Tooltip("The input to use for detecting a UI press.")]
        InputHelpers.Button m_UIPressUsage = InputHelpers.Button.Trigger;

        /// <summary>
        /// The input to use for detecting a UI press.
        /// </summary>
        public InputHelpers.Button uiPressUsage
        {
            get => m_UIPressUsage;
            set => m_UIPressUsage = value;
        }

        [SerializeField]
        [Tooltip("The amount an axis needs to be pressed to trigger an interaction event.")]
        float m_AxisToPressThreshold = 0.1f;

        /// <summary>
        /// The amount an axis needs to be pressed to trigger an interaction event.
        /// </summary>
        public float axisToPressThreshold
        {
            get => m_AxisToPressThreshold;
            set => m_AxisToPressThreshold = value;
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

        /// <inheritdoc />
        protected override void UpdateInput(XRControllerState controllerState)
        {
            controllerState.ResetFrameDependentStates();
            HandleInteractionAction(m_UIPressUsage, ref controllerState.uiPressInteractionState);
        }

        void HandleInteractionAction(InputHelpers.Button button, ref InteractionState interactionState)
        {
            inputDevice.IsPressed(button, out var pressed, m_AxisToPressThreshold);

            if (pressed)
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

