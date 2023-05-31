using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using System;
using Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


public class WorldManager : MonoBehaviour
{
    [SerializeField] private float _chunkSize;          //Width of the chunk in units (Unity units)
    [SerializeField] private int _renderDistance;     // Distance from the center of the render field to the outside bounds in chunks
    [SerializeField] private GameObject _chunkPrefab;
    [SerializeField] private Rigidbody2D _playerRigidBody;
    [SerializeField] private Tilemap _tilemap;

    public static event Action<List<GameObject>> OnRemoveMagnets;

    private int _loadedChunkGridSize;
    private Vector2Int _relativePlayerChunkIndex = Vector2Int.zero;
    private Vector2Int _previousRelativePlayerChunkIndex = Vector2Int.zero;
    private Vector2Int _absolutePlayerChunkIndex = Vector2Int.zero;

    private LinkedList<LinkedList<GameObject>> _loadedChunksGrid = new LinkedList<LinkedList<GameObject>>();
    private List<GameObject> _loadedEntities = new List<GameObject>();
    // unloaded entities associated with each chunk
    private Dictionary<Vector2Int, List<Storable>> _visitedEntities = new Dictionary<Vector2Int, List<Storable>>();
    // Start is called before the first frame update
    void Start()
    {
        _loadedChunkGridSize = _renderDistance * 2 + 1;
        _relativePlayerChunkIndex = GetRelativeChunkIndex(_playerRigidBody.position);
        _previousRelativePlayerChunkIndex = _relativePlayerChunkIndex;

        for (int i = 0; i < _loadedChunkGridSize; i++) {
            LinkedList<GameObject> row = new LinkedList<GameObject>();

            for (int j = 0; j < _loadedChunkGridSize; j++) {
                var chunkObject = InitializeChunk(GetRelativeChunkIndex(i, j));
                row.AddLast(chunkObject);
            }

            _loadedChunksGrid.AddLast(row);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // identifies the index of the chunk containing the player
        // the UpdateLoadedChunks function ensures that the player chunk is always at the center of
        // the 2D array of loaded chunks
        _relativePlayerChunkIndex = GetRelativeChunkIndex(_playerRigidBody.position);

        if (_relativePlayerChunkIndex == _previousRelativePlayerChunkIndex) return;

        Vector2Int playerMoveDirection = _relativePlayerChunkIndex - _previousRelativePlayerChunkIndex;
        _absolutePlayerChunkIndex += playerMoveDirection;

        //Debug.Log($"New Chunks Move {playerMoveDirection}");

        UpdateLoadedChunks(playerMoveDirection);

        _previousRelativePlayerChunkIndex = _relativePlayerChunkIndex;

        // Teleportation script tp move everything back towards the center offset.

        if (Math.Abs(_relativePlayerChunkIndex.x) > _renderDistance)
        {

            Vector2 shiftDelta = new Vector2(
                // Jack Guo: from my testing, the expression below is equivalent to
                // -Math.Clamp(_relativePlayerChunkIndex.x, -1, 1) * _chunkSize * (_renderDistance + 1f)
                // and both expressions seem to be working correctly. Until we run into any issues, I
                // think we should stick to the simpler one.
                -_relativePlayerChunkIndex.x * _chunkSize,
                0
            );
            TeleportWorld(shiftDelta);
            _previousRelativePlayerChunkIndex.x = 0;
        }

        if (Math.Abs(_relativePlayerChunkIndex.y) > _renderDistance)
        {
            Vector2 shiftDelta = new Vector2(
                0,
                -_relativePlayerChunkIndex.y * _chunkSize
            );
            TeleportWorld(shiftDelta);
            _previousRelativePlayerChunkIndex.y = 0;
        }
    }

    private void UpdateLoadedChunks( Vector2Int playerMoveDirection)
    {
        // new row must be added first in the event that the player moves diagonally to avoid
        // misaligned chunks in the column
        if (playerMoveDirection.y != 0)
        {
            LinkedList<GameObject> rowToRemove;
            LinkedList<GameObject> rowToAdd = new LinkedList<GameObject>();
            if (playerMoveDirection.y < 0)
            {
                // chunks shift up, player moves down
                rowToRemove = _loadedChunksGrid.First.Value;
                _loadedChunksGrid.RemoveFirst();
                _loadedChunksGrid.AddLast(rowToAdd);
            }
            else
            {
                // chunk shift down, player moves up
                rowToRemove = _loadedChunksGrid.Last.Value;
                _loadedChunksGrid.RemoveLast();
                _loadedChunksGrid.AddFirst(rowToAdd);
            }
            int j = 0;
            for (var chunk = rowToRemove.First; chunk != null; chunk = chunk.Next)
            {
                var topRelativeChunkIndex = GetRelativeChunkIndex(0, j);
                var bottomRelativeChunkIndex = GetRelativeChunkIndex(_loadedChunkGridSize - 1, j);
                if (playerMoveDirection.y < 0)
                {
                    UnloadChunk(topRelativeChunkIndex - playerMoveDirection, chunk.Value);
                    rowToAdd.AddLast(InitializeChunk(bottomRelativeChunkIndex));
                }
                else
                {
                    UnloadChunk(bottomRelativeChunkIndex - playerMoveDirection, chunk.Value);
                    rowToAdd.AddLast(InitializeChunk(topRelativeChunkIndex));
                }
                j++;
            }
        }

        // column shift
        if (playerMoveDirection.x != 0)
        {
            int i = 0;
            for (var row = _loadedChunksGrid.First; row != null; row = row.Next)
            {
                // if player is moving diagonally, then we don't add a chunk to the new row otherwise
                // it would create a duplicate chunk
                if ((playerMoveDirection.y > 0 && i == 0) || (playerMoveDirection.y < 0 && i == _loadedChunkGridSize - 1))
                {
                    i++;
                    continue;
                }

                var leftRelativeChunkIndex = GetRelativeChunkIndex(i, 0);
                var rightRelativeChunkIndex = GetRelativeChunkIndex(i, _loadedChunkGridSize - 1);
                if (playerMoveDirection.x > 0)
                {
                    // chunks shift left, player moves right
                    // we need to know the previous index of the chunk to remove before the player moved
                    // so we subtract the player move direction
                    UnloadChunk(leftRelativeChunkIndex - playerMoveDirection, row.Value.First.Value);
                    row.Value.RemoveFirst();
                    row.Value.AddLast(InitializeChunk(rightRelativeChunkIndex));
                }
                else
                {
                    // chunks shift right, player moves left
                    UnloadChunk(rightRelativeChunkIndex - playerMoveDirection, row.Value.Last.Value);
                    row.Value.RemoveLast();
                    row.Value.AddFirst(InitializeChunk(leftRelativeChunkIndex));
                }
                i++;
            }
        }
    }

    private void TeleportWorld(Vector2 shiftDelta) {
        //Debug.Log($"Teleporting by {shiftDelta}");
        GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var gameObject in allGameObjects)
        {
            gameObject.transform.position += (Vector3)shiftDelta;
        }
        
        int numVcams = CinemachineCore.Instance.VirtualCameraCount;
        for (int i = 0; i < numVcams; ++i)
        {
            CinemachineCore.Instance.GetVirtualCamera(i)
                .OnTargetObjectWarped(_playerRigidBody.gameObject.transform, shiftDelta);
        }
    }

