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
		msg.position = this.bulletSpawn.position;
		msg.rotation = this.bulletSpawn.rotation;
		msg.speed = this.bulletSpeed;

		NetworkManager.singleton.client.Send(CustomMsgType.BulletSpawn, msg);

		// Mask network request/spawning speed
		this.ApplyWeaponKickback();
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
