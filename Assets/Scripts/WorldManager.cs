using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using System;
using Cinemachine;
using UnityEngine.SceneManagement;


public class WorldManager : MonoBehaviour
{
    [SerializeField] private float _chunkSize;          //Width of the chunk in units (Unity units)
    [SerializeField] private int _renderDistance;     // Distance from the center of the render field to the outside bounds in chunks
    [SerializeField] private GameObject _chunkPrefab;
    [SerializeField] private Rigidbody2D _playerRigidBody;

    public static event Action<List<GameObject>> OnRemoveMagnets;

    private Vector2Int _previousRelativePlayerChunkIndex = Vector2Int.zero;
    private Vector2Int _absolutePlayerChunkIndex = Vector2Int.zero;

    private LinkedList<LinkedList<GameObject>> _loadedChunks = new LinkedList<LinkedList<GameObject>>();
    private List<GameObject> _loadedEntities = new List<GameObject>();
    // unloaded entities associated with each chunk
    private Dictionary<Vector2Int, List<Storable>> _visitedEntities = new Dictionary<Vector2Int, List<Storable>>();
    // Start is called before the first frame update
    void Start()
    {
        for (int i = -_renderDistance; i <= _renderDistance; i++) {
            LinkedList<GameObject> row = new LinkedList<GameObject>();

            for (int j = -_renderDistance; j <= _renderDistance; j++) {
                var chunkObject = InitializeChunk(new Vector2Int(j, i));
                row.AddLast(chunkObject);
            }

            _loadedChunks.AddFirst(row);
        }

        _previousRelativePlayerChunkIndex = GetRelativeChunkIndex(_playerRigidBody.position);
    }

    // Update is called once per frame
    void Update()
    {
        var relativePlayerChunkIndex = GetRelativeChunkIndex(_playerRigidBody.position);

        if (relativePlayerChunkIndex == _previousRelativePlayerChunkIndex) return;

        var chunkOffset = relativePlayerChunkIndex - _previousRelativePlayerChunkIndex;
        _absolutePlayerChunkIndex += chunkOffset;

        // column shift
        if (chunkOffset.x != 0) {
            int i = _renderDistance;
            for (var row = _loadedChunks.First; row != null; row = row.Next) {
                var leftRelativeChunkIndex = new Vector2Int(-_renderDistance, i);
                var rightRelativeChunkIndex = new Vector2Int(_renderDistance, i);
                if (chunkOffset.x > 0) {
                    // chunks shift left, player moves right
                    UnloadChunk(leftRelativeChunkIndex, row.Value.First.Value);
                    row.Value.RemoveFirst();
                    row.Value.AddLast(InitializeChunk(rightRelativeChunkIndex));
                } else {
                    // chunks shift right, player moves left
                    UnloadChunk(rightRelativeChunkIndex, row.Value.Last.Value);
                    row.Value.RemoveLast();
                    row.Value.AddFirst(InitializeChunk(leftRelativeChunkIndex));
                }
                i--;
            }
        }

        if (chunkOffset.y != 0) {
            LinkedList<GameObject> rowToRemove;
            LinkedList<GameObject> rowToAdd = new LinkedList<GameObject>();
            if (chunkOffset.y < 0) {
                rowToRemove = _loadedChunks.First.Value;
                _loadedChunks.RemoveFirst();
                _loadedChunks.AddLast(rowToAdd);
            } else {
                rowToRemove = _loadedChunks.Last.Value;
                _loadedChunks.RemoveLast();
                _loadedChunks.AddFirst(rowToAdd);
            }
            int j = -_renderDistance;
            for (var chunk = rowToRemove.First; chunk != null; chunk = chunk.Next) {
                var topRelativeChunkIndex = new Vector2Int(j, _renderDistance);
                var bottomRelativeChunkIndex = new Vector2Int(j, -_renderDistance);
                if (chunkOffset.y < 0) {
                    // shift down
                    UnloadChunk(topRelativeChunkIndex, chunk.Value);
                    rowToAdd.AddLast(InitializeChunk(bottomRelativeChunkIndex));
                } else {
                    // shift up
                    UnloadChunk(bottomRelativeChunkIndex, chunk.Value);
                    rowToAdd.AddLast(InitializeChunk(topRelativeChunkIndex));
                }
                j++;
            }
        }
        
        _previousRelativePlayerChunkIndex = relativePlayerChunkIndex;

        // Teleportation script tp move everything back towards the center offset.
        
        if (Math.Abs(relativePlayerChunkIndex.x) > _renderDistance) {
            Vector2 shiftDelta = new Vector2(
                - Math.Clamp(relativePlayerChunkIndex.x, -1, 1) * _chunkSize * _renderDistance, 
                0
            );
            TeleportWorld(shiftDelta);
        }
        
        if (Math.Abs(relativePlayerChunkIndex.y) > _renderDistance) {
            Vector2 shiftDelta = new Vector2(
                0, 
                - Math.Clamp(relativePlayerChunkIndex.y, -1, 1)  *_chunkSize * _renderDistance
            );
            TeleportWorld(shiftDelta);
        }
    }

