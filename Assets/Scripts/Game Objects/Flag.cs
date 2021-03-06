﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Flag : NetworkBehaviour
{
	private bool isHeld = false;
	public bool IsHeld 
	{
		get { return this.isHeld; }
		set { this.isHeld = value; }
	}

	[SerializeField]
	private float launchSpeed;
	[SerializeField]
	private Rigidbody rb;
	[SerializeField]
	private Collider collider;
	[SerializeField]
	private ParticleSystem particleSystem;

	[SerializeField]
	NetworkIdentity networkIdentity;
	[SerializeField]
	private float networkSendRate = 5;
	private float networkSendCount = 0.0f;
	private float timeBetweenMovementStart = 0.0f;
	private float timeBetweenMovementEnd = 0.0f;

	// Replication
	private bool isLerpingPosition = false;
	private bool isLerpingRotation = false;
	private Vector3 realPosition;
	private Quaternion realRotation;
	private Vector3 lastRealPosition;
	private Quaternion lastRealRotation;
	private float timeStartedLerping;
	private float timeToLerp;

	public GameManager gameManager;

	// Server
	private float lerpSpeed = 3.0f;
	private float stopDistance = 1.5f;
	private Transform holderTransform;


	void Start () 
	{
		if(!this.isClient)
		{
			this.StartCoroutine(this.MovementMessageRoutine());
			this.rb = this.GetComponent<Rigidbody>();
		}
		else
		{
			this.realPosition = this.transform.position;
			this.realRotation = this.transform.rotation;
		}
	}

	// Server & client
	private IEnumerator MovementMessageRoutine()
	{
		this.timeBetweenMovementStart = Time.time;
		yield return new WaitForSeconds((1 / this.networkSendRate));
		this.SendMovementMessage();
		this.StartCoroutine(this.MovementMessageRoutine());
	}

	// Server
	private void SendMovementMessage()
	{
		this.timeBetweenMovementEnd = Time.time;
		MoveMessage msg = new MoveMessage();

		msg.objectId = (int)this.netId.Value;
		msg.position = this.transform.position;
		msg.rotation = this.transform.rotation;
		msg.time = (this.timeBetweenMovementEnd - this.timeBetweenMovementStart);
		msg.objectType = 2;

		//NetworkManager.singleton.client.Send(CustomMsgType.Move, msg);

		NetworkServer.SendToAll(CustomMsgType.Move, msg);
	}
		
	// Server
	public IEnumerator DropFlag()
	{
		//this.transform.parent = null;
		this.holderTransform = null;
		this.rb.isKinematic = false;

		// shoot flag up into the air
		Vector3 vel = Vector3.up * this.launchSpeed;
		this.rb.velocity = vel;

		yield return new WaitForSeconds(1.5f);
		this.collider.enabled = true;
		this.isHeld = false;
	}

	// Server
	private void OnTriggerEnter(Collider _col)
	{
		if(!isServer)
		{
			return;
		}

		if(this.isHeld)
		{
			return;
		}

		if(_col.transform.tag == "Player")
		{
			this.isHeld = true;
			this.collider.enabled = false;
			//this.transform.parent = _col.transform;
			this.holderTransform = _col.transform;
			this.rb.isKinematic = true;
			this.gameManager.OnFlagInteraction(true, (int)_col.transform.GetComponent<NetworkIdentity>().netId.Value);
		}
	}

	private void LerpToHolder()
	{
		if(this.isHeld && this.holderTransform)
		{
			if(Vector3.Distance(this.transform.position, this.holderTransform.position) > this.stopDistance)
			{
				this.transform.position = Vector3.Lerp(this.transform.position, this.holderTransform.position, this.lerpSpeed * Time.deltaTime);
			}
		}
	}

	// Replication - client
	private void FixedUpdate()
	{
		if(!this.isClient)
		{
			this.LerpToHolder();
			return;
		}

		this.NetworkMovementLerp();
	}

	// Replication - client
	public void OnMovementReceived(Vector3 _position, Quaternion _rotation, float _time)
	{
		if(!this.isClient)
		{
			return;
		}

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

	// Replication - client
	private void NetworkMovementLerp()
	{
		//Debug.Log("Movement Lerp");

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

	// Replication - client
	public void OnFlagInteraction(bool _isHeld)
	{
		if(_isHeld)
		{
			this.particleSystem.Stop();
		}
		else
		{
			this.particleSystem.Play();
		}
	}
}
