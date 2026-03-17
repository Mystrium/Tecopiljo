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
    public int w;
    public int h;
    public byte[,] mapBytes; 

    public static byte[] Pack(MapPayload map) {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms)) {
            writer.Write(map.w);  
            writer.Write(map.h); 
            
            for (int x = 0; x < map.w; x++)
                for (int y = 0; y < map.h; y++)
                    writer.Write(map.mapBytes[x, y]);

            return ms.ToArray();
        }
    }

    public static MapPayload Unpack(byte[] payloadData) {
        using (MemoryStream ms = new MemoryStream(payloadData))
        using (BinaryReader reader = new BinaryReader(ms)) {
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();

            byte[,] receivedMap = new byte[w, h];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    receivedMap[x, y] = reader.ReadByte();

            return new MapPayload{ w = w, h = h, mapBytes = receivedMap };
        }
    }
}