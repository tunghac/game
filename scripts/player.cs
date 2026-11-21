using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)] // [MỚI] chạy SAU RandomScaleEffect để bù kích thước vật phẩm đúng
[RequireComponent(typeof(CharacterController))]
public class player : MonoBehaviour
{
    public bool IsHoldingItem => heldObject != null;
    private Vector3 lastSafePos;
    private float lastSafeTime;
    private float noGravityUntil = 0f;
    [Header("Tham chieu")]
    public Transform playerCamera;
    public Transform holdPoint;
    public Transform inspectPoint;
    public TMP_Text interactPrompt;

    [Header("Hong Tam (Crosshair)")]
    public Image crosshair;
    public Color normalCrosshairColor = Color.white;
    public Color interactCrosshairColor = Color.red;

    [Header("Di chuyen")]
    public float moveSpeed = 4f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Nhin xung quanh")]
    public float mouseSensitivity = 2f;
    public float minLookAngle = -85f;
    public float maxLookAngle = 85f;

    [Header("Tuong tac")]
    public float interactDistance = 2f;
    public float holdRotateSpeed = 5f;
    public float pushForce = 3f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;
    private Rigidbody heldObject;
    private Collider[] heldColliders;
    private bool oldUseGravity;
    private bool oldIsKinematic;
    private Vector3 heldOriginalWorldScale;
    private Camera cam;
    private float baseNear, baseSkin, baseStep, baseScaleY;
    private const float MaxFall = 30f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        baseScaleY = Mathf.Max(0.0001f, transform.lossyScale.y);
        if (cam != null) baseNear = cam.nearClipPlane;
        baseSkin = controller.skinWidth;
        baseStep = controller.stepOffset;
    }
    void Start()
    {

        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;
        }

        if (interactPrompt != null) interactPrompt.gameObject.SetActive(false);
        if (crosshair != null) crosshair.color = normalCrosshairColor;

        MainMenuManager lobbyMenu = FindFirstObjectByType<MainMenuManager>();
        if (lobbyMenu != null && lobbyMenu.gameObject.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (PauseMenu.GameIsPaused) return;

        LookAround();
        MovePlayer();
        UpdateInteractionPrompt();
        HandleInteraction();

        if (heldObject != null)
        {
            Vector3 localPos = Input.GetMouseButton(1)
                ? holdPoint.InverseTransformPoint(inspectPoint.position)
                : Vector3.zero;
            heldObject.transform.localPosition = localPos;
        }
        if (controller != null && controller.enabled && controller.isGrounded && Time.time - lastSafeTime > 0.5f)
        {
            lastSafePos = transform.position;
            lastSafeTime = Time.time;
        }

        // Rơi quá sâu -> kéo về
        if (transform.position.y < -50f && lastSafePos != Vector3.zero)
        {
            controller.enabled = false;
            transform.position = lastSafePos + Vector3.up * 1f;
            controller.enabled = true;
            verticalVelocity = 0f;
        }
    }
    void AdaptToScale()
    {
        float s = transform.lossyScale.y / baseScaleY;

        if (cam != null) cam.nearClipPlane = Mathf.Max(0.005f, baseNear * s);

        if (controller != null)
        {
            controller.skinWidth = Mathf.Max(0.002f, baseSkin * s);
            controller.stepOffset = Mathf.Max(0.02f, baseStep * s);
        }
    }
    void LateUpdate()
    {
        // Giữ nguyên kích thước thật của vật phẩm trên tay dù nhân vật đang bị phóng to/thu nhỏ
        if (heldObject != null && holdPoint != null)
        {
            Vector3 parentScale = holdPoint.lossyScale;
            heldObject.transform.localScale = new Vector3(
                parentScale.x != 0 ? heldOriginalWorldScale.x / parentScale.x : heldOriginalWorldScale.x,
                parentScale.y != 0 ? heldOriginalWorldScale.y / parentScale.y : heldOriginalWorldScale.y,
                parentScale.z != 0 ? heldOriginalWorldScale.z / parentScale.z : heldOriginalWorldScale.z
            );
        }
    }

    void LookAround()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (heldObject != null && Input.GetMouseButton(1))
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");
            heldObject.transform.Rotate(playerCamera.up, -x * holdRotateSpeed, Space.World);
            heldObject.transform.Rotate(playerCamera.right, y * holdRotateSpeed, Space.World);
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void MovePlayer()
    {
        if (controller == null || !controller.enabled) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        // Chặn dt quá lớn (khung hình giật lúc đổi map) -> không bị rơi xuyên sàn
        float dt = Mathf.Min(Time.deltaTime, 0.033f);

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump")) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (Time.time < noGravityUntil) verticalVelocity = 0f; // nếu bạn đã thêm SetSpawnGrace

        verticalVelocity += gravity * dt;
        verticalVelocity = Mathf.Max(verticalVelocity, -MaxFall); // giới hạn tốc độ rơi

        Vector3 delta = (move * moveSpeed + Vector3.up * verticalVelocity) * dt;

        // Chia nhỏ bước di chuyển nếu quá dài so với bán kính (chống xuyên tường mỏng)
        float worldRadius = controller.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float maxStep = Mathf.Max(0.01f, worldRadius * 0.5f);
        int steps = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / maxStep), 1, 8);

        Vector3 part = delta / steps;
        for (int i = 0; i < steps; i++) controller.Move(part);
    }

    void UpdateInteractionPrompt()
    {
        if (interactPrompt == null || playerCamera == null) return;

        if (heldObject != null)
        {
            interactPrompt.gameObject.SetActive(false);
            if (crosshair != null) crosshair.color = normalCrosshairColor;
            return;
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            DoorPortal door = hit.collider.GetComponentInParent<DoorPortal>();
            Rigidbody rb = hit.collider.attachedRigidbody;

            if (door != null)
            {
                RandomScaleEffect scaleEffect = GetComponent<RandomScaleEffect>();
                if (scaleEffect != null && !scaleEffect.IsNormalState())
                {
                    interactPrompt.text = "Ảo giác - Cửa bị khóa!";
                    interactPrompt.gameObject.SetActive(true);
                    if (crosshair != null) crosshair.color = Color.gray;
                }
                else
                {
                    interactPrompt.text = "E - Mở cửa";
                    interactPrompt.gameObject.SetActive(true);
                    if (crosshair != null) crosshair.color = interactCrosshairColor;
                }
                return;
            }

            if (rb != null && !rb.isKinematic)
            {
                interactPrompt.text = "F - Nhặt đồ";
                interactPrompt.gameObject.SetActive(true);
                if (crosshair != null) crosshair.color = interactCrosshairColor;
                return;
            }
        }

        interactPrompt.gameObject.SetActive(false);
        if (crosshair != null) crosshair.color = normalCrosshairColor;
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
            {
                DoorPortal door = hit.collider.GetComponentInParent<DoorPortal>();

                if (door != null)
                {
                    RandomScaleEffect scaleEffect = GetComponent<RandomScaleEffect>();
                    if (scaleEffect != null && !scaleEffect.IsNormalState())
                    {
                        return;
                    }

                    ForceDestroyHeldItem();
                    GameHUD.GlobalDoorCount++;
                    door.Interact();
                    return;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (heldObject != null)
            {
                DropObject();
                return;
            }

            if (playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
                {
                    Rigidbody rb = hit.collider.attachedRigidbody;
                    DoorPortal doorCheck = hit.collider.GetComponentInParent<DoorPortal>();

                    if (rb != null && !rb.isKinematic && doorCheck == null)
                    {
                        PickUpObject(rb);
                    }
                }
            }
        }

        if (heldObject != null && Input.GetKeyDown(KeyCode.Q))
        {
            DropObject();
        }
    }

    void PickUpObject(Rigidbody rb)
    {
        heldObject = rb;
        oldUseGravity = rb.useGravity;
        oldIsKinematic = rb.isKinematic;
        heldOriginalWorldScale = rb.transform.lossyScale;

        rb.useGravity = false;
        rb.isKinematic = true;

        heldColliders = rb.GetComponentsInChildren<Collider>();
        foreach (Collider col in heldColliders) col.enabled = false;

        rb.transform.SetParent(holdPoint, true);
        rb.transform.localPosition = Vector3.zero;
        rb.transform.localRotation = Quaternion.identity;

        if (interactPrompt != null) interactPrompt.gameObject.SetActive(false);

        // Tìm cả trên object gốc lẫn con/cha để không bao giờ bỏ sót hiệu ứng
        CureItem cure = rb.GetComponentInChildren<CureItem>() ?? rb.GetComponentInParent<CureItem>();
        if (cure != null) cure.OnPickedUp(this);

        HackItem hack = rb.GetComponentInChildren<HackItem>() ?? rb.GetComponentInParent<HackItem>();
        if (hack != null) hack.OnPickedUp(this);
    }

    void DropObject()
    {
        if (heldObject == null) return;

        Rigidbody rb = heldObject;

        // Tắt hiệu ứng Hack ngay khi người chơi vứt vật phẩm Hack xuống
        HackItem hack = rb.GetComponentInChildren<HackItem>() ?? rb.GetComponentInParent<HackItem>();
        if (hack != null) hack.OnDropped(this);

        rb.transform.SetParent(null, true);
        rb.transform.localScale = heldOriginalWorldScale;

        if (heldColliders != null)
        {
            foreach (Collider col in heldColliders) col.enabled = true;
        }

        rb.isKinematic = oldIsKinematic;
        rb.useGravity = oldUseGravity;
        if (!rb.isKinematic) rb.velocity = playerCamera.forward * 2f;

        heldObject = null;
        heldColliders = null;
    }

    public void ForceDestroyHeldItem()
    {
        if (heldObject != null)
        {
            // Nếu là vật phẩm Hack -> GIỮ NGUYÊN trên tay để mang thẳng sang map mới!
            HackItem hack = heldObject.GetComponentInChildren<HackItem>() ?? heldObject.GetComponentInParent<HackItem>();
            if (hack != null)
            {
                return;
            }

            Destroy(heldObject.gameObject);
            heldObject = null;
            heldColliders = null;

            RandomScaleEffect scaleEffect = GetComponent<RandomScaleEffect>();
            if (scaleEffect != null) scaleEffect.RemoveCure();
        }
    }

    public void ForceEquipHackItem(Rigidbody rb)
    {
        if (rb != null && heldObject == null)
        {
            PickUpObject(rb);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        Vector3 pushDirection = hit.moveDirection;
        pushDirection.y = 0f;
        if (pushDirection.sqrMagnitude > 0.01f)
        {
            rb.AddForceAtPosition(pushDirection.normalized * pushForce, hit.point, ForceMode.Impulse);
        }
    }
    public void RefreshHeldItem()
    {
        if (heldObject == null) return;

        Transform t = heldObject.transform;
        if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
        if (t.parent != holdPoint) t.SetParent(holdPoint, true);
        t.localPosition = Vector3.zero;

        heldObject.isKinematic = true;
        heldObject.useGravity = false;
        if (heldColliders != null)
            foreach (Collider col in heldColliders) if (col != null) col.enabled = false;
    }
    public void SetSpawnGrace(float seconds)
    {
        noGravityUntil = Time.time + seconds;
        verticalVelocity = 0f;
    }
    public void ResetVelocity() { verticalVelocity = 0f; }
}