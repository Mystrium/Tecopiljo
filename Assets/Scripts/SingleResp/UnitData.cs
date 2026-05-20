using System.IO;

public enum UnitType : byte {
    NULL = 0,
    Worker = 1,
    Fighter = 2
}

public struct UnitPayload { // from client
    public UnitType type;
    public int playerIdx;
    public int spawnerId;

    public int x;
    public int y;

    public static byte[] Pack(UnitPayload unit) {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms)) {
            w.Write((byte)unit.type);
            w.Write(unit.playerIdx);
            w.Write(unit.spawnerId);
            w.Write(unit.x);
            w.Write(unit.y);
            
            return ms.ToArray();
        }
    }

    public static UnitPayload Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms)) {
            return new UnitPayload {
                type = (UnitType)r.ReadByte(),
                playerIdx = r.ReadInt32(),
                spawnerId = r.ReadInt32(),
                x = r.ReadInt32(),
                y = r.ReadInt32(),
            };
        }
    }
}


public class NetUnit { // for server
    public int unitId;
    public UnitType type;
    public int playerIdx;

    public int x;
    public int y;

    public int curHealth;
    public int curMobility;

    public NetUnit(int id, UnitType type, int player, int x, int y, int maxHealth, int maxMobility) {
        this.unitId = id;
        this.type = type;
        this.playerIdx = player;
        this.x = x;
        this.y = y;
        this.curHealth = maxHealth;
        this.curMobility = maxMobility;
    }

    private NetUnit() {}

    public static byte[] Pack(NetUnit unit) {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms)) {
            w.Write((byte)unit.type);
            w.Write(unit.unitId);
            w.Write(unit.playerIdx);
            w.Write(unit.x);
            w.Write(unit.y);
            w.Write(unit.curHealth);
            w.Write(unit.curMobility);
            
            return ms.ToArray();
        }
    }

    public static NetUnit Unpack(byte[] data) {
        using (var ms = new MemoryStream(data))
        using (var r = new BinaryReader(ms)) {
            return new NetUnit {
                type = (UnitType)r.ReadByte(),
                unitId = r.ReadInt32(),
                playerIdx = r.ReadInt32(),
                x = r.ReadInt32(),
                y = r.ReadInt32(),
                curHealth = r.ReadInt32(),
                curMobility = r.ReadInt32()
            };
        }
    }
}