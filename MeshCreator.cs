using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCreator
{
    public static MeshData GenerateMesh(float[,] map, float heightScale, AnimationCurve aniCurve, int lod, bool useFlatShading){

        AnimationCurve heightCurve = new AnimationCurve(aniCurve.keys);
        int borderedSize = map.GetLength(0);
        int LODincrement = (lod == 0) ? 1 : lod * 2;
        int meshSize = borderedSize - 2 * LODincrement;
        int meshUnSimpleSize = borderedSize - 2;
        float TLX = (meshSize - 1) / -2f;
        float TLZ = (meshSize - 1) / 2f;


        int vertPerLine = (meshSize - 1) / LODincrement + 1;

        MeshData mesh = new MeshData(vertPerLine, useFlatShading);
        
        int[,] vertexIndicesMap =  new int[borderedSize,borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for(int y = 0; y < borderedSize; y+= LODincrement){
            for(int x = 0; x < borderedSize; x+=LODincrement){
                bool isBorderVertex = y == 0 || y == borderedSize -1 || x == 0 || x == borderedSize - 1;
                if(isBorderVertex){
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                } else{
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        

        for(int y = 0; y < borderedSize; y+= LODincrement){
            for(int x = 0; x < borderedSize; x+=LODincrement){
                int vIndex = vertexIndicesMap[x , y];
                Vector2 percent = new Vector2((x - LODincrement)/(float)meshSize, (y - LODincrement) / (float)meshSize);
                float height = heightScale * heightCurve.Evaluate(map[x, y]);
                Vector3 vertexPosition  = new Vector3(TLX + percent.x * meshUnSimpleSize, height, TLZ -percent.y * meshUnSimpleSize);

                mesh.AddVertex(vertexPosition, percent, vIndex);
              
                if(x <  borderedSize - 1 && y < borderedSize - 1){
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + LODincrement, y];
                    int c = vertexIndicesMap[x, y + LODincrement];
                    int d = vertexIndicesMap[x + LODincrement, y + LODincrement];
                    mesh.AddTriangle(a, d, c);
                    mesh.AddTriangle(d, a, b);
                }
                vIndex++;
            }
        }

        mesh.Finalize();

        return mesh;
    }
}

public class MeshData{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] UVs;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;


    int tIndex;
    int borderTriangleIndex;
    bool flatShading;

    public MeshData(int verticesPerLine, bool flatShading){
        this.flatShading = flatShading;
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        UVs = new Vector2[verticesPerLine * verticesPerLine];
        vertices = new Vector3[verticesPerLine * verticesPerLine]; 

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 Vertex, Vector2 vertexUV, int vertexIndex){
        if(vertexIndex < 0){
            borderVertices[-vertexIndex - 1] = Vertex;
        } else{
            vertices[vertexIndex] = Vertex;
            UVs[vertexIndex] = vertexUV;
        }
    }

    public void AddTriangle(int a, int b, int c){
        if(a < 0 || b < 0|| c < 0){
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;

        } else{
            triangles[tIndex] = a;
            triangles[tIndex + 1] = b;
            triangles[tIndex + 2] = c;
            tIndex += 3;
        }
    }

    public void Finalize(){
        if(flatShading){
            FlatShading();
        } else{
            bakeNormals();
        }
    }

    private void bakeNormals(){
        bakedNormals = CalculateNormals();
    }

    Vector3[] CalculateNormals(){
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++){
            int normalTriangleIndex = i * 3;
            int vertexIndex1 = triangles[normalTriangleIndex];
            int vertexIndex2 = triangles[normalTriangleIndex + 1];
            int vertexIndex3 = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndex1, vertexIndex2, vertexIndex3);
            vertexNormals[vertexIndex1] += triangleNormal;
            vertexNormals[vertexIndex2] += triangleNormal;
            vertexNormals[vertexIndex3] += triangleNormal;

        }

        int borderTriangleCount = borderTriangles.Length / 3;

        for(int i = 0; i < borderTriangleCount; i++){
            int normalTriangleIndex = i * 3;
            int vertexIndex1 = triangles[normalTriangleIndex];
            int vertexIndex2 = triangles[normalTriangleIndex + 1];
            int vertexIndex3 = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndex1, vertexIndex2, vertexIndex3);
            if(vertexIndex1 >= 0){vertexNormals[vertexIndex1] += triangleNormal;}
            if(vertexIndex2 >= 0){vertexNormals[vertexIndex2] += triangleNormal;}
            if(vertexIndex3 >= 0){vertexNormals[vertexIndex3] += triangleNormal;}

        }

        for(int i = 0; i < vertexNormals.Length; i++){
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC){
        Vector3 pointA = (indexA < 0)? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0)? borderVertices[-indexB - 1] :vertices[indexB];
        Vector3 pointC = (indexC < 0)? borderVertices[-indexC - 1] :vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    void FlatShading(){
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUV = new Vector2[triangles.Length];
        for(int i = 0; i < flatShadedVertices.Length; i++){
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUV[i] = UVs[triangles[i]];
            triangles[i] = i;

        }

        vertices = flatShadedVertices;
        UVs = flatShadedUV;
    }

    public Mesh createMesh(){
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = UVs;
        if(flatShading){
            mesh.RecalculateNormals();
        }
        else{
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
