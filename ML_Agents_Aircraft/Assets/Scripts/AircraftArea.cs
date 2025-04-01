using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {

        [Tooltip("The path the race will take")]
        public CinemachineSmoothPath racePath;

        [Tooltip("The prefab to use for checkpoint")]
        public GameObject checkpointPrefab;

        [Tooltip("The prefab to use for the start/end checkpoint")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("If true, enable training mode")]
        public bool trainingMode;

        public GameObject bonusPrefab;

        public List<AircraftAgent> aircraftAgents { get; private set; } 
        public List<GameObject> checkPoints {  get; private set; }
        public List<GameObject> bonusBoxes { get; private set; } = new List<GameObject>();
   


        /// <summary>
        /// Set up the area
        /// </summary>
        private void Start()
        {
            if(checkPoints == null) CreateCheckpoints();

        }


        /// <summary>
        /// Actions to perform when the script wakes up
        /// </summary>
        private void Awake()
        {
            if (aircraftAgents == null) FindAircraftAgents();
        }

        /// <summary>
        /// Finds aircraft agents in the area
        /// </summary>
        private void FindAircraftAgents()
        {
            // Find all aircraft agents in the area
            aircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();

            Debug.Assert(aircraftAgents.Count > 0, "No Aircraft agents found");
        }

        /// <summary>
        /// Create the checkpoints
        /// </summary>
        private void CreateCheckpoints()
        {
            // Create checkpoints along the race path
            Debug.Assert(racePath != null, "Race Path was not set");
            checkPoints = new List<GameObject>();
            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);

            for (int i = 0; i < numCheckpoints; i++)
            {
                // Instantiate either a checkpoint or finish line checkpoint
                GameObject checkpoint;
                if (i == numCheckpoints - 1) checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else checkpoint = Instantiate<GameObject>(checkpointPrefab);

                // Set the parent, position, and rotation
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                // Add the checkpoint to the list
                checkPoints.Add(checkpoint);

                // Create bonus boxes
                if (i != numCheckpoints - 1 && i != 0 && bonusPrefab != null)
                {
                    // Calculate side offsets using the checkpoint's local right direction
                    Vector3 rightOffset = checkpoint.transform.right * 3f; // Distance to place the bonus boxes apart

                    // First bonus box
                    GameObject bonusBox1 = Instantiate(bonusPrefab);
                    //bonusBox1.GetComponent<BonusBox>().bonusType = BonusBox.BonusType.SpeedBoost; // Example bonus type
                    BonusBox.BonusType randomType = (Random.Range(0, 2) == 0) ? BonusBox.BonusType.SpeedBoost : BonusBox.BonusType.Bullets;
                    bonusBox1.GetComponent<BonusBox>().bonusType = randomType; // Assign the random type
                    bonusBox1.transform.SetParent(racePath.transform);
                    bonusBox1.transform.localPosition = racePath.m_Waypoints[i].position + Vector3.up * 5f + rightOffset;
                    bonusBox1.transform.rotation = checkpoint.transform.rotation;
                    bonusBoxes.Add(bonusBox1);

                    // Second bonus box
                    GameObject bonusBox2 = Instantiate(bonusPrefab);
                    //bonusBox2.GetComponent<BonusBox>().bonusType = BonusBox.BonusType.SpeedBoost; // Example bonus type
                    randomType = (Random.Range(0, 2) == 0) ? BonusBox.BonusType.SpeedBoost : BonusBox.BonusType.Bullets;
                    bonusBox2.GetComponent<BonusBox>().bonusType = randomType; // Assign the random type
                    bonusBox2.transform.SetParent(racePath.transform);
                    bonusBox2.transform.localPosition = racePath.m_Waypoints[i].position + Vector3.up * 5f - rightOffset;
                    bonusBox2.transform.rotation = checkpoint.transform.rotation;
                    bonusBoxes.Add(bonusBox2);
                }
            }
        }

        /// <summary>
        ///  Resets the position of an agent using its current NextCheckpointIndex, unless
        ///  randomize is true, then will pick a new random checkpoint
        /// </summary>
        /// <param name="agent"> The agent to reset</param>
        /// <param name="randomize"> it true, will pick a new NextCheckpointIndex before reset</param>
        public void ResetAgentPosition(AircraftAgent agent, bool randomize = false)
        {
            if (aircraftAgents == null) FindAircraftAgents();
            if (checkPoints == null) CreateCheckpoints();


            if (randomize)
            {
                // Pick a new next checkpoint at random
                agent.NextCheckpointIndex = Random.Range(0, checkPoints.Count);
            }

            // Set start position to the previous checkpoint
            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if(previousCheckpointIndex == -1) previousCheckpointIndex = checkPoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            // Convert the position on the racePath to a position in 3d space
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            // Get the orientation of that position on the race path
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            // Calculate a horizontal offset so that agents are spread out
            Vector3 positionOffset = Vector3.right * (aircraftAgents.IndexOf(agent) - aircraftAgents.Count / 2f)
                                     * Random.Range(9f, 10f);

            // Set the aircraft position and rotation
            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }

    }

}

