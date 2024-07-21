using UnityEngine;
using ej.TraceUtil;

namespace ej.Movement {
    public class MovementController {

        ///// Fields /////

        [HideInInspector] public Transform PlayerTransform;
        private ISurfControllable _surfer;
        private MovementConfig _config;
        private float _deltaTime;

        public bool Jumping = false;
        public bool Crouching = false;
        public float Speed = 0f;
        
        public Transform Camera;
        public float cameraYPos = 0f;

        private float slideSpeedCurrent = 0f;
        private Vector3 slideDirection = Vector3.forward;

        private bool sliding = false;
        private bool wasSliding = false;
        private float slideDelay = 0f;
        
        private bool uncrouchDown = false;
        private float crouchLerp = 0f;

        private float frictionMult = 1f;

        public bool Grounded { get; private set; }

        ///// Methods /////

        Vector3 groundNormal = Vector3.up;
        public bool locked;
        /// <summary>
        /// 
        /// </summary>
        public void ProcessMovement (ISurfControllable surfer, MovementConfig config, float deltaTime) {

            if (locked) return;
            // cache instead of passing around parameters
            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;
            
            if (_surfer.moveData.LaddersEnabled && !_surfer.moveData.ClimbingLadder) {

                // Look for ladders
                LadderCheck (new Vector3(1f, 0.95f, 1f), _surfer.moveData.Velocity * Mathf.Clamp (Time.deltaTime * 2f, 0.025f, 0.25f));

            }
            
            if (_surfer.moveData.LaddersEnabled && _surfer.moveData.ClimbingLadder) {
                
                LadderPhysics ();
                
            } else if (!_surfer.moveData.Underwater) {

                if (_surfer.moveData.Velocity.y <= 0f)
                    Jumping = false;

                // apply gravity
                if (_surfer.groundObject == null) {

                    _surfer.moveData.Velocity.y -= (_surfer.moveData.GravityFactor * _config.Gravity * _deltaTime);
                    _surfer.moveData.Velocity.y += _surfer.baseVelocity.y * _deltaTime;

                }
                
                // input velocity, check for ground
                Grounded = CheckGrounded ();
                CalculateMovementVelocity ();
                
            } else {

                // Do Underwater logic
                UnderwaterPhysics ();

            }

            if (suckVelocity == Vector3.zero)
            {
                float yVel = _surfer.moveData.Velocity.y;
                _surfer.moveData.Velocity.y = 0f;
                _surfer.moveData.Velocity = Vector3.ClampMagnitude (_surfer.moveData.Velocity, _config.MaxVelocity);
                Speed =  _surfer.moveData.Velocity.magnitude;
                _surfer.moveData.Velocity.y = yVel;

                //can probably bind the player to the anchor point here
                _surfer.moveData.Velocity += additionVelocity;
                Debug.Log("Adding velocity: " + additionVelocity);
            
                if (counterDir != Vector3.zero)
                {
                    _surfer.moveData.Velocity -= _surfer.moveData.Velocity * 1;
                    _surfer.moveData.Velocity += counterDir;
                }
            }
            else
            {
                _surfer.moveData.Velocity = suckVelocity;
            }


            if (_surfer.moveData.Velocity.sqrMagnitude == 0f) {

                // Do collisions while standing still
                SurfPhysics.ResolveCollisions (_surfer.collider, ref _surfer.moveData.Origin, ref _surfer.moveData.Velocity, _surfer.moveData.RigidbodyPushForce, 1f, _surfer.moveData.StepOffset, _surfer);

            } else {

                float maxDistPerFrame = 0.2f;
                Vector3 velocityThisFrame = _surfer.moveData.Velocity * _deltaTime;
                float velocityDistLeft = velocityThisFrame.magnitude;
                float initialVel = velocityDistLeft;
                while (velocityDistLeft > 0f) {

                    float amountThisLoop = Mathf.Min (maxDistPerFrame, velocityDistLeft);
                    velocityDistLeft -= amountThisLoop;

                    // increment origin
                    Vector3 velThisLoop = velocityThisFrame * (amountThisLoop / initialVel);
                    _surfer.moveData.Origin += velThisLoop;

                    // don't penetrate walls
                    SurfPhysics.ResolveCollisions (_surfer.collider, ref _surfer.moveData.Origin, ref _surfer.moveData.Velocity, _surfer.moveData.RigidbodyPushForce, amountThisLoop / initialVel, _surfer.moveData.StepOffset, _surfer);

                }

            }

            _surfer.moveData.GroundedTemp = _surfer.moveData.Grounded;

            _surfer = null;
            additionVelocity = Vector3.zero;
            counterDir = Vector3.zero;
            suckVelocity = Vector3.zero;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CalculateMovementVelocity () 
        {
            switch (_surfer.moveType) {

                case MoveType.Walk:

                if (_surfer.groundObject == null) {

                    /*
                    // AIR MOVEMENT
                    */

                    wasSliding = false;

                    // apply movement from input
                    _surfer.moveData.Velocity += AirInputMovement ();

                    // let the magic happen
                    SurfPhysics.Reflect (ref _surfer.moveData.Velocity, _surfer.collider, _surfer.moveData.Origin, _deltaTime);

                } else {

                    /*
                    //  GROUND MOVEMENT
                    */

                    // Sliding
                    if (!wasSliding) {

                        slideDirection = new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z).normalized;
                        slideSpeedCurrent = Mathf.Max (_config.MaximumSlideSpeed, new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z).magnitude);

                    }

                    sliding = false;
                    if (_surfer.moveData.Velocity.magnitude > _config.MinimumSlideSpeed && _surfer.moveData.SlidingEnabled && _surfer.moveData.Crouching && slideDelay <= 0f) 
                    {
                        if (!wasSliding)
                            slideSpeedCurrent = Mathf.Clamp (slideSpeedCurrent * _config.SlideSpeedMultiplier, _config.MinimumSlideSpeed, _config.MaximumSlideSpeed);

                        sliding = true;
                        wasSliding = true;
                        SlideMovement ();
                        return;
                    } 
                    else 
                    {

                        if (slideDelay > 0f)
                            slideDelay -= _deltaTime;

                        if (wasSliding)
                            slideDelay = _config.SlideDelay;

                        wasSliding = false;
                    }
                    
                        float fric = Crouching ? _config.CrouchFriction : _config.Friction;
                        float accel = Crouching ? _config.CrouchAcceleration : _config.Acceleration;
                        float decel = Crouching ? _config.CrouchDeceleration : _config.Deceleration;
                    
                        // Get movement directions
                        Vector3 forward = Vector3.Cross (groundNormal, -PlayerTransform.right);
                        Vector3 right = Vector3.Cross (groundNormal, forward);

                        float speed = _surfer.moveData.Sprinting ? _config.SprintSpeed : _config.WalkSpeed;
                        if (Crouching)
                            speed = _config.CrouchSpeed;

                        Vector3 _wishDir;

                        // Jump and friction
                        if (_surfer.moveData.WishJump) {

                            ApplyFriction (0.0f, true, true);
                            Jump ();
                            return;

                        } else {

                            ApplyFriction (1.0f * frictionMult, true, true);

                        }

                        float forwardMove = _surfer.moveData.VerticalAxis;
                        float rightMove = _surfer.moveData.HorizontalAxis;

                        _wishDir = forwardMove * forward + rightMove * right;
                        _wishDir.Normalize ();
                        Vector3 moveDirNorm = _wishDir;

                        Vector3 forwardVelocity = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z));

