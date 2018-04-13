using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkManager
{
	private struct PlayerInfo
	{
		public NetworkConnection connection;
		public NetworkIdentity netowrkIdentity;
		public string name;
		public Color color;
	}
		
	private List<PlayerInfo> playerInfoList;

	#region Setup/Create

	private IEnumerator Start()
	{
		bool isWaiting = true;
		while(isWaiting)
		{
			if(Input.GetKeyDown(KeyCode.S))
			{
				isWaiting = false;
				yield return this.StartCoroutine(this.SetupManager());
				this.StartCoroutine(this.CreateServer());
				Debug.Log("Server selected");
			}
			else if(Input.GetKeyDown(KeyCode.C))
			{
				isWaiting = false;
				this.StartCoroutine(this.CreateClient());
				Debug.Log("Client selected");
			}

			yield return null;
		}
	}

	private IEnumerator SetupManager()
	{
		this.playerInfoList = new List<PlayerInfo>();

		yield return null;
	}

	private IEnumerator CreateServer()
	{
		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartServer();

		Debug.Log("Server created");
		yield return null;
	}

	private IEnumerator CreateClient()
	{
		NetworkManager.singleton.networkAddress = "192.168.2.99";
		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartClient();

		Debug.Log("Client created");
		yield return null;
	}

	#endregion

	#region Connect

	// Server - when the client has connected
	public override void OnServerConnect (NetworkConnection _networkConnection)
	{
		PlayerInfo pInfo = new PlayerInfo();
		pInfo.connection = _networkConnection;

		Debug.Log("Player " + _networkConnection.connectionId.ToString() + " has connected");
	}

	// Client - when the client has connected
	public override void OnClientConnect (NetworkConnection _connection)
	{
		// Ready client scene
		ClientScene.Ready(_connection);
		//ClientScene.AddPlayer(client.connection, 0);

		// bring up intro canvas

		Debug.Log("This client has connected");
	}

	#endregion

	#region Start Game

	// Client - tell the server to spawn a player for them
	public void StartGame()
	{
		ClientScene.AddPlayer(client.connection, 0);
	}

	#endregion

	#region Spawn Objects

	// Server - when the client calls AddPlayer()
	public override void OnServerAddPlayer (NetworkConnection _networkConnection, short _playerControllerId)
	{
		// Instantiate player prefab on all clients with requested client's authority
		GameObject player = Instantiate(this.playerPrefab) as GameObject;
		player.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
		player.transform.position = new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f));
		NetworkServer.AddPlayerForConnection(_networkConnection, player, _playerControllerId);
	}

	#endregion
}
