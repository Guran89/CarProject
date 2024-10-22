using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    private Vector2 SteerInput;

    [SerializeField] InputAction _driveAction;
    [SerializeField] InputAction _brakeAction;
    [SerializeField] InputAction _handbrakeAction;
    private WheelCollider[] _wheels;
    private Transform[] _wheelTransforms;
    private TrailRenderer[] _trails;
    private Rigidbody rb;

    [SerializeField] private float _acceleration = 500f;
    [SerializeField] private float _brakingForce = 300f;
    [SerializeField] private float _maxTurnAngle = 30f;
    [SerializeField] private float _steeringSpeed = 5f;
    [SerializeField] private float _handbrakeTorque = 2000f;
    [SerializeField] private float _deceleration = 10f;

    // Drifting variables
    [SerializeField] private float _traction = 1f;
    [SerializeField] private float _tractionAngleThreshold = 10f; // Angle in degrees
    [SerializeField] private float _minDriftSpeed = 10f; // Minimum speed for drifting

    private float _currentSteerAngle = 0f;
    private float _currentAcceleration = 0f;
    private float _currentBrakeForce = 0f;
    private float _currentTurnAngle = 0f;
    private bool _isAccelerating;
    private bool _isHandbrakeActive;
    private bool _isAccelerationKeyHeld;

    public event EventHandler<OnForwardPressedEventArgs> OnForwardPressed;
    public class OnForwardPressedEventArgs : EventArgs
    {
        public bool _isAccelerating;
    }

    private void Start()
    {
        _driveAction.Enable();
        _brakeAction.Enable();
        _handbrakeAction.Enable();

        // Get references to wheel colliders and meshes
        _wheels = GetComponentsInChildren<WheelCollider>();
        Transform[] _allTransforms = GetComponentsInChildren<Transform>();

        // Find all transforms whose names start with "Wheel_"
        _wheelTransforms = Array.FindAll(_allTransforms, t => t.name.StartsWith("Wheel_"));

        // Get references to trails for skid marks
        _trails = GetComponentsInChildren<TrailRenderer>();

        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        ReadInput();
        ApplySteering();
        ApplyHandbrake();
        UpdateWheelsPosition();
        SendSignal();
        ApplyTractionControl();
        ApplyAccelerationAndBraking();
        Debug.DrawRay(transform.position, transform.forward * 10);

    }

    private void ReadInput()
    {
        // Read player input
        SteerInput = _driveAction.ReadValue<Vector2>();
        float brakeInput = _brakeAction.ReadValue<float>();

        // Check if acceleration key is held
        _isAccelerationKeyHeld = Mathf.Abs(SteerInput.y) > 0.1f;

        // Accelerate when up or down is pressed
        _currentAcceleration = _acceleration * SteerInput.y;

        // Apply braking when Space is pressed
        _currentBrakeForce = brakeInput > 0f ? _brakingForce : 0f;

        // Handle steering
        _currentTurnAngle = _maxTurnAngle * SteerInput.x;

        _isHandbrakeActive = _handbrakeAction.ReadValue<float>() > 0.5f;
    }

    private void ApplyAccelerationAndBraking()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        float currentSpeed = rb.linearVelocity.magnitude;

        if (_isAccelerationKeyHeld)
        {
            // Apply acceleration
            foreach (WheelCollider wheel in _wheels)
            {
                if (wheel.transform.localPosition.z > 0) // Front wheels
                {
                    wheel.motorTorque = _currentAcceleration;
                }
                else // Rear wheels
                {
                    wheel.motorTorque = 0f;
                }
            }
        }
        else if (currentSpeed > 0.1f) // Apply deceleration only if the car is moving
        {
            float decelerationForce = -_deceleration * Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward));

            foreach (WheelCollider wheel in _wheels)
            {
                wheel.motorTorque = decelerationForce;
            }
        }
        else
        {
            // If not accelerating and speed is very low, stop the car completely
            foreach (WheelCollider wheel in _wheels)
            {
                wheel.motorTorque = 0f;
            }
            rb.linearVelocity = Vector3.zero;
        }

        // Apply braking to all wheels
        foreach (WheelCollider wheel in _wheels)
        {
            wheel.brakeTorque = _currentBrakeForce;
        }

        // Draw the debug ray
        Debug.DrawRay(transform.position, rb.linearVelocity * 3, Color.blue);
        Vector3 inputDirection = new Vector3(SteerInput.x, 0, SteerInput.y).normalized;
        Debug.DrawRay(transform.position, inputDirection * 3, Color.green);
    }

    private void ApplyHandbrake()
    {
        if (_isHandbrakeActive)
        {
            // Apply handbrake torque to rear wheels
            foreach (WheelCollider wheel in _wheels)
            {
                if (wheel.transform.localPosition.z < 0) // Check if it's a rear wheel
                {
                    wheel.brakeTorque = _handbrakeTorque;
                    wheel.motorTorque = 0f; // Cut power to rear wheels when handbrake is active
                }
            }
        }
        foreach (TrailRenderer trail in _trails)
        {
            trail.emitting = _isHandbrakeActive;
        }
    }

    private void ApplySteering()
    {
        // Smoothly interpolate to the target steering angle
        _currentSteerAngle = Mathf.Lerp(_currentSteerAngle, _currentTurnAngle, Time.deltaTime * _steeringSpeed);

        // Apply steering only to front wheels
        foreach (WheelCollider wheel in _wheels)
        {
            if (wheel.transform.localPosition.z > 0) // Check if it's a front wheel
            {
                wheel.steerAngle = _currentSteerAngle;
            }
        }
    }

    private void UpdateWheelsPosition()
    {
        // Execute the WheelPosition method for each wheel
        for (int i = 0; i < _wheels.Length; i++)
        {
            WheelPosition(_wheels[i], _wheelTransforms[i]);
        }
    }

    private void WheelPosition(WheelCollider col, Transform trans)
    {
        col.GetWorldPose(out Vector3 _position, out Quaternion _rotation);

        trans.SetPositionAndRotation(_position, _rotation);
    }

    private void ApplyTractionControl()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        float angle = Vector3.Angle(transform.forward, rb.linearVelocity);
        float speedRatio = rb.linearVelocity.magnitude / _minDriftSpeed;
        float tractionFactor = Mathf.Lerp(1f, _traction, Mathf.Clamp01(speedRatio - 1));

        if (angle > _tractionAngleThreshold && rb.linearVelocity.magnitude > _minDriftSpeed)
        {
            // Drifting
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, forwardVelocity, tractionFactor * Time.fixedDeltaTime);
            rb.linearVelocity += (1 - tractionFactor) * Time.fixedDeltaTime * rightVelocity;
        }
        else
        {
            // Normal driving
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, forwardVelocity, _traction * Time.fixedDeltaTime);
        }

        // Debug visualization
        Debug.DrawRay(transform.position, transform.forward * 3, Color.white); // Car's forward direction
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 3, Color.blue); // Actual movement direction
    }

    private void SendSignal()
    {
        _isAccelerating = SteerInput.y > 0f;

        OnForwardPressed?.Invoke(this, new OnForwardPressedEventArgs { _isAccelerating = _isAccelerating });
    }
}
