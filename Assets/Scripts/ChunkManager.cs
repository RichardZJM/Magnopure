using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ChunkManager handles creating and destroying chunks as the player
/// moves throughout the world.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    // The side length of a square chunk
    [SerializeField] private float _chunkSize;
    // Defines a padding surrounding the camera view that will still render
    // chunks despite being outside of the viewport.
    [SerializeField] private float _renderPadding;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GameObject _chunkPrefab;
    [SerializeField] private UnityEvent<List<GameObject>> _onAddMagnets;
    [SerializeField] private UnityEvent<List<GameObject>> _onRemoveMagnets;

    // The chunks currently present in the world
    // Chunks are identified by an index which is its position in a coordiate
    // space. A chunk with index (x, y) means it is x chunks away horizontally and
    // y chunks away vertically from the origin.
    private Dictionary<Vector2Int, GameObject> _activeChunks;
    // This is the origin around which chunk positions are calculated
    // It will change when player gets teleports and the chunks must teleport with
    // the player
    private Vector3 _chunkOrigin;
    private Vector2Int? _storedMinChunkIndex;

    private void Awake()
    {
        _activeChunks = new Dictionary<Vector2Int, GameObject>();
        _chunkOrigin = Vector3.zero;
        _storedMinChunkIndex = null;        
    }

    private void Update()
    {
        var cameraBounds = GetCameraBounds();

        // This identifies the chunks at the opposite corners of the rendering
        // area.
        var minChunkIndex = GetChunkIndex(
            new Vector2(cameraBounds.min.x - _renderPadding, cameraBounds.min.y - _renderPadding));
        var maxChunkIndex = GetChunkIndex(
            new Vector2(cameraBounds.max.x + _renderPadding, cameraBounds.max.y + _renderPadding));

        // The player has moved to a new chunk, in which case we must recalculated
        // chunks that need to be generated and removed
        if (_storedMinChunkIndex == null || _storedMinChunkIndex != minChunkIndex)
        {
            // Assume all chunks need to be removed, then go through chunks still in the 
            // rendering area and take them out of the set.
            var chunksToRemove = new HashSet<Vector2Int>(_activeChunks.Keys);
            // Loop through all chunks in the rendering area and check if they should be
            // generated
            for (var x = minChunkIndex.x; x < maxChunkIndex.x; ++x)
            {
                for (var y = minChunkIndex.y; y < maxChunkIndex.y; ++y)
                {
                    var currentChunkIndex = new Vector2Int(x, y);
                    // This chunk is still inside the rendering area so it does not need to be
                    // removed
                    chunksToRemove.Remove(currentChunkIndex);

                    if (!_activeChunks.ContainsKey(currentChunkIndex))
                    {
                        // Generate a new chunk
                        var centerPosition = new Vector3(
                            currentChunkIndex.x * _chunkSize + _chunkOrigin.x,
                            currentChunkIndex.y * _chunkSize + _chunkOrigin.y,
                            _chunkOrigin.z
                        );

                        var chunkObject = Instantiate(_chunkPrefab, centerPosition, Quaternion.identity);
                        var chunkHandler = chunkObject.GetComponent<Chunk>();
                        chunkHandler.Initialize(_chunkSize, _onAddMagnets, _onRemoveMagnets);

                        _activeChunks.Add(currentChunkIndex, chunkObject);
                    }
                }
            }

            // Chunks that are still in the 'to remove' list will be removed
            foreach (var chunk in chunksToRemove)
            {
                Destroy(_activeChunks[chunk]);
                _activeChunks.Remove(chunk);
            }

            _storedMinChunkIndex = minChunkIndex;
        }
    }

    /// <returns>
    /// The a bound for the viewport of the camera
    /// </returns>
    private Bounds GetCameraBounds()
    {
        float screenAspect = Screen.width / (float)Screen.height;
        float cameraHeight = _mainCamera.orthographicSize * 2;
        Bounds bounds = new Bounds(
            _mainCamera.transform.position,
            new Vector3(cameraHeight * screenAspect, cameraHeight, 0)
        );
        return bounds;
    }

    /// <summary>
    /// A chunk index identifies its position in a coordiate space. 
    /// A chunk with index (x, y) means it is x chunks away horizontally and
    /// y chunks away vertically from the origin.
    /// </summary>
    /// <param name="position">A position in the 2D world</param>
    /// <returns>The index identifying the chunk which covers the position</returns>
    private Vector2Int GetChunkIndex(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt((position.x + _chunkSize / 2 - _chunkOrigin.x) / _chunkSize),
            Mathf.FloorToInt((position.y + _chunkSize / 2 - _chunkOrigin.y) / _chunkSize)
        );
    }

    /// <summary>
    /// Updates the index identifying the chunks in the dictionary
    /// </summary>
    public void OnChunkPositionsChanged()
    {
        var newActiveChunks = new Dictionary<Vector2Int, GameObject>();
        
        foreach (var chunk in _activeChunks.Values)
        {
            newActiveChunks.Add(GetChunkIndex(chunk.transform.position), chunk);
        }

        if (newActiveChunks.Count > 0)
        {
            var someChunkIndex = newActiveChunks.First().Key;
            var someChunkPosition = newActiveChunks[someChunkIndex].transform.position;
            // If the chunk position did not change, we would get back the same origin
            // if we subtract the position of a chunk by the distance vector to the
            // origin chunk. But if chunk positions were changed, we get a new origin.
            _chunkOrigin = new Vector3(
                someChunkPosition.x - someChunkIndex.x * _chunkSize, 
                someChunkPosition.y - someChunkIndex.y * _chunkSize, 
                someChunkPosition.z
            );
        }

        _activeChunks = newActiveChunks;
    }
}
