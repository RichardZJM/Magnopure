using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ProceduralGeneration : MonoBehaviour {
    [SerializeField] private int _seed = 69420;
    [SerializeField] private Rigidbody2D _playerRigidbody2D;

    [SerializeField] private Texture2D biomeColorMap;
    private Color[]  biomeMap;
    private HashSet<float> biomes = new HashSet<float>();

    public void Start(){
        biomeMap = biomeColorMap.GetPixels();
        foreach (var square in biomeMap) biomes.Add(square.r);
        Debug.Log(biomes.Count);
    }

   private float GetContinentalness(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, new Vector2(0.005f, 0.005f));
    }

    private float GetPeaksValleys(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, new Vector2(0.05f, 0.05f));
    }

    private float GetTemperature(Vector2 blockCoordinate){
        blockCoordinate.x += _seed;
        blockCoordinate.y += _seed*20;
        return PerlinComponent(blockCoordinate, new Vector2(0.0002f, 0.0002f));
    }

    public bool EvaluateTerrain(Vector2 blockCoordinate){
        float continentalness = GetContinentalness(blockCoordinate);
        float peaksValley = GetPeaksValleys(blockCoordinate);

        return continentalness * peaksValley > 0.35;
    }

    public Color GetBiome(Vector2 blockCoordinate){
        float continentalness = GetContinentalness(blockCoordinate);
        float temperature = GetTemperature(blockCoordinate);
        return biomeMap[GetBiomeMapIndex(new Vector2Int((int)(continentalness*32),(int)(temperature*32)))];
    }

    private int GetBiomeMapIndex(Vector2Int coordinate){
        return biomeColorMap.width*Math.Clamp(coordinate.y, 0, biomeColorMap.width) + Math.Clamp(coordinate.x, 0, biomeColorMap.width) ;
    }

    private float PerlinComponent(Vector2 blockCoordinate, Vector2 perlinScaling){
        Vector2 perlinCoordinate = blockCoordinate * perlinScaling;
        float perlinValue = Mathf.PerlinNoise(perlinCoordinate.x + _seed, perlinCoordinate.y + _seed);
        // Debug.Log(perlinValue);
        return perlinValue;
    }
}
