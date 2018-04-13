using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour {

	[SerializeField]
	private float degreesPerSecond;

	[SerializeField]
	private bool isRotatingRight;

	private void Update()
	{
		if(this.isRotatingRight)
			this.transform.Rotate(0, Time.deltaTime * this.degreesPerSecond, 0);
		else
			this.transform.Rotate(0, -(Time.deltaTime * this.degreesPerSecond), 0);
	}


}
