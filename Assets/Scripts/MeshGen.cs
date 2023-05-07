using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGen
{
    //The height map will be mapped to mesh data
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightMapCurve, int levelOfDetail)
    {
        AnimationCurve heightMapCurve = new AnimationCurve(_heightMapCurve.keys);
        //This centers the mesh to the center rather than the corner
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplifyingIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail*2;
        int verticesPerLine = (width - 1) / meshSimplifyingIncrement+1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;
        //At each vertex index it creates 2 triangles for each square unless at the right or bottom edge
        for(int x = 0; x < width; x += meshSimplifyingIncrement)
        {
            for(int y = 0; y < height; y += meshSimplifyingIncrement)
            {
                //Each vertex is given a position and a height value based on the animation curve in unity and the generated height map
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightMapCurve.Evaluate(heightMap[x,y])*heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                if(x < width - 1 && y< height - 1)
                {
                    meshData.AddTriangle(vertexIndex + verticesPerLine, vertexIndex + verticesPerLine + 1, vertexIndex);
                    meshData.AddTriangle(vertexIndex + 1,vertexIndex ,vertexIndex + verticesPerLine + 1);
                }
                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData
{
    //Mesh data contains all of the vertices and triangles
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    int triangleIndex;
    

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth-1) * (meshHeight-1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex +1] = b;
        triangles[triangleIndex +2] = c;
        triangleIndex += 3;
    }
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}