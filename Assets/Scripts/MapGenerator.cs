using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //Creating the dropdown to choose the map type
    public enum DrawMode {NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;
    public NoiseGenerator.NormalizeMode normalizeMode;
    //Map Dimensions
    public const int chunkSize = 241;
    [Range(0,6)]
    public int previewLOD;
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

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    public bool autoUpdate;
    public Renderer renderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    //General draw texture function for either height or color maps
    public void DrawTexture2D(Texture2D texture)
    {
        renderer.sharedMaterial.mainTexture = texture;
        renderer.transform.localScale = new Vector3(chunkSize, 1, chunkSize);
    }
    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    //Creates a Texture 2D with a 1D color map
    public Texture2D TextureFromColorMap(Color[] colorMap)
    {
        Texture2D mapTexture = new Texture2D(chunkSize, chunkSize);
        mapTexture.filterMode = FilterMode.Point;
        mapTexture.wrapMode = TextureWrapMode.Clamp;
        mapTexture.SetPixels(colorMap);
        mapTexture.Apply();
        return mapTexture;
    }
    //Creates a Texture 2D from a 2D noise/height map
    public Texture2D TextureFromHeightMap(float[,] noiseMap)
    {
        Color[] colorMap = new Color[chunkSize * chunkSize];
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                //At each point creates a color between black and white using the noise/height value at the point
                float perlinNoiseValue = noiseMap[x, y];
                colorMap[y * chunkSize + x] = Color.Lerp(Color.black, Color.white, perlinNoiseValue);
            }
        }
        //Returns a texture using a colorMap that's grayscaled using the Color.Lerp
        return TextureFromColorMap(colorMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        //Different draw Modes
        if (drawMode == DrawMode.NoiseMap)
        {
            this.DrawTexture2D(this.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            this.DrawTexture2D(this.TextureFromColorMap(mapData.colorMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            this.DrawMesh(MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightMapCurve, previewLOD), this.TextureFromColorMap(mapData.colorMap));
        }

    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapdata, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapdata,lod, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.heightMap, heightMultiplier,heightMapCurve,lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i< mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoise(chunkSize, chunkSize,seed, noiseScale, octaves, lacunarity, persistence, center + offset, normalizeMode);
        //Creating a colorMap based on the regions
        Color[] colorMap = new Color[chunkSize*chunkSize];
        for(int x= 0; x < chunkSize; x++)
        {
            for(int y= 0; y < chunkSize; y++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight>= regions[i].height)
                    {
                        colorMap[y*chunkSize +x] = regions[i].color;

                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        return new MapData(noiseMap, colorMap);
    }
    //Creates some bounds for the public variables
    void OnValidate()
    {
        if(lacunarity<1)
            lacunarity= 1;
        if (octaves < 0)
            octaves = 0;
    }

    struct MapThreadInfo<T> 
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
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

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
