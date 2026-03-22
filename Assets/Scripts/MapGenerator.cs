using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator {
    public static int discreteDistribution(int[] coef, int sum) {
        int d = UnityEngine.Random.Range(0, sum);

        for (int i = 0; i < coef.Length; i++) {
            if (d < coef[i]) {
                return i;
            }
        }

        Debug.LogError("Unreachable");
        return -1;
    }

    public static void Generate(LocalMap map, int[] landTypeCoef) {
        int[] cumCoef = (int[])landTypeCoef.Clone();
        int sum = 0;

        for (int i = 0; i < cumCoef.Length; i++) {
            int c = cumCoef[i];
            cumCoef[i] += sum;
            sum += c;
        }

        for (int x = 0; x < map.w; x++) {
            for (int y = 0; y < map.h; y++) {
                map.tileArr[x, y].landType = (TileLandType)discreteDistribution(cumCoef, sum);
            }
        }
    }
}
