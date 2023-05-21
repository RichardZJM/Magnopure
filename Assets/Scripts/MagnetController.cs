using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

public class MagnetController : MonoBehaviour
{
    //Constants
    private const float _slimeMaxCharge = 100;  
    [SerializeField] private float  _magneticScalingConstant = 1;
    
    //References
    private Rigidbody2D _slimeRigidBody;
    private GameObject[]  _magnetObjects;
    private List<Rigidbody2D> _magnetRigidBodies = new List<Rigidbody2D>(); 

    // Start is called before the first frame update
    void Awake()
    {
        //Get References
        _slimeRigidBody = GetComponent<Rigidbody2D>();
        _slimeRigidBody.AddForce(new Vector2(300,0));
        _slimeRigidBody.AddTorque(40);

        _magnetObjects = GameObject.FindGameObjectsWithTag("magnet");
        Debug.Log(_magnetObjects.Length);
        foreach (var magnetObject in _magnetObjects){
            _magnetRigidBodies.Add(magnetObject.GetComponent<Rigidbody2D>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Calculate Forces
        Vector2 force = new Vector2(0,0);
        foreach (var magnetRigidBody in _magnetRigidBodies){
            force += CalculatePairwiseForce(1.0f ,magnetRigidBody);
            Debug.Log(force);
        }

        _slimeRigidBody.AddForce(force);
        

    }

    private Vector2 CalculatePairwiseForce(float activeCharge, Rigidbody2D magnetRigidBody){
        Vector2 relativePosition = magnetRigidBody.position - _slimeRigidBody.position;
        Vector2 force = _magneticScalingConstant * activeCharge * _slimeMaxCharge / (float)Math.Pow(relativePosition.magnitude,2) * relativePosition.normalized;
        return force;
    }

}
