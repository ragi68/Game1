using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 2.5f;
    const float minMove = 25f;
    const float sqrMinMove = 625f;
    public Transform player;
    public Material mapMaterial;
    public LODInfo[] LevelOfDetails;
    public static float maxViewDistance;



    public static Vector2 viewerPosition;
    int chunkSize;
    int chunksVisibleInView;

    Dictionary<Vector2, TerrainChunk> terrainDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
    static MapGenerator mapGenerator;

    Vector2 oldPosition;

    void Start(){
        chunkSize = MapGenerator.ChunkSize - 1;
        maxViewDistance = LevelOfDetails[LevelOfDetails.Length - 1].activeDistance;
        chunksVisibleInView = Mathf.RoundToInt(maxViewDistance / chunkSize);
        mapGenerator = FindObjectOfType<MapGenerator>();
        UpdateVisibleChunks();
    }

    void Update(){
        viewerPosition = new Vector2(player.position.x, player.position.z) / scale;
        if((oldPosition - viewerPosition).sqrMagnitude > sqrMinMove){
            UpdateVisibleChunks();
            oldPosition = viewerPosition;
        }
    }
    void UpdateVisibleChunks(){
        for(int i = 0; i < visibleTerrainChunks.Count; i++){
            visibleTerrainChunks[i].SetVisible(false);
        }
        visibleTerrainChunks.Clear();
        int ChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int ChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int y = -chunksVisibleInView; y <= chunksVisibleInView; y++){
            for(int x = -chunksVisibleInView; x <= chunksVisibleInView; x++){
                Vector2 viewedChunk = new Vector2(ChunkX + x, ChunkY + y);

                if(terrainDict.ContainsKey(viewedChunk) && transform.childCount < 200){
                    terrainDict[viewedChunk].UpdateChunk();

                } else if(transform.childCount < 200){
                    terrainDict.Add(viewedChunk, new TerrainChunk(viewedChunk, chunkSize, LevelOfDetails, transform, mapMaterial));
                } else if(transform.childCount >= 200){

                }
            }
        }
    }

    public class TerrainChunk{
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;

        MapData mapData;
        bool MapDataGiven;

        MeshRenderer renderer;
        MeshFilter filter;
        MeshCollider collider;

        LODInfo[] detailLevels;
        LODMesh[] LODmeshes;
        LODMesh collisionMesh;
        int previousLOD = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material){
            position = coord * size;
            this.detailLevels = detailLevels;
            Vector3 pos3D = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);

            meshObject = new GameObject("Terrain Chunk");
            filter = meshObject.AddComponent<MeshFilter>();
            renderer = meshObject.AddComponent<MeshRenderer>();
            collider = meshObject.AddComponent<MeshCollider>();
            meshObject.transform.position = pos3D * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            meshObject.layer = LayerMask.NameToLayer("Ground");
            renderer.material = material;
            SetVisible(false);

            LODmeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < detailLevels.Length; i++){
                LODmeshes[i] = new LODMesh(detailLevels[i].LevelOfDetail, UpdateChunk);
                if(detailLevels[i].useForCollider){
                    collisionMesh = LODmeshes[i];
                }
            }

           

            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData){
            this.mapData = mapData;
            MapDataGiven = true;
            Texture2D texture = TextureCreator.TextureFromColors(mapData.colorMap, MapGenerator.ChunkSize, MapGenerator.ChunkSize);
            renderer.material.mainTexture = texture;

            UpdateChunk();
        }

        public void UpdateChunk(){

            if(MapDataGiven){
                float dist = bounds.SqrDistance(viewerPosition);
            bool visible = dist <= maxViewDistance * maxViewDistance;
            if(visible){
                int lodIndex = 0;
                for(int i = 0; i < detailLevels.Length - 1; i++){
                    if(dist > detailLevels[i].activeDistance * detailLevels[i].activeDistance){
                        lodIndex = i+1;
                    } else{
                        break;
                    }
                }

                if(lodIndex != previousLOD){
                    LODMesh lodMesh = LODmeshes[lodIndex];
                    if(lodMesh.hasMesh){
                        filter.mesh = lodMesh.mesh;
                        collisionMesh.mesh = lodMesh.mesh;
                    } else if(!lodMesh.requestedMesh){
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if(lodIndex == 0){
                    if(collisionMesh.hasMesh){
                        collider.sharedMesh = collisionMesh.mesh;
                    } else if(!collisionMesh.requestedMesh){
                        collisionMesh.RequestMesh(mapData);
                    }
                }

                visibleTerrainChunks.Add(this);
            }
            SetVisible(visible);
            }
            
        }


        public void SetVisible(bool visible){
            meshObject.SetActive(visible);
        }
        public bool isVisible(){
            return meshObject.activeSelf;
        }

    }

    class LODMesh{
        public Mesh mesh;
        public bool requestedMesh;
        public bool hasMesh;
        public int LOD;
        System.Action updateCallBack;

        public LODMesh(int LOD, System.Action updateCallBack){
            this.updateCallBack = updateCallBack;
            this.LOD = LOD;
        }

        public void OnMeshDataRecieved(MeshData meshData){
            mesh = meshData.createMesh();
            hasMesh = true;

            updateCallBack();
        }

        public void RequestMesh(MapData mapData){
            requestedMesh = true;
            mapGenerator.RequestMeshData(mapData, LOD, OnMeshDataRecieved);
        }

        
    }
    [System.Serializable]
    public struct LODInfo{
        public int LevelOfDetail;
        public float activeDistance;
        public bool useForCollider;

    }

}
