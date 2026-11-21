using UnityEngine;

public class CureItem : MonoBehaviour
{
    public float lifetime = 30f;
    private bool isActivated = false;

    public void OnPickedUp(player p)
    {
        if (isActivated) return;
        isActivated = true;

        RandomScaleEffect scaleEffect = p.GetComponent<RandomScaleEffect>();
        if (scaleEffect != null)
        {
            scaleEffect.ApplyCure(lifetime);
        }
    }
}