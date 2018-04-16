using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerWeapon : NetworkBehaviour
{
	[Header("Components/Objects")]
	[SerializeField]
	private GameObject bullet;
	[SerializeField]
	private Transform bulletSpawn;
	[SerializeField]
	private Rigidbody rigidbody;

	[Header("Properties")]
	[SerializeField]
	private float fireDelay;
	[SerializeField]
	private float bulletSpeed;
	[SerializeField]
	private float weaponKickbackForce;

	private bool canFire = true;


	void Start () 
	{
		this.StartCoroutine(this.FireCount());
	}

	void Update () 
	{
		
	}

	public void Fire()
	{
		if(!this.canFire)
			return;

		this.canFire = false;

		// Send msg to server to spawn object
		BulletSpawnMessage msg = new BulletSpawnMessage();


		//msg.speed = this.bulletSpeed;

		GameObject clone = Instantiate(this.bullet, this.bulletSpawn.position, this.bulletSpawn.rotation) as GameObject;
		//clone.GetComponent<NetworkIdentity>().AssignClientAuthority(this.connectionToClient); - should have local player authority

		msg.objectId = (int)clone.GetComponent<NetworkIdentity>().netId.Value;
		msg.position = this.bulletSpawn.position;
		msg.rotation = this.bulletSpawn.rotation;

		NetworkManager.singleton.client.Send(CustomMsgType.BulletSpawn, msg);

		clone.GetComponent<Rigidbody>().velocity = clone.transform.forward * this.bulletSpeed;

		// Mask network request/spawning speed
		this.ApplyWeaponKickback();
		//this.CmdFire();
	}

	[Command]
	public void CmdFire()
	{
		Debug.Log("Run command fire for player " + this.GetComponent<NetworkIdentity>().netId);
		GameObject clone = Instantiate(this.bullet, this.bulletSpawn.position, this.bulletSpawn.rotation) as GameObject;
		clone.GetComponent<NetworkIdentity>().AssignClientAuthority(this.connectionToClient);
		NetworkServer.SpawnWithClientAuthority(clone, this.bullet.GetComponent<NetworkIdentity>().assetId, this.connectionToClient);
		clone.GetComponent<Rigidbody>().velocity = clone.transform.forward * this.bulletSpeed;
		Debug.Log("Done command " + this.GetComponent<NetworkIdentity>().netId);
	}

	// mas the terrible delay for a bullet spawn request
	private void ApplyWeaponKickback()
	{
		this.rigidbody.AddForce(-(this.transform.forward) * weaponKickbackForce);
	}

	// Particle effect?

	public IEnumerator FireCount()
	{
		float count = 0.0f;

		while(count <= fireDelay)
		{
			count += Time.deltaTime;
			yield return null;
		}
			
		this.canFire = true;

		while(this.canFire)
		{
			yield return null;
		}

		this.StartCoroutine(this.FireCount());

		yield return null;
	}
}