    private void TeleportWorld(Vector2 shiftDelta) {
        GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var gameObject in allGameObjects)
        {
            gameObject.transform.position += (Vector3)shiftDelta;
        }
        _previousRelativePlayerChunkIndex = Vector2Int.zero;
        int numVcams = CinemachineCore.Instance.VirtualCameraCount;
        
        for (int i = 0; i < numVcams; ++i)
        {
            CinemachineCore.Instance.GetVirtualCamera(i)
                .OnTargetObjectWarped(_playerRigidBody.gameObject.transform, shiftDelta);
        }
    }


    private Vector2Int GetRelativeChunkIndex(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / _chunkSize),
            Mathf.FloorToInt(position.y / _chunkSize)
        );
    }

    private void UnloadChunk (Vector2Int chunkRelativeIndex, GameObject chunk) {
        var chunkScript = chunk.GetComponent<Chunk>();
        var absoluteChunkIndex = chunkScript.AbsoluteChunkIndex;
        var entitiesInChunk = GetEntitiesInChunk(chunkRelativeIndex + _previousRelativePlayerChunkIndex);
        Debug.Log(entitiesInChunk.Count);
        
        OnRemoveMagnets.Invoke(entitiesInChunk);
        var entityStorables = new List<Storable>();
        foreach (var entity in entitiesInChunk) {
            Debug.Log(entity.transform.position);
            // entityStorables.Add(new Storable());
            Destroy(entity);
        }

        Destroy(chunk);

        // _visitedEntities.Add(absoluteChunkIndex, entityStorables);
    }
    
    private GameObject InitializeChunk (Vector2Int chunkRelativeIndex) {
        Vector2Int absoluteChunkIndex = _absolutePlayerChunkIndex + chunkRelativeIndex;
        var relativePlayerChunkIndex= GetRelativeChunkIndex(_playerRigidBody.position);
        Vector2 centerPosition = new Vector2((chunkRelativeIndex.x + relativePlayerChunkIndex.x) * _chunkSize, (chunkRelativeIndex.y + relativePlayerChunkIndex.y) * _chunkSize);
        var chunkObject = Instantiate(_chunkPrefab, centerPosition, Quaternion.identity);
        var chunkScript = chunkObject.GetComponent<Chunk>();
        chunkScript.InitializeTerrain(_chunkSize,absoluteChunkIndex); // Uses seeds + location to determine terrain. Will need to implement world generation algorithm
        
        if(_visitedEntities.ContainsKey(absoluteChunkIndex)){
            var previousEntitiesToLoad = _visitedEntities[absoluteChunkIndex];
            foreach (var entity in previousEntitiesToLoad) entity.Load(new Vector2(chunkRelativeIndex.x * _chunkSize, chunkRelativeIndex.y * _chunkSize));
            // _visitedEntites.Remove(absoluteChunkIndex); // This may be bug prone if things are not added to the unloadedEntities correctly, but theoretically should work.
            return chunkObject;
        } else {
            
        }
        
        var newEntities = chunkScript.InitializeNewEntities(_chunkSize, absoluteChunkIndex);
        _loadedEntities.AddRange(newEntities);

        return chunkObject;
    }

    private List<GameObject> GetEntitiesInChunk(Vector2Int relativeChunkIndex) {
        List<GameObject> containedEntities = new List<GameObject>();
        Vector2 lowerBound = new Vector2(relativeChunkIndex.x * _chunkSize, relativeChunkIndex.y * _chunkSize);
        Vector2 upperBound = new Vector2(relativeChunkIndex.x * (_chunkSize + 1), relativeChunkIndex.y * (_chunkSize + 1));
        
        foreach (var entity in _loadedEntities) {
            Vector2 position = entity.transform.position;
            if (position.x < lowerBound.x || position.x > upperBound.x || position.y < lowerBound.y || position.y > upperBound.y) continue;
            containedEntities.Add(entity);
        }
        return containedEntities;
    }
}
