using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGeneration 
{

    private const int _seed = 69420;

   public static float GetContinentalness(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, new Vector2(0.005f, 0.005f));
    }

    public static float GetPeaksValleys(Vector2 blockCoordinate){
        return PerlinComponent(blockCoordinate, new Vector2(0.05f, 0.05f));
    }

    public static bool EvaluateTerrain(Vector2 blockCoordinate){
        float continentalness = GetContinentalness(blockCoordinate);
        float peaksValley = GetPeaksValleys(blockCoordinate);

        return continentalness * peaksValley > 0.35;
    }
    private static float PerlinComponent(Vector2 blockCoordinate, Vector2 perlinScaling){
        Vector2 perlinCoordinate = blockCoordinate * perlinScaling;
        float perlinValue = Mathf.PerlinNoise(perlinCoordinate.x + _seed, perlinCoordinate.y + _seed);
        // Debug.Log(perlinValue);
        return perlinValue;
    }

    // private static float EvaluateBiome(Vector2 blockCoordinate){

    // }
}
