using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer tRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    
    public void DrawTexture(Texture2D texture){
        tRender.sharedMaterial.mainTexture = texture;
        tRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData mesh, Texture2D texture){
        meshFilter.sharedMesh = mesh.createMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

}
