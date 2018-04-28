using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// CTRL + SHIFT + A = your best friend
// CTRL + M + O? in vs

#region Network/Game Manager

public class GameManager : NetworkManager
{
	public struct PlayerInfo
	{
		public int connectionId;
		public int playerIndex; // player 0 to 3
		public uint playerObjectId;
		public string name;
		public Color colour;
		public bool hasFlag;
	}

	// Server
	private float lobbyCountDownStartTime = 11.0f;
	private float lobbyCountDownTime = 11.0f;
	private int lastLobbyCountDownTimeSent = 11;
	private bool isLobbyTimerCountingDown = false;

	private List<PlayerInfo> playerInfoList; // used by server to update clients (connectiond id, player info)
	private List<bool> isClientSlotTaken; //  for keeping clients ordered if a client drops
	private List<bool> isClientConnected;
	private List<bool> isPlayerReadyList;

	private bool hasMadeSelection = false;
	private bool isGameStarted = false; // TODO: change to enum for states
	private bool hasInitializedPlayers = false;
	private bool isPlayingGame = false;

	// Client
	public PlayerInfo clientPlayerInfo; // used to set up player by client
	private bool isClientReady = false; // used by the client when ready to play the game 'ready up'

	[Header("Debug")]
	[SerializeField]
	private string netAddressToUse; // Client & Server
	[SerializeField]
	private int portToUse; // Client & Server

	[Header("Components/Objects")]
	[SerializeField]
	private CanvasManager canvasManager; // Client
	[SerializeField]
	private List<GameObject> menuCameras; // Client & Server
	[SerializeField]
	private GameObject serverCam; // Server
	[SerializeField]
	private GameObject gameCanvas; // Server

	[Header("Level")]
	[SerializeField]
	private GameObject levelParent; // Client & Server
	[SerializeField]
	private List<Transform> spawnTransforms; // Server
	[SerializeField]
	private Transform flagSpawn; // Server
	[SerializeField]
	private GameObject bullet; // Server
	private Flag gameFlag; // Server
	private int flagId; // Server

	[Header("Player")]
	[SerializeField]
	private int baseHealth = 100; // Server
	[SerializeField]
	private int baseDamage = 10; // Server
	[SerializeField]
	private List<int> playerHealthList; // Server

	[Header("Time")]
	[SerializeField]
	private float gameTime = 60.0f; // Server
	[SerializeField]
	private float countdownTime = 4.0f; // Server
	[SerializeField]
	private float respawnTime = 3.0f; // Server
	[SerializeField]
	private float scoreAdditionInterval;

	private float currentGameTime = 0.0f; // Server
	private int lastSentGameTime = 0; // Server


	[SerializeField]
	private List<int> playerScoreList; // Server

	#region Setup/Create

	public void SetConnectionInfo(string _address, int _port)
	{
		this.netAddressToUse = _address;
		this.portToUse = _port;
	}

	private IEnumerator SetupManager()
	{
		this.playerInfoList = new List<PlayerInfo>();
		for(int i = 0; i < maxConnections; i++)
			this.playerInfoList.Add(new PlayerInfo());

		this.isClientConnected = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isClientConnected.Add(false);

		this.isPlayerReadyList = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isPlayerReadyList.Add(false);

		this.isClientSlotTaken = new List<bool>();
		for(int i = 0; i < maxConnections; i++)
			this.isClientSlotTaken.Add(false);

		yield return null;
	}

	public IEnumerator CreateServer()
	{
		// Wait for manager to setup
		yield return this.StartCoroutine(this.SetupManager());

		// Start Server
		this.networkPort = this.portToUse;
		this.networkAddress = this.netAddressToUse;
		NetworkManager.singleton.StartServer();

		// Register Handlers
		yield return this.StartCoroutine(this.RegisterServerMessages());

		// Switch flag
		this.hasMadeSelection = true;

		Debug.Log("Server created");
		yield return null;
	}

	public IEnumerator CreateClient()
	{
		// Start Client
		NetworkManager.singleton.networkAddress = this.netAddressToUse;
		NetworkManager.singleton.networkPort = this.portToUse;
		NetworkManager.singleton.StartClient();

		// Register Handlers
		yield return this.StartCoroutine(this.RegisterClientMessages());

		Debug.Log("Client created");
		yield return null;
	}

