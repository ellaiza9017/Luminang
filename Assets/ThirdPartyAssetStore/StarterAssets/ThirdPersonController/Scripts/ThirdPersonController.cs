 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Fall & Respawn")]
        [Tooltip("Enable automatic respawn if the player falls off terrain or out of bounds")]
        public bool EnableFallRespawn = true;

        [Tooltip("Y-level below which the player is considered falling into the void (e.g. falling off cliffs/terrain).")]
        public float FallYThreshold = -15.0f;

        [Tooltip("If true, respawn to the last safe position where the player was grounded. If false, respawn to the initial spawn position.")]
        public bool RespawnToLastSafePosition = true;

        [Tooltip("Optional custom respawn point Transform. Overrides initial spawn if set.")]
        public Transform CustomRespawnPoint;

        [Tooltip("Optional SFX played upon respawn.")]
        public AudioClip RespawnAudioClip;

        // respawn tracking
        private Vector3 _initialSpawnPosition;
        private Quaternion _initialSpawnRotation;
        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;
        private bool _hasSafePosition;
        private float _safePositionRecordTimer;
        private const float SafePositionRecordInterval = 0.5f;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private AudioSource _sfxAudioSource; // New dedicated SFX source
        protected GameObject _mainCamera;
        private Camera _cameraComponent;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            RefreshCamera();
        }

        public void RefreshCamera()
        {
            // 1. Preference: Find MainCamera in the SAME scene as the player
            if (gameObject.scene.IsValid())
            {
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    Camera[] potential = root.GetComponentsInChildren<Camera>(true);
                    foreach (Camera cam in potential)
                    {
                        if (cam.CompareTag("MainCamera"))
                        {
                            _mainCamera = cam.gameObject;
                            Debug.Log($"[ThirdPersonController] Found Scene-Specific Camera: {_mainCamera.name} in {gameObject.scene.name}");
                            return;
                        }
                    }
                }
            }

            // 2. Secondary: Find any MainCamera NOT in a Loading scene
            GameObject[] allMainCams = GameObject.FindGameObjectsWithTag("MainCamera");
            foreach (GameObject camObj in allMainCams)
            {
                if (!camObj.scene.name.ToLower().Contains("loading"))
                {
                    _mainCamera = camObj;
                    Debug.Log($"[ThirdPersonController] Found Game Camera (Non-Loading): {_mainCamera.name} in {camObj.scene.name}");
                    return;
                }
            }

            // 3. Fallback: Standard Unity logic
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            
            if (_mainCamera != null)
            {
                _cameraComponent = _mainCamera.GetComponent<Camera>();
            }

            Debug.Log("[ThirdPersonController] Camera fallback to: " + (_mainCamera != null ? _mainCamera.name : "NULL"));
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _sfxAudioSource = GetComponent<AudioSource>(); // Get the AudioSource on the player
            
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            // record initial spawn transform for respawning
            _initialSpawnPosition = transform.position;
            _initialSpawnRotation = transform.rotation;
            _lastSafePosition = _initialSpawnPosition;
            _lastSafeRotation = _initialSpawnRotation;
            _hasSafePosition = true;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            UpdateSafeGroundedPosition();
            CheckFallAndRespawn();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }



        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch -= _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private Coroutine _cameraLerpCoroutine;

        public void ForceCameraLookAt(Vector3 targetPosition, float duration = 0.5f)
        {
            if (CinemachineCameraTarget != null)
            {
                if (_cameraLerpCoroutine != null) StopCoroutine(_cameraLerpCoroutine);
                _cameraLerpCoroutine = StartCoroutine(SmoothCameraLookAt(targetPosition, duration));
            }
        }

        private System.Collections.IEnumerator SmoothCameraLookAt(Vector3 targetPosition, float duration)
        {
            Vector3 direction = (targetPosition - CinemachineCameraTarget.transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(direction);
            
            float targetYaw = lookRot.eulerAngles.y;
            float targetPitch = lookRot.eulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            targetPitch = ClampAngle(targetPitch, BottomClamp, TopClamp);

            float startYaw = _cinemachineTargetYaw;
            float startPitch = _cinemachineTargetPitch;

            // Handle wrap-around for Yaw so it takes the shortest path instead of spinning 360 degrees
            targetYaw = startYaw + Mathf.DeltaAngle(startYaw, targetYaw);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // SmoothStep provides a nice ease-in-out curve
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                
                _cinemachineTargetYaw = Mathf.Lerp(startYaw, targetYaw, t);
                _cinemachineTargetPitch = Mathf.Lerp(startPitch, targetPitch, t);
                
                yield return null;
            }

            _cinemachineTargetYaw = targetYaw;
            _cinemachineTargetPitch = targetPitch;
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    if (_sfxAudioSource != null)
                        _sfxAudioSource.PlayOneShot(FootstepAudioClips[index], FootstepAudioVolume);
                    else
                        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (_sfxAudioSource != null)
                    _sfxAudioSource.PlayOneShot(LandingAudioClip, FootstepAudioVolume);
                else
                    AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        // =========================================================================
        // RESPAWN & FALL RECOVERY SYSTEM
        // =========================================================================

        private void UpdateSafeGroundedPosition()
        {
            // Only capture safe grounded point if player is grounded, stationary/walking, and not falling
            if (Grounded && _verticalVelocity <= 0.1f && transform.position.y > FallYThreshold + 5.0f)
            {
                _safePositionRecordTimer += Time.deltaTime;
                if (_safePositionRecordTimer >= SafePositionRecordInterval)
                {
                    _safePositionRecordTimer = 0f;
                    _lastSafePosition = transform.position;
                    _lastSafeRotation = transform.rotation;
                    _hasSafePosition = true;
                }
            }
        }

        private void CheckFallAndRespawn()
        {
            if (EnableFallRespawn && transform.position.y < FallYThreshold)
            {
                Respawn();
            }
        }

        /// <summary>
        /// Respawns the player safely back to solid ground or spawn point.
        /// </summary>
        public void Respawn()
        {
            Vector3 targetPosition = _initialSpawnPosition;
            Quaternion targetRotation = _initialSpawnRotation;

            if (CustomRespawnPoint != null)
            {
                targetPosition = CustomRespawnPoint.position;
                targetRotation = CustomRespawnPoint.rotation;
            }
            else if (RespawnToLastSafePosition && _hasSafePosition)
            {
                // Elevate slightly (0.35m) so the character drops cleanly onto the ground mesh without clipping
                targetPosition = _lastSafePosition + Vector3.up * 0.35f;
                targetRotation = _lastSafeRotation;
            }

            Teleport(targetPosition, targetRotation);

            if (RespawnAudioClip != null)
            {
                if (_sfxAudioSource != null)
                    _sfxAudioSource.PlayOneShot(RespawnAudioClip, FootstepAudioVolume);
                else
                    AudioSource.PlayClipAtPoint(RespawnAudioClip, targetPosition, FootstepAudioVolume);
            }

            Debug.Log($"[ThirdPersonController] Player fell off terrain! Safely respawned to: {targetPosition}");
        }

        /// <summary>
        /// Reliably teleports the player with CharacterController and Cinemachine target synchronization.
        /// </summary>
        public void Teleport(Vector3 targetPosition, Quaternion targetRotation)
        {
            // 1. Disable CharacterController so Unity doesn't reject direct transform manipulation
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            // 2. Clear falling momentum and velocity state
            _verticalVelocity = 0.0f;
            _speed = 0.0f;
            _animationBlend = 0.0f;
            _fallTimeoutDelta = FallTimeout;
            _jumpTimeoutDelta = JumpTimeout;

            // 3. Reset animator falling parameters
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, false);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetFloat(_animIDSpeed, 0.0f);
                _animator.SetFloat(_animIDMotionSpeed, 1.0f);
            }

            // 4. Re-enable CharacterController
            if (_controller != null)
            {
                _controller.enabled = true;
            }

            // 5. Align camera look direction with new orientation
            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = targetRotation.eulerAngles.y;
                _cinemachineTargetPitch = 0.0f;
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
            }
        }

        /// <summary>
        /// Sets a custom respawn point transform dynamically (e.g. from a quest checkpoint).
        /// </summary>
        public void SetCustomRespawnPoint(Transform respawnPoint)
        {
            CustomRespawnPoint = respawnPoint;
        }
    }
}