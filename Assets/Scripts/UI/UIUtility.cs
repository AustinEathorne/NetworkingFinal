using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIUtility : MonoBehaviour 
{

	#region General - MoveToPosition, SetGroupPositions

	// Move singular rect transform to target position - based off travel time
	public IEnumerator MoveTransformToPosition(RectTransform rTransform, Vector2 target, float time)
	{
		Vector3 distance = rTransform.anchoredPosition - target;
		float speed = distance.magnitude/time;

		while(distance.magnitude >= 0.001f)
		{
			distance = rTransform.anchoredPosition - target;
			rTransform.anchoredPosition = Vector3.MoveTowards(rTransform.anchoredPosition, target, speed * Time.deltaTime);
			yield return null;
		}
	}

	// Move singular rect transform to target position - based off travel speed, can toggle linear interpolation
	public IEnumerator MoveTransformToPosition(RectTransform rTransform, Vector2 target, float speed, bool isLerping)
	{
		Vector3 distance = rTransform.anchoredPosition - target;

		while(distance.magnitude >= 0.001f)
		{
			distance = rTransform.anchoredPosition - target;
			rTransform.anchoredPosition = isLerping ?
				Vector3.Lerp(rTransform.anchoredPosition, target, speed * Time.deltaTime) :
				Vector3.MoveTowards(rTransform.anchoredPosition, target, speed * Time.deltaTime);
			yield return null;
		}
	}

	// Move list of rect transforms to target positions - based off travel time, can toggle moving simultaneously
	public IEnumerator MoveTransformsToPositions(List<RectTransform> rTransforms, List<Vector2> targets, float time,
		bool isSimultaneous)
	{
		if(isSimultaneous)
		{
			for(int i = 0; i < rTransforms.Count; i++)
			{
				if(i < rTransforms.Count -1)
				{
					this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], time));
				}
				else
				{
					yield return this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], time));
				}
			}
		}
		else
		{
			for(int i = 0; i < rTransforms.Count; i++)
			{
				yield return this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], time));
			}
		}
	}

	// Move list of rect transforms to target positions - based off travel speed, can toggle linear interpolation and
	// moving simultaneously
	public IEnumerator MoveTransformsToPositions(List<RectTransform> rTransforms, List<Vector2> targets, float speed,
		bool isLerping, bool isSimultaneous)
	{
		if(isSimultaneous)
		{
			for(int i = 0; i < rTransforms.Count; i++)
			{
				if(i < rTransforms.Count -1)
				{
					this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], speed, isLerping));
				}
				else
				{
					yield return this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], speed, isLerping));
				}
			}
		}
		else
		{
			for(int i = 0; i < rTransforms.Count; i++)
			{
				yield return this.StartCoroutine(this.MoveTransformToPosition(rTransforms[i], targets[i], speed, isLerping));
			}
		}
	}

	// Set list of rect transforms' positions
	public void SetTransformListPositions(List<RectTransform> rTransforms, Vector2[] position)
	{
		for(int i = 0; i < rTransforms.Count; i++)
		{
			rTransforms[i].anchoredPosition = position[i];
		}
	}

	#endregion

	/*
	#region Text

	// Fade singular text component's alpha to target alpha - based off fade time
	public IEnumerator Fade(Text txt, bool isFadingIn, float fadeTime, float targetAlpha)
	{
		float alpha = txt.color.a;
		float distance = isFadingIn ? targetAlpha - alpha : alpha - targetAlpha;
		float fadeSpeed = distance/fadeTime;

		if(isFadingIn)
		{
			while(alpha <= (targetAlpha - 0.001f))
			{
				alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
				txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
				yield return null;
			}
		}
		else
		{
			while(alpha >= (targetAlpha + 0.001f))
			{
				alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
				txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
				yield return null;
			}
		}
		yield return null;
	}

	// Fade singular text component's alpha to target alpha - based off fade speed, can toggle linear interpolation
	public IEnumerator Fade(Text txt, bool isFadingIn, float fadeSpeed, float targetAlpha, bool isLerping)
	{
		float alpha = txt.color.a;

		if(isFadingIn)
		{
			while(alpha <= (targetAlpha - 0.001f))
			{
				alpha = isLerping ? Mathf.Lerp(alpha, targetAlpha, fadeSpeed * Time.deltaTime) :
					Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
				txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
				yield return null;
			}
		}
		else
		{
			while(alpha >=  (targetAlpha + 0.001f))
			{
				alpha = isLerping ? Mathf.Lerp(alpha, targetAlpha, fadeSpeed * Time.deltaTime) :
					Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
				txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
				yield return null;
			}
		}
		yield return null;
	}

	// Fade list of text components' alpha to target alpha - based off fade time, can toggle moving simultaneously
	public IEnumerator FadeList(List<Text> textList, bool isFadingIn, float fadeTime, float targetAlpha,
		bool isSimultaneous)
	{
		float alpha = textList[0].color.a;
		float distance = isFadingIn ? 1.0f - alpha : alpha;
		float fadeSpeed = distance/fadeTime;

		if(isSimultaneous)
		{
			for(int i = 0; i < textList.Count; i++)
			{
				if(i < textList.Count - 1)
				{
					this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeTime, targetAlpha));
				}
				else
				{
					yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeTime, targetAlpha));
				}
			}
		}
		else
		{
			for(int i = 0; i < textList.Count; i++)
			{
				yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeTime, targetAlpha));
			}
		}
	}

	// Fade list of text components' alpha to target alpha - based off fade speed, can toggle linear interpolation and
	// moving simultaneously
	public IEnumerator FadeList(List<Text> textList, bool isFadingIn, float fadeSpeed, float targetAlpha,
		bool isLerping, bool isSimultaneous)
	{
		if(isSimultaneous)
		{
			for(int i = 0; i < textList.Count; i++)
			{
				if(i < textList.Count -1)
				{
					this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeSpeed, targetAlpha, isLerping));
				}
				else
				{
					yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeSpeed, targetAlpha, isLerping));
				}
			}
		}
		else
		{
			for(int i = 0; i < textList.Count; i++)
			{
				yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, fadeSpeed, targetAlpha, isLerping));
			}
		}

		yield return null;
	}
		

	// TODO : Figure out how to shift colours based off speed
	//			- How do we know when to stop the while loop?
	//			- Determine when a colour is equal to another??? HOW
	// TODO : Add lerp toggle to time param functions, add speed param alts of time only param functions


	// Shift singular text component's colour to target colour - based off shift time
	public IEnumerator ShiftColor(Text text, Color targetColour, float shiftTime)
	{
		Color startCol = text.color;
		float elapsedTime = 0.0f;

		while(elapsedTime < shiftTime)
		{
			text.color = Color.Lerp(startCol, targetColour, elapsedTime / shiftTime);
			elapsedTime += Time.deltaTime;

			yield return null;
		}

		text.color = targetColour;

		yield return null;
	}

	// Shift list of text components' colour to target colour - based off shift time TODO: test
	public IEnumerator ShiftListColours(List<Text> textList, Color targetColour, float shiftTime, 
		bool isSimultaneous)
	{
		for(int i = 0; i < textList.Count; i++)
		{
			if(isSimultaneous)
			{
				if(i < textList.Count - 1)
				{
					this.StartCoroutine(this.ShiftColor(textList[i], targetColour, shiftTime));
				}
				else
				{
					yield return this.StartCoroutine(this.ShiftColor(textList[i], targetColour, shiftTime));
				}
			}
			else
			{
				this.StartCoroutine(this.ShiftColor(textList[i], targetColour, shiftTime));
			}
		}
		yield return null;
	}

	#endregion

*/

	#region Images

	// TODO: test, comment
	public IEnumerator Fade(Image image, bool isFadingIn, float targetAlpha, float speed, bool isLerping)
	{
		float alpha = image.color.a;

		if(isFadingIn)
		{
			while(alpha <= (targetAlpha - 0.001f))
			{
				alpha = isLerping ? Mathf.Lerp(alpha, targetAlpha, speed * Time.deltaTime) :
					Mathf.MoveTowards(alpha, targetAlpha, speed * Time.deltaTime);
				image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
				yield return null;
			}
		}
		else
		{
			while(alpha >= (targetAlpha + 0.001f))
			{
				alpha = isLerping ? Mathf.Lerp(alpha, targetAlpha, speed * Time.deltaTime) :
					Mathf.MoveTowards(alpha, targetAlpha, speed * Time.deltaTime);	
				image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
				yield return null;
			}
		}
		yield return null;
	}

	// TODO: test, comment
	public IEnumerator Fade(Image image, bool isFadingIn, float targetAlpha, float fadeTime)
	{
		float alpha = image.color.a;
		float distance = isFadingIn ? targetAlpha - alpha : alpha - targetAlpha;
		float speed = distance/fadeTime;

		if(isFadingIn)
		{
			while(alpha <= (targetAlpha - 0.001f))
			{
				alpha = Mathf.MoveTowards(alpha, targetAlpha, speed * Time.deltaTime);
				image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
				yield return null;
			}
		}
		else
		{
			while(alpha >= (targetAlpha + 0.001f))
			{
				alpha = Mathf.MoveTowards(alpha, targetAlpha, speed * Time.deltaTime);	
				image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
				yield return null;
			}
		}
		yield return null;
	}

	// TODO: test, comment
	private IEnumerator FadeList(List<Image> images, bool isFadingIn, float targetAlpha, float speed, bool isLerping, bool isSimultaneous)
	{
		for(int i = 0; i < images.Count; i++)
		{
			if(isSimultaneous)
			{
				if(i < images.Count - 1)
				{
					this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, speed, isLerping));
				}
				else
				{
					yield return this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, speed, isLerping));
				}
			}
			else
			{
				yield return this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, speed, isLerping));
			}
		}
		yield return null;
	}

	// TODO: test, comment
	private IEnumerator FadeList(List<Image> images, bool isFadingIn, float targetAlpha, float fadeTime, bool isSimultaneous)
	{
		for(int i = 0; i < images.Count; i++)
		{
			if(isSimultaneous)
			{
				if(i < images.Count - 1)
				{
					this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, fadeTime));
				}
				else
				{
					yield return this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, fadeTime));
				}
			}
			else
			{
				yield return this.StartCoroutine(this.Fade(images[i], isFadingIn, targetAlpha, fadeTime));
			}
		}
		yield return null;
	}

	// Shift singular image component's colour to target colour - based off shift time TODO: test - implement lerp with time(^) like this
	public IEnumerator ShiftColor(Image image, Color targetColour, float shiftTime)
	{
		Color startCol = image.color;
		float elapsedTime = 0.0f;

		while(elapsedTime < shiftTime)
		{
			image.color = Color.Lerp(startCol, targetColour, elapsedTime / shiftTime);
			elapsedTime += Time.deltaTime;

			yield return null;
		}

		image.color = targetColour;

		yield return null;
	}

	// Shift list of image components' colour to target colour - based off shift time TODO: test
	public IEnumerator ShiftListColours(List<Image> images, Color targetColour, float shiftTime, bool isSimultaneous)
	{
		for(int i = 0; i < images.Count; i++)
		{
			if(isSimultaneous)
			{
				if(i < images.Count - 1)
				{
					this.StartCoroutine(this.ShiftColor(images[i], targetColour, shiftTime));
				}
				else
				{
					yield return this.StartCoroutine(this.ShiftColor(images[i], targetColour, shiftTime));
				}
			}
			else
			{
				this.StartCoroutine(this.ShiftColor(images[i], targetColour, shiftTime));
			}
		}
		yield return null;
	}

	#endregion

	/*
	#region Containers (Buttons, etc)

	// Buttons will essentiall be performing the same operations that are previously listed, although they will have child objects to worry about
	// perhaps write some functions to dynamically find all children + their types(Text, Image, )

	private IEnumerator Fade(RectTransform rTransform, bool isFadingIn, float targetAlpha, float speed, bool isLerping, bool isSimultaneous)
	{
		List<Text> textList = new List<Text>();
		List<Image> imageList = new List<Image>();

		// Find out the type of component the parent has & add it to the list
		if(rTransform.GetComponent<Text>())
		{
			textList.Add(rTransform.GetComponent<Text>());
		}
		else if (rTransform.GetComponent<Image>())
		{
			imageList.Add(rTransform.GetComponent<Image>());
		}

		// Store references to any child object containing a txt/img components
		textList.AddRange(rTransform.GetComponentsInChildren<Text>());
		imageList.AddRange(rTransform.GetComponentsInChildren<Image>());


		// Fade all UI elements
		if(isSimultaneous)
		{
			if(textList.Count > imageList.Count) // yield return at last index of text list
			{
				for(int i = 0; i < imageList.Count; i++)
				{
					this.StartCoroutine(this.Fade(imageList[i], isFadingIn, speed, targetAlpha, isLerping));
				}

				for(int i = 0; i < textList.Count; i++)
				{
					if(i < textList.Count - 1)
					{
						this.StartCoroutine(this.Fade(textList[i], isFadingIn, speed, targetAlpha, isLerping));
					}
					else
					{
						yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, speed, targetAlpha, isLerping));
					}
				}

			}
			else // yield return at last index of img list
			{
				for(int i = 0; i < textList.Count; i++)
				{
					this.StartCoroutine(this.Fade(textList[i], isFadingIn, speed, targetAlpha, isLerping));
				}

				for(int i = 0; i < imageList.Count; i++)
				{
					if(i < imageList.Count - 1)
					{
						this.StartCoroutine(this.Fade(imageList[i], isFadingIn, speed, targetAlpha, isLerping));
					}
					else
					{
						yield return this.StartCoroutine(this.Fade(imageList[i], isFadingIn, speed, targetAlpha, isLerping));
					}
				}
			}
		}
		else
		{
			for(int i = 0; i < textList.Count; i++)
			{
				yield return this.StartCoroutine(this.Fade(imageList[i], isFadingIn, speed, targetAlpha, isLerping));
			}
			for(int i = 0; i < textList.Count; i++)
			{
				yield return this.StartCoroutine(this.Fade(textList[i], isFadingIn, speed, targetAlpha, isLerping));
			}
		}

		yield return null;
	}

	#endregion
	*/
}