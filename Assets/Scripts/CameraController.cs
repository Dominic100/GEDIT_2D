using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The player to follow
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10); // Default offset for 2D games

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 0.125f; // Lower = smoother but slower camera
    [SerializeField] private bool followX = true; // Follow on X axis
    [SerializeField] private bool followY = true; // Follow on Y axis

    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 10f;

    private Vector3 desiredPosition;
    private Vector3 smoothedPosition;

    private void Start() {
        // If target isn't assigned, try to find the player automatically
        if (target == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Make sure to assign the 'Player' tag to your player game object.
            if (player != null) {
                target = player.transform;
                Debug.Log("CameraController: Player found automatically");
            }
            else {
                Debug.LogWarning("CameraController: No target assigned and no Player tag found");
            }
        }
    }

    private void FixedUpdate() {
        if (target == null)
            return;

        // Calculating the position the camera should move toward
        desiredPosition = target.position + offset;

        // Maintain current position for axes we don't want to follow
        if (!followX) desiredPosition.x = transform.position.x;
        if (!followY) desiredPosition.y = transform.position.y;

        // Apply boundaries if enabled
        if (useBoundaries) {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smoothly move the camera towards the target position
        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        // Learn more about Lerp: https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html

        // Always keep the same Z position (camera depth)
        smoothedPosition.z = offset.z;

        // Update camera position
        transform.position = smoothedPosition;
    }
}
