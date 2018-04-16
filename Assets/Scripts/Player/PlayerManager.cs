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
	private PlayerCamera playerCamera;
	[SerializeField]
	private GameManager gameManager; // not used
	[SerializeField]
	private GameObject gameCanvas;

	[Header("Player Values")]
	[SerializeField]
	private int health = 100;
	public string name;
	public Color colour;

	private bool isInputEnabled = false;

	// Set object colour, etc
	public void Initialize(string _name, Color _colour, Vector3 _spawnPosition, GameManager _gameManager, GameObject _gameCanvas)
	{
		this.name = _name;
		this.GetComponent<MeshRenderer>().material.color = _colour;
		this.transform.position = _spawnPosition;
		this.gameManager = _gameManager;
		this.gameCanvas = _gameCanvas;
		//Debug.Log("Player initialized");

		if(this.isLocalPlayer)
		{
			// enable input
			this.isInputEnabled = true;
			this.playerController.SetIsEnabled(true);
			//Debug.Log("Player input enabled");

			// enable camera
			this.playerCamera.EnableCamera(true);

			this.gameCanvas.GetComponent<Canvas>().worldCamera = this.playerCamera.GetCamera();
			this.gameCanvas.GetComponent<Canvas>().planeDistance = 1;
			// TODO set active on first update
			this.gameCanvas.SetActive(true);

			Debug.Log("Initialized local player");
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
}
