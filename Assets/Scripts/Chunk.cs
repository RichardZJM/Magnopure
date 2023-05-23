using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

/// <summary>
/// Chunks are squares that cover the entire render area and generate 
/// interactable game objects
/// </summary>
public class Chunk : MonoBehaviour
{
    // Side length of the chunk
    private float _size;
    private bool _isInitialized = false;
    // Magnets generated by the chunk
    public List<GameObject> Magnets { get; private set; }

    [SerializeField] private GameObject _magnetPrefab;

    private int _minNumMagnets = 0;
    private int _maxNumMagnets = 3;

    public void Initialize(float chunkSize)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _size = chunkSize;

        Magnets = new List<GameObject>();
    }

    public void Start()
    {
        // Set the scale of the chunk so the background size is updated
        transform.localScale = new Vector3(_size, _size, 0);

        // Decide how many magnets to spawn in this chunk
        int numMagnets = Random.Range(_minNumMagnets, _maxNumMagnets + 1);
        for (int i = 0; i < numMagnets; ++i)
        {
            // position of the magnet relative to the center of the chunk
            var magnetRelativePos = new Vector3(
                Random.Range(-_size / 2, _size / 2),
                Random.Range(-_size / 2, _size / 2),
                0
            );
            Magnets.Add(
                Instantiate(
                    _magnetPrefab, 
                    transform.position + magnetRelativePos, 
                    Quaternion.identity
                )
            );
        }
        
    }

    public void OnDestroy()
    {
        foreach (var magnet in Magnets)
        {
            Destroy(magnet);
        }
    }
}
