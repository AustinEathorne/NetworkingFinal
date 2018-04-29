using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Pickup : NetworkBehaviour
{
	public enum PickupType
	{
		Damage = 0, Speed
	}

	public GameManager gameManager;

	[SerializeField]
	private PickupType pType;

	[SerializeField]
	private ParticleSystem particlesystem;

	[SerializeField]
	private List<Color> particleColors;

	private int spawnPos = 0;

	// Server
	public void Initialize(GameManager _gameManager, PickupType _type, int _spawnPos)
	{
		this.gameManager = _gameManager;
		this.pType = _type;
		this.spawnPos = _spawnPos;

		ParticleSystem.MainModule main = this.particlesystem.main;

		switch(this.pType)
		{
		case PickupType.Damage:
			main.startColor = this.particleColors[0];
			break;
		case PickupType.Speed:
			main.startColor = this.particleColors[1];
			break;
		default:
			break;
		}
	}

	public void InitializeClient(int _color)
	{
		ParticleSystem.MainModule main = this.particlesystem.main;
		main.startColor = this.particleColors[_color];
	}

	// Server
	private void OnTriggerEnter(Collider _col)
	{
		if(!isServer)
		{
			return;
		}

		if(_col.transform.tag == "Player")
		{
			this.gameManager.OnPickupInteraction(_col.transform.GetComponent<NetworkIdentity>().netId.Value, this.pType, this.spawnPos);
			this.DestroyThis();
		}
	}

	// Server
	private void DestroyThis()
	{
		NetworkServer.Destroy(this.gameObject);
	}
}
