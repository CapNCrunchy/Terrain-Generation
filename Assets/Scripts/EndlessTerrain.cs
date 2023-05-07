using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThreshUpdateChunk = 25f;
    const float sqrViewerMoveThreshUpdateChunk = viewerMoveThreshUpdateChunk * viewerMoveThreshUpdateChunk;

    public static float maxViewDistance;
    public LODInfo[] detailLevels;
    public Transform viewer;
    Vector2 oldViewPos;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksLastVisible = new List<TerrainChunk>();


    void Start()
    {
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.chunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance/chunkSize);
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if((oldViewPos-viewerPosition).sqrMagnitude > sqrViewerMoveThreshUpdateChunk)
        {
            oldViewPos = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for(int i = 0; i<terrainChunksLastVisible.Count; ++i)
        {
            terrainChunksLastVisible[i].SetVisisble(false);
        }
        terrainChunksLastVisible.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x/chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y/chunkSize);

        for(int yOff = -chunksVisibleInViewDistance; yOff <= chunksVisibleInViewDistance; yOff++)
        {
            for(int xOff = -chunksVisibleInViewDistance; xOff <= chunksVisibleInViewDistance; xOff++)
            {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX+xOff, currentChunkCoordY+yOff);
                if(terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord,chunkSize,detailLevels,transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MapData mapData;
        bool mapDataRecieved;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            position = coord * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);
            this.detailLevels = detailLevels;

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisisble(false);

            this.lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            mapGenerator.RequestMapData(position,OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            this.mapDataRecieved = true;
            Texture2D texture = mapGenerator.TextureFromColorMap(mapData.colorMap);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }
        

        public void UpdateTerrainChunk()
        {
            if(mapDataRecieved)
            {
                float viewerDistromEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool isVisible = viewerDistromEdge <= maxViewDistance;
                if (isVisible)
                {
                    int lodIndex = 0;
                    for(int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if(viewerDistromEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if(lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksLastVisible.Add(this);
                }
                SetVisisble(isVisible);
            }
        }

        public void SetVisisble(bool visisble)
        {
            meshObject.SetActive(visisble);
        }
        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,lod, OnMeshDataRecieved);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
