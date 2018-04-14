using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerMovement : NetworkBehaviour
{
	void Update()
	{
		if(!isLocalPlayer)
		{
			return;
		}

		float x = Input.GetAxis("Horizontal") * Time.deltaTime * 25.0f;
		float z = Input.GetAxis("Vertical") * Time.deltaTime * 15.0f;

		transform.Translate(x, 0, z);
	}

}
