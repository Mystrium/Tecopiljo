using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class Unit {
    public UnitData data;

    public int playerIdx;
    public int curHealth;
    public int curMobility;

    public static Dictionary<UnitType, UnitData> type2data = new Dictionary<UnitType, UnitData>()
    {
        { UnitType.TEST, Resources.Load<UnitData>("Units/TestUnit") },
    };

    public Unit(UnitType _type, int _playerIdx) {
        UnitData _data = GameObject.Instantiate(type2data[_type]);
        data = _data;
        curHealth = data.baseMaxHealth;
        curMobility  = data.baseMobility;
        playerIdx = _playerIdx;
    }

    public Unit(UnitData _data, int _playerIdx) {
        data = _data;
        curHealth = data.baseMaxHealth;
        curMobility  = data.baseMobility;
        playerIdx = _playerIdx;
    }

    public void startTurn() {
        curMobility = data.baseMobility;
    }

    public static void Pack(BinaryWriter w, Unit? unit) {
        if (unit == null) {
            w.Write((int)UnitType.NULL);
            return;
        }

        if (unit is Unit u) {
            w.Write((int)u.data.type);

            w.Write(u.playerIdx);
            w.Write(u.curHealth);
            w.Write(u.curMobility);
        }
    }

    public static Unit? Unpack(BinaryReader r) {
        UnitType type = (UnitType)r.ReadInt32();
        if (type == UnitType.NULL) {
            return null;
        }

        int playerIdx = r.ReadInt32();

        Unit result = new Unit(type, playerIdx);

        result.curHealth = r.ReadInt32();
        result.curMobility = r.ReadInt32();

        return result;
    }
}
