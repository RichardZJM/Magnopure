using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ProceduralGeneration : MonoBehaviour {
    [SerializeField] private int _baseSeed = 69420;
    [SerializeField] private Rigidbody2D _playerRigidbody2D;

    private float _terrainThreshold = 0.35f;
    private Vector2 _continentalnessSeed = Vector2.zero;
    private Vector2 _continentalnessScale = new Vector2(0.01f, 0.01f);
    private Vector2 _peaksValleysSeed = Vector2.zero;
    private Vector2 _peaksValleysScale = new Vector2(0.05f, 0.05f);
    private Vector2 _temperatureSeed = Vector2.zero;
    private Vector2 _temperatureScale = new Vector2(0.0002f, 0.0002f);

    [SerializeField] private Texture2D biomeColorMap;
    private Color[]  biomeMap;
    private HashSet<float> biomes = new HashSet<float>();

    public void Start(){
        float maxSeedSize = 10000f;
        System.Random rnd = new System.Random(_baseSeed);
        _continentalnessSeed = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()) * maxSeedSize;
        _peaksValleysSeed = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()) * maxSeedSize;
        _temperatureSeed = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()) * maxSeedSize;

        biomeMap = biomeColorMap.GetPixels();
        foreach (var square in biomeMap) biomes.Add(square.r);
        Debug.Log(biomes.Count);
    }

   private float GetContinentalness(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, _continentalnessScale, _continentalnessSeed);
    }

    private float GetPeaksValleys(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, _peaksValleysScale, _peaksValleysSeed);
    }

    private float GetTemperature(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, _temperatureScale, _temperatureSeed);
    }

    public bool EvaluateTerrain(Vector2 blockCoordinate){
        float continentalness = GetContinentalness(blockCoordinate);
        float peaksValley = GetPeaksValleys(blockCoordinate);

        // will generate block of terrain if above threshold
        return continentalness * peaksValley > _terrainThreshold;
    }

    public Color GetBiome(Vector2 blockCoordinate){
        float continentalness = GetContinentalness(blockCoordinate);
        float temperature = GetTemperature(blockCoordinate);
        // The biome map is an RGB image of size 32 by 32, but the output of perlin
        // noise is only in the range 0 to 1. We scale the perlin noise by the biome
        // map size so that the entire biome map will be accessible.
        return biomeMap[
            GetBiomeMapIndex(
                new Vector2Int((int)(continentalness * biomeColorMap.width),
                (int)(temperature * biomeColorMap.width))
            )
        ];
    }

    private int GetBiomeMapIndex(Vector2Int coordinate){
        return biomeColorMap.width * Math.Clamp(coordinate.y, 0, biomeColorMap.width) + Math.Clamp(coordinate.x, 0, biomeColorMap.width) ;
    }

    private float PerlinComponent(Vector2 blockCoordinate, Vector2 perlinScaling, Vector2 seed){
        Vector2 perlinCoordinate = blockCoordinate * perlinScaling;
        float perlinValue = Mathf.PerlinNoise(perlinCoordinate.x + seed.x, perlinCoordinate.y + seed.y);
        return perlinValue;
    }
}
