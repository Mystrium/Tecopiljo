using UnityEngine;

public class UnitView : MonoBehaviour {
    public NetUnit state;

    public void Initialize(NetUnit serverData) {
        this.state = serverData;
        UpdateVisuals();
    }

    public void UpdateData(NetUnit newData) {
        this.state = newData;
        UpdateVisuals();
    }

    private void UpdateVisuals() {
        // healthBar.SetValue(state.curHealth);
        // some unit rerender
        Debug.Log($"Unit {state.unitId} updated. HP: {state.curHealth}");
    }

    public void Select() {
        transform.localScale = new Vector3(1.1f, 1.1f, 1);
        transform.position += new Vector3(0, 0.2f, 0);
    }

    public void Deselect() {
        transform.localScale = new Vector3(1, 1, 1);
        transform.position -= new Vector3(0, 0.2f, 0);
    }
}