using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    private float playerSpeed = 5f;
    private Rigidbody2D rb;
    private float moveInputX;

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log("We are in Start function");
    }

    private void Update() {
        moveInputX = Input.GetAxis("Horizontal"); // (-1, 0) or (1, 0)
        rb.linearVelocity = new Vector2(moveInputX * playerSpeed, rb.linearVelocity.y);

        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log("Jumping now");
            rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
        }
    }
}
