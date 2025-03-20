using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float playerSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float gravityValue;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float groundCheckDistance;

    [Header("Animation & Aiming")]
    [SerializeField] private Transform hipBone;
    [SerializeField] private Transform leftLegBone;
    [SerializeField] private Transform rightLegBone;
    [SerializeField] private AimConstraint upperBodyAimConstraint;
    [SerializeField] private float transitionSpeed = 5f;  // For aim layer weight

    [Header("Jump & Landing Settings")]
    [SerializeField] private float fallThreshold = -2f; // When below this, consider jump "big" enough to fall.
    [SerializeField] private float minAirTime = 0.3f;
    [SerializeField] private float landingDuration = 0.3f;
    [SerializeField] private float jumpCooldown = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;     // Buffer to allow jump shortly after leaving ground.
    [SerializeField] private float jumpLockDuration = 0.3f; // Prevents immediate reactivation.

    [Header("Speed Modifiers")]
    [SerializeField] private float aimingSpeedMultiplier = 0.5f;  // Slows movement when aiming
    [SerializeField] private float forwardBoostMultiplier = 1.5f;

    private CharacterController controller;
    private Vector2 movement;
    private Vector2 aim;
    private Vector3 playerVelocity;
    private PlayerControls playerControls;
    private PlayerInput playerInput;
    private Animator animator;

    public bool isAiming;
    public bool isGrounded;
    private bool wasGrounded;

    [SerializeField] private float smoothTimeMoving = 0.2f;
    [SerializeField] private float smoothTimeStopping = 0.05f;
    private Vector3 smoothVelocity = Vector3.zero;

    private InputAction aimingAction;
    private InputAction jumpAction;

    private float currentWeightAim = 0f;

    // Jump/landing timing variables.
    private float jumpStartTime;
    private float lastJumpTime;
    private float lastGroundedTime;
    private float lastAirTime = 0f;
    private float landingTimer = 0f;

    // For variable jump height.
    private bool jumpButtonHeld = false;
    public bool wasHighJump = false;

    // Used in UpdateAnimator to determine landing state.
    private int groundHitCount = 0;

    private float originalStepOffset;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerControls = new PlayerControls();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        aimingAction = playerControls.Controls.Aiming;
        aimingAction.started += OnRightClickPressed;
        aimingAction.canceled += OnRightClickCancel;

        jumpAction = playerControls.Controls.Jump;
        jumpAction.performed += ctx => OnJump();
        jumpAction.canceled += OnJumpReleased;

        originalStepOffset = controller.stepOffset;
    }

    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    void Update()
    {
        // Get raw ground detection.
        bool rawGrounded = IsGrounded();
        // Determine if we're within our jump lock period.
        bool jumpLockActive = (Time.time - jumpStartTime < jumpLockDuration);
        // If in jump lock, force isGrounded to be false.
        isGrounded = jumpLockActive ? false : rawGrounded;

        // If we just landed (and not in jump lock), reset velocity and timers.
        if (isGrounded && !wasGrounded)
        {
            lastGroundedTime = Time.time;
            playerVelocity = Vector3.zero;
            controller.stepOffset = originalStepOffset;
            landingTimer = landingDuration;
            jumpStartTime = 0f;
            lastAirTime = 0f;
            // Force a slight downward movement to snap the character to the ground.
            controller.Move(Vector3.down * 0.2f);
        }

        // Gravity accumulation.
        if (isGrounded)
        {
            playerVelocity.y = -0.1f;
        }
        else
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
            playerVelocity.y = Mathf.Max(playerVelocity.y, -50f);
            lastAirTime = Time.time;
        }

        // Decrement landing timer when grounded.
        if (isGrounded && landingTimer > 0f)
        {
            landingTimer -= Time.deltaTime;
            if (landingTimer < 0f)
                landingTimer = 0f;
        }

        HandleInput();
        HandleMovement();
        HandleRotation();
        UpdateAnimator();

        // Update aim layer weight.
        float targetWeight = isAiming ? 1f : 0f;
        currentWeightAim = Mathf.Lerp(currentWeightAim, targetWeight, Time.deltaTime * transitionSpeed);
        animator.SetLayerWeight(1, currentWeightAim);

        wasGrounded = isGrounded;
    }

    private void LateUpdate()
    {
        if (isAiming)
            upperBodyAimConstraint.weight = Mathf.Lerp(upperBodyAimConstraint.weight, 1f, Time.deltaTime * 10f);
        else
            upperBodyAimConstraint.weight = Mathf.Lerp(upperBodyAimConstraint.weight, 0f, Time.deltaTime * 10f);
    }

    private void OnJump()
    {
        bool canJump = isGrounded || (Time.time - lastGroundedTime < coyoteTime);
        if (canJump && (Time.time - lastJumpTime > jumpCooldown))
        {
            // Prevent rapid reactivation.
            if (Time.time - jumpStartTime < 0.1f)
                return;
            controller.stepOffset = 0f;
            jumpStartTime = Time.time;
            jumpButtonHeld = true;
            wasHighJump = false;

            // Reset velocity when grounded before jumping.
            if (isGrounded)
            {
                playerVelocity = Vector3.zero;
            }

            // Calculate jump force.
            float jumpForceValue = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            jumpForceValue = Mathf.Clamp(jumpForceValue, 0, Mathf.Abs(gravityValue) * 0.5f);
            playerVelocity.y = jumpForceValue;

            isGrounded = false;
            lastAirTime = Time.time;
            lastJumpTime = Time.time;

            // Reset horizontal movement animations.
            animator.SetFloat("VelocityX", 0);
            animator.SetFloat("VelocityZ", 0);
            animator.SetFloat("Speed", 0);
        }
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpButtonHeld = false;
    }

    // Ground detection using multiple raycasts.
    private bool IsGrounded()
    {
        Vector3 baseOrigin = transform.position + Vector3.up * 0.1f;
        int groundLayerMask = ~LayerMask.GetMask("Player", "Enemies");

        Vector3[] offsets = new Vector3[]
        {
            Vector3.zero,
            new Vector3(controller.radius, 0, 0),
            new Vector3(-controller.radius, 0, 0),
            new Vector3(0, 0, controller.radius),
            new Vector3(0, 0, -controller.radius)
        };

        int hitCount = 0;
        foreach (Vector3 offset in offsets)
        {
            Vector3 origin = baseOrigin + offset;
            if (Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayerMask))
            {
                hitCount++;
            }
        }
        groundHitCount = hitCount;
        return hitCount >= 2;
    }

    private void OnRightClickPressed(InputAction.CallbackContext context)
    {
        isAiming = true;
    }
    private void OnRightClickCancel(InputAction.CallbackContext context)
    {
        isAiming = false;
    }

    private void HandleInput()
    {
        movement = playerControls.Controls.Movement.ReadValue<Vector2>();
        aim = playerControls.Controls.Aim.ReadValue<Vector2>();
    }

    private void HandleMovement()
    {
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y).normalized;
        Vector3 movementVelocity = Vector3.zero;

        if (moveDirection.magnitude > 0.1f)
        {
            Vector3 worldMoveDirection = (Camera.main.transform.right * movement.x) +
                                         (Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized * movement.y);
            worldMoveDirection.Normalize();

            float speedMultiplier = isAiming ? aimingSpeedMultiplier : 1f;

            // Determine the angle between movement direction and aiming direction.
            Vector3 aimDirection = GetAimDirection();
            float dotProduct = Vector3.Dot(worldMoveDirection, aimDirection);
            if (dotProduct > 0.7f)
            {
                speedMultiplier *= forwardBoostMultiplier;
            }

            movementVelocity = worldMoveDirection * playerSpeed * speedMultiplier;
        }

        controller.Move(movementVelocity * Time.deltaTime);
        controller.Move(playerVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        Vector3 mousePos = GetMouseWorldPosition();
        Vector3 lookDirection = (mousePos - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public Vector3 GetAimDirection()
    {
        Vector3 targetPoint = GetMouseWorldPosition();
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(aim);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float rayDistance))
            return ray.GetPoint(rayDistance);
        return transform.position;
    }


    private void UpdateAnimator()
    {
        Vector3 moveDirection = new Vector3(movement.x, 0, movement.y);
        Vector3 localMoveDirection = transform.InverseTransformDirection(moveDirection);
        float targetVelocityX = localMoveDirection.x;
        float targetVelocityZ = localMoveDirection.z;
        float smoothTime = (Mathf.Abs(targetVelocityX) < 0.01f && Mathf.Abs(targetVelocityZ) < 0.01f)
            ? smoothTimeStopping : smoothTimeMoving;
        smoothVelocity.x = Mathf.SmoothDamp(smoothVelocity.x, targetVelocityX, ref smoothVelocity.x, smoothTime);
        smoothVelocity.z = Mathf.SmoothDamp(smoothVelocity.z, targetVelocityZ, ref smoothVelocity.z, smoothTime);

        bool isJumpingNow = false;
        bool isFallingNow = false;
        bool isLandingNow = false;

        if (!isGrounded)
        {
            if (playerVelocity.y > 0.1f)
            {
                isJumpingNow = true;
                if (jumpButtonHeld && (Time.time - jumpStartTime) > minAirTime &&
                    playerVelocity.y > (Mathf.Sqrt(jumpHeight * -2f * gravityValue) * 0.5f))
                {
                    wasHighJump = true;
                }
            }
            else
            {
                if (groundHitCount == 0 && ((!jumpButtonHeld && playerVelocity.y < fallThreshold) ||
                    (wasHighJump && playerVelocity.y < fallThreshold)))
                {
                    isFallingNow = true;
                }
                else
                {
                    isJumpingNow = false;
                    isFallingNow = false;
                }
            }
        }
        else
        {
            isJumpingNow = false;
            isFallingNow = false;
            wasHighJump = false;
        }

        if (isGrounded)
        {
            float speed = new Vector2(movement.x, movement.y).magnitude;
            if (speed > 0.1f)
                landingTimer = 0f;
            isLandingNow = landingTimer > 0f;
            wasHighJump = false;
        }

        animator.SetBool("IsJumping", isJumpingNow);
        animator.SetBool("IsFalling", isFallingNow);
        animator.SetBool("IsLanding", isLandingNow);
        animator.SetBool("IsGrounded", isGrounded);

        bool canUpdateHorizontal = isGrounded && (Time.time - jumpStartTime > minAirTime);
        if (canUpdateHorizontal)
        {
            animator.SetFloat("VelocityX", smoothVelocity.x);
            animator.SetFloat("VelocityZ", smoothVelocity.z);
            float movementSpeed = new Vector2(movement.x, movement.y).magnitude;
            if (isAiming)
            {
                movementSpeed *= 0.5f;
            }
            animator.SetFloat("Speed", movementSpeed);
        }
        else
        {
            animator.SetFloat("VelocityX", 0);
            animator.SetFloat("VelocityZ", 0);
            animator.SetFloat("Speed", 0);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody hitRb = hit.collider.attachedRigidbody;

        if (hitRb != null && hitRb.CompareTag("Ragdoll"))
        {
            Vector3 pushDirection = hit.point - transform.position;
            pushDirection.y = 0; // Prevents pushing upwards
            hitRb.AddForce(pushDirection.normalized * 5f, ForceMode.Impulse);
        }

        // Prevent the player from standing on enemies
        if (hit.collider.CompareTag("Enemy"))
        {
            Vector3 playerBottom = transform.position - Vector3.up * controller.height * 0.5f;
            if (hit.point.y > playerBottom.y + 0.2f) // If player is standing on enemy
            {
                playerVelocity.y = -1f; // Push player down slightly
            }
        }
    }
}
