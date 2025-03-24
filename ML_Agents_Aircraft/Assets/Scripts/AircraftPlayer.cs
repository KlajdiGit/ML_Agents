using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Aircraft
{
    public class AircraftPlayer : AircraftAgent
    {

        [Header("Input Bindings")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;


        /// <summary>
        /// Calls base Initialize and initializes input
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            pitchInput.Enable();
            yawInput.Enable();
            boostInput.Enable();
            pauseInput.Enable();
        }


        /// <summary>
        /// Reads player input and converts it to a vector action array
        /// </summary>
        /// <param name="actionsOut">An array of floats for ONActionReceived to use</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActions = actionsOut.DiscreteActions;

            // Pitch: 1 == up, 0 == none, -1 == down
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            // Yaw: 1 == turn right, 0 == none, -1 == turn left
            float yawValue = Mathf.Round(yawInput.ReadValue<float>());

            // Boost: 1 == boost, 0 == no boost
            float boostValue = Mathf.Round(boostInput.ReadValue<float>());

            // New action: Bonus activation (1 == activate, 0 == no bonus)
            //float bonusValue = Mathf.Round(boostInput.ReadValue<float>());  


            // Convert -1 (down) to discrete value 2
            if (pitchValue == -1) pitchValue = 2f;

            // Convert -1 (turn left) to discrete value 2
            if (yawValue == -1) yawValue = 2f;

            discreteActions[0] = (int)pitchValue;
            discreteActions[1] = (int)yawValue;
            discreteActions[2] = (int)boostValue;


            //discreteActions[3] = (int)bonusValue;  

        }

        /// <summary>
        /// Cleans up the inputs when destroyed
        /// </summary>
        private void OnDestroy()
        {
            pitchInput.Disable();
            yawInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
}