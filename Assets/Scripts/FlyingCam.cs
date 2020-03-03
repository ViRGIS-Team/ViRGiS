﻿//https://stackoverflow.com/questions/58328209/how-to-make-a-free-fly-camera-script-in-unity-with-acceleration-and-decceleratio
// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlyingCam : MonoBehaviour
{
    [Header("Constants")]

    //unity controls and constants input
    public float AccelerationMod;
    public float XAxisSensitivity;
    public float YAxisSensitivity;
    public float DecelerationMod;

    [Space]

    [Range(0, 89)] public float MaxXAngle = 60f;

    [Space]

    public float MaximumMovementSpeed = 1f;

    [Header("Controls")]

    public KeyCode Forwards = KeyCode.UpArrow;
    public KeyCode Backwards = KeyCode.DownArrow;
    public KeyCode Left = KeyCode.LeftArrow;
    public KeyCode Right = KeyCode.RightArrow;
    public KeyCode Up = KeyCode.Q;
    public KeyCode Down = KeyCode.A;
    public KeyCode EditSession = KeyCode.E;
    public KeyCode ExitEditSession = KeyCode.D;
    public KeyCode Exit = KeyCode.X;
    public KeyCode RotLeft = KeyCode.Comma;
    public KeyCode RotRight = KeyCode.Period;
    public KeyCode ScaleIn = KeyCode.L;
    public KeyCode ScaleOut = KeyCode.M;
    public KeyCode ZoomIn = KeyCode.Plus;
    public KeyCode ZoomOut = KeyCode.Minus;

    private Vector3 _moveSpeed;

    private bool editSelected = false;
    private Camera self;
    private Rigidbody selectedRigibody;
    private float selectedDistance;

    public EventManager eventManager;


    private void Start()
    {
        _moveSpeed = Vector3.zero;
        self = gameObject.GetComponent<Camera>();

    }

    // Update is called once per frame
    private void Update()
    {
        HandleMouseRotation();
        HandleMouseClick();
        Vector3 acceleration = HandleMotionKeyInput();
        HandleProceduralKeyInput();

        _moveSpeed += acceleration;

        HandleDeceleration(acceleration);

        // clamp the move speed
        if (_moveSpeed.magnitude > MaximumMovementSpeed)
        {
            _moveSpeed = _moveSpeed.normalized * MaximumMovementSpeed;
        }

        transform.Translate(_moveSpeed);
    }

    private Vector3 HandleMotionKeyInput()
    {
        Vector3 acceleration = Vector3.zero;
        //key input detection
        if (Input.GetKey(Forwards))
        {
            acceleration.z += 1;
        }

        if (Input.GetKey(Backwards))
        {
            acceleration.z -= 1;
        }

        if (Input.GetKey(Left))
        {
            acceleration.x -= 1;
        }

        if (Input.GetKey(Right))
        {
            acceleration.x += 1;
        }

        if (Input.GetKey(Up))
        {
            acceleration.y += 1;
        }

        if (Input.GetKey(Down))
        {
            acceleration.y -= 1;
        }

        return acceleration.normalized * AccelerationMod;
    }

    private float _rotationX;

    private void HandleMouseRotation()
    {
        if (!editSelected)
        {
            //mouse input
            var rotationHorizontal = XAxisSensitivity * Input.GetAxis("Mouse X");
            var rotationVertical = YAxisSensitivity * Input.GetAxis("Mouse Y");

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
            Ray ray = self.ScreenPointToRay(Input.mousePosition);
            Vector3 newPos = ray.GetPoint(selectedDistance);
            if (selectedRigibody != null)
            {
                selectedRigibody.gameObject.SendMessage("MoveTo", newPos);
            }
        }
    }

    private void HandleDeceleration(Vector3 acceleration)
    {
        //deceleration functionality
        if (Mathf.Approximately(Mathf.Abs(acceleration.x), 0))
        {
            if (Mathf.Abs(_moveSpeed.x) < DecelerationMod)
            {
                _moveSpeed.x = 0;
            }
            else
            {
                _moveSpeed.x -= DecelerationMod * Mathf.Sign(_moveSpeed.x);
            }
        }

        if (Mathf.Approximately(Mathf.Abs(acceleration.y), 0))
        {
            if (Mathf.Abs(_moveSpeed.y) < DecelerationMod)
            {
                _moveSpeed.y = 0;
            }
            else
            {
                _moveSpeed.y -= DecelerationMod * Mathf.Sign(_moveSpeed.y);
            }
        }

        if (Mathf.Approximately(Mathf.Abs(acceleration.z), 0))
        {
            if (Mathf.Abs(_moveSpeed.z) < DecelerationMod)
            {
                _moveSpeed.z = 0;
            }
            else
            {
                _moveSpeed.z -= DecelerationMod * Mathf.Sign(_moveSpeed.z);
            }
        }
    }

    private void HandleProceduralKeyInput()
    {
        if (Input.GetKey(EditSession) && !Global.EditSession)
        {
            Global.EditSession = true;
        }
        if (Input.GetKey(ExitEditSession) && Global.EditSession)
        {
            Global.EditSession = false;
            EventManager eventManager = Global.Map.GetComponent<EventManager>();
            eventManager.OnEditsessionEnd.Invoke();
        }
        if (Input.GetKey(Exit))
        {
            Debug.Log("Exit");
            Application.Quit();
        }
        if (Input.GetKey(RotLeft))
        {
            gameObject.transform.RotateAround(Vector3.zero, Vector3.up, 10f);
        }
        if (Input.GetKey(RotRight))
        {
            gameObject.transform.RotateAround(Vector3.zero, Vector3.up, -10f);
        }
        if (Input.GetKey(ScaleIn))
        {
            gameObject.transform.localScale = gameObject.transform.localScale * 0.9f;
        }
        if (Input.GetKey(ScaleOut))
        {
            gameObject.transform.localScale = gameObject.transform.localScale * 1.1f;
        }
        if (Input.GetKey(ZoomIn))
        {
            Global._map.SetZoom(Global._map.Zoom - 1.0f);
        }
        if (Input.GetKey(ZoomOut))
        {
            Global._map.SetZoom(Global._map.Zoom + 1.0f);
        }
    }

    private void HandleMouseClick()
    {
        if (Input.GetMouseButtonDown(0) && Global.EditSession)
        {
            //left button
            ClickHandler(0);

        }
        if (Input.GetMouseButtonUp(0))
        {
            //left button
            UnClickHandler(0);
        }
        if (Input.GetMouseButtonDown(1) && Global.EditSession)
        {
            //right button
            ClickHandler(1);
        }
        if (Input.GetMouseButtonUp(1)) {
            //right button
            UnClickHandler(1);
        }
    }

    private void ClickHandler(int button)
    {
        RaycastHit hitInfo = new RaycastHit();
        bool hit = Physics.Raycast(self.ScreenPointToRay(Input.mousePosition), out hitInfo);
        if (hit)
        {
            //Debug.Log("Hit " + hitInfo.transform.gameObject.name);
            selectedRigibody = hitInfo.rigidbody;
            if (selectedRigibody != null)
            {
                editSelected = true;
                selectedRigibody.gameObject.SendMessage("Selected", button);
                selectedDistance = hitInfo.distance;
            }
        }
        else
        {
            //Debug.Log("No hit");
            editSelected = false;
        }
    }


    private void UnClickHandler(int button)
    {
        editSelected = false;
        if (selectedRigibody != null)
        {
            selectedRigibody.gameObject.SendMessage("UnSelected", button);
            selectedRigibody = null;
        }
    }

}
