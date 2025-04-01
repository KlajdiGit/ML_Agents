using Unity.MLAgents;
using UnityEngine;

namespace Aircraft
{
    public class Bullet : MonoBehaviour
    {
        [Tooltip("Life duration of the bullet")]
        public float lifeDuration = 2f;

        private void Start()
        {
            Destroy(gameObject, lifeDuration); // Automatically destroy after its lifespan
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("agent"))
            {
                AircraftAgent agent = collision.gameObject.GetComponent<AircraftAgent>();
                if (agent != null)
                {
                    // Destroy the plane
                    Debug.Log("Plane destroyed!");
                    agent.StartCoroutine(agent.ExplosionReset());
                }
            }

            // Destroy the bullet itself
            Destroy(gameObject);
        }
    }
}
