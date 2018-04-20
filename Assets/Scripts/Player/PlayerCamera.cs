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
			this.transform.position.x,
			this.cameraTransform.position.y,
			this.transform.position.z);
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
