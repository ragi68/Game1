using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallOffMap : MonoBehaviour
{
    public static float[,] FallOffGenerator(int size){
        float[,] fallMap = new float[size, size];

        for(int y = 0; y < size; y++){
            for(int x = 0; x < size; x++){
                float i = y / (float) size * 2 -1;
                float j = x / (float) size * 2 -1;

                float value = Mathf.Max(Mathf.Abs(i), Mathf.Abs(j));
                fallMap[x, y] = Evaluate(value);
            }
        }
        return fallMap;
    }

    static float Evaluate(float value){
        float a = 3f;
        float b = 2.2f;
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b*value, a));
    }
}