	private IEnumerator RegisterServerMessages()
	{
		NetworkServer.RegisterHandler(CustomMsgType.PlayerInfo, this.OnPlayerInfoReceived);
		NetworkServer.RegisterHandler(CustomMsgType.ReadyUp, this.OnReadyUpMessage);
		NetworkServer.RegisterHandler(CustomMsgType.Move, this.OnMovement);
		NetworkServer.RegisterHandler(CustomMsgType.BulletSpawn, this.OnBulletSpawn);
		NetworkServer.RegisterHandler(CustomMsgType.DropFlag, this.OnFlagDrop);
		yield return null;
	}

	private IEnumerator RegisterClientMessages()
	{
		this.client.RegisterHandler(CustomMsgType.Lobby, this.canvasManager.OnLobbyUpdateReceived);
		this.client.RegisterHandler(CustomMsgType.StartGame, this.OnGameStart);
		this.client.RegisterHandler(CustomMsgType.InitPlayer, this.OnPlayerInitialize);
		this.client.RegisterHandler(CustomMsgType.Move, this.OnMoveMessage);
		this.client.RegisterHandler(CustomMsgType.GameUISetup, this.OnGameUISetupMessage);
		this.client.RegisterHandler(CustomMsgType.Health, this.OnHealthMessage);
		this.client.RegisterHandler(CustomMsgType.Death, this.OnDeathMessage);
		this.client.RegisterHandler(CustomMsgType.Flag, this.OnFlagInteraction);
		this.client.RegisterHandler(CustomMsgType.ShotBullet, this.OnShotBulletMessage);
		this.client.RegisterHandler(CustomMsgType.GameTime, this.OnGameTimeMessage);
		this.client.RegisterHandler(CustomMsgType.GameScore, this.OnScoreMessage);
		this.client.RegisterHandler(CustomMsgType.Countdown, this.OnGameCountDown);
		this.client.RegisterHandler(CustomMsgType.Input, this.OnInputEnabled);
		yield return null;
	}

	#endregion

	#region Connect / Disconnect

	// Server - when a client connects
	public override void OnServerConnect (NetworkConnection _networkConnection)
	{
		// Create new player info
		PlayerInfo newPlayerInfo  = new PlayerInfo();

		// Find an open slot to assign the player
		for(int i = 0; i < this.maxConnections; i++)
		{
			if(this.isClientSlotTaken[i] == false)
			{
				// Set flags for client slot taken and connected client
				this.isClientSlotTaken[i] = true;
				this.isClientConnected[i] = true;

				// Update player info
				newPlayerInfo.connectionId = _networkConnection.connectionId;
				newPlayerInfo.playerIndex = i;
				newPlayerInfo.hasFlag = false;
				this.playerHealthList[i] = this.baseHealth;

				// Add new player info to list
				this.playerInfoList[i] = newPlayerInfo;

				break;
			}
		}

		Debug.Log("Player " + _networkConnection.connectionId.ToString() + " has connected");
	}

	// Client - when connected to a server
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

	// Server - when a client disconnects
	public override void OnServerDisconnect (NetworkConnection _connection)
	{
		// Check where we are in the game
		if(this.isGameStarted)
		{
			return;
		}
		else
		{
			Debug.Log("Lost connection: " + _connection.connectionId.ToString());
			this.StartCoroutine(this.OnLostClientMenu(_connection));
		}
	}

	// Client - when disconnected from a server
	public override void OnClientDisconnect (NetworkConnection _connection)
	{
		base.OnClientDisconnect (_connection);
	}

