using System.IO;
using System;

public static class NetMsg {
    public static byte[] Pack(NetType type, byte[] payloadData) {
        if (payloadData == null) payloadData = new byte[0];

        byte[] finalData = new byte[payloadData.Length + 1];

        finalData[0] = (byte)type;

        Buffer.BlockCopy(payloadData, 0, finalData, 1, payloadData.Length);

        return finalData;
    }

    public static NetType GetType(byte[] data) {
        return (NetType)data[0];
    }

    public static byte[] GetPayload(byte[] data) {
        if (data.Length <= 1) return new byte[0];

        byte[] payload = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, payload, 0, payload.Length);
        return payload;
    }
}

public enum NetType : byte { // --- messages types ---
    PlayerJoined = 1,
    Move = 2,
    Size = 3,
    Map = 4,
}


// --- messages structs ---
public struct CoordPayload {
    public int x;
    public int y;

    public static byte[] Pack(CoordPayload coords) {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms)) {
            writer.Write(coords.x);
            writer.Write(coords.y);
            return ms.ToArray();
        }
    }

    public static CoordPayload Unpack(byte[] payloadData) {
        using (MemoryStream ms = new MemoryStream(payloadData))
        using (BinaryReader reader = new BinaryReader(ms)) {
            return new CoordPayload {
                x = reader.ReadInt32(),
                y = reader.ReadInt32()
            };
        }
    }
}

public struct MapPayload {
    public LocalMap map;

    public static byte[] Pack(MapPayload p) {
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter writer = new BinaryWriter(ms)) {
                writer.Write(p.map.w);
                writer.Write(p.map.h);

                for (int x = 0; x < p.map.w; x++) {
                    for (int y = 0; y < p.map.h; y++) {
                        writer.Write((byte)p.map.tileArr[x, y].landType);
                    }
                }
            }
            return ms.ToArray();
        }
    }

    public static MapPayload Unpack(byte[] payloadData) {
        MapPayload result = new MapPayload();

        using (MemoryStream ms = new MemoryStream(payloadData)) {
            using (BinaryReader reader = new BinaryReader(ms)) {
                int w = reader.ReadInt32();
                int h = reader.ReadInt32();

                result.map = new LocalMap(w, h);

                for (int x = 0; x < w; x++) {
                    for (int y = 0; y < h; y++) {
                        result.map.tileArr[x, y].landType = (TileLandType)reader.ReadByte();
                    }
                }
            }
        }

        return result;
    }
}

public struct MapInitPayload {
    public int w;
    public int h;

    public static byte[] Pack(MapInitPayload data) {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms)) {
            writer.Write(data.w);
            writer.Write(data.h);
            return ms.ToArray();
        }
    }

    public static MapInitPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms)) {
            return new MapInitPayload {
                w = reader.ReadInt32(),
                h = reader.ReadInt32(),
            };
        }
    }
}
