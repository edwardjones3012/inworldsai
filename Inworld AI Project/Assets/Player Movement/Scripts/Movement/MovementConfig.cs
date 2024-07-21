using UnityEngine;
namespace ej.Movement {

    [System.Serializable]
    public class MovementConfig {

        [Header ("Jumping and gravity")]
        public bool AutoBhop = true;
        public float Gravity = 20f;
        public float JumpForce = 6.5f;
        
        [Header ("General physics")]
        public float Friction = 6f;
        public float MaxSpeed = 6f;
        public float MaxVelocity = 50f;
        [Range (30f, 75f)] public float SlopeLimit = 45f;

        [Header ("Air movement")]
        public bool ClampAirSpeed = true;
        public float AirCap = 0.4f;
        public float AirAcceleration = 12f;
        public float AirFriction = 0.4f;

        [Header ("Ground movement")]
        public float WalkSpeed = 7f;
        public float SprintSpeed = 12f;
        public float Acceleration = 14f;
        public float Deceleration = 10f;

        [Header ("Crouch movement")]
        public float CrouchSpeed = 4f;
        public float CrouchAcceleration = 8f;
        public float CrouchDeceleration = 4f;
        public float CrouchFriction = 3f;

        [Header ("Sliding")]
        public float MinimumSlideSpeed = 9f;
        public float MaximumSlideSpeed = 18f;
        public float SlideSpeedMultiplier = 1.75f;
        public float SlideFriction = 14f;
        public float DownhillSlideSpeedMultiplier = 2.5f;
        public float SlideDelay = 0.5f;

        [Header ("Underwater")]
        public float SwimUpSpeed = 12f;
        public float UnderwaterSwimSpeed = 3f;
        public float UnderwaterAcceleration = 6f;
        public float UnderwaterDeceleration = 3f;
        public float UnderwaterFriction = 2f;
        public float UnderwaterGravity = 6f;
        public float UnderwaterVelocityDampening = 2f;

        [Header("Dashing")]
        public float DashMultiplier = 8;

    }

}