	// Client - called when requesting a disconnect
	public IEnumerator DisconnectClient()
	{
		Debug.Log("Attempting disconnect");
		NetworkManager.singleton.StopClient();
		this.canvasManager.DisplayLeaving();

		this.clientPlayerInfo = new PlayerInfo();
		this.isClientReady = false;

		// update button text
		this.canvasManager.UpdateReadyButtonText(false);

		yield return new WaitForSeconds(5.0f); //  buffer time to disconnect
		Debug.Log("Disconnected succefully");
		this.canvasManager.OpenMenu();
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

		string lobbyMsg = "";

		// find the sending client's player in our info list by connection id
		for(int i = 0; i < this.maxConnections; i++)
		{
			if(this.playerInfoList[i].connectionId == _networkMessage.conn.connectionId)
			{
				// update the player info list for the sending client
				PlayerInfo temp = this.playerInfoList[i];
				temp.name = infoMsg.name;
				temp.colour = infoMsg.colour;
				this.playerInfoList[i] = temp;

				lobbyMsg = temp.name + " has joined";
			}
		}



		this.SendLobbyUpdates(lobbyMsg);
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

		string lobbyMsg = "";

		// Find player in our info list
		for(int i = 0; i < this.maxConnections; i++)
		{
			if(this.playerInfoList[i].connectionId == _networkMessage.conn.connectionId)
			{
				// Set our ready bool in our list
				this.isPlayerReadyList[i] = incomingMsg.isReady;

				if(incomingMsg.isReady)
				{
					//Debug.Log("Player " + _networkMessage.conn.connectionId.ToString() + " is ready");
					lobbyMsg = this.playerInfoList[i].name + " is ready";
				}
				else
				{
					//Debug.Log("Player " + _networkMessage.conn.connectionId.ToString() + " is NOT ready");
					lobbyMsg = this.playerInfoList[i].name + " is not ready";
					this.isLobbyTimerCountingDown = false;
				}
			}
		}



		this.SendLobbyUpdates(lobbyMsg);
	}

	//Server
	private void SendLobbyUpdates(string _lobbyMsg)
	{
		// Create msg for all clients
		LobbyMessage msg = new LobbyMessage();
		//msg.connectionId = _networkMessage.conn.connectionId;

		string[] names = new string[maxConnections];
		Color[] colours = new Color[maxConnections];
		for(int i = 0; i < maxConnections; i++)
		{
			if(this.isClientConnected[i])
			{
				names[i] = this.playerInfoList[i].name;
				colours[i] = this.playerInfoList[i].colour;
			}
		}

		msg.playerNames = names;
		msg.playerColours = colours;

		msg.isPlayerConnected = this.isClientConnected.ToArray();
		msg.isReadyList = this.isPlayerReadyList.ToArray();

		msg.isLobbyCountingDown = this.isLobbyTimerCountingDown;
		msg.countDownTime = this.lastLobbyCountDownTimeSent;

		msg.lobbyMsg = _lobbyMsg;

		// Send to all
		NetworkServer.SendToAll(CustomMsgType.Lobby, msg);
	}

	// Server
	private IEnumerator OnLostClientMenu(NetworkConnection _connection)
	{
		bool foundPlayer = false;

		string lobbyMsg = "";

		// Shift player info in list
		for(int i = 0; i < this.maxConnections; i++)
		{
			// Find the player that we've lost
			if(this.playerInfoList[i].connectionId == _connection.connectionId && foundPlayer == false)
			{
				foundPlayer = true;
				lobbyMsg = this.playerInfoList[i].name + " has left";
			}

			// Make sure we're not on the last element
			if(i < (this.maxConnections - 1))
			{
				// Shift elements down
				if(foundPlayer)
				{
					this.playerInfoList[i] = this.playerInfoList[i + 1];
					this.isClientConnected[i] = this.isClientConnected[i + 1];
					this.isClientSlotTaken[i] = this.isClientSlotTaken[i + 1];
					this.isPlayerReadyList[i] = this.isPlayerReadyList[i + 1];
				}
			}
			else
			{
				// Set last element
				if(foundPlayer)
				{
					this.playerInfoList[i] = new PlayerInfo();
					this.isClientConnected[i] = false;
					this.isClientSlotTaken[i] = false;
					this.isPlayerReadyList[i] = false;
				}
				else
				{
					Debug.Log("FUCKKKKKKKDHSJBFGKLDSHBGF");
				}
			}
		}

		//yield return new WaitForSeconds(0.5f);

		// send lobby updates
		this.SendLobbyUpdates(lobbyMsg);

		yield return null;
	}

	#endregion

	#region Start Game

	// Server - tell the clients the game has started
	public void StartGame()
	{
		// Turn on level
		this.levelParent.SetActive(true);

		// Spawn flag
		this.SpawnFlag();

		this.canvasManager.CloseMenu();

		this.serverCam.SetActive(true);
		this.menuCameras[0].SetActive(false);
		this.menuCameras[1].SetActive(false);

		StartGameMessage msg = new StartGameMessage();
		NetworkServer.SendToAll(CustomMsgType.StartGame, msg);
	}

