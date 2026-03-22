public enum TileLandType : byte {
    MIN = 0,
    WATER = 0,
    HILL = 1,
    DESERT = 2,
    MAX,
}

public struct TileData {
    public TileLandType landType;
}

public class LocalMap {
    public TileData[,] tileArr;
    public int w;
    public int h;

    public LocalMap(int width, int height) {
        w = width;
        h = height;
        tileArr = new TileData[w, h];
    }
}
