using UnityEngine;

public class StartCrystalBounce : MonoBehaviour
{
    public Animator crystal1;
    public Animator crystal2;
    public Animator crystal3;

    public void StartBounce()
    {
        crystal1.enabled = true;
        crystal2.enabled = true;
        crystal3.enabled = true;
    }
}