	// Client - tell the server to spawn a player for them
	public void OnGameStart(NetworkMessage _networkMessage)
	{
		// Turn on level for this client
		if(_networkMessage.conn.connectionId == client.connection.connectionId)
		{
			this.levelParent.SetActive(true);
		}

		// Add player
		ClientScene.AddPlayer(client.connection, 0);

		// Turn off menu cameras
		this.menuCameras[0].gameObject.SetActive(false);
		this.menuCameras[1].gameObject.SetActive(false);
	}

	#endregion

	#region Spawn Objects

	// Server - when the client calls AddPlayer()
	public override void OnServerAddPlayer (NetworkConnection _networkConnection, short _playerControllerId)
	{
		// Instantiate player prefab on all clients with requested client's authority
		GameObject player = Instantiate(this.playerPrefab) as GameObject;
		NetworkServer.AddPlayerForConnection(_networkConnection, player, _playerControllerId);

		//TODO: find player info that matches network connection inour player info list, then get the net id
		for(int i = 0; i < this.maxConnections; i++)
		{
			if(this.playerInfoList[i].connectionId == _networkConnection.connectionId)
			{
				PlayerInfo temp = playerInfoList[i];
				temp.playerObjectId = player.GetComponent<NetworkIdentity>().netId.Value;
				playerInfoList[i] = temp;
				player.GetComponent<MeshRenderer>().material.color = playerInfoList[i].colour;
				Debug.Log("Player: " + _networkConnection.connectionId.ToString() + " has obj Id: " + temp.playerObjectId.ToString());
			}
		}
	}

	public void OnBulletSpawn(NetworkMessage _networkMessage)
	{
		// Read msg
		BulletSpawnMessage msg = _networkMessage.ReadMessage<BulletSpawnMessage>();

		// Create Bullet
		GameObject clone = Instantiate(this.bullet, msg.position, msg.rotation) as GameObject;
		clone.GetComponent<Bullet>().ownerId = msg.objectId;
		//NetworkServer.Spawn(clone);

		// Set velocity
		clone.GetComponent<Rigidbody>().velocity = clone.transform.forward * msg.speed;

		// Tell client player's to play a particle effect
		ShotBulletMessage newMsg = new ShotBulletMessage();
		newMsg.playerId = msg.objectId;
		NetworkServer.SendToAll(CustomMsgType.ShotBullet, newMsg);

	}

	// Server
	private void SpawnFlag()
	{
		GameObject clone = Instantiate(this.spawnPrefabs[2], this.flagSpawn.position, this.flagSpawn.rotation) as GameObject;
		this.gameFlag = clone.GetComponent<Flag>();
		this.gameFlag.gameManager = this;
		NetworkServer.Spawn(clone);

		this.flagId = (int)clone.GetComponent<NetworkIdentity>().netId.Value;
	}

	#endregion

	#region Update

	// Server
	private void Update()
	{
		this.NetworkLobbyUpdate();
		this.NetworkGameUpdate();
	}

