﻿using UnityEngine;
using System.Collections;

namespace Cirrus.Circuit
{
    public class DebugTool : MonoBehaviour
    {
        [SerializeField]
        private Controls.Lobby _lobby;

        public void OnValidate()
        {
            if (_lobby == null)
                _lobby = FindObjectOfType<Controls.Lobby>();

        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.P))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    _lobby.Controllers[0].Score += 10f;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    _lobby.Controllers[1].Score += 10f;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    _lobby.Controllers[2].Score += 10f;
                }
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    _lobby.Controllers[3].Score += 10f;
                }
            }
        }

    }
}
