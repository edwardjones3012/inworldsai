using UnityEngine;

namespace ej.Movement {

    public enum MoveType {
        None,
        Walk,
        Noclip, // not implemented
        Ladder, // not implemented
    }

    public class MoveData {

        ///// Fields /////
        
        public Transform PlayerTransform;
        public Transform ViewTransform;
        public Vector3 ViewTransformDefaultLocalPos;
        
        public Vector3 Origin;
        public Vector3 ViewAngles;
        public Vector3 Velocity;
        public float ForwardMove;
        public float SideMove;
        public float UpMove;
        public float SurfaceFriction = 1f;
        public float GravityFactor = 1f;
        public float WalkFactor = 1f;
        public float VerticalAxis = 0f;
        public float HorizontalAxis = 0f;
        public bool WishJump = false;
        public bool Crouching = false;
        public bool Sprinting = false;
        public bool WishDash = false;
        public float DashCooling = 0;

        public float SlopeLimit = 45f;

        public float RigidbodyPushForce = 1f;

        public float DefaultHeight = 2f;
        public float CrouchingHeight = 1f;
        public float CrouchingSpeed = 10f;
        public bool ToggleCrouch = false;

        public bool SlidingEnabled = false;
        public bool LaddersEnabled = false;
        public bool AngledLaddersEnabled = false;
        
        public bool ClimbingLadder = false;
        public Vector3 LadderNormal = Vector3.zero;
        public Vector3 LadderDirection = Vector3.forward;
        public Vector3 LadderClimbDir = Vector3.up;
        public Vector3 LadderVelocity = Vector3.zero;

        public bool Underwater = false;
        public bool CameraUnderwater = false;

        public bool Grounded = false;
        public bool GroundedTemp = false;
        public float FallingVelocity = 0f;

        public bool UseStepOffset = false;
        public float StepOffset = 0f; 

    }
}
