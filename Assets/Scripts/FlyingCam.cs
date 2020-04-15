//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using UnityEngine.InputSystem;
using Zinnia.Pointer;
using Zinnia.Cast;

public class FlyingCam : MonoBehaviour
{
    [Header("Constants")]

    public Camera self;
    public GameObject trackingSpace;
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
    public float SlideMod;

    [Space]

    [Range(0, 89)] public float MaxXAngle = 60f;

    [Space]



    private bool editSelected = false;
    private Transform  selectedRigibody;
    private float selectedDistance;
    private Vector3 speed;
    private float _rotationX;
    private Transform currentPointerHit;
    private bool triggerState;
    private bool gripState;


    private void Start()
    {
        Global.trackingSpace = trackingSpace;
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

    //
    // Methods for Input System based UIs
    //

    //
    // link this to an a 2D axis control for in plane movement
    //
    public void HandleMove(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += Quaternion.AngleAxis(90.0f, Vector3.right) * speed_input;
    }

    // 
    // link this to a 1D axis control for vertical movement
    //
    public void HandleVertical(InputAction.CallbackContext context)
    {
        Vector3 speed_input = context.ReadValue<Vector2>().normalized * AccelerationMod;
        speed += speed_input;
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
            Ray ray = self.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 newPos = ray.GetPoint(selectedDistance);
            if (selectedRigibody != null)
            {
                selectedRigibody.gameObject.SendMessage("MoveTo", newPos, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    //
    // use this for processing any key inputs
    // 

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

    //
    // Link this to the mouse click control
    //

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
        Ray ray = self.ScreenPointToRay(mousePos);
       //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1000f);
        bool hit = Physics.Raycast(ray, out hitInfo);
        if (hit)
        {
            selectedRigibody = hitInfo.transform;
            if (selectedRigibody != null)
            {
                editSelected = true;
                select(selectedRigibody, button);
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
            unSelect(selectedRigibody, button);
            selectedRigibody = null;
        }
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
            Vector3 speed_input = Quaternion.AngleAxis(90.0f, Vector3.right) * axis * HorizontalMod;
            Vector3 reference =  Global.trackingSpace.transform.localRotation.eulerAngles;
            speed += Quaternion.AngleAxis(reference.y, Vector3.up)  * speed_input;
        }
    }

    //
    // Link this to a boolean control for movement up
    //
    public void Up(bool thisEvent)
    {
        if (thisEvent)
        {
            Vector3 speed_input = Vector3.up * VerticalMod;
            speed += speed_input;
        }
    }

    //
    // link this to a boolean control for movement down
    //
    public void Down(bool thisEvent)
    {
        if (thisEvent)
        {
            Vector3 speed_input = Vector3.down * VerticalMod;
            speed += speed_input;
        }
    }


    //
    // Link this to a 2d axis control for pan and zoom and move away
    //
    public void PanZoom(Vector2 axis)
    {
        Vector2 pz_input = axis.normalized;
        if (!editSelected)
        {
            float pan = pz_input.x * PandSensitvity;
            Pan(pan);
            float zoom = pz_input.y * ZoomSensitivity;
            Zoom(zoom);
        } else
        {
            selectedDistance += pz_input.y * SlideMod;
        }
    }


    //
    // link to boolean action for when the ray hits a collider
    //
    // e.g VRTK Pointer Entering event
    //
    public void PointerHit(ObjectPointer.EventData data )
    {
        if (!editSelected)
        {
            RaycastHit hitInfo = data.CurrentPointsCastData.HitData.Value;
            currentPointerHit = hitInfo.transform;
            selectedDistance = hitInfo.distance;
            if (triggerState)
            {
                editSelected = true;
                select(currentPointerHit, 0);
            }
            if (gripState)
            {
                editSelected = true;
                select(currentPointerHit, 1);
            }
        }

    }

    //
    // link to boolean action for when the ray stops hiting a collider
    //
    // e.g VRTK Pointer Exiting event
    //
    public void PointerUnhit(ObjectPointer.EventData data)
    {
        if (!editSelected)
        {
            currentPointerHit = null;
            selectedDistance = 0;
        }
    }

    //
    // link to boolean action for first event type
    //
    // you can have as many as you like, you just have to clone this call and change button value
    //
    public void triggerPressed(bool thisEvent)
    {
        triggerState = true;
        if (currentPointerHit != null)
        {
            editSelected = true;
            select(currentPointerHit, 0);
        }
           
    }

    public void gripPressed(bool thisEvent)
    {
        gripState = true;
        if (currentPointerHit != null)
        {
            editSelected = true;
            select(currentPointerHit, 1);
        }
            
    }

    //
    // call this when any event type ends
    //
    public void triggerReleased(bool thisEvent)
    {
        triggerState = false;
        editSelected = false;
        selectedDistance = 0;
        if (currentPointerHit != null)
        {
            unSelect(currentPointerHit, 0);
            currentPointerHit = null;
        }
    }

    public void gripReleased(bool thisEvent)
    {
        gripState = false; 
        editSelected = false;
        selectedDistance = 0;
        if (currentPointerHit != null)
        {
            unSelect(currentPointerHit, 1);
            currentPointerHit = null;
        }
    }

    //
    // this is used to get data on the movement of the pointer to allow move events to be sent to enitties
    //
    // link to the StraightCaster Changed event
    //
    public void receiveRay(PointsCast.EventData data)
    {
        if (editSelected)
        {
        Vector3 dir = data.Points[1] - data.Points[0];
        dir = dir.normalized * selectedDistance;
        moveTo(currentPointerHit, data.Points[0] + dir);
        }
    }

    //
    // loink to boolean evcents to increase and decrease scale
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
        if (pan != 0) gameObject.transform.RotateAround(Vector3.zero, Vector3.down, pan);
    }

    private void Zoom(float zoom)
    {
        if (zoom != 0)
        {
            //transform. Translate(Vector3.forward * Vector3.Distance(transform.position, Vector3.zero) * zoom);
            Global.Map.transform.localScale *= 1 + zoom;
        }
    }

    private void Scale(float scale)
    {
        Vector3 here = Global.Map.transform.InverseTransformPoint(transform.position);
        Global.Map.transform.localScale *= 1 + scale;
        transform.position = Global.Map.transform.TransformPoint(here);
    }

    private void moveTo(Transform target, Vector3 pos)
    {
        target.gameObject.SendMessage("MoveTo", pos, SendMessageOptions.DontRequireReceiver);
    }

    private void select(Transform target, int button)
    {
        target.gameObject.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
    }

    private void unSelect(Transform target, int button)
    {
        target.gameObject.SendMessage("UnSelected", button, SendMessageOptions.DontRequireReceiver);
    }


}
