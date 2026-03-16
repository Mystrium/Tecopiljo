using UnityEngine;

public class CameraControls : MonoBehaviour {
    private Vector3 dragOrigin;
    private Camera cam;

    void Awake() { cam = Camera.main; }

    void Update() {
        if (Input.GetMouseButtonDown(0))
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0)) {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += difference;
        }
    }
}
