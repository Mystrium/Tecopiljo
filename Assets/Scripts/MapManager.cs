using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour {
    public Camera mainCamera;
    [Header("Tilemap / Tiles")]
    public TileBase[] tilePalette;
    public GameObject[] unitPrefs;
    public Tilemap tilemap;
    public Vector2Int offset = Vector2Int.zero;

    public event System.Action<int, Vector2Int, int> OnMoveIntent;
    private Dictionary<int, UnitView> activeUnits = new Dictionary<int, UnitView>();

    private LocalMap map;
    private UnitView[,] unitGrid;
    private UnitView selectedUnit = null;

    public void Awake() { }

    public Vector3Int pos2unityPos(Vector2Int p) {
        return new Vector3Int(p.y + offset.y - p.x / 2, p.x + offset.x, 0);
    }

    public Vector2Int unityPos2pos(Vector3Int up) {
        int x = up.y - offset.x;
        int y = up.x - offset.y + (x / 2);

        return new Vector2Int(x, y);
    }

    public void RenderMap(MapPayload mapData) {
        map = mapData.map;
        unitGrid = new UnitView[map.w, map.h];

        tilemap.ClearAllTiles();

        for (int x = 0; x < map.w; x++) {
            for (int y = 0; y < map.h; y++) {
                Vector3Int pos = pos2unityPos(new Vector2Int(x, y));

                int idx = (int)map.tileArr[x, y].landType;

                tilemap.SetTile(pos, tilePalette[idx]);
            }
        }

        tilemap.RefreshAllTiles();
    }

    public void RenderUnit(NetUnit netUnit) {
        GameObject prefab = null;
        foreach(var pref in unitPrefs) {
            if(pref.name == netUnit.type.ToString()) {
                prefab = pref;
                break;
            }
        }

        if (prefab == null) {
            Debug.LogError($"[MapManager] No prefab for {netUnit.type}");
            return;
        }

        Vector3 worldPos = tilemap.GetCellCenterWorld(pos2unityPos(new Vector2Int(netUnit.x, netUnit.y)));

        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);
        UnitView view = obj.GetComponent<UnitView>();
        view.Initialize(netUnit);

        unitGrid[netUnit.x, netUnit.y] = view;
        activeUnits[netUnit.unitId] = view;

        Debug.Log($"[MapManager] Spawned {netUnit.type} on tile [{netUnit.x}, {netUnit.y}]");
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
        if((prevPos - mouseScreenPos).magnitude < 0.5) {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);

            Vector2Int pos = unityPos2pos(cellPos);

            if (pos.x >= 0 && pos.x < map.w && pos.y >= 0 && pos.y < map.h) {
                Debug.Log($"[Input] Click! Tilemap: {cellPos} -> Arr: [{pos.x}, {pos.y}]");
                UnitView clickedUnit = unitGrid[pos.x, pos.y];

                if (clickedUnit != null) {
                    DeselectUnit();
                    SelectUnit(clickedUnit);
                } else if (selectedUnit != null) {
                    OnMoveIntent?.Invoke(selectedUnit.state.unitId, pos, selectedUnit.state.playerIdx);
                    DeselectUnit();
                }
            }
        }
    }

    private void SelectUnit(UnitView unit) {
        Debug.Log($"[Input] Unit: {unit.name}");
        selectedUnit = unit;
        selectedUnit.Select();
    }

    private void DeselectUnit() {
        if(selectedUnit) {
            selectedUnit.Deselect();
            selectedUnit = null;
        }
    }

    public void MoveUnitVisual(int unitId, int newX, int newY) {
        if (activeUnits.TryGetValue(unitId, out UnitView unit)) {
            unitGrid[unit.state.x, unit.state.y] = null;

            unit.state.x = newX;
            unit.state.y = newY;

            unitGrid[newX, newY] = unit;

            Vector3 worldPos = tilemap.GetCellCenterWorld(pos2unityPos(new Vector2Int(newX, newY)));
            unit.transform.position = worldPos; 
        }
    }
}
