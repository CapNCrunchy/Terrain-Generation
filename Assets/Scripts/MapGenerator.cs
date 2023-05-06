using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //Creating the dropdown to choose the map type
    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;
    //Map Dimensions
    public int mapWidth =100;
    public int mapHeight =100;
    public float noiseScale =13;
    public float heightMultiplier = 65f;
    public AnimationCurve heightMapCurve;
    //Map details and perlin noise layering variables
    public int octaves = 3;
    [Range(0,1)]
    public float persistence= 0.5f;
    public float lacunarity = 2;
    public int seed = 0;
    public Vector2 offset = default(Vector2);
    //Color map regions (ie height map categories)
    public TerrainType[] regions = { new TerrainType("Water",0.4f,new Color(0f,0.043f,1f,0)),
    new TerrainType("Sand",0.5f,new Color(0.72f,0.52f,0.3f,0)),
    new TerrainType("Grass",0.7f,new Color(0.13f,0.5f,0.18f,0)),
    new TerrainType("Rock",0.9f,new Color(0.3f,0.23f,0.21f,0)),
    new TerrainType("Snow",1f,new Color(1f,1f,1f,0))};

    public bool autoUpdate;
    public Renderer renderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    //General draw texture function for either height or color maps
    public void DrawTexture2D(Texture2D texture)
    {
        renderer.sharedMaterial.mainTexture = texture;
        renderer.transform.localScale = new Vector3(mapWidth, 1, mapHeight);
    }
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
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
        //Creating a colorMap based on the regions
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

        //Different draw Modes
        if (drawMode == DrawMode.NoiseMap) {
            this.DrawTexture2D(this.TextureFromHeightMap(noiseMap));
        }else if (drawMode == DrawMode.ColorMap)
        {
            this.DrawTexture2D(this.TextureFromColorMap(colorMap));
        }else if (drawMode == DrawMode.Mesh)
        {
            this.DrawMesh(MeshGen.GenerateTerrainMesh(noiseMap, heightMultiplier, heightMapCurve), this.TextureFromColorMap(colorMap));
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

    public TerrainType(string _name, float _height, Color _color)
    {
        name =_name;
        height = _height;
        color = _color;
    }
}
