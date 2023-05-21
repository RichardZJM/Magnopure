using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class MagnetController : MonoBehaviour
{
    //Constants
    private const float _slimeMaxCharge = 100;  
    [SerializeField] private float  _magneticScalingConstant = 1;
    private float _slimeActiveCharge = 0;
    
    //References
    private Rigidbody2D _slimeRigidBody;
    private GameObject[]  _magnetObjects;
    private List<Rigidbody2D> _magnetRigidBodies = new List<Rigidbody2D>(); 
    private List<Light2D> _magnetLights = new List<Light2D>(); 

    // Start is called before the first frame update
    void Awake()
    {
        //Get References
        _slimeRigidBody = GetComponent<Rigidbody2D>();
        // _slimeRigidBody.AddForce(new Vector2(300,0));
        _slimeRigidBody.AddTorque(40);

        _magnetObjects = GameObject.FindGameObjectsWithTag("magnet");
        Debug.Log(_magnetObjects.Length);
        foreach (var magnetObject in _magnetObjects){
            _magnetRigidBodies.Add(magnetObject.GetComponent<Rigidbody2D>());
            _magnetLights.Add(magnetObject.GetComponent<Light2D>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Calculate Forces
        Vector2 force = new Vector2(0,0);
        foreach (var magnetRigidBody in _magnetRigidBodies){
            force += CalculatePairwiseForce(magnetRigidBody);
            
        }
        //Change colours
        foreach (var magnetLight in _magnetLights){
            magnetLight.color = Color.Lerp(Color.red, Color.blue, (float)_slimeActiveCharge/2 +0.5f);
        }

        _slimeRigidBody.AddForce(force);
        

    }

    public void OnSliderChange(System.Single activeCharge){
        _slimeActiveCharge = - (float) activeCharge;
    }

    private Vector2 CalculatePairwiseForce( Rigidbody2D magnetRigidBody){
        Vector2 relativePosition = magnetRigidBody.position - _slimeRigidBody.position;
        Vector2 force = _magneticScalingConstant * _slimeActiveCharge * _slimeMaxCharge / (float)Math.Pow(relativePosition.magnitude,2) * relativePosition.normalized;
        return force;
    }

}