                        // Set the target speed of the player
                        float _wishSpeed = _wishDir.magnitude;
                        _wishSpeed *= speed;

                        // Accelerate
                        float yVel = _surfer.moveData.Velocity.y;
                        Accelerate (_wishDir, _wishSpeed, accel * Mathf.Min (frictionMult, 1f), false);

                        float maxVelocityMagnitude = _config.MaxVelocity;
                        _surfer.moveData.Velocity = Vector3.ClampMagnitude (new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z), maxVelocityMagnitude);
                        _surfer.moveData.Velocity.y = yVel;

                        // Calculate how much slopes should affect movement
                        float yVelocityNew = forwardVelocity.normalized.y * new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z).magnitude;

                        // Apply the Y-movement from slopes
                        _surfer.moveData.Velocity.y = yVelocityNew * (_wishDir.y < 0f ? 1.2f : 1.0f);
                        float removableYVelocity = _surfer.moveData.Velocity.y - yVelocityNew;

                        if (_surfer.moveData.WishDash && _surfer.moveData.DashCooling <=0)
                        {
                            Dash();
                        }
                }
                
                break;

            } // END OF SWITCH STATEMENT
        }

        private void UnderwaterPhysics () {

            _surfer.moveData.Velocity = Vector3.Lerp (_surfer.moveData.Velocity, Vector3.zero, _config.UnderwaterVelocityDampening * _deltaTime);

            // Gravity
            if (!CheckGrounded ())
                _surfer.moveData.Velocity.y -= _config.UnderwaterGravity * _deltaTime;

            // Swimming upwards
            if (Input.GetButton ("Jump"))
                _surfer.moveData.Velocity.y += _config.SwimUpSpeed * _deltaTime;

            float fric = _config.UnderwaterFriction;
            float accel = _config.UnderwaterAcceleration;
            float decel = _config.UnderwaterDeceleration;

            ApplyFriction (1f, true, false);

            // Get movement directions
            Vector3 forward = Vector3.Cross (groundNormal, -PlayerTransform.right);
            Vector3 right = Vector3.Cross (groundNormal, forward);

            float speed = _config.UnderwaterSwimSpeed;

            Vector3 _wishDir;

            float forwardMove = _surfer.moveData.VerticalAxis;
            float rightMove = _surfer.moveData.HorizontalAxis;

            _wishDir = forwardMove * forward + rightMove * right;
            _wishDir.Normalize ();
            Vector3 moveDirNorm = _wishDir;

            Vector3 forwardVelocity = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z));

            // Set the target speed of the player
            float _wishSpeed = _wishDir.magnitude;
            _wishSpeed *= speed;

            // Accelerate
            float yVel = _surfer.moveData.Velocity.y;
            Accelerate (_wishDir, _wishSpeed, accel, false);

            float maxVelocityMagnitude = _config.MaxVelocity;
            _surfer.moveData.Velocity = Vector3.ClampMagnitude (new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z), maxVelocityMagnitude);
            _surfer.moveData.Velocity.y = yVel;

            float yVelStored = _surfer.moveData.Velocity.y;
            _surfer.moveData.Velocity.y = 0f;

            // Calculate how much slopes should affect movement
            float yVelocityNew = forwardVelocity.normalized.y * new Vector3 (_surfer.moveData.Velocity.x, 0f, _surfer.moveData.Velocity.z).magnitude;

            // Apply the Y-movement from slopes
            _surfer.moveData.Velocity.y = Mathf.Min (Mathf.Max (0f, yVelocityNew) + yVelStored, speed);

            // Jumping out of water
            bool movingForwards = PlayerTransform.InverseTransformVector (_surfer.moveData.Velocity).z > 0f;
            Trace waterJumpTrace = TraceBounds (PlayerTransform.position, PlayerTransform.position + PlayerTransform.forward * 0.1f, SurfPhysics.groundLayerMask);
            if (waterJumpTrace.hitCollider != null && Vector3.Angle (Vector3.up, waterJumpTrace.planeNormal) >= _config.SlopeLimit && Input.GetButton ("Jump") && !_surfer.moveData.CameraUnderwater && movingForwards)
                _surfer.moveData.Velocity.y = Mathf.Max (_surfer.moveData.Velocity.y, _config.JumpForce);

        }
        
        private void LadderCheck (Vector3 colliderScale, Vector3 direction) {

            if (_surfer.moveData.Velocity.sqrMagnitude <= 0f)
                return;
            
            bool foundLadder = false;

            RaycastHit [] hits = Physics.BoxCastAll (_surfer.moveData.Origin, Vector3.Scale (_surfer.collider.bounds.size * 0.5f, colliderScale), Vector3.Scale (direction, new Vector3 (1f, 0f, 1f)), Quaternion.identity, direction.magnitude, SurfPhysics.groundLayerMask, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits) {

                Ladder ladder = hit.transform.GetComponentInParent<Ladder> ();
                if (ladder != null) {

                    bool allowClimb = true;
                    float ladderAngle = Vector3.Angle (Vector3.up, hit.normal);
                    if (_surfer.moveData.AngledLaddersEnabled) {

                        if (hit.normal.y < 0f)
                            allowClimb = false;
                        else {
                            
                            if (ladderAngle <= _surfer.moveData.SlopeLimit)
                                allowClimb = false;

                        }

                    } else if (hit.normal.y != 0f)
                        allowClimb = false;

                    if (allowClimb) {
                        foundLadder = true;
                        if (_surfer.moveData.ClimbingLadder == false) {

                            _surfer.moveData.ClimbingLadder = true;
                            _surfer.moveData.LadderNormal = hit.normal;
                            _surfer.moveData.LadderDirection = -hit.normal * direction.magnitude * 2f;

                            if (_surfer.moveData.AngledLaddersEnabled) {

                                Vector3 sideDir = hit.normal;
                                sideDir.y = 0f;
                                sideDir = Quaternion.AngleAxis (-90f, Vector3.up) * sideDir;

                                _surfer.moveData.LadderClimbDir = Quaternion.AngleAxis (90f, sideDir) * hit.normal;
                                _surfer.moveData.LadderClimbDir *= 1f/ _surfer.moveData.LadderClimbDir.y; // Make sure Y is always 1

                            } else
                                _surfer.moveData.LadderClimbDir = Vector3.up;
                            
                        }
                        
                    }

                }

            }

            if (!foundLadder) {
                
                _surfer.moveData.LadderNormal = Vector3.zero;
                _surfer.moveData.LadderVelocity = Vector3.zero;
                _surfer.moveData.ClimbingLadder = false;
                _surfer.moveData.LadderClimbDir = Vector3.up;

            }

        }

        private void LadderPhysics () {
            
            _surfer.moveData.LadderVelocity = _surfer.moveData.LadderClimbDir * _surfer.moveData.VerticalAxis * 6f;

            _surfer.moveData.Velocity = Vector3.Lerp (_surfer.moveData.Velocity, _surfer.moveData.LadderVelocity, Time.deltaTime * 10f);

            LadderCheck (Vector3.one, _surfer.moveData.LadderDirection);
            
            Trace floorTrace = TraceToFloor ();
            if (_surfer.moveData.VerticalAxis < 0f && floorTrace.hitCollider != null && Vector3.Angle (Vector3.up, floorTrace.planeNormal) <= _surfer.moveData.SlopeLimit) {

                _surfer.moveData.Velocity = _surfer.moveData.LadderNormal * 0.5f;
                _surfer.moveData.LadderVelocity = Vector3.zero;
                _surfer.moveData.ClimbingLadder = false;

            }

            if (_surfer.moveData.WishJump) {

                _surfer.moveData.Velocity = _surfer.moveData.LadderNormal * 4f;
                _surfer.moveData.LadderVelocity = Vector3.zero;
                _surfer.moveData.ClimbingLadder = false;
                
            }
            
        }
        
        private void Accelerate (Vector3 wishDir, float wishSpeed, float acceleration, bool yMovement) {

            // Initialise variables
            float _addSpeed;
            float _accelerationSpeed;
            float _currentSpeed;
            
            // again, no idea
            _currentSpeed = Vector3.Dot (_surfer.moveData.Velocity, wishDir);
            _addSpeed = wishSpeed - _currentSpeed;

            // If you're not actually increasing your speed, stop here.
            if (_addSpeed <= 0)
                return;

            // won't bother trying to understand any of this, really
            _accelerationSpeed = Mathf.Min (acceleration * _deltaTime * wishSpeed, _addSpeed);

            // Add the velocity.
            _surfer.moveData.Velocity.x += _accelerationSpeed * wishDir.x;
            if (yMovement) { _surfer.moveData.Velocity.y += _accelerationSpeed * wishDir.y; }
            _surfer.moveData.Velocity.z += _accelerationSpeed * wishDir.z;

        }

        private void ApplyFriction (float t, bool yAffected, bool grounded) {

            // Initialise variables
            Vector3 _vel = _surfer.moveData.Velocity;
            float _speed;
            float _newSpeed;
            float _control;
            float _drop;

            // Set Y to 0, speed to the magnitude of movement and drop to 0. I think drop is the amount of speed that is lost, but I just stole this from the internet, idk.
            _vel.y = 0.0f;
            _speed = _vel.magnitude;
            _drop = 0.0f;

            float fric = Crouching ? _config.CrouchFriction : _config.Friction;
            float accel = Crouching ? _config.CrouchAcceleration : _config.Acceleration;
            float decel = Crouching ? _config.CrouchDeceleration : _config.Deceleration;

            // Only apply friction if the player is grounded
            if (grounded) {
                
                // i honestly have no idea what this does tbh
                _vel.y = _surfer.moveData.Velocity.y;
                _control = _speed < decel ? decel : _speed;
                _drop = _control * fric * _deltaTime * t;

            }

            // again, no idea, but comments look cool
            _newSpeed = Mathf.Max (_speed - _drop, 0f);
            if (_speed > 0.0f)
                _newSpeed /= _speed;

            // Set the end-velocity
            _surfer.moveData.Velocity.x *= _newSpeed;
            if (yAffected == true) { _surfer.moveData.Velocity.y *= _newSpeed; }
            _surfer.moveData.Velocity.z *= _newSpeed;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Vector3 AirInputMovement () {

            Vector3 wishVel, wishDir;
            float wishSpeed;

            GetWishValues (out wishVel, out wishDir, out wishSpeed);

            if (_config.ClampAirSpeed && (wishSpeed != 0f && (wishSpeed > _config.MaxSpeed))) {

                wishVel = wishVel * (_config.MaxSpeed / wishSpeed);
                wishSpeed = _config.MaxSpeed;

            }

            return SurfPhysics.AirAccelerate (_surfer.moveData.Velocity, wishDir, wishSpeed, _config.AirAcceleration, _config.AirCap, _deltaTime);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wishVel"></param>
        /// <param name="wishDir"></param>
        /// <param name="wishSpeed"></param>
        private void GetWishValues (out Vector3 wishVel, out Vector3 wishDir, out float wishSpeed) {

            wishVel = Vector3.zero;
            wishDir = Vector3.zero;
            wishSpeed = 0f;

            Vector3 forward = _surfer.forward,
                right = _surfer.right;

            forward [1] = 0;
            right [1] = 0;
            forward.Normalize ();
            right.Normalize ();

            for (int i = 0; i < 3; i++)
                wishVel [i] = forward [i] * _surfer.moveData.ForwardMove + right [i] * _surfer.moveData.SideMove;
            wishVel [1] = 0;

            wishSpeed = wishVel.magnitude;
            wishDir = wishVel.normalized;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="velocity"></param>
        /// <param name="jumpPower"></param>
        private void Jump () {
            
            if (!_config.AutoBhop)
                _surfer.moveData.WishJump = false;
            
            _surfer.moveData.Velocity.y += _config.JumpForce;
            Jumping = true;
            //GameEvents.Instance.PlayerJumped.Invoke();
        }
        private Vector3 additionVelocity;
        private Vector3 suckVelocity;
        private Vector3 counterDir;
        public void AddVelocity(Vector3 force)
        {
            additionVelocity += force;
        }

        public void AddSuckVelocity(Vector3 velocity)
        {
            suckVelocity += velocity;
        }


        public void CounterInDirection(Vector3 dir)
        {
            counterDir = dir;
        }

        private void Dash()
        {
            Debug.Log("Dash");
            _surfer.moveData.WishDash = false;
            _surfer.moveData.DashCooling = 0.1f;

            // Calculate the dot product between velocity and right vector of the view transform

            float dotProduct = Vector3.Dot(_surfer.moveData.Velocity, _surfer.moveData.ViewTransform.right);

            // If dot product is negative, it means the surfer is moving left relative to the view transform
            bool movingLeft = dotProduct < 0;

            Vector3 dir = _surfer.moveData.ViewTransform.right * 35 * (movingLeft ? -1 : 1);

            // Modify velocity based on desired movement
            _surfer.moveData.Velocity += dir;
        }


        /// <summary>
        /// 
        /// </summary>
        private bool CheckGrounded () {

            _surfer.moveData.SurfaceFriction = 1f;
            var movingUp = _surfer.moveData.Velocity.y > 0f;
            var trace = TraceToFloor ();

            float groundSteepness = Vector3.Angle (Vector3.up, trace.planeNormal);

            if (trace.hitCollider == null || groundSteepness > _config.SlopeLimit || (Jumping && _surfer.moveData.Velocity.y > 0f)) {

                SetGround (null);

                if (movingUp && _surfer.moveType != MoveType.Noclip)
                    _surfer.moveData.SurfaceFriction = _config.AirFriction;
                
                return false;

            } else {

                groundNormal = trace.planeNormal;
                SetGround (trace.hitCollider.gameObject);
                return true;

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        private void SetGround (GameObject obj) {

            if (obj != null) {

                _surfer.groundObject = obj;
                _surfer.moveData.Velocity.y = 0;

            } else
                _surfer.groundObject = null;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        private Trace TraceBounds (Vector3 start, Vector3 end, int layerMask) {

            return Tracer.TraceCollider (_surfer.collider, start, end, layerMask);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Trace TraceToFloor () {

            var down = _surfer.moveData.Origin;
            down.y -= 0.001f;

            return Tracer.TraceCollider (_surfer.collider, _surfer.moveData.Origin, down, SurfPhysics.groundLayerMask);

        }

        public void Crouch (ISurfControllable surfer, MovementConfig config, float deltaTime) {

            _surfer = surfer;
            _config = config;
            _deltaTime = deltaTime;

            if (_surfer == null)
                return;

            if (_surfer.collider == null)
                return;

            bool grounded = _surfer.groundObject != null;
            bool wantsToCrouch = _surfer.moveData.Crouching;

            float crouchingHeight = Mathf.Clamp (_surfer.moveData.CrouchingHeight, 0.01f, 1f);
            float heightDifference = _surfer.moveData.DefaultHeight - _surfer.moveData.DefaultHeight * crouchingHeight;

            if (grounded)
                uncrouchDown = false;

            // Crouching input
            if (grounded)
                crouchLerp = Mathf.Lerp (crouchLerp, wantsToCrouch ? 1f : 0f, _deltaTime * _surfer.moveData.CrouchingSpeed);
            else if (!grounded && !wantsToCrouch && crouchLerp < 0.95f)
                crouchLerp = 0f;
            else if (!grounded && wantsToCrouch)
                crouchLerp = 1f;

            // Collider and position changing
            if (crouchLerp > 0.9f && !Crouching) {
                
                // Begin crouching
                Crouching = true;
                if (_surfer.collider.GetType () == typeof (BoxCollider)) {

                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                    boxCollider.size = new Vector3 (boxCollider.size.x, _surfer.moveData.DefaultHeight * crouchingHeight, boxCollider.size.z);

                } else if (_surfer.collider.GetType () == typeof (CapsuleCollider)) {

                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                    capsuleCollider.height = _surfer.moveData.DefaultHeight * crouchingHeight;

                }

                // Move position and stuff
                _surfer.moveData.Origin += heightDifference / 2 * (grounded ? Vector3.down : Vector3.up);
                foreach (Transform child in PlayerTransform) {

                    if (child == _surfer.moveData.ViewTransform)
                        continue;

                    child.localPosition = new Vector3 (child.localPosition.x, child.localPosition.y * crouchingHeight, child.localPosition.z);

                }

                uncrouchDown = !grounded;

            } else if (Crouching) {

                // Check if the player can uncrouch
                bool canUncrouch = true;
                if (_surfer.collider.GetType () == typeof (BoxCollider)) {

                    // Box collider
                    BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                    Vector3 halfExtents = boxCollider.size * 0.5f;
                    Vector3 startPos = boxCollider.transform.position;
                    Vector3 endPos = boxCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;

                    Trace trace = Tracer.TraceBox (startPos, endPos, halfExtents, boxCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                        canUncrouch = false;

                } else if (_surfer.collider.GetType () == typeof (CapsuleCollider)) {

                    // Capsule collider
                    CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                    Vector3 point1 = capsuleCollider.center + Vector3.up * capsuleCollider.height * 0.5f;
                    Vector3 point2 = capsuleCollider.center + Vector3.down * capsuleCollider.height * 0.5f;
                    Vector3 startPos = capsuleCollider.transform.position;
                    Vector3 endPos = capsuleCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * heightDifference;

                    Trace trace = Tracer.TraceCapsule (point1, point2, capsuleCollider.radius, startPos, endPos, capsuleCollider.contactOffset, SurfPhysics.groundLayerMask);

                    if (trace.hitCollider != null)
                        canUncrouch = false;

                }

                // Uncrouch
                if (canUncrouch && crouchLerp <= 0.9f) {

                    Crouching = false;
                    if (_surfer.collider.GetType () == typeof (BoxCollider)) {

                        // Box collider
                        BoxCollider boxCollider = (BoxCollider)_surfer.collider;
                        boxCollider.size = new Vector3 (boxCollider.size.x, _surfer.moveData.DefaultHeight, boxCollider.size.z);

                    } else if (_surfer.collider.GetType () == typeof (CapsuleCollider)) {

                        // Capsule collider
                        CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
                        capsuleCollider.height = _surfer.moveData.DefaultHeight;

                    }

                    // Move position and stuff
                    _surfer.moveData.Origin += heightDifference / 2 * (uncrouchDown ? Vector3.down : Vector3.up);
                    foreach (Transform child in PlayerTransform) {

                        child.localPosition = new Vector3 (child.localPosition.x, child.localPosition.y / crouchingHeight, child.localPosition.z);

                    }

                }

                if (!canUncrouch)
                    crouchLerp = 1f;

            }

            // Changing camera position
            if (!Crouching)
                _surfer.moveData.ViewTransform.localPosition = Vector3.Lerp (_surfer.moveData.ViewTransformDefaultLocalPos, _surfer.moveData.ViewTransformDefaultLocalPos * crouchingHeight + Vector3.down * heightDifference * 0.5f, crouchLerp);
            else
                _surfer.moveData.ViewTransform.localPosition = Vector3.Lerp (_surfer.moveData.ViewTransformDefaultLocalPos - Vector3.down * heightDifference * 0.5f, _surfer.moveData.ViewTransformDefaultLocalPos * crouchingHeight, crouchLerp);

        }

        void SlideMovement () {
            
            // Gradually change direction
            slideDirection += new Vector3 (groundNormal.x, 0f, groundNormal.z) * slideSpeedCurrent * _deltaTime;
            slideDirection = slideDirection.normalized;

            // Set direction
            Vector3 slideForward = Vector3.Cross (groundNormal, Quaternion.AngleAxis (-90, Vector3.up) * slideDirection);
            
            // Set the velocity
            slideSpeedCurrent -= _config.SlideFriction * _deltaTime;
            slideSpeedCurrent = Mathf.Clamp (slideSpeedCurrent, 0f, _config.MaximumSlideSpeed);
            slideSpeedCurrent -= (slideForward * slideSpeedCurrent).y * _deltaTime * _config.DownhillSlideSpeedMultiplier; // Accelerate downhill (-y = downward, - * - = +)

            _surfer.moveData.Velocity = slideForward * slideSpeedCurrent;
            
            // Jump
            if (_surfer.moveData.WishJump && slideSpeedCurrent < _config.MinimumSlideSpeed * _config.SlideSpeedMultiplier) {

                Jump ();
                return;
            }

        }

    }
}
