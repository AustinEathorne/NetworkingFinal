﻿using System.Collections;
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
	private List<bool> isPlayerReadyList;

	// client
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
		this.client.RegisterHandler(CustomMsgType.UpdateLobby, this.canvasManager.OnLobbyUpdateReceived);
		this.client.RegisterHandler(CustomMsgType.UpdatePlayerReady, this.canvasManager.OnPlayerReadyStatusReceived);

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
		UpdateLobbyMessage msg = new UpdateLobbyMessage();
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

		// Send to all
		NetworkServer.SendToAll(CustomMsgType.UpdateLobby, msg);
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
		}

		this.SendLobbyUpdates();

		//UpdatePlayerReadyStatusMessage msg = new UpdatePlayerReadyStatusMessage();
		//msg.isReadyList = this.isPlayerReadyList.ToArray();

		// Send to all
		//NetworkServer.SendToAll(CustomMsgType.UpdatePlayerReady, msg);
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
	public static short ReadyUp = MsgType.Highest + 3;
	public static short UpdatePlayerReady = MsgType.Highest + 4;
}

// Client to Server
public class RegisterPlayerInfoMessage : MessageBase
{
	public string name;
	public Color colour;
}

// Server to Clients
public class UpdateLobbyMessage : MessageBase
{
	public int connectionId;
	public bool[] isPlayerConnected;
	public bool[] isReadyList;
	public string[] playerNames;
	public Color[] playerColours;
}

// Client to Server
public class ReadyUpMessage : MessageBase
{
	public bool isReady;
}

// Server to Clients
public class UpdatePlayerReadyStatusMessage : MessageBase
{
	public bool[] isReadyList;
}

#endregion