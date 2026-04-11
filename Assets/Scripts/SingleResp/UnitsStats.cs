public class UnitStats {
    public static (int hp, int mobility) GetBaseStats(UnitType type) {
        switch (type) {
            case UnitType.Worker: return (hp: 100, mobility: 3);
            case UnitType.TestUnit: return (hp: 50, mobility: 5);
            // another units and buildings
            default: return (10, 1);
        }
    }
}