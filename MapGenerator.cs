using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading;


public class MapGenerator : MonoBehaviour
{

    
    public enum DrawMode {NoiseMap, ColorMap, Mesh, FallOffMap};
    public DrawMode drawMode;

    public NoiseGenerator.NormalMode mode;

    public float NoiseScale;
    public int Octaves;
    [Range(0, 1)]
    public float Persistence;
    [Range(1, 6)]
    public float Lacunarity;

    public int Seed;
    public Vector2 OffSet;
    
    
    
    
    [SerializeField][Header ("Map Size and Scale")]
    [Range(0, 6)]
    public int EditorLOD;
    public TerrainType[] HeightRegions;
    [SerializeField][Header("Technicalities")]
    public bool autoUpdate;
    static MapGenerator instance;
    float[,] fallOffMap;

    public bool useFallMap;
    public bool useFlatShading;

    public float HeightScale;
    public AnimationCurve animationCurve;

    Queue<MapThreadInfo<MapData>> mapDataInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake(){
        fallOffMap = FallOffMap.FallOffGenerator(ChunkSize);
    }

    public static int ChunkSize{
        get{
            if(instance == null){
                instance = FindObjectOfType<MapGenerator>();
            }
            if(instance.useFlatShading){
                return 95;
            }
            else{
                return 239;
            }
        }
    }

    public void DrawMapInEditor(){
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
         if(drawMode == DrawMode.NoiseMap){
            display.DrawTexture(TextureCreator.TextureFromNoise(mapData.heightMap));
        } else if(drawMode == DrawMode.ColorMap){
            display.DrawTexture(TextureCreator.TextureFromColors(mapData.colorMap, ChunkSize, ChunkSize));
        } else if(drawMode == DrawMode.Mesh){
            display.DrawMesh(MeshCreator.GenerateMesh(mapData.heightMap, HeightScale, animationCurve, EditorLOD, useFlatShading), TextureCreator.TextureFromColors(mapData.colorMap, ChunkSize, ChunkSize));
        } else if(drawMode == DrawMode.FallOffMap){
            display.DrawTexture(TextureCreator.TextureFromNoise(FallOffMap.FallOffGenerator(ChunkSize)));
        }
    }
#region MapDataThread
    public void RequestMapData(Vector2 center, Action<MapData> callback){

        ThreadStart threadStart = delegate{
            MapDataThread(center, callback);
        };

        new Thread (threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback){
        MapData data = GenerateMapData(center);
        lock(mapDataInfoQueue){
            mapDataInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, data));
        }
    }

    public void RequestMeshData(MapData mapData, int lod,  Action<MeshData> callback){;
        ThreadStart threadStart = delegate{
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();

    }
    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback){
        MeshData meshData = MeshCreator.GenerateMesh(mapData.heightMap, HeightScale, animationCurve, lod, useFlatShading);
        lock(meshDataInfoQueue){
            meshDataInfoQueue.Enqueue(new MapThreadInfo<MeshData> (callback, meshData));
        }
    }


    void Update(){

        lock(mapDataInfoQueue){
            while(mapDataInfoQueue.Count > 0){
                MapThreadInfo<MapData> threadInfo = mapDataInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        lock(meshDataInfoQueue){
            while(meshDataInfoQueue.Count > 0){
                MapThreadInfo<MeshData> threadInfo = meshDataInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    

    #endregion

    MapData GenerateMapData(Vector2 center){
        float[,] NoiseMap = NoiseGenerator.GenerateNoise(ChunkSize + 2, ChunkSize + 2, NoiseScale, Octaves, Persistence, Lacunarity, Seed, center + OffSet, mode);
        Color[] colors = new Color[ChunkSize * ChunkSize];
        for(int y = 0; y < ChunkSize; y++){
            for(int x = 0; x < ChunkSize; x++){
                if(useFallMap){
                    NoiseMap[x, y] = Mathf.Clamp01(NoiseMap[x, y] - fallOffMap[x, y]);
                }
                float currentHeight = NoiseMap[x,y ];
                for(int i = 0; i < HeightRegions.Length; i++){
                    if(currentHeight >= HeightRegions[i].height){
                        colors[y * ChunkSize + x] = HeightRegions[i].color;
                        
                    } else{break;}
                }
            }
        }

        return new MapData(NoiseMap, colors);
    }

    struct MapThreadInfo<T>{
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter){
            this.callback = callback;
            this.parameter = parameter;
        }

    }

    void OnValidate(){
        fallOffMap = FallOffMap.FallOffGenerator(ChunkSize);
    }
}


[System.Serializable]
public struct TerrainType{
    public string name;
    public float height;
    public Color color;
}

public struct MapData{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap){
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
