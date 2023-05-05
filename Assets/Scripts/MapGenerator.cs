using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;
    public bool autoUpdate;
    public Renderer renderer;

    public void DrawNoiseMap(float[,] noiseMap)
    {
        Texture2D mapTexture = new Texture2D(mapWidth, mapHeight);
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float perlinNoiseValue = noiseMap[x, y];
                colorMap[y*mapWidth+x] = Color.Lerp(Color.black, Color.white, perlinNoiseValue);
            }
        }
        mapTexture.SetPixels(colorMap);
        mapTexture.Apply();
        renderer.sharedMaterial.mainTexture = mapTexture;
        renderer.transform.localScale = new Vector3(mapWidth, 1, mapHeight);
    }

    public void GenerateMap()
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoise(mapWidth, mapHeight, noiseScale);
        this.DrawNoiseMap(noiseMap);
    }
}
