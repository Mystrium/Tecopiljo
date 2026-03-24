using UnityEngine;
using System.IO;

public class UnitData : MonoBehaviour {
    [Header("Important")]
    public string unitName;
    public int baseMaxHealth;
    public int baseAttack;
    // TODO: this probably should be an array of values or smth if roads/railroads will be added
    public int baseMobility;

    [Header("Changed by game")]
    public uint playerIdx;

    public int health;
    public int curMobility;

    public void Awake() {
        health = baseMaxHealth;
    }

    public static void Pack(BinaryWriter w, UnitData? unit) {
        w.Write(unit == null);

        if (unit is UnitData u) {
            w.Write(unit.unitName);
            w.Write(unit.baseMaxHealth);
            w.Write(unit.baseAttack);
            w.Write(unit.baseMobility);

            w.Write(unit.playerIdx);
            w.Write(unit.health);
            w.Write(unit.curMobility);
        }
    }

    public static UnitData? Unpack(BinaryReader r) {
        bool isNull = r.ReadBoolean();
        UnitData result = new UnitData();

        if (isNull) {
            return null;
        }

        result.unitName = r.ReadString();
        result.baseMaxHealth = r.ReadInt32();
        result.baseAttack = r.ReadInt32();
        result.baseMobility = r.ReadInt32();

        result.playerIdx = r.ReadUInt32();
        result.health = r.ReadInt32();
        result.curMobility = r.ReadInt32();

        return result;
    }
}
