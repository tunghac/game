using UnityEngine;

public class HackItem : MonoBehaviour
{
    public void OnPickedUp(player p)
    {
        RandomScaleEffect scaleEffect = p.GetComponent<RandomScaleEffect>();
        if (scaleEffect != null) scaleEffect.ApplyPermanentCure();
    }

    public void OnDropped(player p)
    {
        RandomScaleEffect scaleEffect = p.GetComponent<RandomScaleEffect>();
        if (scaleEffect != null) scaleEffect.RemovePermanentCure();
    }
}