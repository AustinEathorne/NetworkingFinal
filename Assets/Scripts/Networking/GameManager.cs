using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkManager
{
	public struct PlayerInfo
	{
		public int connectionId;
		public int playerObjectId;
		public string name;
		public Color colour;
	}


	// server
	private List<PlayerInfo> playerInfoList; // used by server to update clients (connectiond id, player info)
	private List<bool> isClientConnected;

	// client
	public PlayerInfo clientPlayerInfo; // used to set up player by client

	[SerializeField]
	private CanvasManager canvasManager;



	#region Setup/Create

	private IEnumerator SetupManager()
	{
		this.playerInfoList = new List<PlayerInfo>();

		this.isClientConnected = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isClientConnected.Add(false);

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

		this.client.RegisterHandler(CustomMsgType.JoinLobby, this.canvasManager.OnLobbyUpdateReceived);

		Debug.Log("Client created");
		yield return null;
	}

	#endregion

	#region Connect

	// Server - when the client has connected
	public override void OnServerConnect (NetworkConnection _networkConnection)
	{
		// Create new player info
		PlayerInfo temp  = new PlayerInfo();
		temp.connectionId = _networkConnection.connectionId;

		// Add player info to the list
		this.playerInfoList.Add(temp);

		//TODO: update this... only works if no one drops from lobby
		this.isClientConnected[(_networkConnection.connectionId - 1)] = true;

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
		msg.name = this.clientPlayerInfo.name;
		msg.colour = this.clientPlayerInfo.colour;

		this.client.Send(CustomMsgType.PlayerInfo, msg);
	}

	// Server
	public void OnPlayerInfoReceived(NetworkMessage _networkMessage) // pass the client's player info
	{
		Debug.Log("Player " + _networkMessage.conn.connectionId.ToString() + "'s info received!");

		//Read msg with player info
		RegisterPlayerInfoMessage infoMsg = _networkMessage.ReadMessage<RegisterPlayerInfoMessage>();

		// update the player info list for the sending client
		PlayerInfo temp = this.playerInfoList[(_networkMessage.conn.connectionId - 1)];
		temp.name = infoMsg.name;
		temp.colour = infoMsg.colour;
		this.playerInfoList[(_networkMessage.conn.connectionId - 1)] = temp;

		// Create msg for all clients
		UpdateLobbyMessage msg = new UpdateLobbyMessage();
		msg.connectionId = _networkMessage.conn.connectionId;
		msg.isPlayerConnected = this.isClientConnected.ToArray();

		string[] names = new string[maxConnections];
		for(int i = 0; i < maxConnections; i++)
		{
			if(this.isClientConnected[i])
			{
				names[i] = this.playerInfoList[i].name;
			}
		}
		msg.playerNames = names;

		Color[] colours = new Color[maxConnections];
		for(int i = 0; i < maxConnections; i++)
		{
			if(this.isClientConnected[i])
			{
				colours[i] = this.playerInfoList[i].colour;
			}
		}
		msg.playerColours = colours;

		// Send to all
		NetworkServer.SendToAll(CustomMsgType.JoinLobby, msg);

		// update all client's lobby UI
		Debug.Log("Updating " + this.playerInfoList.Count.ToString() + " clients' lobbies");
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
	public static short JoinLobby = MsgType.Highest + 2;
}

public class RegisterPlayerInfoMessage : MessageBase
{
	public string name;
	public Color colour;
}

public class UpdateLobbyType
{
	public static short Info = MsgType.Highest + 2;
}

public class UpdateLobbyMessage : MessageBase
{
	public int connectionId;
	public bool[] isPlayerConnected;
	public string[] playerNames;
	public Color[] playerColours;
}

#endregion