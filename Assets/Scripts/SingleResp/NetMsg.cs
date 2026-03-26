using System.IO;
using System;

// These probably should be split for client and server...
public enum NetType : byte {
    PlayerJoined = 1,
    PlayerDisconnected = 2,
    // other player related messages
    MapInit = 10,
    Map = 11,
    // other map related messages
    Error = 255,
}

public struct NetMsgHeader {
    public uint size;
    public NetType type;

    public static byte[] Pack(NetMsgHeader head) {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms)) {
            writer.Write(head.size);
            writer.Write((int)head.type);
            return ms.ToArray();
        }
    }

    public static NetMsgHeader Unpack(BinaryReader reader) {
        NetMsgHeader result = new NetMsgHeader();

        result.size = reader.ReadUInt32();
        result.type = (NetType)reader.ReadInt32();

        return result;
    }

    public static NetMsgHeader Unpack(byte[] data) {
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms)) {
            return Unpack(reader);
        }
    }
}

// TODO: Somehow turn this into a union or smth similar
public struct NetMsg {
    public NetMsgHeader header;
    public byte[] payload;

    public static byte[] Pack(NetMsg msg) {
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms)) {
            writer.Write(NetMsgHeader.Pack(msg.header));
            writer.Write(msg.payload);
            return ms.ToArray();
        }
    }

    public static NetMsg Unpack(byte[] data) {
        NetMsg result = new NetMsg();

        using (MemoryStream ms = new MemoryStream(data)) {
            using (BinaryReader reader = new BinaryReader(ms)) {
                result.header = NetMsgHeader.Unpack(reader);
                result.payload = reader.ReadBytes((int)result.header.size);
            }
        }

        return result;
    }
}

public struct MapPayload {
    public LocalMap map;

    public static byte[] Pack(MapPayload p) {
        using (MemoryStream ms = new MemoryStream()) {
            using (BinaryWriter writer = new BinaryWriter(ms)) {
                LocalMap.Pack(writer, p.map);
            }
            return ms.ToArray();
        }
    }

    public static MapPayload Unpack(byte[] payloadData) {
        MapPayload result = new MapPayload();

        using (MemoryStream ms = new MemoryStream(payloadData)) {
            using (BinaryReader reader = new BinaryReader(ms)) {
                result.map = LocalMap.Unpack(reader);
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
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();

            return new MapInitPayload {
                w = w,
                h = h,
            };
        }
    }
}