	// Server
	private void NetworkLobbyUpdate()
	{
		if(Network.isClient || !hasMadeSelection)
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
				this.SendLobbyUpdates("");
			}
		}
		else if (!this.isGameStarted)
		{
			this.lobbyCountDownTime = this.lobbyCountDownStartTime;
			this.lastLobbyCountDownTimeSent = (int)this.lobbyCountDownStartTime;
			this.isLobbyTimerCountingDown = false;
		}
	}

	// Server
	private void NetworkGameUpdate()
	{
		if(Network.isClient || !this.isGameStarted)
			return;

		if(!this.hasInitializedPlayers)
		{
			this.hasInitializedPlayers = true;
			this.StartCoroutine(this.SetUpGame());
		}
	}

	// Server
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

	#region Game

	// Server
	private IEnumerator SetUpGame()
	{
		Debug.Log("Set up Game");
		yield return new WaitForSeconds(1.0f); // just in case

		// Shuffle Spawn positions
		for(int i = 0; i < this.spawnTransforms.Count; i++)
		{
			int ran = (int)Random.Range(0, spawnTransforms.Count);
			Transform temp = spawnTransforms[i];
			spawnTransforms[i] = spawnTransforms[ran];
			spawnTransforms[ran] = temp;
		}


		// Send msg to all clients for each player
		Debug.Log("Send initialize update to " +  this.numPlayers.ToString() + " players");

		for(int i = 0; i < this.numPlayers; i++)
		{
			InitializePlayerMessage msg = new InitializePlayerMessage();
			msg.name = this.playerInfoList[i].name;
			msg.colour = this.playerInfoList[i].colour;
			msg.objectId = (int)this.playerInfoList[i].playerObjectId;
			msg.spawnPosition = this.spawnTransforms[i].position;
			NetworkServer.SendToAll(CustomMsgType.InitPlayer, msg);

			// Update server replication
			PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.objectId, true);
			tempPlayer.Initialize(msg.name, msg.colour, msg.spawnPosition, this, this.gameCanvas);
		}

		// Update player game UI
		GameUISetupMessage sendMsg = new GameUISetupMessage();

		List<string> tempNames = new List<string>();
		List<Color> tempCol = new List<Color>();
		for(int i = 0; i < this.maxConnections; i++)
		{
			tempNames.Add(this.playerInfoList[i].name);
			tempCol.Add(this.playerInfoList[i].colour);
		}

		sendMsg.isPlaying = this.isPlayerReadyList.ToArray();
		sendMsg.playerHealth = this.playerHealthList.ToArray();
		sendMsg.playerNames = tempNames.ToArray();
		sendMsg.playerColours = tempCol.ToArray();

		NetworkServer.SendToAll(CustomMsgType.GameUISetup, sendMsg);

		// Start game routines
		this.StartCoroutine(this.CheckDeathRoutine());
		this.StartCoroutine(this.GameTimerRoutine());
		this.StartCoroutine(this.ScoreRoutine());
		this.StartCoroutine(this.GameCountdown());

		yield return null;
	}

	// Client
	private void OnPlayerInitialize(NetworkMessage _networkMessage)
	{
		// Read msg
		InitializePlayerMessage msg = _networkMessage.ReadMessage<InitializePlayerMessage>();

		// Send msg to player object
		PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.objectId, false);
		tempPlayer.Initialize(msg.name, msg.colour, msg.spawnPosition, this, this.gameCanvas);
	}

	// Server
	private void OnMovement(NetworkMessage _networkMessage)
	{
		// Read message
		MoveMessage temp = _networkMessage.ReadMessage<MoveMessage>();

		// this is player movement
		if(temp.objectType == 0)
		{
			// Update server replication
			PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)temp.objectId, true);
			tempPlayer.OnPlayerMove(temp.position, temp.rotation, temp.time);

			NetworkServer.SendToAll(CustomMsgType.Move, temp);
		}
	}

	// Client
	private void OnMoveMessage(NetworkMessage _networkMessage)
	{
		// Read message
		MoveMessage msg = _networkMessage.ReadMessage<MoveMessage>();

		//Debug.Log("Received movement update for object: " + msg.objectId.ToString());

		if(msg.objectType == 0)
		{
			// Send msg to player object
			PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.objectId, false);
			tempPlayer.OnPlayerMove(msg.position, msg.rotation, msg.time);
		}
		else if(msg.objectType == 1) // bullet
		{
			Bullet temp = NetworkHelper.GetObjectByNetIdValue<Bullet>((uint)msg.objectId, false);
			if(temp)
				temp.OnMovementReceived(msg.position, msg.rotation, msg.time);
		}
		else if (msg.objectType == 2)
		{
			Flag temp = NetworkHelper.GetObjectByNetIdValue<Flag>((uint)msg.objectId, false);
			if(temp)
				temp.OnMovementReceived(msg.position, msg.rotation, msg.time);
		}
	}

	// Server - bullets are server owned objects
	public void OnBulletHit(int _objectId)
	{
		Debug.Log("Player object " + _objectId.ToString() + " got hit!");

		// Find player in our list
		for(int i = 0; i < this.maxConnections; i++)
		{
			if(this.playerInfoList[i].playerObjectId == _objectId)
			{
				// Check to make sure the player isn't already dead
				if(this.playerHealthList[i] > 0)
				{
					this.playerHealthList[i] -= this.baseDamage;

					HealthMessage msg = new HealthMessage();
					msg.isPlaying = this.isPlayerReadyList.ToArray();
					msg.playerHealth = this.playerHealthList.ToArray();
					NetworkServer.SendToAll(CustomMsgType.Health, msg);

					// Check if the player is holding the flag
					if(this.playerInfoList[i].hasFlag)
					{
						// update our flag
						this.gameFlag.StartCoroutine(this.gameFlag.DropFlag());
						this.OnFlagInteraction(false, _objectId);
					}
				}

				break;
			}
		}
	}

	// Client
	private void OnHealthMessage(NetworkMessage _networkMessage)
	{
		Debug.Log("Received Health message");

		// send msg details to canvas manager
		HealthMessage msg = _networkMessage.ReadMessage<HealthMessage>();
		this.canvasManager.OnHealthUpdate(msg.isPlaying, msg.playerHealth);
	}

	// Client
	private void OnGameUISetupMessage(NetworkMessage _networkMessage)
	{
		GameUISetupMessage msg = _networkMessage.ReadMessage<GameUISetupMessage>();
		this.canvasManager.OnGameUISetup(msg.isPlaying, msg.playerHealth, msg.playerNames, msg.playerColours);
	}

	// Server
	private IEnumerator CheckDeathRoutine()
	{
		yield return new WaitUntil(() => this.isPlayingGame == true);
		Debug.Log("Check death routine");

		while(this.isPlayingGame)
		{
			for(int i = 0; i < this.maxConnections; i++)
			{
				if(this.playerHealthList[i] <= 0)
				{
					Debug.Log(this.playerInfoList[i].name + " has died");

					// Send death msg to all clients
					DeathMessage msg = new DeathMessage();
					msg.objectId = (int)this.playerInfoList[i].playerObjectId;
					msg.nextSpawnPosition = this.spawnTransforms[Random.Range(0, this.spawnTransforms.Count)].position;
					NetworkServer.SendToAll(CustomMsgType.Death, msg);

					this.StartCoroutine(this.RespawnPlayer(i));
					break;
				}
			}
			yield return null;
		}
	}

	// Client
	public void OnDeathMessage(NetworkMessage _networkMessage)
	{
		DeathMessage msg = _networkMessage.ReadMessage<DeathMessage>();

		// Send msg to player object
		PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.objectId, false);
		tempPlayer.OnDeath(msg.nextSpawnPosition);

		// Run respawn coroutine
		tempPlayer.StartCoroutine(tempPlayer.OnRespawn(this.respawnTime));
	}

	// Server
	private IEnumerator RespawnPlayer(int _index)
	{
		yield return new WaitForSeconds(this.respawnTime);

		// Reset player health
		this.playerHealthList[_index] = this.baseHealth;

		// Send health update msg
		HealthMessage healthMsg = new HealthMessage();
    	healthMsg.isPlaying = this.isPlayerReadyList.ToArray();
    	healthMsg.playerHealth = this.playerHealthList.ToArray();
     	NetworkServer.SendToAll(CustomMsgType.Health, healthMsg);

		yield return null;
	}

	// Server - When a client collides with the flag, or a bullet causing them to drop it
	public void OnFlagInteraction(bool _isHeld, int _playerId)
	{
		Debug.Log("On Flag Interaction");

		// Update our list
		for(int i = 0; i < this.numPlayers; i++)
		{
			if(this.playerInfoList[i].playerObjectId == _playerId)
			{
				PlayerInfo pInfo = this.playerInfoList[i];
				pInfo.hasFlag = _isHeld;
				this.playerInfoList[i] = pInfo;
			}
		}

		// Send msg to clients
		FlagInteractionMessage msg = new FlagInteractionMessage();
		msg.playerId = _playerId;
		msg.flagId = this.flagId;
		msg.isHeld = _isHeld;
		NetworkServer.SendToAll(CustomMsgType.Flag, msg);
	}

	// Client - When a client collides with the flag, or a bullet causing them to drop it
	public void OnFlagInteraction(NetworkMessage _networkMessage)
	{
		Debug.Log("On Flag Interaction Client");
		FlagInteractionMessage msg = _networkMessage.ReadMessage<FlagInteractionMessage>();

		Flag clientFlag = NetworkHelper.GetObjectByNetIdValue<Flag>((uint)msg.flagId, false);
		clientFlag.OnFlagInteraction(msg.isHeld);

		PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.playerId, false);
		tempPlayer.SetHasFlag(msg.isHeld);
	}

	// Server - when the client chooses to drop the flag (when they shoot a bullet)
	public void OnFlagDrop(NetworkMessage _networkMessage)
	{
		FlagDropMessage netMsg = _networkMessage.ReadMessage<FlagDropMessage>();

		Debug.Log("On Flag Drop");

		// update our flag
		this.gameFlag.StartCoroutine(this.gameFlag.DropFlag());

		// Update our list
		for(int i = 0; i < this.numPlayers; i++)
		{
			if(this.playerInfoList[i].playerObjectId == netMsg.playerId)
			{
				PlayerInfo pInfo = this.playerInfoList[i];
				pInfo.hasFlag = false;
				this.playerInfoList[i] = pInfo;
			}
		}

		// update client flags
		FlagInteractionMessage msg = new FlagInteractionMessage();
		msg.playerId = netMsg.playerId;
		msg.flagId = this.flagId;
		msg.isHeld = false;
		NetworkServer.SendToAll(CustomMsgType.Flag, msg);
	}

	// Client - tell client replications to play a gunshot particle effect
	public void OnShotBulletMessage(NetworkMessage _networkMessage)
	{
		ShotBulletMessage msg = _networkMessage.ReadMessage<ShotBulletMessage>();

		PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.playerId, false);
		tempPlayer.OnShotTaken();
	}

	// Client
	public void OnGameTimeMessage(NetworkMessage _networkMessage)
	{
		// Read msg
		GameTimeMessage msg = _networkMessage.ReadMessage<GameTimeMessage>();

		// Update canvas
		this.canvasManager.OnGameTimeUpdate(msg.gameTime);
	}

	// Server
	private IEnumerator GameTimerRoutine()
	{		
		// Set initial values
		this.currentGameTime = this.gameTime;
		this.lastSentGameTime = (int)this.gameTime;

		yield return new WaitUntil(() => this.isPlayingGame == true);

		// Main loop
		while(this.isPlayingGame)
		{
			this.currentGameTime -= Time.deltaTime;

			if(this.currentGameTime < this.lastSentGameTime)
			{
				//Debug.Log("Send time update");
				this.lastSentGameTime = (int)this.currentGameTime;

				// Send msg
				GameTimeMessage msg = new GameTimeMessage();
				msg.gameTime = this.lastSentGameTime;
				NetworkServer.SendToAll(CustomMsgType.GameTime, msg);

				if(this.currentGameTime <= 0)
				{
					this.isPlayingGame = false;
					Debug.Log("End Game!!");
				}
			}
			
			yield return null;
		}

		yield return null;
	}

	// Server
	private IEnumerator ScoreRoutine()
	{
		yield return new WaitUntil(() => this.isPlayingGame == true);

		while(this.isPlayingGame)
		{
			// Traverse players
			for(int i = 0; i < this.numPlayers; i++)
			{
				if(playerInfoList[i].hasFlag)
				{
					this.playerScoreList[i] += 1;

					Debug.Log("Add score for player " + i.ToString());

					yield return new WaitForSeconds(this.scoreAdditionInterval);

					// Send score message
					GameScoresMessage msg = new GameScoresMessage();
					msg.scores = this.playerScoreList.ToArray();
					NetworkServer.SendToAll(CustomMsgType.GameScore, msg);
				}
			}

			yield return null;
		}

		yield return null;
	}

	// Client
	public void OnScoreMessage(NetworkMessage _networkMessage)
	{
		Debug.Log("Update score msg received");
		GameScoresMessage msg = _networkMessage.ReadMessage<GameScoresMessage>();
		this.canvasManager.OnScoreUpdate(msg.scores);
	}

	// Server
	private IEnumerator GameCountdown()
	{
		int lastTimeSent = (int)this.countdownTime;

		// Countdown
		while(this.countdownTime >= 0.0f)
		{
			this.countdownTime -= Time.deltaTime;

			if((int)this.countdownTime < lastTimeSent)
			{
				lastTimeSent = (int)this.countdownTime;

				CountdownMessage msg = new CountdownMessage();
				msg.countdownString = lastTimeSent <= 0 ? "GO!" : lastTimeSent.ToString();
				NetworkServer.SendToAll(CustomMsgType.Countdown, msg);

				if(msg.countdownString == "GO!")
				{
					EnableInputMessage inputMsg = new EnableInputMessage();

					for(int i = 0; i < this.numPlayers; i++)
					{
						inputMsg.playerId = (int)this.playerInfoList[i].playerObjectId;
						inputMsg.isEnabled = true;

						NetworkServer.SendToAll(CustomMsgType.Input, inputMsg);
					}
				}
			}

			yield return null;
		}

		// Start game
		this.isPlayingGame = true;
	}

	// Client
	private void OnGameCountDown(NetworkMessage _networkMessage)
	{
		CountdownMessage msg = _networkMessage.ReadMessage<CountdownMessage>();
		this.canvasManager.OnGameCountdown(msg.countdownString);
	}

	// Client
	private void OnInputEnabled(NetworkMessage _networkMessage)
	{
		EnableInputMessage msg = _networkMessage.ReadMessage<EnableInputMessage>();

		PlayerManager tempPlayer = NetworkHelper.GetObjectByNetIdValue<PlayerManager>((uint)msg.playerId, false);
		tempPlayer.EnableInput(msg.isEnabled);
	}

	#endregion
}

