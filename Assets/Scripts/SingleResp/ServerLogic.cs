using System.Collections.Generic;
using UnityEngine;
using System;

public class ServerLogic  {
    private LocalMap map;

    private int nextUnitId = 1;
    public Dictionary<int, NetUnit> activeUnits = new Dictionary<int, NetUnit>();

    // https://youtu.be/ac4E_UsmB1g?si=1cUgCgYAVqSsnJq5
    public readonly int[] landTypeCoef = new int[(int)TileLandType.MAX]{// TODO: This probably should be in MapInitPayload
        1, // Water
        5, // Hill
        2, // Desert
    };

    public NetMsg ProcessRequest(ServerConnectionMsg cmsg) {
        NetMsg result = new NetMsg();
        result.header.type = NetType.Error;
        result.payload = new byte[0]; // This is stupid. // ok, and ?

        try {
            switch (cmsg.msg.header.type) {
                case NetType.MapInit:
                    result = InnitMap(result, cmsg);
                    break;

                case NetType.InnitUnit:
                    result = SpawnUnit(result, cmsg);
                    break;

                case NetType.MoveUnit:
                    result = MoveUnit(result, cmsg);
                    break;

                default:
                    Debug.LogWarning($"[Server] Unknown message type: {cmsg.msg.header.type}");
                    break;
            }
        } catch (Exception ex) {
            Debug.LogError($"[Server] Помилка обробки: {ex.Message}");
        }

        result.header.size = (uint)result.payload.Length;
        return result;
    }

    private NetMsg InnitMap(NetMsg msg, ServerConnectionMsg income) {
        var mapInit = MapInitPayload.Unpack(income.msg.payload);

        map = new LocalMap(mapInit.w, mapInit.h);
        MapGenerator.Generate(map, landTypeCoef);

        Debug.Log("[Server] Generating map: (" + mapInit.w + ":" + mapInit.h + ")");

        var mapPayload = new MapPayload { map = map };
        msg.header.type = NetType.Map;
        msg.payload = MapPayload.Pack(mapPayload);
        return msg;
    }

    // player render map -> player wonts start unit -> send to server id and pos by sector -> server spawns virtual unit, broadcast unit data -> all players render unit
    private NetMsg SpawnUnit(NetMsg msg, ServerConnectionMsg income) {
        var newUnit = UnitPayload.Unpack(income.msg.payload);
        var stats = UnitStats.GetBaseStats(newUnit.type);

        NetUnit toSpawn = new NetUnit(nextUnitId++, newUnit.type, newUnit.playerIdx, newUnit.x, newUnit.y, stats.hp, stats.mobility);
        
        activeUnits.Add(toSpawn.unitId, toSpawn);

        msg.header.type = NetType.SpawnUnit;
        msg.payload = NetUnit.Pack(toSpawn);
        return msg;
    }

    private NetMsg MoveUnit(NetMsg msg, ServerConnectionMsg income) {
        MoveUnitPayload request = MoveUnitPayload.Unpack(income.msg.payload);

        if (!activeUnits.TryGetValue(request.unitId, out NetUnit unit)) {
            Debug.LogWarning($"[Server] Move Error: Unit {request.unitId} not found.");
            return GenerateErrorMsg($"Unit {request.unitId} not found.");
        }

        if (request.x < 0 || request.x >= map.w || request.y < 0 || request.y >= map.h) {
            Debug.LogWarning($"[Server] Move Error: Coords [{request.x}, {request.y}] out of bounds.");
            return GenerateErrorMsg($"Coords [{request.x}, {request.y}] out of bounds.");
        }

        int dx = request.x - unit.x;
        int dy = request.y - unit.y;

        // fix to rombus dist
        int distance = (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dx + dy)) / 2;

        if (distance != 1) {
            Debug.LogWarning($"[Server] Move Error: Long {distance} move.");
            return GenerateErrorMsg($"Long {distance} move.");
        }

        foreach (var existingUnit in activeUnits.Values) {
            if (existingUnit.x == request.x && existingUnit.y == request.y) {
                Debug.LogWarning($"[Server] Move Error: Tile [{request.x}, {request.y}] ocupied {existingUnit.unitId}.");
                return GenerateErrorMsg($"Tile [{request.x}, {request.y}] ocupied {existingUnit.unitId}.");
            }
        }

        unit.x = request.x;
        unit.y = request.y;

        Debug.Log($"[Server] Unit {unit.unitId} can move to [{unit.x}, {unit.y}].");

        msg.header.type = NetType.MoveUnit;
        msg.payload = MoveUnitPayload.Pack(request);
        return msg;
    }

    private NetMsg GenerateErrorMsg(string errorText) {
        var errorData = new ErrorPayload { message = errorText };
        byte[] payloadBytes = ErrorPayload.Pack(errorData);

        return new NetMsg {
            header = new NetMsgHeader { 
                type = NetType.Error, 
                size = (uint)payloadBytes.Length 
            },
            payload = payloadBytes
        };
    }
}
