using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class MagnetObject
{
    public Rigidbody2D rigidBody;
    public Light2D light;
}

public class MagnetController : MonoBehaviour
{
    //Constants
    private const float _slimeMaxCharge = 100;  
    [SerializeField] private float  _magneticScalingConstant = 1;
    private float _slimeActiveCharge = 0;
    
    //References
    private Rigidbody2D _slimeRigidBody;
    private Dictionary<int, MagnetObject> _magnetObjects;

    // Start is called before the first frame update
    void Awake()
    {
        //Get References
        _slimeRigidBody = GetComponent<Rigidbody2D>();
        // _slimeRigidBody.AddForce(new Vector2(300,0));
        _slimeRigidBody.AddTorque(40);

        _magnetObjects = new Dictionary<int, MagnetObject>();

        // Find manually placed magnets
        OnAddMagnets(new List<GameObject>(GameObject.FindGameObjectsWithTag("magnet")));
    }

    // Update is called once per frame
    void Update()
    {
        //Calculate forces and change colours
        Vector2 force = new Vector2(0,0);
        foreach (var magnetObject in _magnetObjects.Values){
            force += CalculatePairwiseForce(magnetObject.rigidBody);
            magnetObject.light.color = Color.Lerp(Color.red, Color.blue, (float)_slimeActiveCharge / 2 + 0.5f);
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

    public void OnAddMagnets(List<GameObject> magnets)
    {
        foreach (var magnet in magnets)
        {
            _magnetObjects.Add(
                magnet.GetInstanceID(), 
                new MagnetObject
                {
                    rigidBody = magnet.GetComponent<Rigidbody2D>(),
                    light = magnet.GetComponent<Light2D>()
                }
            );
        }
    }

    public void OnRemoveMagnets(List<GameObject> magnets)
    {
        foreach (var magnet in magnets)
        {
            _magnetObjects.Remove(magnet.GetInstanceID());
        }
    }
}
