using Cinemachine;
using System.Collections;
using System.Collections.Generic;
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

        public List<AircraftAgent> aircraftAgents { get; private set; } 
        public List<GameObject> checkPoints {  get; private set; }  
    }

}

