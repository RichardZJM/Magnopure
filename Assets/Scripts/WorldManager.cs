using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;


public class WorldManager : MonoBehaviour
{
    [SerializeField] private float _chunkSize;          //Width of the chunk in units (Unity units)
    [SerializeField] private float _renderDistance;     // Distance from the center of the render field to the outside bounds
    [SerializeField] private GameObject _chunkPrefab;

    [SerializeField] private Rigidbody2D _playerRigidBody;

    private Vector2Int _previousRelativePlayerChunkIndex = Vector2Int.zero;
    private Vector2Int _absolutePlayerChunkIndex = Vector2Int.zero;



    private LinkedList<LinkedList<GameObject>> _loadedChunks = new LinkedList<LinkedList<GameObject>>();
    private List<GameObject> _loadedEntities = new List<GameObject>();
    // unloaded entities associated with each chunk
    private Dictionary<Vector2Int, List<Storable>> _unloadedEntities = new Dictionary<Vector2Int, List<Storable>>();
    // Start is called before the first frame update
    void Start()
    {
        for (int i = -_renderDistance; i < _renderDistance; i++) {

            List<GameObject> row = new List<GameObject>();

            for (int j = -_renderDistance; j < _renderDistance; j++) {
                Vector2 centerPosition = new Vector2(i * _chunkSize + _chunkSize/2, j * _chunkSize + _chunkSize/2);
                var chunkObject = Instantiate(_chunkPrefab, centerPosition, Quaternion.identity);
                chunkObject.Initialize(_chunkSize, new Vector2Int(x, y) + _absolutePlayerChunkIndex);
                row.AddLast(chunkObject);
            }

            loadChunks.AddLast(row);
        }
    }

    // Update is called once per frame
    void Update()
    {
        var relativePlayerChunkIndex= GetChunkIndex(_playerRigidBody.position);

        if(relativePlayerChunkIndex == _previousRelativePlayerChunkIndex) return;

        var chunkOffset = relativePlayerChunkIndex - _previousRelativePlayerChunkIndex;
        _absolutePlayerChunkIndex += chunkOffset; 

        var chunksToUnload = new List<(Vector2Int, GameObject)>();

        // column shift
        if (chunkOffset.x > 0) {
            // shift left
            
            for (var row = _loadedChunks.First; row != null; row = row.Next) {

            }

            for (int i = -_renderDistance; i < _renderDistance; i++) {
                var relativeChunkIndex = new Vector2Int(i, -renderDistance);
                chunksToUnload.Add((_loadedChunks[i + _renderDistance].RemoveFirst()));
                _loadedChunksInitializeChunk(relativeChunkIndex);
            }
        } else {
            // shift right

        }

        // row shift
        if (chunkOffset.y > 0) {
            

        } else {

        }

        foreach ((var relativeChunkIndex, var chunk) in chunksToUnload) {
            var chunkScript = chunk.GetComponent<Chunk>();
            var absoluteChunkIndex = chunkScript.absoluteChunkIndex;
            var entitiesInChunk = GetEntitiesInChunk(relativeChunkIndex);
            
            foreach (var entity in entitiesInChunk) {
                
            }
        }
        
        _previousRelativePlayerChunkIndex = playerRelativeChunkIndex;
    }

    private Vector2Int GetRelativeChunkIndex(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt((position.x + _chunkSize / 2 - _chunkOrigin.x) / _chunkSize),
            Mathf.FloorToInt((position.y + _chunkSize / 2 - _chunkOrigin.y) / _chunkSize)
        );
    }
    
    private GameObject InitializeChunk (Vector2Int chunkRelativeIndex){
        Vector2Int absoluteChunkIndex = _absolutePlayerChunkIndex + chunkRelativeIndex;

        Vector2 centerPosition = new Vector2(chunkRelativeIndex* _chunkSize + _chunkSize/2, chunkRelativeIndex * _chunkSize + _chunkSize/2);
        var chunkObject = Instantiate(_chunkPrefab, centerPosition, Quaternion.identity);
        chunkObject.InitializeTerrain(_chunkSize,absoluteChunkIndex);
        
        if(_unloadedEntities.ContainsKey(absoluteChunkIndex)){
            var newEntities = _unloadedEntities[absoluteChunkIndex];
            foreach (var entity in newEntities) entity.Load(chunkRelativeIndex * _chunkSize);
            _unloadedEntities.Remove(absoluteChunkIndex);
            return chunkObject;
        }
        
        
        chunkObject.InitializeNewEntities(_chunkSize, absoluteChunkIndex);
        
        return chunkObject;
    }

    private List<GameObjects> GetEntitiesInChunk(Vector2Int relativeChunkIndex) {
        List<GameObject> containedEntities = new List<GameObject>();
        Vector2 lowerBound = new Vector2(relativeChunkIndex.x * _chunkSize, relativeChunkIndex.y * _chunkSize);
        Vector2 upperBound = new Vector2(relativeChunkIndex.x * (_chunkSize + 1), relativeChunkIndex.y * (_chunkSize + 1));
        
        foreach (var entity in _loadedEntities) {
            Vector2 position = entity.transform.positon;
            if (position.x < lowerBound.x || position.x >upperBound.x || position.y < lowerBound.y || position.y >upperBound.y) continue;
            containedEntities.Add(entity);
        }
        return containedEntities;
    }

    
}
