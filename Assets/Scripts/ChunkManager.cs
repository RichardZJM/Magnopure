using System.Collections.Generic;
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

    private Vector2Int? _storedMinChunkIndex;

    private void Awake()
    {
        _activeChunks = new Dictionary<Vector2Int, GameObject>();
        _storedMinChunkIndex = null;
    }

    private void Update()
    {
        var cameraBounds = GetCameraBounds();

        // This identifies the chunks at the opposite corners of the rendering
        // area.
        var minChunkIndex = new Vector2Int(
            Mathf.FloorToInt((cameraBounds.min.x - _renderPadding + _chunkSize / 2) / _chunkSize),
            Mathf.FloorToInt((cameraBounds.min.y - _renderPadding + _chunkSize / 2) / _chunkSize)
        );
        var maxChunkIndex = new Vector2Int(
            (int)((cameraBounds.max.x + _renderPadding) / _chunkSize),
            (int)((cameraBounds.max.y + _renderPadding) / _chunkSize)
        );

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
                            currentChunkIndex.x * _chunkSize,
                            currentChunkIndex.y * _chunkSize
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
}
