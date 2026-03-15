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


    void Start() {
        Generate();
    }

    public void Generate() {
        int sum = 0;
        foreach (int c in coef)
            sum += c;

        TileBase[] bePlased = new TileBase[sum];
        int k = 0;
        for(int i = 0; i < tiles.Length; i++)
            for(int j = 0; j < coef[i]; j++){
                bePlased[k] = tiles[i];
                k++;
            }


        tilemap.ClearAllTiles();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3Int pos = new Vector3Int(x + offset.x, y + offset.y, 0);
                tilemap.SetTile(pos, bePlased[Random.Range(0, sum)]);
            }
        }

        tilemap.RefreshAllTiles();
    }
}
