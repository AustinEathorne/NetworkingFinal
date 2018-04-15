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

	[Header("Properties")]
	[SerializeField]
	private float linearSpeed;
	[SerializeField]
	private float angularSpeed;
	[SerializeField]
	private float networkSendRate;
	private float networkSendCount = 0.0f;
	private float timeBetweenMovementStart = 0.0f;
	private float timeBetweenMovementEnd = 0.0f;

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
		this.canSendMessage = true;

		if(!isLocalPlayer)
		{
			this.realPosition = this.transform.position;
			this.realRotation = this.transform.rotation;
		}
	}

	private void Update()
	{
		if(!isLocalPlayer)
		{
			return;
		}

		this.UpdatePlayerMovement();
	}

	private void FixedUpdate()
	{
		if(isLocalPlayer)
		{
			return;
		}

		this.NetworkMovementLerp();
	}

	// local
	private void UpdatePlayerMovement()
	{
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		Vector3 linearVel = new Vector3(x * this.linearSpeed, 0.0f, z * this.linearSpeed);

//		Vector3 linearVel = this.transform.forward * (Input.GetAxis("Vertical") * this.linearSpeed);
//		Vector3 angularVel = this.transform.up * (Input.GetAxis("Horizontal") * this.angularSpeed);

		this.rigidbody.velocity = linearVel;
		this.rigidbody.angularVelocity = Vector3.zero;

		if(this.canSendMessage)
		{
			this.canSendMessage = false;
			this.StartCoroutine(this.MovementMessageRoutine());
		}
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

	private void NetworkMovementLerp()
	{
		if(this.isLerpingPosition)
		{
			float lerpPercentage = (Time.time - this.timeStartedLerping) / this.timeToLerp;
			this.transform.position = Vector3.Lerp(this.lastRealPosition, this.realPosition, lerpPercentage);

			if(lerpPercentage >= 1.0f)
			{
				this.isLerpingPosition = false;
				this.isLerpingRotation = false;
			}
		}

		if(this.isLerpingRotation)
		{
			float lerpPercentage = (Time.time - this.timeStartedLerping) / this.timeToLerp;
			this.transform.rotation = Quaternion.Lerp(this.lastRealRotation, this.realRotation, lerpPercentage);
		}
	}
}
