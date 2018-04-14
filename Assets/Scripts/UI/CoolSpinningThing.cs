using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolSpinningThing : MonoBehaviour 
{
	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private RectTransform childRectTransform;

	[SerializeField]
	private float degreesPerSecond;
	[SerializeField]
	private float childDegreesPerSecond;

	[SerializeField]
	private bool isRotatingRight;
	[SerializeField]
	private bool isChildRotatingRight;

	private void Update()
	{
		if(this.isRotatingRight)
			this.rectTransform.Rotate(0, 0, Time.deltaTime * this.degreesPerSecond);
		else
			this.rectTransform.Rotate(0, 0, -(Time.deltaTime * this.degreesPerSecond));

		if(isChildRotatingRight)
			this.childRectTransform.Rotate(0, 0, Time.deltaTime * this.childDegreesPerSecond);
		else
			this.childRectTransform.Rotate(0, 0, -(Time.deltaTime * this.childDegreesPerSecond));
	}
}
