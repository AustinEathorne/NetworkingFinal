using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerWeapon : MonoBehaviour {

	[Header("Components/Objects")]
	[SerializeField]
	private PlayerMovement playerMovement;
	[SerializeField]
	private GameObject bullet;
	[SerializeField]
	public Transform bulletSpawn;
	[SerializeField]
	private Rigidbody rigidbody;

	[Header("Properties")]
	[SerializeField]
	private float fireDelay;
	[SerializeField]
	public float bulletSpeed;

	private bool canFire = true;
	private int netId;

	void Start () 
	{
		this.netId = (int)this.GetComponent<NetworkIdentity>().netId.Value;
		this.StartCoroutine(this.FireCount());
	}

	public void Fire()
	{
		if(!this.canFire)
			return;

		this.canFire = false;

		//GameObject clone = Instantiate(this.bullet, this.bulletSpawn.position, this.bulletSpawn.rotation) as GameObject;

		// Send msg to server to spawn object
		BulletSpawnMessage msg = new BulletSpawnMessage();
		msg.objectId = this.netId;
		msg.position = this.bulletSpawn.position;
		msg.rotation = this.bulletSpawn.rotation;
		msg.speed = this.bulletSpeed;

		NetworkManager.singleton.client.Send(CustomMsgType.BulletSpawn, msg);

		// Mask network request/spawning speed
		//this.playerMovement.KickBack();
	}

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