    private Vector2Int GetRelativeChunkIndex(int chunkRowInGrid, int chunkColumnInGrid)
    {
        // relative chunk index is calculated as the chunk's offset from the center chunk
        // (containing player) summed with the relative chunk index of the center chunk
        return GetChunkOffsetFromPlayer(chunkRowInGrid, chunkColumnInGrid) + _relativePlayerChunkIndex;
    }

    private Vector2Int GetChunkOffsetFromPlayer(int chunkRowInGrid, int chunkColumnInGrid)
    {
        // it's really easy to screw this math up, so better to encapsulate it in a function
        return new Vector2Int(chunkColumnInGrid - _renderDistance, -chunkRowInGrid + _renderDistance);
    }

    private Vector2Int GetAbsoluteChunkIndex(Vector2Int relativeChunkIndex)
    {
        return relativeChunkIndex - _relativePlayerChunkIndex + _absolutePlayerChunkIndex;
    }

    private Vector2Int GetRelativeChunkIndex(Vector2 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt((position.x + _chunkSize / 2) / _chunkSize),
            Mathf.FloorToInt((position.y + _chunkSize / 2) / _chunkSize)
        );
    }

    // private int GetPlayerBiome(){
    //     Vector2 playerAbsolutePosition = _absolutePlayerChunkIndex * _chunkSize + _playerRigidBody,position;
        
    // }

    private void UnloadChunk (Vector2Int relativeChunkIndex, GameObject chunk) {
        var chunkScript = chunk.GetComponent<Chunk>();
        var absoluteChunkIndex = chunkScript.AbsoluteChunkIndex;
        var entitiesInChunk = GetEntitiesInChunk(relativeChunkIndex);
        
        OnRemoveMagnets.Invoke(entitiesInChunk);
        var entityStorables = new List<Storable>();
        foreach (var entity in entitiesInChunk) {
            _loadedEntities.Remove(entity);
            Destroy(entity);
        }
        chunkScript.KillTerrain(_chunkSize, absoluteChunkIndex);
        Destroy(chunk);
    }
    
    private GameObject InitializeChunk (Vector2Int relativeChunkIndex) {
        Vector2Int absoluteChunkIndex = GetAbsoluteChunkIndex(relativeChunkIndex);
        Vector2 centerPosition = new Vector2(relativeChunkIndex.x * _chunkSize, relativeChunkIndex.y * _chunkSize);
        var chunkObject = Instantiate(_chunkPrefab, centerPosition, Quaternion.identity);
        var chunkScript = chunkObject.GetComponent<Chunk>();
        chunkScript.InitializeTerrain(_chunkSize,absoluteChunkIndex, _tilemap); // Uses seeds + location to determine terrain. Will need to implement world generation algorithm
        
        if(_visitedEntities.ContainsKey(absoluteChunkIndex)) {
            var previousEntitiesToLoad = _visitedEntities[absoluteChunkIndex];
            foreach (var entity in previousEntitiesToLoad) entity.Load(new Vector2(relativeChunkIndex.x * _chunkSize, relativeChunkIndex.y * _chunkSize));
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
        Vector2 lowerBound = new Vector2((relativeChunkIndex.x - 0.5f) * _chunkSize , (relativeChunkIndex.y-0.5f) * _chunkSize);
        Vector2 upperBound = new Vector2((relativeChunkIndex.x + 0.5f) * _chunkSize , (relativeChunkIndex.y + 0.5f) * _chunkSize);
        // Debug.Log(upperBound-lowerBound);
        foreach (var entity in _loadedEntities) {
            Vector2 position = entity.transform.position;
            if (position.x < lowerBound.x || position.x > upperBound.x || position.y < lowerBound.y || position.y > upperBound.y) continue;
            containedEntities.Add(entity);
        }
        return containedEntities;
    }
}

