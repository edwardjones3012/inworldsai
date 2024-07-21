using UnityEngine;
using System.Collections.Generic;
using System;

namespace ej.Movement {

    /// <summary>
    /// Easily add a surfable character to the scene
    /// </summary>
    [AddComponentMenu("Fragsurf/Surf Character")]
    public class SurfCharacter : MonoBehaviour, ISurfControllable {

        public enum ColliderType {
            Capsule,
            Box
        }

        ///// Fields /////

        [Header("Physics Settings")]
        public Vector3 colliderSize = new Vector3(1f, 2f, 1f);
        [HideInInspector] public ColliderType collisionType { get { return ColliderType.Box; } } // Capsule doesn't work
        public float weight = 75f;
        public float rigidbodyPushForce = 2f;
        public bool solidCollider = false;

        [Header("View Settings")]
        public Transform viewTransform;
        public Transform playerRotationTransform;

        [Header("Crouching setup")]
        public float crouchingHeightMultiplier = 0.5f;
        public float crouchingSpeed = 10f;
        float defaultHeight;
        bool allowCrouch = true; // This is separate because you shouldn't be able to toggle crouching on and off during gameplay for various reasons
        [HideInInspector] public bool AllowJump = true;

        [Header("Features")]
        public bool crouchingEnabled = true;
        public bool slidingEnabled = false;
        public bool dashingEnabled = false;
        public bool laddersEnabled = true;
        public bool supportAngledLadders = true;

        [Header("Step offset (can be buggy, enable at your own risk)")]
        public bool useStepOffset = false;
        public float stepOffset = 0.35f;

        [Header("Movement Config")]
        [SerializeField]
        public MovementConfig movementConfig;

        private GameObject _groundObject;
        private Vector3 _baseVelocity;
        private Collider _collider;
        private Vector3 _angles;
        private Vector3 _startPosition;
        private GameObject _colliderObject;
        private GameObject _cameraWaterCheckObject;
        private CameraWaterCheck _cameraWaterCheck;

        private MoveData _moveData = new MoveData();
        private MovementController _controller = new MovementController();
        private Rigidbody rb;

        private List<Collider> triggers = new List<Collider>();
        private int numberOfTriggers = 0;

        private bool underwater = false;

        ///// Properties /////

        public MoveType moveType { get { return MoveType.Walk; } }
        public MovementConfig moveConfig { get { return movementConfig; } }
        public MoveData moveData { get { return _moveData; } }
        public new Collider collider { get { return _collider; } }

        public bool Moving { get; private set; }

        public GameObject groundObject {

            get { return _groundObject; }
            set { _groundObject = value; }

        }

        public bool Grounded { get { return groundObject != null; } }

        public Vector3 baseVelocity { get { return _baseVelocity; } }

        public Vector3 forward { get { return viewTransform.forward; } }
        public Vector3 right { get { return viewTransform.right; } }
        public Vector3 up { get { return viewTransform.up; } }

        Vector3 prevPosition;

        public DateTime TimeLastInWater { get; private set; }

        public bool AllowMove = true;


        ///// Methods /////

        private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube( transform.position, colliderSize );
		}
		
