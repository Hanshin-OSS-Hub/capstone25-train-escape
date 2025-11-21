// Assets/Scripts/PuzzleObject.cs
// ChatGPT로 제작
// 상호작용 오브젝트에 추가
using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    // DOF는 한 번에 0.25 줄이고, UI는 0.1만 줄이기
    public float dofClarityStep = 0.25f;
    public float uiClarityStep = 0.25f;

    public void Interact(Transform interactor)
    {
        // 퍼즐 처리...
        Debug.Log("퍼즐 상호작용!");

        if (ViewBlurController.Instance != null)
        {
            ViewBlurController.Instance.AddClaritySeparate(dofClarityStep, uiClarityStep);
        }
    }
}
