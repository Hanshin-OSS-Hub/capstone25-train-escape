// Assets/Scripts/ViewBlurController.cs
// ChatGPT로 제작
// Player 오브젝트에 추가
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;   // URP용
using UnityEngine.UI;                   // UI Image 제어용

public class ViewBlurController : MonoBehaviour
{
    public static ViewBlurController Instance { get; private set; }

    [Header("Volume References")]
    [Tooltip("DepthOfField가 들어 있는 Global Volume 오브젝트")]
    public Volume globalVolume;

    [Header("Blur Logic (DOF)")]
    [Tooltip("게임 시작 시 블러 정도 (0=선명, 1=최대 블러)")]
    [Range(0f, 1f)] public float initialBlur = 1f;

    [Tooltip("current DOF Blur=0일 때 focalLength 값 (가장 선명)")]
    public float minFocalLength = 1f;

    [Tooltip("current DOF Blur=1일 때 focalLength 값 (가장 흐림)")]
    public float maxFocalLength = 300f;

    [Tooltip("블러 값이 바뀔 때 전환 소요 시간(초)")]
    public float transitionTime = 0.5f;

    [Header("UI Blur Overlay (Canvas)")]
    [Tooltip("화면을 덮는 BlurOverlay 패널의 Image 컴포넌트")]
    public Image blurOverlayImage;

    [Tooltip("UI Blur=0일 때 Overlay 알파")]
    [Range(0f, 1f)] public float minOverlayAlpha = 0f;

    [Tooltip("UI Blur=1일 때 Overlay 알파")]
    [Range(0f, 1f)] public float maxOverlayAlpha = 0.8f;

    [Tooltip("true이면 Blur 값을 UI 알파에도 반영")]
    public bool controlOverlayAlpha = true;

    // ★ 이제 DOF용, UI용 블러 값을 따로 둠 (0~1)
    float currentBlurDOF;
    float currentBlurUI;

    Coroutine currentRoutine;
    DepthOfField dof;

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

        if (globalVolume != null && globalVolume.profile != null)
        {
            if (!globalVolume.profile.TryGet(out dof))
            {
                Debug.LogError("Global Volume Profile에 DepthOfField Override가 없습니다!");
            }
            else
            {
                dof.focalLength.overrideState = true;
            }
        }
        else
        {
            Debug.LogError("Global Volume 또는 Volume Profile이 설정되지 않았습니다.");
        }

        if (globalVolume != null)
            globalVolume.weight = 1f; // 항상 켜두고 파라미터만 제어
    }

    void Start()
    {
        // 시작 시 DOF/UI 둘 다 initialBlur로
        currentBlurDOF = Mathf.Clamp01(initialBlur);
        currentBlurUI = Mathf.Clamp01(initialBlur);
        ApplyBlurOutputs();
    }

    void ApplyBlurOutputs()
    {
        ApplyToDepthOfField();
        ApplyToOverlay();
    }

    void ApplyToDepthOfField()
    {
        if (dof == null) return;

        float focal = Mathf.Lerp(minFocalLength, maxFocalLength, currentBlurDOF);
        dof.focalLength.value = focal;
    }

    void ApplyToOverlay()
    {
        if (!controlOverlayAlpha || blurOverlayImage == null) return;

        Color c = blurOverlayImage.color;
        float alpha = Mathf.Lerp(minOverlayAlpha, maxOverlayAlpha, currentBlurUI);
        c.a = alpha;
        blurOverlayImage.color = c;
    }

    /// <summary>
    /// [기존 기능 유지] 블러 정도(0~1)를 설정하면
    /// DOF/UI 둘 다 같은 값으로 맞춰서 부드럽게 전환
    /// </summary>
    public void SetBlurLevel(float value)
    {
        float v = Mathf.Clamp01(value);
        SetBlurLevels(v, v);
    }

    /// <summary>
    /// DOF와 UI 블러 값을 각각 따로 설정 (0~1)
    /// </summary>
    public void SetBlurLevels(float dofValue, float uiValue)
    {
        dofValue = Mathf.Clamp01(dofValue);
        uiValue = Mathf.Clamp01(uiValue);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(LerpBlur(dofValue, uiValue));
    }

    /// <summary>
    /// [기존 기능 유지] 일부 선명해지기: amount만큼 둘 다 감소
    /// </summary>
    public void AddClarity(float amount)
    {
        float step = Mathf.Abs(amount);
        float targetDOF = Mathf.Clamp01(currentBlurDOF - step);
        float targetUI = Mathf.Clamp01(currentBlurUI - step);
        SetBlurLevels(targetDOF, targetUI);
    }

    /// <summary>
    /// [기존 기능 유지] 일부 더 흐릿해지기: amount만큼 둘 다 증가
    /// </summary>
    public void AddBlur(float amount)
    {
        float step = Mathf.Abs(amount);
        float targetDOF = Mathf.Clamp01(currentBlurDOF + step);
        float targetUI = Mathf.Clamp01(currentBlurUI + step);
        SetBlurLevels(targetDOF, targetUI);
    }

    /// <summary>
    /// ★ 새 기능: DOF와 UI를 서로 다른 양으로 선명하게 만들기
    /// ex) AddClaritySeparate(0.2f, 0.05f);
    /// </summary>
    public void AddClaritySeparate(float dofAmount, float uiAmount)
    {
        float targetDOF = Mathf.Clamp01(currentBlurDOF - Mathf.Abs(dofAmount));
        float targetUI = Mathf.Clamp01(currentBlurUI - Mathf.Abs(uiAmount));
        SetBlurLevels(targetDOF, targetUI);
    }

    /// <summary>
    /// (옵션) DOF와 UI를 서로 다른 양으로 더 흐리게 만들기
    /// </summary>
    public void AddBlurSeparate(float dofAmount, float uiAmount)
    {
        float targetDOF = Mathf.Clamp01(currentBlurDOF + Mathf.Abs(dofAmount));
        float targetUI = Mathf.Clamp01(currentBlurUI + Mathf.Abs(uiAmount));
        SetBlurLevels(targetDOF, targetUI);
    }

    IEnumerator LerpBlur(float targetDOF, float targetUI)
    {
        float startDOF = currentBlurDOF;
        float startUI = currentBlurUI;
        float t = 0f;

        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / transitionTime);

            currentBlurDOF = Mathf.Lerp(startDOF, targetDOF, k);
            currentBlurUI = Mathf.Lerp(startUI, targetUI, k);

            ApplyBlurOutputs();
            yield return null;
        }

        currentBlurDOF = targetDOF;
        currentBlurUI = targetUI;
        ApplyBlurOutputs();
    }
}