        private void Awake () {
            
            _controller.PlayerTransform = playerRotationTransform;
            
            if (viewTransform != null) {

                _controller.Camera = viewTransform;
                _controller.cameraYPos = viewTransform.localPosition.y;

            }

        }
        private void Start () {
            
            _colliderObject = new GameObject ("PlayerCollider");
            _colliderObject.layer = gameObject.layer;
            _colliderObject.tag = gameObject.tag;
            _colliderObject.transform.SetParent (transform);
            _colliderObject.transform.rotation = Quaternion.identity;
            _colliderObject.transform.localPosition = Vector3.zero;
            _colliderObject.transform.SetSiblingIndex (0);
            //CollisionDetector = _colliderObject.AddComponent<CollisionDetector>();

            // Water check
            _cameraWaterCheckObject = new GameObject ("Camera water check");
            _cameraWaterCheckObject.layer = gameObject.layer;
            _cameraWaterCheckObject.tag = gameObject.tag;
            _cameraWaterCheckObject.transform.position = viewTransform.position;

            SphereCollider _cameraWaterCheckSphere = _cameraWaterCheckObject.AddComponent<SphereCollider> ();
            _cameraWaterCheckSphere.radius = 0.1f;
            _cameraWaterCheckSphere.isTrigger = true;

            Rigidbody _cameraWaterCheckRb = _cameraWaterCheckObject.AddComponent<Rigidbody> ();
            _cameraWaterCheckRb.useGravity = false;
            _cameraWaterCheckRb.isKinematic = true;

            _cameraWaterCheck = _cameraWaterCheckObject.AddComponent<CameraWaterCheck> ();

            prevPosition = transform.position;

            if (viewTransform == null)
                viewTransform = Camera.main.transform;

            if (playerRotationTransform == null && transform.childCount > 0)
                playerRotationTransform = transform.GetChild (0);

            _collider = gameObject.GetComponent<Collider> ();

            if (_collider != null)
                GameObject.Destroy (_collider);

            // rigidbody is required to collide with triggers
            rb = gameObject.GetComponent<Rigidbody> ();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody> ();

            allowCrouch = crouchingEnabled;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.angularDrag = 0f;
            rb.drag = 0f;
            rb.mass = weight;

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            switch (collisionType) {

                // Box collider
                case ColliderType.Box:

                _collider = _colliderObject.AddComponent<BoxCollider> ();

                var boxc = (BoxCollider)_collider;
                boxc.size = colliderSize;

                defaultHeight = boxc.size.y;

                break;

                // Capsule collider
                case ColliderType.Capsule:

                _collider = _colliderObject.AddComponent<CapsuleCollider> ();

                var capc = (CapsuleCollider)_collider;
                capc.height = colliderSize.y;
                capc.radius = colliderSize.x / 2f;

                defaultHeight = capc.height;

                break;

            }

            _moveData.SlopeLimit = movementConfig.SlopeLimit;

            _moveData.RigidbodyPushForce = rigidbodyPushForce;

            _moveData.SlidingEnabled = slidingEnabled;
            _moveData.LaddersEnabled = laddersEnabled;
            _moveData.AngledLaddersEnabled = supportAngledLadders;

            _moveData.PlayerTransform = transform;
            _moveData.ViewTransform = viewTransform;
            _moveData.ViewTransformDefaultLocalPos = viewTransform.localPosition;

            _moveData.DefaultHeight = defaultHeight;
            _moveData.CrouchingHeight = crouchingHeightMultiplier;
            _moveData.CrouchingSpeed = crouchingSpeed;
            
            _collider.isTrigger = !solidCollider;
            _moveData.Origin = transform.position;
            _startPosition = transform.position;

            _moveData.UseStepOffset = useStepOffset;
            _moveData.StepOffset = stepOffset;

        }

        private void Update () {

            //if (!Player.Instance.Active) return;
            _colliderObject.transform.rotation = Quaternion.identity;

            //UpdateTestBinds ();
            UpdateMoveData ();
            
            // Previous movement code
            Vector3 positionalMovement = transform.position - prevPosition;
            transform.position = prevPosition;
            moveData.Origin += positionalMovement;

            // Triggers
            if (numberOfTriggers != triggers.Count) {
                numberOfTriggers = triggers.Count;

                underwater = false;
                triggers.RemoveAll (item => item == null);
                foreach (Collider trigger in triggers) {

                    if (trigger == null)
                        continue;

                    if (trigger.GetComponentInParent<Water>())
                    {
                        if (TimeLastInWater.AddSeconds(.5f) < DateTime.Now)
                        {
                            if (!underwater)
                            {
                                //GameEvents.Instance.PlayerEnteredWater.Invoke();
                            }
                        }
                        underwater = true;
                    }
                }

            }

            _moveData.CameraUnderwater = _cameraWaterCheck.IsUnderwater ();
            _cameraWaterCheckObject.transform.position = viewTransform.position;
            moveData.Underwater = underwater;
            
            if (allowCrouch)
                _controller.Crouch (this, movementConfig, Time.deltaTime);

            _controller.ProcessMovement (this, movementConfig, Time.deltaTime);

            transform.position = moveData.Origin;
            prevPosition = transform.position;

            _colliderObject.transform.rotation = Quaternion.identity;

            _moveData.DashCooling -= Time.deltaTime;
            if (moveData.DashCooling < 0) moveData.DashCooling = 0;

            if (underwater)
            {
                TimeLastInWater = DateTime.Now;
            }
        }
        
        private void UpdateTestBinds () {

            if (Input.GetKeyDown (KeyCode.Backspace))
                ResetPosition ();

        }

