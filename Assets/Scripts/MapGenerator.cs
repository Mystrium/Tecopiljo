using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour {
    [Header("Tilemap / Tiles")]
    public Tilemap tilemap;

    public TileBase[] tiles;
    public int[] coef;


    [Header("Map size (tiles)")]
    public int width = 40;
    public int height = 30;
    public Vector2Int offset = Vector2Int.zero;

    byte[,] map;

    void Start() {
        map = new byte[width, height];
        Generate();
    }

    public void Generate() {
        int sum = 0;
        foreach (int c in coef)
            sum += c;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = (byte)Random.Range(0, sum);

        var tx = MapPayload.Pack(new MapPayload { w = width, h = height, mapBytes = map });


        var rx = MapPayload.Unpack(tx);

        tilemap.ClearAllTiles();

        TileBase[] bePlased = new TileBase[sum];
        int k = 0;
        for(int i = 0; i < tiles.Length; i++)
            for(int j = 0; j < coef[i]; j++){
                bePlased[k] = tiles[i];
                k++;
            }

        for (int x = 0; x < rx.w; x++) {
            for (int y = 0; y < rx.h; y++) {
                Vector3Int pos = new Vector3Int(y + offset.y - x / 2, x + offset.x, 0);
                var ind = rx.mapBytes[x, y];
                var test = bePlased[ind];
                tilemap.SetTile(pos, test);
            }
        }

        tilemap.RefreshAllTiles();
    }
}
