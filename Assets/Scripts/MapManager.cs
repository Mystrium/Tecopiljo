using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
    [Header("Tilemap / Tiles")]
    public Tilemap tilemap;
    public TileBase[] tiles;
    public int[] coef;
    public Vector2Int offset = Vector2Int.zero;

    private TileBase[] flatTileArray;
    public byte TotalCoefSum { get; private set; }

    void Awake() {
        TotalCoefSum = 0;
        foreach (byte c in coef) TotalCoefSum += c;

        flatTileArray = new TileBase[TotalCoefSum];
        int k = 0;
        for (int i = 0; i < tiles.Length; i++) {
            for (int j = 0; j < coef[i]; j++) {
                flatTileArray[k] = tiles[i];
                k++;
            }
        }
    }

    public byte getMax() { return TotalCoefSum; }

    public void RenderMap(MapPayload mapData) {
        tilemap.ClearAllTiles();

        for (int x = 0; x < mapData.w; x++) {
            for (int y = 0; y < mapData.h; y++) {
                Vector3Int pos = new Vector3Int(x + offset.x, y + offset.y, 0);
                
                byte ind = mapData.mapBytes[x, y]; 
                
                tilemap.SetTile(pos, flatTileArray[ind]);
            }
        }
        
        tilemap.RefreshAllTiles();
        Debug.Log($"[MapRenderer] Rendered {mapData.w}x{mapData.h} map");
    }
}