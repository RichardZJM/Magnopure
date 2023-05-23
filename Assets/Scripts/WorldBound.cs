using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// WorldBound limits the position of the player. If the player crosses
/// the world boundary, they, along with other game objects are teleported
/// back to origin (while preserving relative positions).
/// </summary>
public class WorldBound : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private UnityEvent<Vector3> _onMoveObjectsToOrigin;
    private float _boundMinX;
    private float _boundMinY;
    private float _boundMaxX;
    private float _boundMaxY;

    private void Awake()
    {
        _boundMinX = transform.position.x - transform.localScale.x / 2;
        _boundMinY = transform.position.y - transform.localScale.y / 2;
        _boundMaxX = transform.position.x + transform.localScale.x / 2;
        _boundMaxY = transform.position.y + transform.localScale.y / 2;
    }

    private void Update()
    {
        if (!IsInBounds(_player.transform.position))
        {
            // move player back to origin and move all other game objects to
            // be at the same position relative to the player before the move
            var shiftDelta = -(new Vector3(_player.transform.position.x, _player.transform.position.y, 0));
            GameObject[] allGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var gameObject in allGameObjects)
            {
                gameObject.transform.position += shiftDelta;
            }

            _onMoveObjectsToOrigin.Invoke(shiftDelta);
        }
    }

    private bool IsInBounds(Vector2 position)
    {
        return position.x > _boundMinX && 
            position.x < _boundMaxX && 
            position.y > _boundMinY && 
            position.y < _boundMaxY;
    }
}
