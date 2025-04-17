using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour {
    [Header("Movement Parameters")]
    [SerializeField] private float playerSpeed = 8f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float trapSlowMultiplier = 0.5f;

    [Header("Tilemap References")]
    [SerializeField] private Tilemap climbTilemap;
    [SerializeField] private Tilemap platformTilemap; // I thought I would use this somewhere, but for now I don't think I will.
    [SerializeField] private Tilemap trapTilemap;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private int maxAddtionalJumps = 1;
    [SerializeField] private LayerMask groundLayer; // Adding a layer mask to specify what counts as ground. You will need to assign ground layer to the platform tilemap in Unity

    [Header("Wall Slide Settings")]
    /* Let me explain why we need this section.
     * So wall slide is a mechanic where the player can slide down a wall when they are not grounded.
     * Why do we need this? Because when the player holds down a movement key against a collider (the wall in this case), the horizontal force counteracts gravity, causing the player to stick to the wall instead of sliding down.
     * To get an idea of how this works, try playing Celeste or Hollow Knight. You will see that the player can slide down walls when the player is not grounded.
     * You can get an idea of what will happen if this mechanic is missing. Just comment out the wall slide related code snippets and then run the game. You will understand how annoying it is.
     */
    [SerializeField] private float wallCheckDistance = 0.05f;
    [SerializeField] private bool enableWallSlide = true;

    private Rigidbody2D rb;
    private float moveInputX;
    private float moveInputY;
    private bool isOnLadder = false;
    private bool isOnTrap = false;
    private float gravityScale;
    private bool isGrounded;
    private int jumpCount = 0; // For double jump limitation
    private bool isSprinting = false;
    private bool isTouchingWall = false; // For wall slide mechanic
    private int wallDirection = 0; // 1 for right, -1 for left, 0 for none

    private void Start() { // called at the beginning of the game
        rb = GetComponent<Rigidbody2D>();
        gravityScale = rb.gravityScale;
        Debug.Log("We are in Start function");

        Collider2D playerCollider = GetComponent<Collider2D>(); // Getting the player's collider. Make sure that is has a frictionless physics material.
        if (playerCollider != null && playerCollider.sharedMaterial == null) {
            Debug.LogWarning("Player collider has no Physics Material 2D. Create one with zero friction and assign it to the player collider.");
        }

        /* Physics material? What's that?
         * A physics material is a component that defines the friction and bounciness of a collider. In this case, we want to create a physics material with zero friction to allow smooth sliding on walls.
         * You can create a new Physics Material 2D in Unity by right-clicking in the Project window, selecting Create > Physics Material 2D, and then setting the friction to 0.
         * Assign this material to the player's collider in the Inspector.
         */
    }

    private void Update() { // called at every frame
        CheckGrounded(); // Check if player is grounded or not. This is important for jumping and double jumping.
        CheckWalls(); // Check if player is touching a wall or not. This is important for wall sliding.

        CheckTrap(); // Check if player is on a trap or not. This is important for slowing down the player when on a trap.

        moveInputX = Input.GetAxis("Horizontal");
        moveInputY = Input.GetAxis("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isOnTrap; // Checking if left shift key is pressed or not, to allow sprinting. The player should also not be on a trap.

        if (isOnTrap && Input.GetKeyDown(KeyCode.LeftShift)) {
            Debug.Log("Player is on a trap, sprinting disabled");
        }
        
        if (isGrounded) {
            jumpCount = 0; // Reset jump count when player is grounded
            Debug.Log($"Jump count reset to {jumpCount}");
        }

        CheckLadder(); // As the name suggests, check if player is on a ladder or not
        HandleMovement(); // Then, handle movement accordingly.

        if (Input.GetKeyDown(KeyCode.Space)) { // Wwe want to allow jumping only when the player is not on a trap and is not on a ladder. We will also check if the player is grounded or not.
            if (isOnTrap) {
                Debug.Log("Player is on a trap, jump disabled");
            }
            else if (jumpCount < maxAddtionalJumps && !isOnLadder) {
                Jump();
            }
        }
    }

    private void CheckGrounded() {
        // We will cast a ray from the feet of the player rather than the center. What's a ray? Read ahead.
        Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - 0.5f * GetComponent<Collider2D>().bounds.size.y);

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            groundCheckDistance,
            groundLayer // This is the layer mask we added to specify what counts as ground.
        );
        /* Whoa there's a lot to cover here.
         * RaycastHit2D is a structure that stores information about the raycast hit.
         * What is raycasting? To put it simply, raycasting is a technique used to detect objects in a straight line. 'Rays' are cast from a point in a specified direction, and if they hit an object, the information about that hit is stored in a RaycastHit2D object.
         * Physics2D.Raycast() is a method that casts a ray in the specified direction (in this case, downwards) from the given position (rayOrigin) and checks if it hits any colliders within the specified distance (groundCheckDistance).
         * So what are we storing in the RacastHit2D object 'hit'? Well, it stores information about the object that the ray hit, including its position, rotation, and the collider that was hit.
         * We will use this information to check if the player is grounded or not.
         */

        isGrounded = hit.collider != null; // If the ray hits a collider, isGrounded will be true, else false. So if the player is touching the ground, isGrounded will be true.
    }

    private void CheckWalls() {
        if (!enableWallSlide) return; // If wall slide is not enabled, we don't need to check for walls.

        float width = GetComponent<Collider2D>().bounds.extents.x + 0.05f; // Adding a small offset to the width to ensure we check the wall correctly.

        RaycastHit2D hitRight = Physics2D.Raycast( // Check for walls on the right side
            transform.position,
            Vector2.right,
            width + wallCheckDistance,
            groundLayer
        );

        RaycastHit2D hitLeft = Physics2D.Raycast( // Check for walls on the left side
            transform.position,
            Vector2.left,
            width + wallCheckDistance,
            groundLayer
        );

        if (hitRight.collider != null && moveInputX > 0) { // If the player is touching a wall on the right side and is moving right, we will set the wallDirection to 1.
            isTouchingWall = true;
            wallDirection = 1;
        }
        else if (hitLeft.collider != null && moveInputX < 0) { // If the player is touching a wall on the left side and is moving left, we will set the wallDirection to -1.
            isTouchingWall = true;
            wallDirection = -1;
        }
        else { // If the player is not touching any wall, we will set the wallDirection to 0.
            isTouchingWall = false;
            wallDirection = 0;
        }
    }

    private void Jump() {
        jumpCount++;
        Debug.Log($"Jump {jumpCount} of {maxAddtionalJumps}");

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity to 0 before applying jump force. This is important to ensure a consistent jump height.
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void CheckLadder() {
        Vector3Int cellPosition = climbTilemap.WorldToCell(transform.position);
        /* What's happening here? 
         * WorldToCell converts the world position of the player to the cell position in the tilemap.
         * climbTilemap.WorldToCell(position) returns a Vector3Int (integer vector of 3 dimensions) representing the cell position in the climbTilemap.
         */

        isOnLadder = climbTilemap.HasTile(cellPosition);
        /* In cellposition is stored the position of the player in the tilemap (so the position of a cell).
         * climbTilemap.HasTile() checks if there is a tile of climbTilemap at the given cell position. Returns true if present, else false.
         */

        // Let's adjust the physics of the player when on ladder accordingly.
        if (isOnLadder) { // If the player is on a ladder tile, the player should be allowed to move up and down the ladder. So we will disable gravity, else the player will just fall down.
            rb.gravityScale = 0f;
        }
        else {
            rb.gravityScale = gravityScale; // We will reset to normal gravity when the player is not on a ladder.
        }
    }

    private void CheckTrap() {
        // Similar implementation to CheckLadder. Try doing it on your own if you've understood the CheckLadder() function.
        Vector3Int cellPosition = trapTilemap.WorldToCell(transform.position);
        isOnTrap = trapTilemap.HasTile(cellPosition);

        if (isOnTrap) {
            Debug.Log("Player is on a trap");
        }
    }

    private void HandleMovement() {
        float baseSpeed = playerSpeed;

        if (isOnTrap) {
            baseSpeed *= trapSlowMultiplier;
        }

        // Apply sprint speed multiplier if sprinting
        float currentSpeed = isSprinting ? baseSpeed * sprintSpeedMultiplier : baseSpeed; // We have used the ternary operator here.

        if (isOnLadder) {
            Vector2 climbVelocity = new Vector2(moveInputX * currentSpeed, moveInputY * climbSpeed); // So moveInputY comes into play only when player is on ladder.
            rb.linearVelocity = climbVelocity;
        }
        else { // Handling Wall sliding
            if (isTouchingWall && !isGrounded) {
                /* Player is pressing against a wall and not grounded.
                 * Allow sliding down and limit horizontal velocity.
                 */
                float newXVelocity = 0;

                if ((wallDirection == 1 && moveInputX < 0) || (wallDirection == -1 && moveInputX > 0)) { // If player is moving away from the wall, allow movement
                    newXVelocity = moveInputX * currentSpeed;
                }

                rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y); // Apply movement but maintain downward velocity
            }
            else {
                rb.linearVelocity = new Vector2(moveInputX * currentSpeed, rb.linearVelocity.y); // No vertical movement when the player is not on a ladder.
            }
        }
    }
}
