using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkManager
{
	public struct PlayerInfo
	{
		public int connectionId;
		public NetworkInstanceId playerObjectId;
		public string name;
		public Color colour;
	}

	// Server
	private float lobbyCountDownStartTime = 10.0f;
	private float lobbyCountDownTime = 10.0f;
	private int lastLobbyCountDownTimeSent = 10;
	private bool isLobbyTimerCountingDown = false;

	private List<PlayerInfo> playerInfoList; // used by server to update clients (connectiond id, player info)
	private List<bool> isClientConnected;
	private List<bool> isPlayerReadyList;

	private bool hasMadeSelection = false;
	private bool isGameStarted = false;


	// Client
	public PlayerInfo clientPlayerInfo; // used to set up player by client
	private bool isClientReady = false; // used by the client when ready to play the game 'ready up'

	[SerializeField]
	private CanvasManager canvasManager;


	#region Setup/Create

	private IEnumerator SetupManager()
	{
		this.playerInfoList = new List<PlayerInfo>();

		this.isClientConnected = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isClientConnected.Add(false);

		this.isPlayerReadyList = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isPlayerReadyList.Add(false);

		yield return null;
	}

	public IEnumerator CreateServer()
	{
		// Wait for manager to setup
		yield return this.StartCoroutine(this.SetupManager());

		// Start Server
		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartServer();

		// Register Handlers
		NetworkServer.RegisterHandler(CustomMsgType.PlayerInfo, this.OnPlayerInfoReceived);
		NetworkServer.RegisterHandler(CustomMsgType.ReadyUp, this.OnReadyUpMessage);

		this.hasMadeSelection = true;

		Debug.Log("Server created");
		yield return null;
	}

	public IEnumerator CreateClient()
	{
		// Start Client
		NetworkManager.singleton.networkAddress = "192.168.2.99";
		NetworkManager.singleton.networkPort = 4444;
		NetworkManager.singleton.StartClient();

		// Register Handlers
		this.client.RegisterHandler(CustomMsgType.Lobby, this.canvasManager.OnLobbyUpdateReceived);
		this.client.RegisterHandler(CustomMsgType.StartGame, this.OnGameStart);

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

		this.SendLobbyUpdates();

		// update all client's lobby UI
		Debug.Log("Updating " + this.playerInfoList.Count.ToString() + " clients' lobbies");
	}
	//Server
	private void SendLobbyUpdates()
	{
		// Create msg for all clients
		LobbyMessage msg = new LobbyMessage();
		//msg.connectionId = _networkMessage.conn.connectionId;

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

		msg.isPlayerConnected = this.isClientConnected.ToArray();
		msg.isReadyList = this.isPlayerReadyList.ToArray();

		msg.isLobbyCountingDown = this.isLobbyTimerCountingDown;
		msg.countDownTime = this.lastLobbyCountDownTimeSent;

		// Send to all
		NetworkServer.SendToAll(CustomMsgType.Lobby, msg);
	}

	// Client
	public void SendReadyUpMessage()
	{
		// update button text
		this.canvasManager.UpdateReadyButtonText(!this.isClientReady);

		// Send msg
		ReadyUpMessage msg = new ReadyUpMessage();
		this.isClientReady = !this.isClientReady;
		msg.isReady = this.isClientReady;
		this.client.Send(CustomMsgType.ReadyUp, msg);
	}
	// Server
	public void OnReadyUpMessage(NetworkMessage _networkMessage)
	{
		ReadyUpMessage incomingMsg = _networkMessage.ReadMessage<ReadyUpMessage>();

		if(incomingMsg.isReady)
		{
			Debug.Log("Player " + _networkMessage.conn.connectionId.ToString() + " is ready");
			this.isPlayerReadyList[_networkMessage.conn.connectionId - 1] = true;
		}
		else
		{
			Debug.Log("Player " + _networkMessage.conn.connectionId.ToString() + " is NOT ready");
			this.isPlayerReadyList[_networkMessage.conn.connectionId - 1] = false;
			this.isLobbyTimerCountingDown = false;
		}

		this.SendLobbyUpdates();
	}

	#endregion

	#region Start Game

	// Server - tell the clients the game has started
	public void StartGame()
	{
		StartGameMessage msg = new StartGameMessage();
		NetworkServer.SendToAll(CustomMsgType.StartGame, msg);

	}

	// Client - tell the server to spawn a player for them
	public void OnGameStart(NetworkMessage _networkMessage)
	{
		// Close Menu UI
		//this.canvasManager.CloseMenu();

		//Read msg

		// Add player
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

		PlayerInfo temp = playerInfoList[ _networkConnection.connectionId - 1];
		temp.playerObjectId = player.GetComponent<NetworkIdentity>().netId;
		playerInfoList[ _networkConnection.connectionId - 1] = temp;

		Debug.Log("Player: " + _networkConnection.connectionId.ToString() + " has obj Id: " + temp.playerObjectId.ToString());
	}

	#endregion

	#region Update

	private void Update()
	{
		this.NetworkLobbyUpdate();
	}

	private void NetworkLobbyUpdate()
	{
		if(!Network.isClient && !hasMadeSelection)
			return;

		if(this.isGameReady() && !this.isGameStarted)
		{
			//Debug.Log("COUNTING DOWN: " + this.lobbyCountDownTime.ToString());
			this.isLobbyTimerCountingDown = true;
			this.lobbyCountDownTime -= Time.deltaTime;

			// Start the game
			if(this.lobbyCountDownTime <= 0.0f)
			{
				Debug.Log("START GAME");
				this.isGameStarted = true;
				this.StartGame();
			}

			// Send lobby update
			if(Mathf.Floor(this.lobbyCountDownTime) < this.lastLobbyCountDownTimeSent)
			{
				this.lastLobbyCountDownTimeSent = (int)Mathf.Floor(this.lobbyCountDownTime);
				this.SendLobbyUpdates();
			}
		}
		else
		{
			this.lobbyCountDownTime = this.lobbyCountDownStartTime;
			this.lastLobbyCountDownTimeSent = (int)this.lobbyCountDownStartTime;
			this.isLobbyTimerCountingDown = false;
		}
	}

	private bool isGameReady()
	{
		int readyPlayers = 0;

		// Get number of ready players
		for(int i = 0; i < this.isPlayerReadyList.Count; i++)
		{
			if(this.isPlayerReadyList[i])
			{
				readyPlayers++;
			}
		}

		int numClients = 0;

		// Get number of connected clients
		for(int i = 0; i < this.isClientConnected.Count; i++)
		{
			if(this.isClientConnected[i])
			{
				numClients++;
			}
		}

		// Check for at least two players and that everyone is ready
		if(readyPlayers > 1 && readyPlayers == numClients)
			return true;
		else
			return false;

	}
		
	#endregion
}

#region Messages

public class CustomMsgType
{
	public static short PlayerInfo = MsgType.Highest + 1;
	public static short Lobby = MsgType.Highest + 2;
	public static short ReadyUp = MsgType.Highest + 3;
	public static short StartGame = MsgType.Highest + 4;
}

// Client to Server
public class RegisterPlayerInfoMessage : MessageBase
{
	public string name;
	public Color colour;
}

// Server to Clients
public class LobbyMessage : MessageBase
{
	public int connectionId;
	public bool[] isPlayerConnected;
	public bool[] isReadyList;
	public string[] playerNames;
	public Color[] playerColours;
	public bool isLobbyCountingDown = false;
	public int countDownTime;
}

// Client to Server
public class ReadyUpMessage : MessageBase
{
	public bool isReady;
}

// Server to client
public class StartGameMessage : MessageBase
{
	public string name;
	public Color colour;
}

// Client to Server
public class MoveMessage : MessageBase
{
	public Vector3 position;
	public Vector3 rotation;
}

// Server to client
public class TransformMessage : MessageBase
{
	public Vector3 position;
	public Vector3 rotation;
}

#endregion