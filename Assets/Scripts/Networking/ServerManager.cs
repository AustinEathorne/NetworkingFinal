using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerManager : MonoBehaviour
{
	public enum ServerState
	{
		Initialize = 0, Lobby, Game
	}

	private ServerState m_state = ServerState.Initialize;


	public IEnumerator Initialize()
	{
		NetworkServer.Listen(4444);
		this.m_state = ServerState.Lobby;
		yield return null;
	}

	#region Get/Set

	public void SetState(ServerState ss) { this.m_state = ss; }



	#endregion
}
