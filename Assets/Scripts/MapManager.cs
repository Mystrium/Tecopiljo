using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class MapManager : MonoBehaviour {
    public Camera mainCamera;
    public LocalMap map;
    [Header("Tilemap / Tiles")]
    public TileBase[] tilePalette;
    public Tilemap tilemap;
    public Vector2Int offset = Vector2Int.zero;

    public void Awake() { }

    public Vector3Int pos2unityPos(Vector2Int p) {
        return new Vector3Int(p.y + offset.y - p.x / 2, p.x + offset.x, 0);
    }

    public Vector2Int unityPos2pos(Vector3Int up) {
        int x = up.y - offset.x;
        int y = up.x - offset.y + (x / 2);

        return new Vector2Int(x, y);
    }

    public void UpdateUnitTransforms() {
        for (int x = 0; x < map.w; x++) {
            for (int y = 0; y < map.h; y++) {
                Debug.Log($"Iterating map {x} {y}");
                if (map.tileArr[x, y].unit is Unit u) {
                    Debug.Log($"Found unit at {x}, {y}");
                    // This looks yucky
                    u.data.transform.position = tilemap.CellToWorld(pos2unityPos(new Vector2Int(x, y)));
                }
            }
        }
    }

    public void RenderMap(MapPayload mapData) {
        map = mapData.map;

        tilemap.ClearAllTiles();

        for (int x = 0; x < map.w; x++) {
            for (int y = 0; y < map.h; y++) {
                Vector3Int pos = pos2unityPos(new Vector2Int(x, y));

                int idx = (int)map.tileArr[x, y].landType;

                tilemap.SetTile(pos, tilePalette[idx]);
            }
        }

        tilemap.RefreshAllTiles();

        UpdateUnitTransforms();
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

            Vector2Int pos = unityPos2pos(cellPos);

            if (pos.x >= 0 && pos.x < map.w && pos.y >= 0 && pos.y < map.h) {
                Debug.Log($"[Input] Клік! Tilemap: {cellPos} -> Масив: [{pos.x}, {pos.y}]");
            } else {
                Debug.Log("[Input] Клік поза межами ромбової мапи.");
            }
        }
    }
}
