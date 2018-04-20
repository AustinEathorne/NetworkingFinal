using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerMovement : NetworkBehaviour
{
	[Header("Components")]
	[SerializeField]
	private PlayerManager playerManager; // local
	[SerializeField]
	private Rigidbody rigidbody; // local
	[SerializeField]
	private PlayerCamera playerCamera;

	[Header("Properties")]
	[SerializeField]
	private float linearSpeed;
	[SerializeField]
	private float jumpSpeed;
	[SerializeField]
	private float kickbackSpeed;
	[SerializeField]
	private float networkSendRate;
	private float networkSendCount = 0.0f;
	private float timeBetweenMovementStart = 0.0f;
	private float timeBetweenMovementEnd = 0.0f;
	private int floorMask;
	public bool isInputEnabled = false;
	private bool isJumping = false;
	private bool isInAir = false;
	private bool isKickingBack = false;

	[Header("Replication Properties")]
	private bool isLerpingPosition = false;
	private bool isLerpingRotation = false;
	private Vector3 realPosition;
	private Quaternion realRotation;
	private Vector3 lastRealPosition;
	private Quaternion lastRealRotation;
	private float timeStartedLerping;
	private float timeToLerp;

	private bool canSendMessage = true;


	private void Start()
	{
		if(isLocalPlayer)
		{
			this.floorMask = 1 << 8;
		}
		else
		{
			this.realPosition = this.transform.position;
			this.realRotation = this.transform.rotation;
		}
	}

	// local
	private void Update()
	{
		if(!isLocalPlayer)
		{
			return;
		}

		if(this.isInputEnabled)
		{
			this.UpdatePlayerMovement();
			this.UpdatePlayerRotation();
		}
	}

	// local
	private void UpdatePlayerMovement()
	{
		// linear
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		// jump
		float y = this.rigidbody.velocity.y;
		if(this.isJumping && !this.isInAir)
		{
			this.isInAir = true;
			this.isJumping = false;
			y = this.jumpSpeed;
		}
		else
		{
			this.isJumping = false;
		}

		Vector3 totalVel = new Vector3(x * this.linearSpeed, y, z * this.linearSpeed);

		// weapon kickback
		if(this.isKickingBack)
		{
			this.isKickingBack = false;
			Vector3 kickVel = -this.transform.forward * this.kickbackSpeed;
			totalVel += kickVel;
		}

		this.rigidbody.velocity = totalVel;
		this.rigidbody.angularVelocity = Vector3.zero;


		if(this.canSendMessage)
		{
			this.canSendMessage = false;
			this.StartCoroutine(this.MovementMessageRoutine());
		}
	}

	// local
	private void UpdatePlayerRotation()
	{
		Ray camRay = this.playerCamera.GetCamera().ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		if(Physics.Raycast(camRay, out hit, Mathf.Infinity, this.floorMask))
		{
			if(hit.transform.tag == "Floor")
			{
				//Debug.Log("Hit floor");
				//Debug.DrawRay(this.transform.position, hit.point, Color.green);

				Vector3 lookDirection = hit.point - this.transform.position;
				lookDirection.y = 0.0f;

				Quaternion rot = Quaternion.LookRotation(lookDirection);

				this.rigidbody.MoveRotation(rot);
				//this.transform.rotation = rot;
			}
		}
	}

	// local
	public void Jump()
	{
		this.isJumping = true;
	}

	// local
	public void KickBack()
	{
		this.isKickingBack = true;
	}

	// local
	private IEnumerator MovementMessageRoutine()
	{
		this.timeBetweenMovementStart = Time.time;
		yield return new WaitForSeconds((1 / this.networkSendRate));
		this.SendMovementMessage();
	}

	// local
	private void SendMovementMessage()
	{
		this.timeBetweenMovementEnd = Time.time;

		this.playerManager.SendMovementMessage((int)this.netId.Value, this.transform.position, this.transform.rotation, 
			(this.timeBetweenMovementEnd - this.timeBetweenMovementStart));
		
		this.canSendMessage = true;
	}

	// local
	public void SetIsInAir(bool _isInAir)
	{
		this.isInAir = _isInAir;
	}

	// replication
	private void FixedUpdate()
	{
		if(isLocalPlayer)
		{
			return;
		}

		this.NetworkMovementLerp();
	}

	// replication
	public void ReceiveMovementMessage(Vector3 _position, Quaternion _rotation, float _time)
	{
		this.lastRealPosition = this.realPosition;
		this.lastRealRotation = this.realRotation;
		this.realPosition = _position;
		this.realRotation = _rotation;
		this.timeToLerp = _time;

		if(this.realPosition != this.transform.position)
		{
			this.isLerpingPosition = true;
		}

		if(this.realRotation.eulerAngles != this.transform.rotation.eulerAngles)
		{
			this.isLerpingRotation = true;
		}

		this.timeStartedLerping = Time.time;
	}

	// replication
	private void NetworkMovementLerp()
	{
		if(this.isLerpingPosition)
		{
			float lerpPercentage = (Time.time - this.timeStartedLerping) / this.timeToLerp;
			this.transform.position = Vector3.Lerp(this.lastRealPosition, this.realPosition, lerpPercentage);

			if(lerpPercentage >= 1.0f)
			{
				this.isLerpingPosition = false;
			}
		}

		if(this.isLerpingRotation)
		{
			float lerpPercentage = (Time.time - this.timeStartedLerping) / this.timeToLerp;
			this.transform.rotation = Quaternion.Lerp(this.lastRealRotation, this.realRotation, lerpPercentage);

			if(lerpPercentage >= 1.0f)
			{
				this.isLerpingRotation = false;
			}
		}
	}
}
