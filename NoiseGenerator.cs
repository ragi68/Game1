using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class NoiseGenerator
{

    public enum NormalMode{ Local, Global};
    public static float[,] GenerateNoise(int width, int height, float scale, int octaves, float persistence, float lacunarity, int seed, Vector2 offset, NormalMode mode){
        float[,] noiseMap = new float[width,height];

        System.Random randNum = new System.Random(seed);
        Vector2[] OffSets = new Vector2[octaves];
        float amplitude = 1f;
        float frequency = 1f;
        float maxPossible = 0f;
        //shifting the map by an offset defined by random numbers and a defined offset of our own. 
        for(int i = 0; i < octaves; i++){
            float OffX = randNum.Next(-100000, 100000) + offset.x;
            float OffY = randNum.Next(-100000, 100000) - offset.y;
            OffSets[i] = new Vector2(OffX, OffY);

            maxPossible += amplitude;
            amplitude *= persistence;
        }

        if(scale <= 0){
            scale = 0.01f;
        }

        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
//O(n^3) Shorten?
        for(int y = 0; y< height; y++){
            for(int x = 0; x < width; x++){
                amplitude = 1f; 
                frequency = 1f; 
                float noiseHeight = 0f;
                for(int i = 0; i < octaves; i++){
                    float sX = (x - (width / 2) + OffSets[i].x) / scale * frequency;//adds our displacement to each layer of offsets. 
                    float sY = (y - (height / 2) + OffSets[i].y) / scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sX, sY) * 2 - 1; 
                    noiseHeight += perlinValue * amplitude; 
                    amplitude *= persistence; 
                    frequency *= lacunarity;
                }
                //sets max and min noise values
                if(maxHeight < noiseHeight){ maxHeight = noiseHeight;}

                else if(minHeight > noiseHeight){ minHeight = noiseHeight;}

                noiseMap[x, y] = noiseHeight; 
                
            }
        }

         for(int y = 0; y< height; y++){
            for(int x = 0; x < width; x++){
                if(mode == NormalMode.Local){noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);}
                else{
                    float normalHeight = (noiseMap[x, y] + 1) / (maxPossible);
                    noiseMap[x, y] = Mathf.Clamp(normalHeight, 0, int.MaxValue);
                } //puts where the value is in the range (0, 1) comparative to the range of min to maxin local, but global caluclates min and max through averages. 
            }    
        }

        

        return noiseMap;
    }
}
