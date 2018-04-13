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


	private void Start()
	{
		this.initialPanel.SetActive(true);
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
		}
	}

	#endregion

	#region Lobby

	// Client
	public void OnLobbyUpdateReceived(NetworkMessage _networkMessage)
	{
		Debug.Log("Lobby update received");
		// Check if this is the first time receiving the panel update, if so, turn on the panel
		if(!this.lobbyPanel.activeSelf)
			this.lobbyPanel.SetActive(true);
	}

	private void OnPlayerJoinLobby()
	{
		// update lobby board with msg details (bool isPlayerConnected(to show their panel), name, colour)

	}

	private void OnPlayerLeftLobby()
	{
		// update lobby board with msg details (bool isPlayerConnected(to show their panel), name, colour)
	}

	#endregion
}
