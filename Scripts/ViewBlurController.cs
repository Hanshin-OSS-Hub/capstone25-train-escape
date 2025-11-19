// Assets/Scripts/ViewBlurController.cs
// ChatGPT로 제작
// Player 오브젝트에 추가
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;   // ★ URP용

public class ViewBlurController : MonoBehaviour
{
    public static ViewBlurController Instance { get; private set; }

    [Header("Volume References")]
    [Tooltip("DepthOfField가 들어 있는 Global Volume 오브젝트")]
    public Volume globalVolume;

    [Header("Blur Logic")]
    [Tooltip("게임 시작 시 블러 정도 (0=선명, 1=최대 블러)")]
    [Range(0f, 1f)] public float initialBlur = 1f;

    [Tooltip("currentBlur=0일 때 focalLength 값 (가장 선명)")]
    public float minFocalLength = 10f;

    [Tooltip("currentBlur=1일 때 focalLength 값 (가장 흐림)")]
    public float maxFocalLength = 50f;

    [Tooltip("블러 값이 바뀔 때 전환 소요 시간(초)")]
    public float transitionTime = 0.5f;

    float currentBlur;          // 0~1
    Coroutine currentRoutine;

    DepthOfField dof;           // ★ DepthOfField 컴포넌트 캐시

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (globalVolume == null)
        {
            globalVolume = FindAnyObjectByType<Volume>();
        }

        // Volume Profile에서 DepthOfField 가져오기
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (!globalVolume.profile.TryGet(out dof))
            {
                Debug.LogError("Global Volume Profile에 DepthOfField Override가 없습니다!");
            }
            else
            {
                // focalLength를 우리가 제어한다는 표시
                dof.focalLength.overrideState = true;
            }
        }
        else
        {
            Debug.LogError("Global Volume 또는 Volume Profile이 설정되지 않았습니다.");
        }

        // weight는 항상 1로 고정 (효과 항상 켜두고 파라미터만 바꿈)
        if (globalVolume != null)
            globalVolume.weight = 1f;
    }

    void Start()
    {
        SetBlurInstant(initialBlur);
    }

    void SetBlurInstant(float value)
    {
        currentBlur = Mathf.Clamp01(value);
        ApplyToDepthOfField();
    }

    /// <summary>현재 currentBlur 값을 DepthOfField.focalLength에 반영</summary>
    void ApplyToDepthOfField()
    {
        if (dof == null) return;

        float focal = Mathf.Lerp(minFocalLength, maxFocalLength, currentBlur);
        dof.focalLength.value = focal;
        // 필요한 경우 focusDistance도 함께 조절 가능:
        // dof.focusDistance.overrideState = true;
        // dof.focusDistance.value = 어떤 값;
    }

    /// <summary>블러 정도(0~1)를 설정 (부드럽게 전환)</summary>
    public void SetBlurLevel(float value)
    {
        float target = Mathf.Clamp01(value);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(LerpBlur(target));
    }

    /// <summary>일부 선명해지기: amount만큼 블러 감소 (0.2f = 20% 선명해짐)</summary>
    public void AddClarity(float amount)
    {
        float target = Mathf.Clamp01(currentBlur - Mathf.Abs(amount));
        SetBlurLevel(target);
    }

    /// <summary>일부 더 흐릿해지기: amount만큼 블러 증가</summary>
    public void AddBlur(float amount)
    {
        float target = Mathf.Clamp01(currentBlur + Mathf.Abs(amount));
        SetBlurLevel(target);
    }

    IEnumerator LerpBlur(float target)
    {
        float start = currentBlur;
        float t = 0f;

        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / transitionTime);
            float v = Mathf.Lerp(start, target, k);

            currentBlur = v;
            ApplyToDepthOfField();   // ★ 매 프레임 focalLength 갱신

            yield return null;
        }

        currentBlur = target;
        ApplyToDepthOfField();
    }
}
