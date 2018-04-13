using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CanvasManager : MonoBehaviour {

	[Header("Game Manager")]
	[SerializeField]
	private GameManager gameManager;

	[Header("Parent Panels")]
	[SerializeField]
	private GameObject initialPanel;
	[SerializeField]
	public GameObject playerPreviewPanel;
	[SerializeField]
	public GameObject colourPanel;
	[SerializeField]
	private GameObject namePanel;
	[SerializeField]
	private GameObject lobbyPanel;

	[Header("Colour Select")]
	[SerializeField]
	private MeshRenderer menuPlayerMeshRenderer;
	[SerializeField]
	private List<Color> playerColours;
	private int currentColourIndex = 0;

	[Header("Name")]
	[SerializeField]
	private Text nameInputText;

	[Header("Lobby")]
	[SerializeField]
	private List<GameObject> playerPanels;
	[SerializeField]
	private List<Image> playerColourImages;
	[SerializeField]
	private List<Text> playerNameTexts;
	[SerializeField]
	private List<Image> playerReadyIcons;
	[SerializeField]
	private List<Sprite> playerReadySprites;
	[SerializeField]
	private Text readyButtonText;
	[SerializeField]
	private Text timeText;

	private bool isReadyForLobby = false;


	private void Start()
	{
		this.initialPanel.SetActive(true);
	}

	public void CloseMenu()
	{
		Debug.Log("Close Menu!");

		this.initialPanel.SetActive(false);
		this.playerPreviewPanel.SetActive(false);
		this.colourPanel.SetActive(false);
		this.namePanel.SetActive(false);
		this.lobbyPanel.SetActive(false);
	}

	#region Server/Client Select

	public void OnServerSelect()
	{
		Debug.Log("Selected Server");
		this.initialPanel.SetActive(false);
		this.gameManager.StartCoroutine(this.gameManager.CreateServer());
	}

	public void OnClientSelect()
	{
		Debug.Log("Selected Client");
		this.initialPanel.SetActive(false);
		this.gameManager.StartCoroutine(this.gameManager.CreateClient());
	}

	#endregion

	#region Colour Select

	public void OnColourChange(bool _isRight)
	{
		if(_isRight)
		{
			this.currentColourIndex++;

			if(this.currentColourIndex > this.playerColours.Count - 1)
				this.currentColourIndex = 0;

			this.menuPlayerMeshRenderer.material.color = this.playerColours[this.currentColourIndex];
		}
		else
		{
			this.currentColourIndex--;

			if(this.currentColourIndex < 0)
				this.currentColourIndex = this.playerColours.Count - 1;

			this.menuPlayerMeshRenderer.material.color = this.playerColours[this.currentColourIndex];
		}
	}

	public void OnColourSelect()
	{
		this.colourPanel.SetActive(false);
		this.playerPreviewPanel.SetActive(false);
		this.namePanel.SetActive(true);

		// set client's player colour
		this.gameManager.clientPlayerInfo.colour = this.playerColours[currentColourIndex];
	}

	#endregion

	#region NameSelect

	public void OnNameSelect()
	{
		if(this.nameInputText.text != "")
		{
			// Set client's player name
			this.gameManager.clientPlayerInfo.name = this.nameInputText.text;

			// Send the client's info to the server
			this.gameManager.SendClientPlayerInformation();

			// Turn off panel
			this.namePanel.SetActive(false);

			// Allow panel to be turned on
			this.isReadyForLobby = true;
		}
	}

	#endregion

	#region Lobby

	// Client - Update player panels when a player joins/leaves - TODO: player leaving
	public void OnLobbyUpdateReceived(NetworkMessage _networkMessage)
	{
		Debug.Log("Lobby update received");

		// Read msg
		LobbyMessage msg = _networkMessage.ReadMessage<LobbyMessage>();
		//Debug.Log("Player " + msg.connectionId.ToString() + " has connected");

		// check for players connected
		for(int i = 0; i < 4; i++)
		{
			if(msg.isPlayerConnected[i])
			{
				this.playerColourImages[i].color = msg.playerColours[i];
				this.playerNameTexts[i].text = msg.playerNames[i];
				this.playerPanels[i].SetActive(true);
			}
			else
			{
				this.playerPanels[i].SetActive(false);
			}
		}

		// Check ready up status
		for(int i = 0; i < msg.isReadyList.Length; i++)
		{
			if(msg.isReadyList[i])
			{
				this.playerReadyIcons[i].sprite = this.playerReadySprites[1]; // set icon to ready
			}
			else
			{
				this.playerReadyIcons[i].sprite = this.playerReadySprites[0]; // set icon to not ready
			}
		}

		// Check to update time text
		if(msg.isLobbyCountingDown)
		{
			// Enable text & update with the time
			this.timeText.enabled = true;
			if(msg.countDownTime > -1)
			{
				this.timeText.text = msg.countDownTime > 0 ? msg.countDownTime.ToString() : "GOOD LUCK!";
			}
			else
			{
				this.CloseMenu();
			}
		}
		else
		{
			this.timeText.enabled = false;
		}

		// Check if this is the first time receiving the panel update, if so, turn on the panel
		if(!this.lobbyPanel.activeSelf && this.isReadyForLobby)
		{
			this.lobbyPanel.SetActive(true);
			this.isReadyForLobby = false;
		}
	}

	public void UpdateReadyButtonText(bool _isReady)
	{
		string txt = _isReady ? "UNREADY" : "READY";
		this.readyButtonText.text = txt;
	}

	#endregion
}
