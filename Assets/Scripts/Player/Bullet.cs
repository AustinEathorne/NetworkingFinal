using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Bullet : NetworkBehaviour
{
	[SerializeField]
	NetworkIdentity networkIdentity;

	[SerializeField]
	private float destroyTime = 3.0f;
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
	public int ownerId;

	// Server
	void Start () 
	{
		if(!this.isClient)
		{
			this.StartCoroutine(this.MovementMessageRoutine());
			this.StartCoroutine(this.DeathRoutine());
		}
		else
		{
			this.realPosition = this.transform.position;
			this.realRotation = this.transform.rotation;
		}
	}

	// Server
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
		msg.objectType = 1;

		//NetworkManager.singleton.client.Send(CustomMsgType.Move, msg);

		NetworkServer.SendToAll(CustomMsgType.Move, msg);
	}

	// replication
	private void FixedUpdate()
	{
		if(!this.isClient)
		{
			return;
		}

		this.NetworkMovementLerp();
	}

	// replication - client
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

	// replication - client
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

	// Server
	private IEnumerator DeathRoutine()
	{
		yield return new WaitForSeconds(this.destroyTime);
		NetworkServer.Destroy(this.gameObject);
		//Debug.Log("Destroy bullet");
		yield return null;
	}

	public void OnCollisionEnter(Collision _collision)
	{
		if(this.isClient)
		{
			return;
		}

		//Debug.Log("Bullet Collision");

		if(_collision.gameObject.tag == "Player")
		{
			if((int)_collision.gameObject.GetComponent<NetworkIdentity>().netId.Value != this.ownerId)
			{
				GameManager temp =  GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
				temp.OnBulletHit((int)_collision.transform.GetComponent<NetworkIdentity>().netId.Value, this.ownerId);
				NetworkServer.Destroy(this.gameObject);
			}
		}
		else if(_collision.gameObject.tag == "Floor")
		{
			NetworkServer.Destroy(this.gameObject);
		}

	}
}
