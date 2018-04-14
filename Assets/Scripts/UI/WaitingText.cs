using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaitingText : MonoBehaviour 
{
	[SerializeField]
	private Text textComponent;
	[SerializeField]
	private string msg;
	[SerializeField]
	private float maxDots;
	[SerializeField]
	private float updateInterval;

	private int dotCount = 0;
	private float timeCount = 0.0f;
	
	void Update () 
	{
		this.timeCount += Time.deltaTime;

		if(this.timeCount >= this.updateInterval)
		{
			this.timeCount = 0.0f;

			this.dotCount++;

			if(this.dotCount > this.maxDots)
			{
				dotCount = 0;
			}

			string temp = "";

			// Add dots to string
			for(int i = 0; i < dotCount; i++)
			{
				temp += ".";
			}

			this.textComponent.text = msg + temp;
		}
	}
}
