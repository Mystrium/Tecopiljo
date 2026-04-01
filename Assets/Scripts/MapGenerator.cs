using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

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

    public static void Generate(LocalMap map, int[] landTypeCoef, IEnumerable playerIds) {
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

        foreach (int playerId in playerIds) {
            int x = UnityEngine.Random.Range(0, map.w);
            int y = UnityEngine.Random.Range(0, map.h);

            map.tileArr[x, y].unit = new Unit(UnitType.TEST, playerId);
            Debug.Log($"Spawned a unit for player {playerId} at {x}, {y}");
        }
    }
}
