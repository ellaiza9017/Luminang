using UnityEngine;
using StarterAssets;

namespace Luminang.Utility
{
    /// <summary>
    /// Attach to any trigger volume/collider to create an instant respawn zone (e.g. water, out-of-bounds abyss).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerRespawnTrigger : MonoBehaviour
    {
        [Tooltip("Optional custom target transform to respawn the player to. If null, uses the controller's default respawn logic.")]
        public Transform SpecificRespawnTarget;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ThirdPersonController controller = other.GetComponent<ThirdPersonController>();
            if (controller == null)
            {
                controller = other.GetComponentInParent<ThirdPersonController>();
            }

            if (controller != null)
            {
                if (SpecificRespawnTarget != null)
                {
                    controller.Teleport(SpecificRespawnTarget.position, SpecificRespawnTarget.rotation);
                }
                else
                {
                    controller.Respawn();
                }
            }
        }
    }
}