        private void ResetPosition () {
            
            moveData.Velocity = Vector3.zero;
            moveData.Origin = _startPosition;

        }

        private void UpdateMoveData () {

            if (!AllowMove) return;

            _moveData.VerticalAxis = Input.GetAxisRaw ("Vertical");
            _moveData.HorizontalAxis = Input.GetAxisRaw ("Horizontal");

            _moveData.Sprinting = Input.GetButton ("Sprint");
            
            if (Input.GetButtonDown ("Crouch"))
                _moveData.Crouching = true;

            if (!Input.GetButton ("Crouch"))
                _moveData.Crouching = false;
            
            bool moveLeft = _moveData.HorizontalAxis < 0f;
            bool moveRight = _moveData.HorizontalAxis > 0f;
            bool moveFwd = _moveData.VerticalAxis > 0f;
            bool moveBack = _moveData.VerticalAxis < 0f;
            bool jump = Input.GetButton ("Jump");

            Moving = (moveLeft || moveRight || moveFwd || moveBack) && moveData.Velocity.magnitude > 0;

            if (!moveLeft && !moveRight)
                _moveData.SideMove = 0f;
            else if (moveLeft)
                _moveData.SideMove = -moveConfig.Acceleration;
            else if (moveRight)
                _moveData.SideMove = moveConfig.Acceleration;

            if (!moveFwd && !moveBack)
                _moveData.ForwardMove = 0f;
            else if (moveFwd)
                _moveData.ForwardMove = moveConfig.Acceleration;
            else if (moveBack)
                _moveData.ForwardMove = -moveConfig.Acceleration;

            if (Input.GetButtonDown("Jump") && AllowJump)
                _moveData.WishJump = true;

            if (!Input.GetButton("Jump") && AllowJump)
                _moveData.WishJump = false;

            _moveData.ViewAngles = _angles;

            if (dashingEnabled)
            {
                if (Input.GetButtonDown("Dash"))
                    _moveData.WishDash = true;

                if (!Input.GetButton("Dash"))
                    _moveData.WishDash = false;
            }

        }

        private void DisableInput () {

            _moveData.VerticalAxis = 0f;
            _moveData.HorizontalAxis = 0f;
            _moveData.SideMove = 0f;
            _moveData.ForwardMove = 0f;
            _moveData.WishJump = false;
            _moveData.WishDash = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampAngle (float angle, float from, float to) {

            if (angle < 0f)
                angle = 360 + angle;

            if (angle > 180f)
                return Mathf.Max (angle, 360 + from);

            return Mathf.Min (angle, to);

        }

        public void AddVelocity(Vector3 force)
        {
            _controller.AddVelocity(force);
        }

        public void AddSuckVelocity(Vector3 velocity)
        {
            _controller.AddSuckVelocity(velocity);
        }

        public void AddCounterDir(Vector3 force)
        {
            _controller.CounterInDirection(force);
        }

        private void OnTriggerEnter (Collider other) {
            
            if (!triggers.Contains (other))
                triggers.Add (other);

        }

        private void OnTriggerExit (Collider other) {
            
            if (triggers.Contains (other))
                triggers.Remove (other);

        }

        private void OnCollisionStay (Collision collision) {

            if (collision.rigidbody == null)
                return;

            Vector3 relativeVelocity = collision.relativeVelocity * collision.rigidbody.mass / 50f;
            Vector3 impactVelocity = new Vector3 (relativeVelocity.x * 0.0025f, relativeVelocity.y * 0.00025f, relativeVelocity.z * 0.0025f);

            float maxYVel = Mathf.Max (moveData.Velocity.y, 10f);
            Vector3 newVelocity = new Vector3 (moveData.Velocity.x + impactVelocity.x, Mathf.Clamp (moveData.Velocity.y + Mathf.Clamp (impactVelocity.y, -0.5f, 0.5f), -maxYVel, maxYVel), moveData.Velocity.z + impactVelocity.z);

            newVelocity = Vector3.ClampMagnitude (newVelocity, Mathf.Max (moveData.Velocity.magnitude, 30f));
            moveData.Velocity = newVelocity;

        }

        internal void SetLocked(bool lockPosOnTether)
        {
            _controller.locked = lockPosOnTether;
            _controller.CounterInDirection(Vector3.zero);
            _controller.AddVelocity(Vector3.zero);
        }
    }

}

