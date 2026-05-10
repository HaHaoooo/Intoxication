using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float walkSpeed = 3f;
        public float sprintSpeed = 10f;
        public float mouseSensitivity = 2f;
        public float gravity = 9.81f;

        [Header("Cameras")]
        public CinemachineCamera firstPersonCam;
        public CinemachineCamera thirdPersonCam;

        [Header("Animation")]
        public Transform modelTransform;
        public float speedSmoothTime = 0.15f;

        [HideInInspector] public bool canRotateCamera = true;

        public bool IsFirstPerson => !_isThirdPerson;
        public GameObject crosshairUI;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _cameraPitch;
        private float _currentSpeed;
        private float _speedVelocity;
        private float _verticalVelocity;
        private bool _isThirdPerson;
        private bool _isSprinting;

        private CharacterController _controller;
        private Animator _animator;
        private PlayerPickupController _pickupController;
        private PlayerInteractionController _interactionController;

        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int DrinkTriggerHash = Animator.StringToHash("Drink");

        /// <summary>Last known valid position, used for NaN recovery.</summary>
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            if (_controller == null) _controller = gameObject.AddComponent<CharacterController>();

            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null) _animator.applyRootMotion = false;

            _pickupController = GetComponent<PlayerPickupController>();
            _interactionController = GetComponent<PlayerInteractionController>();

            if (modelTransform == null) modelTransform = transform.Find("Model");
            if (firstPersonCam == null) firstPersonCam = GameObject.Find("FPCamera")?.GetComponent<CinemachineCamera>();
            if (thirdPersonCam == null) thirdPersonCam = GameObject.Find("TPCamera")?.GetComponent<CinemachineCamera>();
            if (crosshairUI == null) crosshairUI = GameObject.Find("Crosshair");

            SetCameraView();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _lastSafePosition = transform.position;
            _lastSafeRotation = transform.rotation;
        }

        private void Update()
        {
            // NaN recovery: if the transform is corrupted, restore last known good state
            if (!float.IsFinite(transform.position.x) || !float.IsFinite(transform.rotation.x))
            {
                Debug.LogWarning("[Movement] NaN detected in transform — restoring last safe position.");
                transform.position = _lastSafePosition;
                transform.rotation = _lastSafeRotation;
                _verticalVelocity = 0f;
                return;
            }

            _lastSafePosition = transform.position;
            _lastSafeRotation = transform.rotation;

            // Freeze all player input during dialogue
            if (_interactionController != null && _interactionController.IsInDialogue)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _isSprinting = false;
                HandleMovement(); // Still apply gravity
                return;
            }

            HandleInput();
            HandleRotation();
            HandleMovement();
            HandleCameraToggle();

            if (modelTransform != null)
                modelTransform.localPosition = Vector3.zero;
        }

        private void HandleInput()
        {
            if (!canRotateCamera)
            {
                _moveInput = Vector2.zero;
                _lookInput = Vector2.zero;
                _isSprinting = false;
                return;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                _moveInput.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                _moveInput.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
                _isSprinting = kb.shiftKey.isPressed;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                _lookInput = mouse.delta.ReadValue() * (0.1f * mouseSensitivity);
            }
            else
            {
                _lookInput = Vector2.zero;
            }
        }

        private void HandleRotation()
        {
            if (!canRotateCamera) return;

            if (float.IsFinite(_lookInput.x))
            {
                transform.Rotate(Vector3.up * _lookInput.x);
            }

            if (!_isThirdPerson)
            {
                if (float.IsFinite(_lookInput.y))
                {
                    _cameraPitch -= _lookInput.y;
                    _cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f);
                    if (firstPersonCam != null)
                        firstPersonCam.transform.localRotation = Quaternion.Euler(_cameraPitch, 0, 0);
                }
            }
        }

        private void HandleMovement()
        {
            // Horizontal movement
            float targetSpeed = 0f;
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;
            }

            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, speedSmoothTime);

            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            
            // Check for finite transform vectors
            if (!float.IsFinite(right.x) || !float.IsFinite(forward.x)) return;

            Vector3 move = right * _moveInput.x + forward * _moveInput.y;
            float sqrMag = move.sqrMagnitude;

            if (sqrMag > 0.01f)
            {
                float mag = Mathf.Sqrt(sqrMag);
                if (float.IsFinite(mag) && mag > 0.0001f)
                {
                    move *= (1f / mag) * _currentSpeed;
                }
                else
                {
                    move = Vector3.zero;
                }
            }
            else
            {
                move = Vector3.zero;
            }

            // Gravity with proper accumulation
            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f; // Small downward force to keep grounded
            }
            else
            {
                _verticalVelocity -= gravity * Time.deltaTime;
            }

            if (float.IsFinite(_verticalVelocity))
            {
                move.y = _verticalVelocity;
            }
            
            Vector3 finalMove = move * Time.deltaTime;
            if (float.IsFinite(finalMove.x))
            {
                _controller.Move(finalMove);
            }

            if (_animator != null)
            {
                bool isMovingInput = _moveInput.sqrMagnitude > 0.01f;
                // Use raw input flags for instant animation response
                _animator.SetBool(IsWalkingHash, isMovingInput && !_isSprinting);
                _animator.SetBool(IsRunningHash, isMovingInput && _isSprinting);
            }
        }

        private void HandleCameraToggle()
        {
            if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
                return;

            // Block camera toggle when holding a drinkable item (F is used for drinking)
            if (_pickupController != null && (_pickupController.IsHoldingDrinkable || _pickupController.IsDrinking))
            {
                Debug.Log("[Movement] Camera toggle blocked by pickup system.");
                return;
            }

            // Block camera toggle when dialogue is active or an interaction occurred this frame
            if (_interactionController != null)
            {
                if (_interactionController.IsInDialogue || 
                    _interactionController.IsLookingAtInteractable || 
                    _interactionController.ConsumedInputThisFrame)
                {
                    Debug.Log("[Movement] Camera toggle blocked by interaction system.");
                    return;
                }
            }

            // If we get here, F was pressed but not used for anything else
            _isThirdPerson = !_isThirdPerson;
            SetCameraView();
        }

        /// <summary>Fires the Drink trigger on the animator to enter the Drinking state.</summary>
        public void TriggerDrinkAnimation()
        {
            _animator?.SetTrigger(DrinkTriggerHash);
        }

        /// <summary>Forces the camera to first person view. Used during drinking.</summary>
        public void ForceFirstPerson()
        {
            if (!_isThirdPerson) return;
            _isThirdPerson = false;
            SetCameraView();
        }

        /// <summary>Forces the camera to third person view. Used during NPC dialogue.</summary>
        public void ForceThirdPerson()
        {
            if (_isThirdPerson) return;
            _isThirdPerson = true;
            SetCameraView();
        }

        private void SetCameraView()
        {
            if (firstPersonCam != null) firstPersonCam.Priority = _isThirdPerson ? 0 : 10;
            if (thirdPersonCam != null) thirdPersonCam.Priority = _isThirdPerson ? 10 : 0;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (crosshairUI != null)
                crosshairUI.SetActive(!_isThirdPerson);
        }

        private void OnAnimatorMove()
        {
            // Discard all root motion delta
        }
    }
}
