using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
	[Header("References")]
	public Transform bodyTransform;

	[Header("Sensitivity")]
	public float sensitivityMultiplier = 1f;
	public float horizontalSensitivity = 1f;
	public float verticalSensitivity   = 1f;

	[Header("Restrictions")]
	public float minYRotation = -90f;
	public float maxYRotation = 90f;

	//The real rotation of the camera without recoil
	private Vector3 realRotation;

	[Header("Aimpunch")]
	[Tooltip("bigger number makes the response more damped, smaller is less damped, currently the system will overshoot, with larger damping values it won't")]
	public float punchDamping = 9.0f;

	[Tooltip("bigger number increases the speed at which the view corrects")]
	public float punchSpringConstant = 65.0f;

	[HideInInspector]
	public Vector2 punchAngle;

	[HideInInspector]
	public Vector2 punchAngleVel;

	private float recoveryMultiplier = 1;

	public bool AllowAim;

	private void Start()
	{
		// Lock the mouse
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible   = false;
	}

	private void Update()
	{
		if (!AllowAim) return;
		//Debug.Log(Cursor.visible);
		//if (!Player.Instance.Active) return;

		// Fix pausing
		if (Mathf.Abs(Time.timeScale) <= 0)
			return;

		DecayPunchAngle();

		// Input
		float xMovement = Input.GetAxisRaw("Mouse X") * horizontalSensitivity * sensitivityMultiplier;
		float yMovement = -Input.GetAxisRaw("Mouse Y") * verticalSensitivity  * sensitivityMultiplier;

		// Calculate real rotation from input
		realRotation   = new Vector3(Mathf.Clamp(realRotation.x + yMovement, minYRotation, maxYRotation), realRotation.y + xMovement, realRotation.z);
		realRotation.z = Mathf.Lerp(realRotation.z, 0f, Time.deltaTime * 3f);

		//Apply real rotation to body
		bodyTransform.eulerAngles = Vector3.Scale(realRotation, new Vector3(0f, 1f, 0f));

		//Apply rotation and recoil
		Vector3 cameraEulerPunchApplied = realRotation;
		cameraEulerPunchApplied.x += punchAngle.x;
		cameraEulerPunchApplied.y += punchAngle.y;

		transform.eulerAngles = cameraEulerPunchApplied;
	}

	public void ViewPunch(Vector2 punchAmount, float recoveryMultiplier)
	{
		//Remove previous recoil
		punchAngle = Vector2.zero;

		//Recoil go up
		punchAngleVel -= punchAmount * 20;

		this.recoveryMultiplier = recoveryMultiplier;

    }

	private void DecayPunchAngle()
	{
		if (punchAngle.sqrMagnitude > 0.001 || punchAngleVel.sqrMagnitude > 0.001)
		{
			punchAngle += punchAngleVel * Time.deltaTime;
			float damping = 1 - (punchDamping * Time.deltaTime);

			if (damping < 0)
				damping = 0;

			punchAngleVel *= damping;

			float springForceMagnitude = punchSpringConstant * Time.deltaTime;
			punchAngleVel -= punchAngle * springForceMagnitude * recoveryMultiplier;
		}
		else
		{
			punchAngle    = Vector2.zero;
			punchAngleVel = Vector2.zero;
		}
	}

	public void ResetAngle()
	{
		transform.localEulerAngles = Vector3.zero;
	}

	public void ResetRealRotation()
	{
		realRotation = Vector3.zero;
	}

    public void SetAngle(Vector3 vector3)
    {
        transform.localEulerAngles = vector3;
    }
}
