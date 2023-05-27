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
        Chunk.OnAddMagnets += this.OnAddMagnets;
        WorldManager.OnRemoveMagnets += this.OnRemoveMagnets;
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

    private Vector2 getBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (t > 1f)
            return new Vector2(1, 0);
        float cx = 3 * (p1.x - p0.x);
        float cy = 3 * (p1.y - p0.y);
        float bx = 3 * (p2.x - p1.x) - cx;
        float by = 3 * (p2.y - p1.y) - cy;
        float ax = p3.x - p0.x - cx - bx;
        float ay = p3.y - p0.y - cy - by;
        float cube = t * t * t;
        float square = t * t;
        float resX = (ax * cube) + (bx * square) + (cx * t) + p0.x;
        float resY = (ay * cube) + (by * square) + (cy * t) + p0.y;
        return new Vector2(resX, resY);
    }

    private Vector2 CalculatePairwiseForce(Rigidbody2D magnetRigidBody)
    {
        Vector2 relativePosition = magnetRigidBody.position - _slimeRigidBody.position;
        if (relativePosition.magnitude > 10f)
            return new Vector2(0, 0);
        else
        {
            Vector2 force = _magneticScalingConstant * _slimeActiveCharge * _slimeMaxCharge * relativePosition.normalized;
            force *= getBezierPoint(relativePosition.magnitude / 10f, new Vector2(0f, 1f), new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0f), new Vector2(1f, 0f)).y;
            return force;
        }
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
