using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerWeapon : MonoBehaviour {

	[Header("Components/Objects")]
	[SerializeField]
	private GameObject bullet;
	[SerializeField]
	private Transform bulletSpawn;

	[Header("Properties")]
	[SerializeField]
	private float fireDelay;
	[SerializeField]
	private float bulletSpeed;

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
		msg.position = this.bulletSpawn.position;
		msg.rotation = this.bulletSpawn.rotation;
		msg.speed = this.bulletSpeed;

		NetworkManager.singleton.client.Send(CustomMsgType.BulletSpawn, msg);
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
