using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MoveWhenSeen : MonoBehaviour
{
    public Camera playerCamera;
    public Transform target;
    public float moveSpeed = 2f;

    private bool hasStarted = false;
    // Start is called before the first frame update
    
    void Update()
    {
        // Update is called once per frame
        // Chỉ kiểm tra tầm nhìn cho đến khi chuyển động bắt đầu
        if (!hasStarted && IsVisibleToPlayer())
        {
            hasStarted = true;
        }

// Khi đã bắt đầu, tiếp tục di chuyển dù người chơi quay đi
if (hasStarted && target != null)
{
    transform.position = Vector3.MoveTowards(
        transform.position,
        target.position,
        moveSpeed * Time.deltaTime
    );
}
    }

    bool IsVisibleToPlayer()
{
    if (playerCamera == null)
        return false;

    Vector3 viewportPoint =
        playerCamera.WorldToViewportPoint(transform.position);

    // Nằm trong khung hình và ở phía trước camera
    if (viewportPoint.z <= 0 ||
        viewportPoint.x < 0 || viewportPoint.x > 1 ||
        viewportPoint.y < 0 || viewportPoint.y > 1)
    {
        return false;
    }

    // Kiểm tra vật thể có bị tường/vật khác che khuất không
    Vector3 direction = transform.position - playerCamera.transform.position;

    if (Physics.Raycast(
        playerCamera.transform.position,
        direction.normalized,
        out RaycastHit hit,
        direction.magnitude))
    {
        return hit.transform == transform ||
               hit.transform.IsChildOf(transform);
    }

    return false;
}
}
