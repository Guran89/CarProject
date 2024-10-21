using System;
using UnityEngine;

public class CarSound : MonoBehaviour
{
    [Header("Speed Variables")]
    [SerializeField] private float _minSpeed;
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _currentSpeed;

    private Rigidbody _rb;
    private AudioSource _carAudio;
    private CarController _carController;
    private bool _isHandbraking;
    [SerializeField] private AudioSource _startAudio;
    [SerializeField] private AudioSource _handbrakeAudio;

    [Header("PitchVariables")]
    [SerializeField] private float _minPitch;
    [SerializeField] private float _maxPitch;
    [SerializeField] private float _lerpSpeed = 5f;
    //private float _pitchFromCar;
    //private bool _isAccelerating;
    
    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _carAudio = GetComponent<AudioSource>();
        _carController = GetComponent<CarController>();
        _carController.OnForwardPressed += CarController_OnForwardPressed;
        StartEngine();
    }

    private void Update()
    {
        EngineSound();
        HandbrakeScreech();
    }

    private void StartEngine()
    {
        _startAudio.Play();
        _carAudio.PlayDelayed(_startAudio.clip.length);
    }

    private void EngineSound()
    {        
        // Calculate the current speed in km/h
        _currentSpeed = _rb.linearVelocity.magnitude * 3.6f;

        // lerp the pitch so that the pitch is higher when the car moves faster
        /*float t = Mathf.InverseLerp(_minSpeed, _maxSpeed, _currentSpeed);
        float targetPitch = Mathf.Lerp(_minPitch, _maxPitch, t);

        _carAudio.pitch = Mathf.Lerp(_carAudio.pitch, targetPitch, Time.deltaTime * _lerpSpeed);*/

        float _lerpSpeedTest = (_currentSpeed / _maxSpeed);

        _carAudio.pitch = Mathf.Lerp(_minPitch, _maxPitch, _lerpSpeedTest);
    }

    private void HandbrakeScreech()
    {
        bool isHandbrakingNow = Input.GetKey(KeyCode.Space);

        if (isHandbrakingNow && !_isHandbraking)
        {
            _handbrakeAudio.Play();
        }
        else if (!isHandbrakingNow && _isHandbraking)
        {
            _handbrakeAudio.Stop();
        }

        _isHandbraking = isHandbrakingNow;
    }

    private void CarController_OnForwardPressed(object sender, CarController.OnForwardPressedEventArgs e)
    {
        // Set _isAccelerating to true if the player presses foward arrow or W
        //_isAccelerating = e._isAccelerating;
    }
}
