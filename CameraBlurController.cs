using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls a URP Depth Of Field effect so the camera starts blurred and can
/// be eased to a clear focus (e.g., after the player picks up an item).
/// Attach to the same GameObject as a Volume or assign a reference.
/// </summary>
public class CameraBlurController : MonoBehaviour
{
	[Header("Volume Setup")]
	[SerializeField] private Volume volume;

	[Header("Blur Settings")]
	[SerializeField, Tooltip("Focus distance when the scene should appear extremely blurry.")]
	private float blurredFocusDistance = 0.2f;
	[SerializeField, Tooltip("Aperture (lower = more blur) for the fully blurred state.")]
	private float blurredAperture = 0.2f;
	[SerializeField, Tooltip("Focus distance when the scene should be crisp/clear.")]
	private float clearFocusDistance = 30f;
	[SerializeField, Tooltip("Aperture for the clear state (higher value = deeper focus).")]
	private float clearAperture = 8f;
	[SerializeField, Tooltip("Duration (seconds) for automatic transitions.")]
	private float defaultTransitionDuration = 1.5f;

	private DepthOfField depthOfField;
	private Coroutine transitionRoutine;

	void Awake()
	{
		if (volume == null)
		{
			volume = GetComponent<Volume>();
		}
		if (volume == null)
		{
			Debug.LogError("CameraBlurController needs a Volume with DepthOfField assigned.");
			enabled = false;
			return;
		}

		if (!volume.profile.TryGet(out depthOfField))
		{
			Debug.LogError("Assigned Volume does not contain a DepthOfField override.");
			enabled = false;
			return;
		}

		// Use Bokeh mode for controllable focus distance.
		depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
		depthOfField.active = true;

		ApplyDepthOfField(blurredFocusDistance, blurredAperture);
	}

	/// <summary>
	/// Sets blur instantly (0 = fully blurred, 1 = fully clear).
	/// </summary>
	public void SetBlurAmount(float normalized)
	{
		if (depthOfField == null) return;
		float t = Mathf.Clamp01(normalized);
		float targetFocus = Mathf.Lerp(blurredFocusDistance, clearFocusDistance, t);
		float targetAperture = Mathf.Lerp(blurredAperture, clearAperture, t);
		ApplyDepthOfField(targetFocus, targetAperture);
	}

	/// <summary>
	/// Smoothly transitions to a clear view.
	/// </summary>
	public void EaseToClear(float duration = -1f)
	{
		if (duration <= 0f)
		{
			duration = defaultTransitionDuration;
		}
		StartBlurTransition(1f, duration);
	}

	/// <summary>
	/// Smoothly transitions back to the blurred state (optional utility).
	/// </summary>
	public void EaseToBlur(float duration = -1f)
	{
		if (duration <= 0f)
		{
			duration = defaultTransitionDuration;
		}
		StartBlurTransition(0f, duration);
	}

	private void StartBlurTransition(float target, float duration)
	{
		if (transitionRoutine != null)
		{
			StopCoroutine(transitionRoutine);
		}
		transitionRoutine = StartCoroutine(AnimateBlur(target, duration));
	}

	private IEnumerator AnimateBlur(float targetNormalized, float duration)
	{
		if (depthOfField == null)
		{
			yield break;
		}

		float startFocus = depthOfField.focusDistance.value;
		float startAperture = depthOfField.aperture.value;
		float clamped = Mathf.Clamp01(targetNormalized);
		float endFocus = Mathf.Lerp(blurredFocusDistance, clearFocusDistance, clamped);
		float endAperture = Mathf.Lerp(blurredAperture, clearAperture, clamped);
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			float currentFocus = Mathf.Lerp(startFocus, endFocus, t);
			float currentAperture = Mathf.Lerp(startAperture, endAperture, t);
			ApplyDepthOfField(currentFocus, currentAperture);
			yield return null;
		}

		ApplyDepthOfField(endFocus, endAperture);
		transitionRoutine = null;
	}

	private void ApplyDepthOfField(float focusDistance, float aperture)
	{
		depthOfField.focusDistance.Override(Mathf.Max(0.01f, focusDistance));
		depthOfField.aperture.Override(Mathf.Clamp(aperture, 0.1f, 32f));
	}
}


