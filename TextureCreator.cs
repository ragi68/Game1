using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureCreator
{
    public static Texture2D TextureFromColors(Color[] colorMap, int width, int height){
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromNoise(float[,] noiseMap){
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Color[] colors = new Color[width * height];
        for(int y = 0; y < height; y++){
            for(int x = 0; x < height; x++){
                colors[y * width + x] = Color.Lerp(Color.white, Color.black, noiseMap[x, y]);
            }
        }

        return TextureFromColors(colors, width, height);

    }

}
