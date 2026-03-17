public class LocalMap {
    public byte[,] CurrentMap { get; private set; } 
    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }

    public LocalMap(int w, int h) {
        MapWidth = w;
        MapHeight = h;
        CurrentMap = new byte[w, h];
    }

    public void GenerateMap(byte maxInd) {
        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                CurrentMap[x, y] = (byte)UnityEngine.Random.Range(0, maxInd);
    }
}