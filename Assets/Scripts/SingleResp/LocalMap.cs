using System.IO;

public enum TileLandType : byte {
    MIN = 0,
    WATER = 0,
    HILL = 1,
    DESERT = 2,
    MAX,
}

public struct TileData {
    public TileLandType landType;
    // Maybe add an array of units?
    public Unit? unit;

    public static void Pack(BinaryWriter w, TileData t) {
        w.Write((byte)t.landType);
        Unit.Pack(w, t.unit);
    }

    public static TileData Unpack(BinaryReader r) {
        TileData result = new TileData();

        result.landType = (TileLandType)r.ReadByte();
        result.unit = Unit.Unpack(r);

        return result;
    }
}

public class TilePos {
    public int x;
    public int y;
};

public class LocalMap {
    public TileData[,] tileArr;
    public int w;
    public int h;

    public LocalMap(int width, int height) {
        w = width;
        h = height;
        tileArr = new TileData[w, h];
    }

    public void moveUnit(TilePos fromPos, TilePos toPos) {
        Unit u = tileArr[fromPos.x, fromPos.y].unit;
        if (u == null) {
            return;
        }

        tileArr[fromPos.x, fromPos.y].unit = null;
        tileArr[toPos.x, toPos.y].unit = u;
    }

    public static void Pack(BinaryWriter w, LocalMap map) {
        w.Write(map.w);
        w.Write(map.h);

        foreach (TileData t in map.tileArr) {
            TileData.Pack(w, t);
        }
    }

    public static LocalMap Unpack(BinaryReader r) {
        int w = r.ReadInt32();
        int h = r.ReadInt32();

        LocalMap result = new LocalMap(w, h);

        for (int i = 0; i < w; i++) {
            for (int j = 0; j < h; j++) {
                result.tileArr[i, j] = TileData.Unpack(r);
            }
        }

        return result;
    }
}
