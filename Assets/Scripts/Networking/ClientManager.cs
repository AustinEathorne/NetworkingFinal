using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ClientManager : MonoBehaviour
{
	public enum ClientState
	{
		Initialize = 0, Lobby, Game
	}

	private ClientState m_state = ClientState.Initialize;

	NetworkClient m_client;


	public IEnumerator Initialize()
	{
		// create new network client instance
		m_client = new NetworkClient();

		// register msg handlers
		m_client.RegisterHandler(MsgType.Connect, OnConnected); // Can use unity's built in msgs or define your own

		// Connect to the server
		m_client.Connect("127.0.0.1", 4444);

		// Check for connection?

		// Update state
		m_state = ClientState.Lobby;

		yield return null;
	}

	// Callbacks
	// debug - remove
	public void OnConnected(NetworkMessage msg)
	{
		Debug.Log("Connected to server");
	}

	#region Get/Set

	public void SetState(ClientState cs) { this.m_state = cs; }



	#endregion
}