#endregion

#region Messages

public class CustomMsgType
{
	public static short PlayerInfo = MsgType.Highest + 1;
	public static short Lobby = MsgType.Highest + 2;
	public static short ReadyUp = MsgType.Highest + 3;
	public static short StartGame = MsgType.Highest + 4;
	public static short InitPlayer = MsgType.Highest + 5;
	public static short Move = MsgType.Highest + 6;
	public static short BulletSpawn = MsgType.Highest + 7;
	public static short GameUISetup = MsgType.Highest + 8;
	public static short Health = MsgType.Highest + 9;
	public static short Death = MsgType.Highest + 10;
	public static short Flag = MsgType.Highest + 11;
	public static short DropFlag = MsgType.Highest + 12;
	public static short ShotBullet = MsgType.Highest + 13;
	public static short GameTime = MsgType.Highest + 14;
	public static short GameScore = MsgType.Highest + 15;
	public static short Countdown = MsgType.Highest + 16;
	public static short Input = MsgType.Highest + 17;
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
	public bool[] isClientSlotTaken;
	public bool[] isPlayerConnected;
	public bool[] isReadyList;
	public string[] playerNames;
	public Color[] playerColours;
	public bool isLobbyCountingDown = false;
	public int countDownTime;
	public string lobbyMsg;
}

