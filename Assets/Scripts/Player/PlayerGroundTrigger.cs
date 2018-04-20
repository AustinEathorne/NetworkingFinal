using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundTrigger : MonoBehaviour 
{
	[SerializeField]
	private PlayerMovement playerMovement;

	private void OnTriggerEnter(Collider _col)
	{
		if(_col.transform.tag == "Floor")
		{
			this.playerMovement.SetIsInAir(false);
		}
	}
}
