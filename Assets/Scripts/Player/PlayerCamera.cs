using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour {

	[Header("Components")]
	[SerializeField]
	private PlayerManager playerManager;
	[SerializeField]
	private Camera playerCamera;
	[SerializeField]
	private Transform cameraTransform;
	[SerializeField]
	private AudioListener audioListener;

	[Header("Properties")]
	[SerializeField]
	private float distance;
	[SerializeField]
	private float speedX;
	[SerializeField]
	private float speedY;
	[SerializeField]
	private float xOffset;
	[SerializeField]
	private float zOffset;

	private void Awake()
	{
		this.cameraTransform.SetParent(null);
	}

	private void Update()
	{
		this.UpdateCameraMovement();
	}

	private void UpdateCameraMovement()
	{
		this.cameraTransform.position = new Vector3(
			this.transform.position.x + xOffset,
			this.cameraTransform.position.y,
			this.transform.position.z + this.zOffset);
	}

	public void EnableCamera(bool isEnabled)
	{
		this.audioListener.enabled = isEnabled;
		this.playerCamera.enabled = isEnabled;
	}

	public Camera GetCamera()
	{
		return this.playerCamera;
	}
}
