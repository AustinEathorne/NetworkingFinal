using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
	[SerializeField]
	private Camera camera;

	[SerializeField]
	private PlayerController playerController;

	public string name;

	public Color colour;

	private bool isInputEnabled = false;

	[SerializeField]
	private int health = 100;

	// Set object colour, etc
	public void Initialize(string _name, Color _colour, Vector3 _spawnPosition)
	{
		//InitializePlayerMessage msg = _networkMessage.ReadMessage<InitializePlayerMessage>();

		this.name = _name;
		this.GetComponent<MeshRenderer>().material.color = _colour;
		this.transform.position = _spawnPosition;
		 
		//Debug.Log("Player initialized");

		if(this.isLocalPlayer)
		{
			// enable input
			this.isInputEnabled = true;
			this.playerController.isInputEnabled = true;
			//Debug.Log("Player input enabled");

			// enable camera
			this.camera.enabled = true;
		}
	}

	private void Update()
	{
		if(this.isInputEnabled)
		{
			
		}
	}
}
