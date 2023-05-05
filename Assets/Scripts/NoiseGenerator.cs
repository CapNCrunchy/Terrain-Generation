using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Static class that generates a 2D map of perlin noise based on a scale factor
public static class NoiseGenerator
{
    public static float[,] GenerateNoise(int width, int height, float scale)
    {
        float[,] noiseMap = new float[width, height];

        //in case scale is 0, prevents a divide by 0 error
        if(scale <= 0)
        {
            scale = 0.0001f;
        }

        for(int x = 0; x < width; x++)
        {
            for(int y=0; y < height; y++)
            {
                //Applying the scale factor to the x and y values for the perlin noise function
                float tempX = x / scale;
                float tempY = y / scale;

                //Generating the perlin noise value
                float perlinNoiseValue = Mathf.PerlinNoise(tempX, tempY);
                noiseMap[x, y] = perlinNoiseValue;
            }
        }
        return noiseMap;
    }
}
