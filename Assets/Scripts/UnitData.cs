using UnityEngine;

public enum UnitType {
    NULL, // NULL value
    TEST,
};

public class UnitData : MonoBehaviour {
    public UnitType type; // Needs to be unique for every
    public string unitName;
    public int baseMaxHealth;
    public int baseAttack;
    public int baseMobility;
}
