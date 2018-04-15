using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour 
{
	[Header("Components")]
	[SerializeField]
	private PlayerManager playerManager;
	[SerializeField]
	private PlayerMovement playerMovement;
	[SerializeField]
	private PlayerWeapon playerWeapon;

	[Header("Bools")]
	[SerializeField]
	private bool isInputEnabled = false;


	private void Update()
	{
		if(this.isInputEnabled)
		{
			this.GetInput();
		}
	}

	public void GetInput()
	{
		if(Input.GetKey(KeyCode.Space))
		{
			this.playerWeapon.Fire();
		}
	}

	public void SetIsEnabled(bool _isEnabled)
	{
		this.isInputEnabled = _isEnabled;
		this.playerMovement.isInputEnabled = _isEnabled;
	}
}
