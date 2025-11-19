// ChatGPT로 제작
// 상호작용 오브젝트에 추가
using UnityEngine;

public class PuzzleObject : MonoBehaviour, IInteractable
{
    public void Interact(Transform interactor)
    {
        // 퍼즐 처리 로직...
        Debug.Log("퍼즐 상호작용!");

        // 여기서 시야 조금 선명하게
        ViewBlurController.Instance?.AddClarity(0.15f);
    }

    public void OnHoverEnter() { /* 하이라이트 */ }
    public void OnHoverExit() { /* 하이라이트 해제 */ }
}
