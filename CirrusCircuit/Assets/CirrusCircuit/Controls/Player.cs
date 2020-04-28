﻿using Cirrus.Circuit.Controls;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;
//using Cirrus.Circuit.World.Objects.Characters.Actions;
//using Cirrus.Circuit.Controls;
using UnityInput = UnityEngine.InputSystem;
//using Cirrus.Circuit.Playable;

using Inputs = UnityEngine.InputSystem;
using Cirrus.Circuit.Networking;

// Controls Navmesh Navigation

// TODO control inv and Hotbar through the inventory user the the inv directly



namespace Cirrus.Circuit.Controls
{    
    //[System.Serializable]
    public class Player : ActionMap.IPlayerActions
    {
        public Networking.PlayerSession _session;

        public delegate void OnReady(Player ctrl);

        public OnReady OnReadyHandler;

        private ActionMap _actionMap;

        private Inputs.InputDevice _device;

        private Inputs.InputControlScheme _scheme;
        
        public World.Objects.Characters.Character _character;        

        public UI.CharacterSelectSlot _characterSlot;

        public UI.Player PlayerDisplay = null;

        public Vector2 AxisLeft => _actionMap.Player.AxesLeft.ReadValue<Vector2>();

        public int _localId = 0;

        public int LocalId => _localId;

        public Player(int localId, Inputs.InputDevice device, Inputs.InputControlScheme scheme)
        {

            _localId = localId;
            _device = device;
            //_user = Inputs.Users.InputUser.CreateUserWithoutPairedDevices();
            //Inputs.Users.InputUser.PerformPairingWithDevice(_device, _user);

            // Each player gets a separate action setup. This makes the state of actions and bindings
            // local to each player and also ensures we're not stepping on the action setup used by
            // DemoGame itself for the main menu (where we are not using control schemes and just blindly
            // bind to whatever devices are available locally).
            _scheme = scheme;
            _actionMap = new ActionMap();
            _actionMap.bindingMask = new Inputs.InputBinding { groups = _scheme.bindingGroup };
            Enable();
        }

        ~Player()
        {
            Enable(false);
        }

        public void Enable(bool enabled = true)
        {
            if (enabled)
            {
                _actionMap.Player.Enable();
                _actionMap.Player.SetCallbacks(this);
            }
            else
            {
                //_actionMap.Player.SetCallbacks(null);
                _actionMap.Player.Disable();
                _actionMap.Disable();
                _actionMap.Player.AxesLeft.Disable();
                _actionMap.Player.AxesLeft.Dispose();
            }
        }

        // TODO: Simulate LeftStick continuous axis with WASD
        public void OnAxesLeft(UnityInput.InputAction.CallbackContext context)
        {
            var axis = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1);
            GameSession.Instance.HandleAxesLeft(this, axis);
        }

        // Cancel
        public void OnAction0(UnityInput.InputAction.CallbackContext context)
        {
            if (!context.performed)
                GameSession.Instance.HandleAction0(this);
        }

        // Accept
        public void OnAction1(UnityInput.InputAction.CallbackContext context)
        {
            //context.
            if (!context.performed)
                GameSession.Instance.HandleAction1(this);
        }
    }
}