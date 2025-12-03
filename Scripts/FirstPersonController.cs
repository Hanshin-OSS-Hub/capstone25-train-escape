// Assets/Scripts/FirstPersonController.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;

    [Header("Move")]
    public float moveSpeed = 4.5f;
    public float gravity = -9.81f;
    public float sprintMultiplier = 1.6f;

    [Header("Crouch")]
    public float standHeight = 1.8f;          // 기본 서 있는 캐릭터 높이
    public float crouchHeight = 1.0f;         // 웅크린 높이
    public float crouchSpeedMultiplier = 0.5f;// 웅클릴 때 이동속도 감소율
    public float crouchTransitionSpeed = 8f;  // 높이가 바뀌는 속도 (부드럽게)
    bool isCrouching = false;
    float targetHeight;

    [Header("Look")]
    public float mouseSensitivity = 10.0f; // 수치 ↑ = 더 빠름
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private const string SensKey = "MouseSensitivity";

    [Header("Cursor")]
    public bool lockCursor = true;

    CharacterController controller;
    float pitch;          // 카메라 상하 각도
    float verticalVel;    // 중력/점프

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        controller.height = standHeight;
        targetHeight = standHeight;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        // 저장된 감도값이 있으면 가져오고, 없으면 지금 Inspector에 적혀 있는 기본값 사용
        float savedSensitivity = PlayerPrefs.GetFloat(SensKey, mouseSensitivity);
        mouseSensitivity = savedSensitivity;
        Debug.Log($"[FPC] Loaded Sensitivity: {mouseSensitivity}");
    }

    void Update()
    {
        Look();
        Move();
        Crouch();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 10f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 10f * Time.deltaTime;

        // 좌우(yaw)는 플레이어 바디 회전
        transform.Rotate(Vector3.up * mouseX);

        // 상하(pitch)는 카메라 로컬 회전
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D, ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S, ↑/↓
        Vector3 input = (transform.right * h + transform.forward * v).normalized;

        float speed = moveSpeed;

        if (!isCrouching && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W))
            speed *= sprintMultiplier;

        if (isCrouching)
            speed *= crouchSpeedMultiplier;

        Vector3 velocity = input * speed;

        if (controller.isGrounded)
        {
            verticalVel = -1f; // 살짝 눌러 붙이기
        }
        else
        {
            verticalVel += gravity * Time.deltaTime;
        }

        velocity.y = verticalVel;

        controller.Move(velocity * Time.deltaTime);
    }

    void Crouch()
    {
        // Ctrl 누르면 웅크리기
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standHeight;
        }

        // 부드럽게 캐릭터 높이 변경
        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        // 카메라도 같이 내려가도록 처리
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(
            camPos.y,
            isCrouching ? (crouchHeight * 0.5f) : (standHeight * 0.5f),
            Time.deltaTime * crouchTransitionSpeed
        );
        playerCamera.transform.localPosition = camPos;
    }
}
