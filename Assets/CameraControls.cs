using UnityEngine;

public class CameraControls : MonoBehaviour {
    public Camera mainCamera;
    private float holdingTime;
    private const float MIN_HOLDING_TIME = 0.1f;
    private const float CAMERA_SPEED = 0.8f;

    void Start() {
        
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            holdingTime += Time.deltaTime;
        } else {
            holdingTime = 0;
        }

        if (holdingTime > MIN_HOLDING_TIME) {
            mainCamera.transform.position += -Input.mousePositionDelta * Time.deltaTime * CAMERA_SPEED;
        }
    }
}
