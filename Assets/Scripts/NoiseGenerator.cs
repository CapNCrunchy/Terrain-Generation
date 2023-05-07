using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

//Static class that generates a 2D map of perlin noise based on a scale factor
public static class NoiseGenerator
{
    public enum NormalizeMode { Local,Global};
    public static float[,] GenerateNoise(int width = 100, int height = 100, int seed = 25, float scale = 15, int octaves = 3, float lacunarity = 2, float persistence = 0.5f,Vector2 offset = default(Vector2), NormalizeMode normalizeMode = NormalizeMode.Local)
    {
        float[,] noiseMap = new float[width, height];
        //Creates a random seed/starting point for the perlin noise
        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;
        for (int i = 0; i<octaves; i++)
        {
            //Creates the offset based on the seed staring point and the input x/y offsets
            float xOffset = rand.Next(-100000,100000) +offset.x;
            float yOffset = rand.Next(-100000,100000) -offset.y;
            octaveOffsets[i] = new Vector2(xOffset, yOffset);
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }
        
        //in case scale is 0, prevents a divide by 0 error
        if(scale <= 0)
        {
            scale = 0.0001f;
        }
        //This is used to find the upper and lower bounds of the perlin noise and the normalize at the end
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue; 

        for(int x = 0; x < width; x++)
        {
            for(int y=0; y < height; y++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                //Loops through each octave to add a layer of perlin noise that's changed by persistence & lacunarity
                for (int i = 0; i < octaves; i++)
                {
                    //Applying the scale factor & offset to the x and y values for the perlin noise function
                    float tempX = (x-width/2f + octaveOffsets[i].x) / scale * frequency ;
                    float tempY = (y-height/2f + octaveOffsets[i].y) / scale * frequency ;

                    //Generating the perlin noise value
                    float perlinNoiseValue = Mathf.PerlinNoise(tempX, tempY) *2 -1;
                    //Every octave the amplitude will decrease and the frequency will increase
                    //This creates more "details" with each octave but will have less effect over time
                    noiseHeight += perlinNoiseValue*amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                //Find the max and min values of the map for normalization
                if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        //Normalizing the noise/height map by giving each value a corresponding point between 0,1 based on the min/max
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }else if(normalizeMode == NormalizeMode.Global)
                {
                    noiseMap[x, y] = Mathf.Clamp((noiseMap[x, y] + 1) / (2f * maxPossibleHeight/1.75f),0,int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
