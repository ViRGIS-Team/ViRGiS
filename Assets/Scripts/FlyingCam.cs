//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zinnia.Pointer;
using Zinnia.Cast;
using VRTK.Prefabs.Interactions.Interactables;
using UnityEngine.Events;
using UnityEngine.UI;


public class FlyingCam : MonoBehaviour
{
    [Header("Constants")]

    //unity controls and constants input - keyboard
    public float AccelerationMod;
    public float DeccelerationMod;
    public float XAxisSensitivity;
    public float YAxisSensitivity;

    //unity controls - VR
    public float HorizontalMod;
    public float VerticalMod;
    public float PandSensitvity;
    public float ZoomSensitivity;

    [Space]

    [Range(0, 89)] public float MaxXAngle = 60f;

    [Space]

    public float MaximumMovementSpeed = 1f;


    private bool editSelected = false;
    public Camera self;
    private Rigidbody selectedRigibody;
    private float selectedDistance;
    private Vector3 speed;

    public EventManager eventManager;

    private Transform currentPointerHit;


    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        transform.Translate(speed);
        OVRInput.Update();
        speed -= speed / DeccelerationMod;
    }

    private void FixedUpdate()
    {
        OVRInput.FixedUpdate();
    }

    public void HandleMove(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += Quaternion.AngleAxis(90.0f, Vector3.right) * speed_input;
    }

    public void Move(Vector2 axis)
    {
        Vector3 speed_input = axis.normalized * HorizontalMod;
        speed += Quaternion.AngleAxis(90.0f, Vector3.right) * speed_input;
    }

    public void Up(bool  thisEvent) 
    {
        if (thisEvent)
        {
            Vector3 speed_input = Vector3.up * VerticalMod;
            speed += speed_input;
        }
    }

    public void Down(bool thisEvent)
    {
        if (thisEvent)
        {
            Vector3 speed_input = Vector3.down * VerticalMod;
            speed += speed_input;
        }
    }

    public void HandleVertical(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += speed_input;
    }

    public void HandlePanZoom(InputAction.CallbackContext context)
    {
        Vector2 pz_input = context.ReadValue<Vector2>().normalized;
        transform.LookAt(Vector3.zero);
        float pan = pz_input.x;
        Pan(pan);
        float zoom = pz_input.y * 0.1f;
        Zoom(zoom);
    }
    public void PanZoom(Vector2 axis)
    {
        Vector2 pz_input = axis.normalized;
        float pan = pz_input.x * PandSensitvity;
        Pan(pan);
        float zoom = pz_input.y * ZoomSensitivity;
        Zoom(zoom);
    }

    private void Pan(float pan)
    {
        gameObject.transform.RotateAround(Vector3.zero, Vector3.down, pan);
    }

    private void Zoom(float zoom)
    {
        transform.Translate(Vector3.forward * Vector3.Distance(transform.position, Vector3.zero) * zoom);
    }

    private float _rotationX;

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
            Ray ray = self.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 newPos = ray.GetPoint(selectedDistance);
            if (selectedRigibody != null)
            {
                selectedRigibody.gameObject.SendMessage("MoveTo", newPos, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public void HandleKeyInput(InputAction.CallbackContext context)
    {
        InputAction action = context.action;
        if (action.name == "Edit" && !Global.EditSession)
        {
            Global.EditSession = true;
        }
        if (action.name == "EndEdit" && Global.EditSession)
        {
            Global.EditSession = false;
            EventManager eventManager = Global.Map.GetComponent<EventManager>();
            eventManager.OnEditsessionEnd.Invoke();
        }
        if (action.name == "Exit")
        {
            Debug.Log("Exit");
            Application.Quit();
        }
    }

    public void HandleMouseClick(InputAction.CallbackContext context)
    {
        InputAction action = context.action;
        int button = 0;
        switch (action.name)
        {
            case "Select":
                button = 0;
                break;
            case "MultiSelect":
                button = 1;
                break;
        }
        if (action.phase == InputActionPhase.Canceled && Global.EditSession)
        {
            UnClickHandler(button);
        }
        else if (action.phase == InputActionPhase.Started && !editSelected && Global.EditSession)
        {
            ClickHandler(button);
        }
    }

    private void ClickHandler(int button)
    {
        RaycastHit hitInfo = new RaycastHit();
        Vector3 mousePos = Input.mousePosition;
        Ray ray = self.ViewportPointToRay(mousePos); //    ScreenPointToRay(mousePos);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1000f);
        bool hit = Physics.Raycast(ray, out hitInfo);
        if (hit)
        {
            selectedRigibody = hitInfo.rigidbody;
            if (selectedRigibody != null)
            {
                editSelected = true;
                selectedRigibody.gameObject.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
                selectedDistance = hitInfo.distance;
            }
        }
        else
        {
            editSelected = false;
        }
    }


    private void UnClickHandler(int button)
    {
        editSelected = false;
        if (selectedRigibody != null)
        {
            selectedRigibody.gameObject.SendMessage("UnSelected", button, SendMessageOptions.DontRequireReceiver);
            selectedRigibody = null;
        }
    }



    public void PointerHit(ObjectPointer.EventData data )
    {
        RaycastHit hitInfo = data.CurrentPointsCastData.HitData.Value;
        currentPointerHit = hitInfo.transform;
        selectedDistance = hitInfo.distance;

    }

    public void PointerUnhit(ObjectPointer.EventData data)
    {
        if (!editSelected)
        {
            currentPointerHit = null;
            selectedDistance = 0;
        }
    }

    public void triggerPressed(bool thisEvent)
    {
        if (currentPointerHit != null)
        {
            editSelected = true;
            int button = 0;
            currentPointerHit.gameObject.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
           
    }

    public void gripPressed(bool thisEvent)
    {
        if (currentPointerHit != null)
        {
            editSelected = true;
            int button = 1;
            currentPointerHit.gameObject.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
            
    }

    public void triggerReleased(bool thisEvent)
    {
        editSelected = false;
        if (currentPointerHit != null)
        {
            int button = 0;
            currentPointerHit.gameObject.SendMessage("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void receiveRay(PointsCast.EventData data)
    {
        if (editSelected)
        {

            if (!data.IsValid)
            {
                Vector3 dir = data.Points[1] - data.Points[0];
                dir = dir.normalized * selectedDistance;
                Vector3 newPos = data.Points[0] + dir;
                currentPointerHit.gameObject.SendMessage("MoveTo", newPos, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

}
