﻿using UnityEngine;
using System.Collections;
using UnityEngine;
using Inputs = UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Cirrus.Circuit.Controls
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private Game _game;

        [SerializeField]
        private Inputs.InputActionAsset _inputActionAsset;

        [SerializeField]
        public string[] Names = { "Red", "Blue", "Yellow", "Green" };

        [SerializeField]
        public Color[] Colors = new Color[4];

        public Controller[] Controllers;

        public int ControllerCount = 0;

        private const int _playerMax = 4;

        public Color GetColor(int number)
        {
            Color color = Color.white;
            if (number < _playerMax)
            {
                color = Colors[number];
            }

            return color;
        }



        public void Awake()
        {
            Controllers = new Controller[_playerMax];
        }

        public void OnValidate()
        {
            if (_game == null)
                _game = FindObjectOfType<Game>();

        }

        // Update is called once per frame
        public void Start()
        {
            var devices = Inputs.InputDevice.all;                     

            // TODO: do not assume one player per device?
            foreach(var device in devices)
            {
                if (device != null)
                {
                    if (device is Inputs.Keyboard ||
                        device is Inputs.Gamepad)
                    {
                        //InputActionAsset actions;
                        foreach (Inputs.InputControlScheme scheme in _inputActionAsset.controlSchemes)
                        {
                            if (scheme.SupportsDevice(device))
                            {
                                Controllers[ControllerCount] = 
                                    new Controller(
                                        ControllerCount, 
                                        Names[ControllerCount], 
                                        Colors[ControllerCount], 
                                        device, 
                                        scheme);

                                ControllerCount++;
                            }
                        }                        
                    }
                }

                if (ControllerCount > _playerMax || ControllerCount > _game._selectedLevel.CharacterCount) break;
            }

            //Inputs.Users.InputUser.onUnpairedDeviceUsed += OnUnpairedInputDeviceUsed;
        }
        

        public void OnUnpairedInputDeviceUsed(Inputs.InputControl control)
        {
            if (control.device is Inputs.Gamepad)
            {
                //for(var i = 0; i < _users.Length; i++)
                //{
                //    // find a user without a paired device
                //    if (_users[i].pairedDevices.Count == 0)
                //    {
                //        // pair the new Gamepad device to that user
                //        _users[i] = InputUser.PerformPairingWithDevice(control.device, _users[i]);
                //        return;
                //    }
                //}
            }
            else if(control.device is Inputs.Keyboard)
            {

            }
        }

    }

}
