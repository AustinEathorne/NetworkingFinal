using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SfxManager : MonoBehaviour 
{
	[Header("Audio Source")]
	[SerializeField]
	private AudioSource audioSource;

	[Header("Button Sfx")]
	[SerializeField]
	private List<AudioClip> buttonSoundEffects;



	public void PlayRandomButtonSfx()
	{
		int ran = Random.Range(0, this.buttonSoundEffects.Count);
		this.audioSource.clip = this.buttonSoundEffects[ran];
		audioSource.Play();
	}
}
