using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class Lights : MonoBehaviour
{
    [SerializeField] private Light[] _frontLights;
    [SerializeField] private Light[] _backLights;

    private void Start()
    {
        Light[] _allLights = GetComponentsInChildren<Light>();
        _frontLights = Array.FindAll(_allLights, t => t.name.StartsWith("Light_"));
        _backLights = Array.FindAll(_allLights, t => t.name.StartsWith("BackLight_"));
    }

    private void Update()
    {        
        ToggleLights();
        ToggleBrakeLights();
    }

    private void ToggleLights()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            foreach (Light light in _frontLights)
            {
                light.enabled = !light.enabled;
            }
        }
    }

    private void ToggleBrakeLights()
    {
        bool _isBraking = Input.GetKey(KeyCode.LeftShift);

        foreach (Light light in _backLights)
        {
            light.enabled = _isBraking;
        }
    }
    

}
