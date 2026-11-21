using System.Collections;
using UnityEngine;

public class LobbyDoor : MonoBehaviour
{
    [Header("Mô hình cánh cửa xoay")]
    public Transform doorModel;
    public float openAngle = 90f;
    public float openSpeed = 3f;

    private Quaternion closedRot;
    private Quaternion openRot;
    private Coroutine animCoroutine;

    void Start()
    {
        if (doorModel != null)
        {
            closedRot = doorModel.localRotation;
            openRot = closedRot * Quaternion.Euler(0, openAngle, 0);
        }
    }

    public void OpenDoor()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateDoor(openRot));
    }

    public void CloseDoor()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateDoor(closedRot));
    }

    IEnumerator AnimateDoor(Quaternion targetRot)
    {
        while (Quaternion.Angle(doorModel.localRotation, targetRot) > 0.1f)
        {
            doorModel.localRotation = Quaternion.Slerp(doorModel.localRotation, targetRot, Time.deltaTime * openSpeed);
            yield return null;
        }
        doorModel.localRotation = targetRot;
    }
}