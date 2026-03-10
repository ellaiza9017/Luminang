using UnityEngine;

public class HorseMove : MonoBehaviour
{
    public float speed = 1.5f;

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
