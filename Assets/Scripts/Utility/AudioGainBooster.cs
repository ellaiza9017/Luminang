using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioGainBooster : MonoBehaviour
{
    [Tooltip("How much louder to make the sound (e.g., 2 = twice as loud)")]
    [Range(1f, 10f)]
    public float gainMultiplier = 3f;

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Clamp(data[i] * gainMultiplier, -1f, 1f);
        }
    }
}
