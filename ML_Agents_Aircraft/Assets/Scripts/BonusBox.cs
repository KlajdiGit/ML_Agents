using UnityEngine;
using System.Collections;

namespace Aircraft
{
    public class BonusBox : MonoBehaviour
    {
        public enum BonusType { SpeedBoost, Shield, Bullets }
        public BonusType bonusType;
        public float duration = 5f; // For timed bonuses like speed boosts
        public float respawnTime = 2f; // Time before the box reappears

        private Collider boxCollider; // Reference to the Collider
        private MeshRenderer boxRenderer; // Reference to the MeshRenderer for visibility

        private void Awake()
        {
            // Get references to the Collider and MeshRenderer
            boxCollider = GetComponent<Collider>();
            boxRenderer = GetComponent<MeshRenderer>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("agent"))
            {
                AircraftAgent agent = other.GetComponent<AircraftAgent>();
                if (agent != null)
                {
                    agent.ActivateSpeedBonus(duration); // Notify the agent about the bonus
                    StartCoroutine(RespawnBonusBox()); // Handle visibility and respawn
                }
            }
        }


        private void GrantBonus(AircraftAgent agent)
        {
            switch (bonusType)
            {
                case BonusType.SpeedBoost:
                    agent.thrust *= 1.5f;
                    Debug.Log("Speed is now " + agent.thrust);
                    StartCoroutine(ResetThrust(agent, duration));
                    break;
                case BonusType.Shield:
                    // Implement shield logic
                    break;
                case BonusType.Bullets:
                    agent.AddReward(1f); // Award additional reward
                    break;
            }
        }

        private IEnumerator ResetThrust(AircraftAgent agent, float delay)
        {
            yield return new WaitForSeconds(delay);
            agent.thrust /= 1.5f; // Reset thrust
        }

        private IEnumerator RespawnBonusBox()
        {
            // Disable the box (make it invisible and non-collidable)
            boxCollider.enabled = false;
            boxRenderer.enabled = false;

            // Wait for the respawn time
            yield return new WaitForSeconds(respawnTime);

            // Re-enable the box
            boxCollider.enabled = true;
            boxRenderer.enabled = true;
        }
    }
}