// Client to Server
public class ReadyUpMessage : MessageBase
{
	public bool isReady;
}

// Server to clients
public class StartGameMessage : MessageBase
{
	public string msg;
}
	
// Server to clients
public class InitializePlayerMessage : MessageBase
{
	public string name;
	public Color colour;
	public int objectId;
	public Vector3 spawnPosition;
}

// Client to Server
public class MoveMessage : MessageBase
{
	public int objectId;
	public Vector3 position;
	public Quaternion rotation;
	public float time;
	public int objectType; // 0 = player, bullet, flag
}

// Client to Server
public class BulletSpawnMessage : MessageBase
{
	public int objectId;
	public Vector3 position;
	public Quaternion rotation;
	public float speed;
}

// Server to Client
public class HealthMessage : MessageBase
{
	public bool[] isPlaying;
	public int[] playerHealth;
}

// Server to Client
public class GameUISetupMessage : MessageBase
{
	public bool[] isPlaying;
	public int[] playerHealth;
	public string[] playerNames;
	public Color[] playerColours;
}

// Server to Client
public class DeathMessage : MessageBase
{
	public int objectId;
	public Vector3 nextSpawnPosition;
}

// Server to Client
public class FlagInteractionMessage : MessageBase
{
	public int playerId;
	public int flagId;
	public bool isHeld;
}

