// Assets/Scripts/PuzzleObject.cs
// ChatGPT로 제작
// 상호작용 오브젝트에 추가
using UnityEngine;

public class PuzzleObject : MonoBehaviour, IInteractable
{
    [Header("시야 선명도 회복량")]
    public float dofClarityStep = 0.25f;
    public float uiClarityStep = 0.25f;

    [Header("상호작용 설정")]
    [Tooltip("true이면 이 오브젝트는 한 번만 상호작용 가능")]
    public bool singleUse = true;

    bool hasInteracted = false;   // 이미 상호작용했는지 여부

    public void Interact(Transform interactor)
    {
        // 이미 사용한 오브젝트라면 더 이상 상호작용하지 않음
        if (singleUse && hasInteracted)
            return;

        // 여기부터는 "첫 번째 상호작용"일 때만 실행
        hasInteracted = true;

        // 퍼즐 처리 로직...
        Debug.Log($"{name} 과(와) 상호작용!");

        if (ViewBlurController.Instance != null)
        {
            ViewBlurController.Instance.AddClaritySeparate(dofClarityStep, uiClarityStep);
        }
    }

    public void OnHoverEnter() { }

    public void OnHoverExit() { }
}
