using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
    public Camera mainCamera;

    [Header("Tilemap / Tiles")]
    public Tilemap tilemap;
    public TileBase[] tiles;
    public int[] coef;
    public Vector2Int offset = Vector2Int.zero;

    private TileBase[] flatTileArray;
    private int[] size = new int[]{0, 0};
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

    public void RenderMap(MapPayload mapData) {
        size[0] = mapData.w;
        size[1] = mapData.h;
        
        tilemap.ClearAllTiles();

        for (int x = 0; x < mapData.w; x++) {
            for (int y = 0; y < mapData.h; y++) {
                Vector3Int pos = new Vector3Int(y + offset.y - x / 2, x + offset.x, 0);
                
                byte ind = mapData.mapBytes[x, y]; 
                
                tilemap.SetTile(pos, flatTileArray[ind]);
            }
        }
        
        tilemap.RefreshAllTiles();
        Debug.Log($"[MapRenderer] Rendered {mapData.w}x{mapData.h} map");
    }


    private Vector3 prevPos = Vector3.zero;
    public void GetClickedTile() {
        if (size[0] > 0) {
            if(Input.GetMouseButtonUp(0))
                HandleMouseClick();
            if(Input.GetMouseButtonDown(0))
                prevPos = Input.mousePosition;
        }
    }

    void HandleMouseClick() {
        Vector3 mouseScreenPos = Input.mousePosition;
        if(prevPos == mouseScreenPos) {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);

            int arrayX = cellPos.y - offset.x;
            int arrayY = cellPos.x - offset.y + (arrayX / 2);

            if (arrayX >= 0 && arrayX < size[0] && arrayY >= 0 && arrayY < size[1]) {

                Debug.Log($"[Input] Клік! Tilemap: {cellPos} -> Масив: [{arrayX}, {arrayY}]");
                
            }
            else 
            {
                Debug.Log("[Input] Клік поза межами ромбової мапи.");
            }
        }
    }
}