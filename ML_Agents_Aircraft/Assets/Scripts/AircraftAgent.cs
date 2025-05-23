using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [Header("Movement Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion Variables")]
        [Tooltip("The aircraft mesh that will disappear on explosion")]
        public GameObject meshObject;

        [Tooltip("The game object of the explosion particle effect")]
        public GameObject explosionEffect;


        [Header("Training")]
        [Tooltip("Number of steps to time out after in training")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex {  get; set; }

        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform bulletSpawnPoint;

        [SerializeField] private float bulletSpeed = 1000f;
        [SerializeField] private float bulletSpawnDuration = 5f;
        [SerializeField] private float bulletFireRate = 2f;
        private bool isBulletSpawning = false;

        [SerializeField] private Material speedBonusTrailColor;
        [SerializeField] private Material originalTrailColor;


        // Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;

        // When the next step timeout will be during training
        private float nextStepTimeout;

        // Whether the aircraft is frozen (intentionally not flying)
        private bool frozen = false;

        // Control
        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;



        private bool hasSpeedBonus = false;
        private float originalThrust; // Store the original thrust value


        /// <summary>
        /// Called when agent is first initialized
        /// </summary>
        public override void Initialize()
        {
            //area = GetComponent<AircraftArea>();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();

            Debug.Assert(area != null, "AircraftArea component is missing.");
            Debug.Assert(rigidbody != null, "Rigidbody component is missing.");
            Debug.Assert(trail != null, "TrailRenderer component is missing.");


            // Override the max step set in the inspector
            // Max 5000 steps if training, infinite steps if racing
            MaxStep = area.trainingMode ? 5000 : 0;
        }

        /// <summary>
        /// Called when a new episode begins
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Reset the velocity, position and orientation
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);

            // Update the step timeout if training
            if (area.trainingMode) nextStepTimeout = StepCount + stepTimeout;
        }

        /// <summary>
        /// Read action inputs from vectorAction
        /// </summary>
        /// <param name="actions"></param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            if (frozen) return;

            // Read values for pitch and yaw
            pitchChange = actions.DiscreteActions[0]; // up or none
            if (pitchChange == 2) pitchChange = -1; // down
            yawChange = actions.DiscreteActions[1]; // turn right or none
            if (yawChange == 2) yawChange = -1f; // turn left

            // Read value for boost and enable/disable trail renderer
            boost = actions.DiscreteActions[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            ProcessMovement();

            if(area.trainingMode)
            {
                // Small negative reward every step
                AddReward(-1f / MaxStep);

                // Make sure we haven't tun out of time if training
                if(StepCount > nextStepTimeout) 
                {
                    AddReward(-.5f);
                    EndEpisode();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if (localCheckpointDir.magnitude < Academy.Instance.EnvironmentParameters.GetWithDefault("checkpoint_radius", 0f))
                {
                    GotCheckpoint();
                }
            }
        }

        /// <summary>
        /// Collects observations used by the agent to make decisions
        /// </summary>
        /// <param name="sensor">The vector sensor</param>
      /*  public override void CollectObservations(VectorSensor sensor)
        {
            // Observer aircraft velocity (1 Vector3)
            sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

            // Where is the next checkpoint? (1 Vector3)
            sensor.AddObservation(VectorToNextCheckpoint());

            // Orientation of the next checkpoint (1 Vector3)
            Vector3 nextCheckpointForward = area.checkPoints[NextCheckpointIndex].transform.forward;
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));

            // Total Observations = 3 + 3 + 3 = 9
        }*/



        /// <summary>
        /// In this project, we only except Heuristic to be used  to be used on AircraftPlayer
        /// </summary>
        /// <param name="actionsOut"></param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            Debug.LogError("Heuristic() was called on " + gameObject.name +
                          " Make sure only the AircraftPlayer is set to Behavior Type: Heuristic Only.");

        }

        /// <summary>
        /// Prevent the agent from moving and taking actions
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// Resume agent movement and actions
        /// </summary>
        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }


        /// <summary>
        /// Gets a vector to the next checkpoint the agent needs to fly through
        /// </summary>
        /// <returns> A local-space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.checkPoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        /// <summary>
        /// Called when the agent flies through the correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            // Next checkpoint reached, update
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.checkPoints.Count;

            if(area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = StepCount + stepTimeout;
            }
        }

        /// <summary>
        /// Calculate and apply movement
        /// </summary>
        private void ProcessMovement()
        {
            // Calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

            // Apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            // Get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            // Calculate the roll angle (between -180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;

            if(yawChange == 0f)
            {
                // Not turning; smoothly roll toward center
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                // Turning; roll in opposite direction of turn
                rollChange = -yawChange;
            }

            // Calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            // Calculate new pitch, yaw, roll. Clamp pitch and roll
            float pitch = curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
            if (pitch > 100f) pitch -= 360f;
            pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

            float roll = curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed;
            if (roll > 100f) roll -= 360f;
            roll = Mathf.Clamp(roll, -maxRollAngle, maxRollAngle);

            // Set the new rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }



        /// <summary>
        /// React to entering a trigger
        /// </summary>
        /// <param name="other"> The collider entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if(other.transform.CompareTag("checkpoint") &&
               other.gameObject == area.checkPoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        /// <summary>
        /// React to collision
        /// </summary>
        /// <param name="collision">Collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                // Check if the object is a Bonus
                if (collision.transform.CompareTag("Bonus"))
                {
                    BonusBox bonusBox = collision.gameObject.GetComponent<BonusBox>();
                    if (bonusBox != null)
                    {
                        Debug.Log("Bonus type is: " + bonusBox.bonusType);

                        // Optional: Add a reward during training mode
                        if (area.trainingMode)
                        {
                            AddReward(1f);
                        }

                    }

                    return; // Exit early since we handled the Bonus
                }

                // Handle collision with other objects
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    EndEpisode();
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }


        /// <summary>
        /// Restes the aircraft to the most recent complete checkpoint
        /// </summary>
        /// <returns> yield return</returns>
        public IEnumerator ExplosionReset()
        {
            FreezeAgent();

            // Disable aircraft mesh object, enable explosion
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            // Disable explosion, re-enable aircraft mesh
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);

            // Reset Position
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }


        public void ActivateSpeedBonus(float duration)
        {
            if (!hasSpeedBonus)
            {
                originalThrust = thrust; // Save the original thrust
                thrust *= 1.5f; // Apply the bonus
                hasSpeedBonus = true; // Mark as active

                // Modify the trail renderer properties
                TrailRenderer trail = GetComponent<TrailRenderer>(); // Ensure your aircraft has a TrailRenderer component
                
                
                if (trail != null)
                {
                    if (trail != null && speedBonusTrailColor != null)
                    {
                        trail.material = speedBonusTrailColor;
                        Debug.Log("Checkpoint material successfully assigned!");
                    }
                    else
                    {
                        Debug.LogWarning("Checkpoint material reference is missing!");
                    }

                }

                Debug.Log("Speed bonus activated for " + gameObject.name + " Current thrust: " + thrust);
                StartCoroutine(ResetSpeedBonus(duration)); // Schedule reset
            }
        }



        private IEnumerator ResetSpeedBonus(float delay)
        {
            yield return new WaitForSeconds(delay);

            thrust = originalThrust; // Restore original thrust
            hasSpeedBonus = false; // Mark as inactive
            if (trail != null)
            {
                if (trail != null && originalTrailColor != null)
                {
                    trail.material = originalTrailColor;
                }
                else
                {
                    Debug.LogWarning("Checkpoint material reference is missing!");
                }

            }
            Debug.Log("Speed bonus ended for" +  gameObject.name +  " Current thrust: " + thrust);
        }
        /*
                public override void CollectObservations(VectorSensor sensor)
                {
                    // Observe aircraft velocity (1 Vector3)
                    sensor.AddObservation(transform.InverseTransformDirection(rigidbody.velocity));

                    // Where is the next checkpoint? (1 Vector3)
                    sensor.AddObservation(VectorToNextCheckpoint());

                    // Orientation of the next checkpoint (1 Vector3)
                    Vector3 nextCheckpointForward = area.checkPoints[NextCheckpointIndex].transform.forward;
                    sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));

                    // Where is the nearest bonus box? (1 Vector3)
                    Vector3 nearestBonusBoxPosition = GetNearestBonusBoxPosition(); // Custom helper method
                    Vector3 bonusBoxDirection = (nearestBonusBoxPosition - transform.position).normalized;
                    sensor.AddObservation(transform.InverseTransformDirection(bonusBoxDirection));

                    // Distance to the nearest bonus box (1 float)
                    float bonusBoxDistance = Vector3.Distance(transform.position, nearestBonusBoxPosition);
                    sensor.AddObservation(bonusBoxDistance);

                    // Total Observations = 3 (velocity) + 3 (next checkpoint direction) + 3 (next checkpoint forward) 
                    //                     + 3 (nearest bonus box direction) + 1 (bonus box distance) = 13
                }
        */

        public override void CollectObservations(VectorSensor sensor)
        {
            // Observe aircraft velocity (1 Vector3)
            var velocity = transform.InverseTransformDirection(rigidbody.velocity);
            //Debug.Log($"Velocity Observation: {velocity}");
            sensor.AddObservation(velocity);

            // Where is the next checkpoint? (1 Vector3)
            var checkpointDir = VectorToNextCheckpoint();
            //Debug.Log($"Checkpoint Direction Observation: {checkpointDir}");
            sensor.AddObservation(checkpointDir);

            // Orientation of the next checkpoint (1 Vector3)
            var nextCheckpointForward = area.checkPoints[NextCheckpointIndex].transform.forward;
            //Debug.Log($"Checkpoint Forward Observation: {nextCheckpointForward}");
            sensor.AddObservation(transform.InverseTransformDirection(nextCheckpointForward));

            // Where is the nearest bonus box? (1 Vector3)
            var nearestBonusBoxPos = GetNearestBonusBoxPosition();
            var bonusBoxDirection = (nearestBonusBoxPos - transform.position).normalized;
            //Debug.Log($"Nearest Bonus Box Direction Observation: {bonusBoxDirection}");
            sensor.AddObservation(transform.InverseTransformDirection(bonusBoxDirection));

            // Distance to the nearest bonus box (1 float)
            var bonusBoxDistance = Vector3.Distance(transform.position, nearestBonusBoxPos);
            //Debug.Log($"Bonus Box Distance Observation: {bonusBoxDistance}");
            sensor.AddObservation(bonusBoxDistance);

            // Validate total number of observations (should be 13)
            //Debug.Log($"Total Observations Added: {sensor.ObservationSize()}");
        }

        private Vector3 GetNearestBonusBoxPosition()
        {
            Vector3 nearestPosition = Vector3.zero;
            float minDistance = float.MaxValue;

            // Iterate through all bonus boxes in the area
            foreach (GameObject bonusBox in area.bonusBoxes) // Ensure `bonusPrefabs` is a list in AircraftArea
            {
                float distance = Vector3.Distance(transform.position, bonusBox.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPosition = bonusBox.transform.position;
                }
            }
            return nearestPosition;
        }

        /// <summary>
        /// Activates the bullet bonus functionality
        /// </summary>
        public void ActivateBulletBonus()
        {
            if (!isBulletSpawning)
            {
                isBulletSpawning = true;
                StartCoroutine(SpawnBullets());
            }
        }

        private IEnumerator SpawnBullets()
        {
            float spawnInterval = 1f / bulletFireRate;
            Debug.Log($"bulletSpawnDuration: {bulletSpawnDuration}, bulletFireRate: {bulletFireRate}, spawnInterval: {spawnInterval}");

            int expectedBullets = Mathf.FloorToInt(bulletSpawnDuration / spawnInterval);
            Debug.Log($"Expected bullets to spawn: {expectedBullets}");

            // Temporarily decrease thrust while firing bullets
            float originalThrust = thrust;
            thrust /= 2; // Reduce thrust by half

            for (int i = 0; i < 10; i++)
            {
                if (!gameObject.activeInHierarchy) // If the plane is destroyed
                {
                    Debug.Log("Plane destroyed. Stopping bullet spawning.");
                    break; // Exit the loop immediately
                }
                GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                rb.velocity = bulletSpawnPoint.forward * bulletSpeed;
                Debug.Log($"Spawn bullet {i + 1}");

                yield return new WaitForSeconds(spawnInterval);
            }

            // Restore original thrust after bullet spawning
            thrust = originalThrust;
            isBulletSpawning = false;
        }

    }
}
