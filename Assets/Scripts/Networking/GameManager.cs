using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkManager
{
	public struct PlayerInfo
	{										// For Server List
		public NetworkConnection connection; // gets in OnServerConnect
		public NetworkIdentity networkIdentity; // gets in OnSpawn?
		public string name; // gets in CmdEnterLobby
		public Color colour; // gets in CmdEnterLobby
	}

	// client
	public PlayerInfo clientPlayerInfo; // used to set up player by client
	[SerializeField]
	private CanvasManager canvasManager;

	// server
	private List<PlayerInfo> playerInfoList; // used by server to update clients



	#region Setup/Create

	private IEnumerator SetupManager()
	{
		this.playerInfoList = new List<PlayerInfo>(4);

		yield return null;
	}

	public IEnumerator CreateServer()
	{
		yield return this.StartCoroutine(this.SetupManager());

		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartServer();

		// Register Handlers
		NetworkServer.RegisterHandler(CustomMsgType.PlayerInfo, this.OnPlayerInfoReceived);

		Debug.Log("Server created");
		yield return null;
	}

	public IEnumerator CreateClient()
	{
		NetworkManager.singleton.networkAddress = "192.168.2.99";
		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartClient();

		this.client.RegisterHandler(CustomMsgType.UpdateLobby, this.canvasManager.OnLobbyUpdateReceived);

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

		// Add player info to the list
		//this.playerInfoList[_networkConnection.connectionId - 1] = pInfo;

		Debug.Log("Player " + _networkConnection.connectionId.ToString() + " has connected");
	}

	// Client - when the client has connected
	public override void OnClientConnect (NetworkConnection _connection)
	{
		// Ready client scene
		ClientScene.Ready(_connection);
		//ClientScene.AddPlayer(client.connection, 0);

		// bring up intro canvas
		this.canvasManager.playerPreviewPanel.SetActive(true);
		this.canvasManager.colourPanel.SetActive(true);

		Debug.Log("This client has connected");
	}

	#endregion

	#region Menu

	// Client
	public void SendClientPlayerInformation()
	{
		RegisterPlayerInfoMessage msg = new RegisterPlayerInfoMessage();
		msg.connection = this.clientPlayerInfo.connection;
		msg.name = this.clientPlayerInfo.name;
		msg.colour = this.clientPlayerInfo.colour;

		this.client.Send(CustomMsgType.PlayerInfo, msg);
	}

	// Server
	public void OnPlayerInfoReceived(NetworkMessage _networkMessage) // pass the client's player info
	{
		

		// update the player info list for the sending client


		// Send msg back to all clients with updated lobby stats (enters lobby as well for client that called this)
		UpdateLobbyMessage msg = new UpdateLobbyMessage();
		msg.msg = "bahhhh";
		NetworkServer.SendToAll(CustomMsgType.UpdateLobby, msg);
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

#region Messages

public class CustomMsgType
{
	public static short PlayerInfo = MsgType.Highest + 1;
	public static short UpdateLobby = MsgType.Highest + 2;
}

public class RegisterPlayerInfoMessage : MessageBase
{
	public NetworkConnection connection;
	public string name;
	public Color colour;
}

public class UpdateLobbyInfoType
{
	public static short Info = MsgType.Highest + 2;
}

public class UpdateLobbyMessage : MessageBase
{
	public string msg;
}

#endregion