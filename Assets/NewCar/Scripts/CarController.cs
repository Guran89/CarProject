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

    [SerializeField] private float _acceleration = 500f;
    [SerializeField] private float _brakingForce = 300f;
    [SerializeField] private float _maxTurnAngle = 30f;
    [SerializeField] private float _handbrakeTorque = 2000f;
    [SerializeField] private float _drag = 0.98f;
    [SerializeField] private float _traction = 1;

    private float _currentAcceleration = 0f;
    private float _currentBrakeForce = 0f;
    private float _currentTurnAngle = 0f;
    private bool _isAccelerating;
    private bool _isHandbrakeActive;

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
    }

    private void FixedUpdate()
    {
        ReadInput();
        ApplyAccelerationAndBraking();
        ApplySteering();
        ApplyHandbrake();
        UpdateWheelsPosition();
        SendSignal();
    }

    private void ReadInput()
    {
        // Read player input
        SteerInput = _driveAction.ReadValue<Vector2>();
        float brakeInput = _brakeAction.ReadValue<float>();

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
        // Apply aceleration
        foreach (WheelCollider wheel in _wheels)
        {
            // Apply acceleration only to front wheels
            wheel.motorTorque = wheel.transform.localPosition.z > 0f ? _currentAcceleration : wheel.transform.localPosition.z;

            // Apply braking to all wheels
            wheel.brakeTorque = _currentBrakeForce;
        }
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
        // Apply steering only to front wheels
        foreach (WheelCollider wheel in _wheels)
        {
            wheel.steerAngle = wheel.transform.localPosition.z > 0 ? _currentTurnAngle : wheel.steerAngle;
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

    private void SendSignal()
    {
        _isAccelerating = SteerInput.y > 0f;

        OnForwardPressed?.Invoke(this, new OnForwardPressedEventArgs { _isAccelerating = _isAccelerating });
    }
}
