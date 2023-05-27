using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants : MonoBehaviour
{
    // The side length of a square chunk
    [SerializeField] public float chunkSize;
    // Defines a padding surrounding the camera view that will still render
    // chunks despite being outside of the viewport.
    [SerializeField] public float renderPadding;
}
