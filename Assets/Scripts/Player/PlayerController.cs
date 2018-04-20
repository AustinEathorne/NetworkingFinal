using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour 
{
	[Header("Input")]
	[SerializeField]
	private KeyCode fireKey;
	[SerializeField]
	private KeyCode jumpKey;

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
		if(Input.GetKey(this.fireKey))
		{
			this.playerWeapon.Fire();
		}

		if(Input.GetKey(this.jumpKey))
		{
			this.playerMovement.Jump();
		}
	}

	public void SetIsEnabled(bool _isEnabled)
	{
		this.isInputEnabled = _isEnabled;
		this.playerMovement.isInputEnabled = _isEnabled;
	}
}
