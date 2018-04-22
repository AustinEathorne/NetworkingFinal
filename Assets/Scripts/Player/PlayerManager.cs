using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerManager : NetworkBehaviour
{
	[Header("Components")]
	[SerializeField]
	private PlayerController playerController;
	[SerializeField]
	private PlayerMovement playerMovement;
	[SerializeField]
	private PlayerWeapon playerWeapon;
	[SerializeField]
	private PlayerCamera playerCamera;
	[SerializeField]
	private GameManager gameManager; // not used
	[SerializeField]
	private GameObject gameCanvas;

	[Header("Particles")]
	[SerializeField]
	private ParticleSystem deathParticle;
	[SerializeField]
	private ParticleSystem gunShotParticle;
	[SerializeField]
	private ParticleSystem bulletParticle;

	[Header("Player Values")]
	[SerializeField]
	private int health = 100;
	[SerializeField]
	private string name;
	[SerializeField]
	private Color colour;
	[SerializeField]
	private bool hasFlag = false;

	private Vector3 nextSpawnPosition = new Vector3(0.0f, 0.0f, 0.0f);

	private bool isInputEnabled = false;

	private int netIdValue;


	// Set object colour, etc
	public void Initialize(string _name, Color _colour, Vector3 _spawnPosition, GameManager _gameManager, GameObject _gameCanvas)
	{
		this.name = _name;
		this.GetComponent<MeshRenderer>().material.color = _colour;
		this.transform.position = _spawnPosition;
		this.gameManager = _gameManager;
		this.gameCanvas = _gameCanvas;

		this.bulletParticle.Play();

		if(this.isLocalPlayer)
		{
			// enable camera
			this.playerCamera.EnableCamera(true);

			this.gameCanvas.GetComponent<Canvas>().worldCamera = this.playerCamera.GetCamera();
			this.gameCanvas.GetComponent<Canvas>().planeDistance = 1;
			// TODO set active on first update
			this.gameCanvas.SetActive(true);

			Debug.Log("Initialized local player");
		}
	}

	public void EnableInput(bool _value)
	{
		if(this.isLocalPlayer)
		{
			this.isInputEnabled = _value;
			this.playerController.SetIsEnabled(_value);

			//Debug.Log("Input Enabled: " + _value.ToString());
		}
	}

	public void SendMovementMessage(int _objectId, Vector3 _position, Quaternion _rotation, float _time)
	{
		MoveMessage msg = new MoveMessage();
		msg.objectId = _objectId;
		msg.position = _position;
		msg.rotation = _rotation;
		msg.time = _time;
		msg.objectType = 0;

		NetworkManager.singleton.client.Send(CustomMsgType.Move, msg);
		//Debug.Log("Sent movement msg");
	}

	// Replication
	public void OnPlayerMove(Vector3 _position, Quaternion _rotation, float _time)
	{
		if(isLocalPlayer)
			return;

		this.playerMovement.ReceiveMovementMessage(_position, _rotation, _time);
	}

	// Local/Replication
	public void OnShotTaken()
	{
		// Player Shot Taken particle
		this.gunShotParticle.Play();

		// Emit bullet particle
		this.bulletParticle.Emit(1);

		// Add vel to the last particle in our forward direction
		ParticleSystem.Particle[] particles = new ParticleSystem.Particle[this.bulletParticle.particleCount];
		int count = this.bulletParticle.GetParticles(particles);
		particles[count - 1].velocity = this.transform.forward * this.playerWeapon.bulletSpeed;
		this.bulletParticle.SetParticles(particles, count);
	}

	// Local/Replication
	public void OnDeath(Vector3 _nextSpawnPosition)
	{
		// set next spawn pos
		this.nextSpawnPosition = _nextSpawnPosition;

		// Player Death particle
		this.deathParticle.Play();

		// Disable input
		if(isLocalPlayer)
		{
			this.isInputEnabled = false;
			this.playerController.SetIsEnabled(false);
		}
	}
		
	// Local/Replication
	public IEnumerator OnRespawn(float _time)
	{
		// run canvas routine if local player

		yield return new WaitForSeconds(_time);

		// Move to spawn
		this.transform.position = this.nextSpawnPosition;

		// Enable Input
		if(isLocalPlayer)
		{
			this.isInputEnabled = true;
			this.playerController.SetIsEnabled(true);
		}

		yield return null;
	}

	// Local
	public void DropFlag()
	{
		if(this.hasFlag)
		{
			Debug.Log("Drop Flag");
			this.hasFlag = false;

			// send msg to server
			FlagDropMessage msg = new FlagDropMessage();
			msg.playerId = (int)this.netId.Value;
			NetworkManager.singleton.client.Send(CustomMsgType.DropFlag, msg);
		}
	}

	public void SetHasFlag(bool _value)
	{
		Debug.Log("Set has flag: " + _value.ToString());
		this.hasFlag = _value;
	}

	public bool GetHasFlag()
	{
		return this.hasFlag;
	}
}
