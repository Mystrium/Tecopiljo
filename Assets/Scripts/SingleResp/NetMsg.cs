using System.IO;

// These probably should be split for client and server...
public enum NetType : byte {
    // player related messages
    PlayerJoined = 1,
    PlayerDisconnected = 2,

    // map related messages
    MapInit = 10,
    Map = 11,

    // units
    InnitUnit = 20,
    SpawnUnit = 21,
    MoveUnit = 22,
    AttackUnit = 23,

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
        NetMsgHeader result = new NetMsgHeader {
            size = reader.ReadUInt32(),
            type = (NetType)reader.ReadInt32()
        };

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

public struct ErrorPayload {
    public string message;

    public static byte[] Pack(ErrorPayload data) {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms)) {
            // BinaryWriter автоматично записує довжину рядка перед самим рядком,
            // тому це 100% безпечно для мережі.
            writer.Write(data.message ?? "Unknown Error");
            return ms.ToArray();
        }
    }

    public static ErrorPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms)) {
            return new ErrorPayload {
                message = reader.ReadString()
            };
        }
    }
}

public struct PlayerJoinedPayload {
    public int playerId;

    public static byte[] Pack(PlayerJoinedPayload data) {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms)) {
            writer.Write(data.playerId);
            return ms.ToArray();
        }
    }

    public static PlayerJoinedPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms)) {
            return new PlayerJoinedPayload {
                playerId = reader.ReadInt32()
            };
        }
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

public struct MoveUnitPayload {
    public int unitId;
    public int x;
    public int y;

    public static byte[] Pack(MoveUnitPayload data) {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms)) {
            writer.Write(data.unitId);
            writer.Write(data.x);
            writer.Write(data.y);
            return ms.ToArray();
        }
    }

    public static MoveUnitPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var reader = new BinaryReader(ms)) {
            return new MoveUnitPayload {
                unitId = reader.ReadInt32(),
                x = reader.ReadInt32(),
                y = reader.ReadInt32()
            };
        }
    }
}

public struct AttackIntentPayload {
    public int attackerId;
    public int targetId;

    public static byte[] Pack(AttackIntentPayload data) {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms)) {
            w.Write(data.attackerId);
            w.Write(data.targetId);
            return ms.ToArray();
        }
    }

    public static AttackIntentPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms)) {
            return new AttackIntentPayload {
                attackerId = r.ReadInt32(),
                targetId = r.ReadInt32()
            };
        }
    }
}

public struct AttackResultPayload {
    public int attackerId;
    public int targetId;
    public int newHp;

    public static byte[] Pack(AttackResultPayload data) {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms)) {
            w.Write(data.attackerId);
            w.Write(data.targetId);
            w.Write(data.newHp);
            return ms.ToArray();
        }
    }

    public static AttackResultPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms)) {
            return new AttackResultPayload {
                attackerId = r.ReadInt32(),
                targetId = r.ReadInt32(),
                newHp = r.ReadInt32()
            };
        }
    }
}


public struct SpawnIntentPayload {
    public int spawnerId;
    public int unitType;
    public int x;
    public int y;

    public static byte[] Pack(AttackIntentPayload data) {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms)) {
            w.Write(data.attackerId);
            w.Write(data.targetId);
            return ms.ToArray();
        }
    }

    public static AttackIntentPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms)) {
            return new AttackIntentPayload {
                attackerId = r.ReadInt32(),
                targetId = r.ReadInt32()
            };
        }
    }
}