// Client to Server
public class FlagDropMessage : MessageBase
{
	public int playerId;
}

// Server to Client
public class ShotBulletMessage : MessageBase
{
	public int playerId;
}

// Server to Client
public class GameTimeMessage : MessageBase
{
	public int gameTime;
}

// Server to Client
public class GameScoresMessage : MessageBase
{
	public int[] scores;
}

// Server to Client
public class CountdownMessage : MessageBase
{
	public string countdownString;
}

// Server to Client
public class EnableInputMessage : MessageBase
{
	public int playerId;
	public bool isEnabled;
}

#endregion

#region Helper

public static class NetworkHelper
{
	public static T GetObjectByNetIdValue<T>(uint _value, bool _isServer)
	{
		NetworkInstanceId netInstanceId = new NetworkInstanceId(_value);
		NetworkIdentity foundNetworkIdentity = null;

		if(_isServer)
		{
			NetworkServer.objects.TryGetValue(netInstanceId, out foundNetworkIdentity);
		}
		else
		{
			ClientScene.objects.TryGetValue(netInstanceId, out foundNetworkIdentity);
		}

		if(foundNetworkIdentity)
		{
			T foundObject = foundNetworkIdentity.GetComponent<T>();
			if(foundObject != null)
			{
				return foundObject;
			}
		}

		return default(T);
	}
}

#endregion