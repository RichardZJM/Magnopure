using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabReference : MonoBehaviour
{
    [SerializeField] private string _prefabPath;

    public string GetPrefabPath()
    {
        return _prefabPath;
    }
}
