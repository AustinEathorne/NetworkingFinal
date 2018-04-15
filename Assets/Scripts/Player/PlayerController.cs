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

	[Header("Bools")]
	public bool isInputEnabled = false;




}
