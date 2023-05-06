using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //Creating the dropdown to choose the map type
    public enum DrawMode {NoiseMap, ColorMap};
    public DrawMode drawMode;
    //Map Dimensions
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;
    //Map details and perlin noise layering variables
    public int octaves;
    [Range(0,1)]
    public float persistence;
    public float lacunarity;
    public int seed;
    public Vector2 offset;
    //Color map regions (ie height map categories)
    public TerrainType[] regions;

    public bool autoUpdate;
    public Renderer renderer;

    //General draw texture function for either height or color maps
    public void DrawTexture2D(Texture2D texture)
    {
        renderer.sharedMaterial.mainTexture = texture;
        renderer.transform.localScale = new Vector3(mapWidth, 1, mapHeight);
    }
    //Creates a Texture 2D with a 1D color map
    public Texture2D TextureFromColorMap(Color[] colorMap)
    {
        Texture2D mapTexture = new Texture2D(mapWidth, mapHeight);
        mapTexture.filterMode = FilterMode.Point;
        mapTexture.wrapMode = TextureWrapMode.Clamp;
        mapTexture.SetPixels(colorMap);
        mapTexture.Apply();
        return mapTexture;
    }
    //Creates a Texture 2D from a 2D noise/height map
    public Texture2D TextureFromHeightMap(float[,] noiseMap)
    {
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                //At each point creates a color between black and white using the noise/height value at the point
                float perlinNoiseValue = noiseMap[x, y];
                colorMap[y * mapWidth + x] = Color.Lerp(Color.black, Color.white, perlinNoiseValue);
            }
        }
        //Returns a texture using a colorMap that's grayscaled using the Color.Lerp
        return TextureFromColorMap(colorMap);
    }

    public void GenerateMap()
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoise(mapWidth, mapHeight,seed, noiseScale, octaves, lacunarity, persistence, offset);
        
        Color[] colorMap = new Color[mapWidth*mapHeight];
        for(int x= 0; x < mapWidth; x++)
        {
            for(int y= 0; y < mapHeight; y++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight<= regions[i].height)
                    {
                        colorMap[y*mapWidth +x] = regions[i].color;
                        break;
                    }
                }
            }
        }


        if (drawMode == DrawMode.NoiseMap) {
            this.DrawTexture2D(this.TextureFromHeightMap(noiseMap));
        }else if (drawMode == DrawMode.ColorMap)
        {
            this.DrawTexture2D(this.TextureFromColorMap(colorMap));
        }
           
    }
    //Creates some bounds for the public variables
    void OnValidate()
    {
        if(mapWidth<1)
            mapWidth= 1;
        if(mapHeight<1)
            mapHeight= 1;
        if(lacunarity<1)
            lacunarity= 1;
        if (octaves < 0)
            octaves = 0;
    }
}
//TerrainType struct for the public regions array
[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}
