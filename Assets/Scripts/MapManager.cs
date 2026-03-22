using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour {
    public Camera mainCamera;
    public LocalMap map;
    [Header("Tilemap / Tiles")]
    public TileBase[] tilePalette;
    public Tilemap tilemap;
    public Vector2Int offset = Vector2Int.zero;

    public void Awake() { }

    public void RenderMap(MapPayload mapData) {
        tilemap.ClearAllTiles();

        map = mapData.map;

        for (int x = 0; x < map.w; x++) {
            for (int y = 0; y < map.h; y++) {
                Vector3Int pos = new Vector3Int(y + offset.y - x / 2, x + offset.x, 0);

                int idx = (int)map.tileArr[x, y].landType;

                tilemap.SetTile(pos, tilePalette[idx]);
            }
        }

        tilemap.RefreshAllTiles();
    }

    private Vector3 prevPos = Vector3.zero;
    public void GetClickedTile() {
        if (map != null) {
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

            if (arrayX >= 0 && arrayX < map.w && arrayY >= 0 && arrayY < map.h) {
                Debug.Log($"[Input] Клік! Tilemap: {cellPos} -> Масив: [{arrayX}, {arrayY}]");
            } else {
                Debug.Log("[Input] Клік поза межами ромбової мапи.");
            }
        }
    }
}
