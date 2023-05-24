using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using Cinemachine;

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
        Vector2 netSlimeForce = new Vector2(0,0);
        foreach (var magnetObject in _magnetObjects.Values){
            
            Vector2 force = CalculatePairwiseForce(magnetObject.rigidBody);
            //Use Newton's Law to apply reaction force to magnets (static magnets resist forces automatically)
            magnetObject.rigidBody.AddForce(-force);
            netSlimeForce += force;
            // Interpolate the glow of the magnets
            magnetObject.light.color = Color.Lerp(Color.red, Color.blue, (float)_slimeActiveCharge / 2 + 0.5f);
        }

        _slimeRigidBody.AddForce(netSlimeForce);
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

    /// <summary>
    /// Pervent jerking motion on camera following slime
    /// https://forum.unity.com/threads/reposition-target-and-camera-runtime-how-to-avoid-the-popping.514293/
    /// </summary>
    /// <param name="shiftDelta"></param>
    public void OnSlimeMoved(Vector3 shiftDelta)
    {
        int numVcams = CinemachineCore.Instance.VirtualCameraCount;
        for (int i = 0; i < numVcams; ++i)
        {

            CinemachineCore.Instance.GetVirtualCamera(i)
                .OnTargetObjectWarped(transform, shiftDelta);
        }
    }
}
