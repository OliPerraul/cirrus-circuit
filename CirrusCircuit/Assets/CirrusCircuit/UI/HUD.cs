﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Cirrus.Circuit.Controls;

namespace Cirrus.Circuit.UI
{
    public class HUD : MonoBehaviour
    {

        [SerializeField]
        private Player[] _playerDisplays;

        private List<Player> _availablePlayerDisplays;


        public void Awake()
        {
            _availablePlayerDisplays = new List<Player>();
            //Game.Instance.OnLevelSelectHandler += OnLevelSelect;
        }

        public void OnValidate()
        {

            //if (_characterSelect == null)
            //    _characterSelect = FindObjectOfType<CharacterSelect>();            
        }

        public void OnWaiting()
        {
            //_player.Enabled = true;

            _availablePlayerDisplays.Clear();
            _availablePlayerDisplays.AddRange(_playerDisplays);

            for (int i = 0; i < Game.Instance._selectedLevel.CharacterCount; i++)
            {
                _playerDisplays[i].TryChangeState(Player.State.Waiting);
            }
        }

        public void Join(Controls.Player controller)
        {
            if (_availablePlayerDisplays.Count != 0)
            {
                _availablePlayerDisplays[0].TryChangeState(Player.State.Ready, controller.Number);
                _availablePlayerDisplays.RemoveAt(0);
                //_playerDisplays[index]?.TryChangeState(state);
            }            
        }

        public void Leave(Controls.Player controller)
        {
            if (controller.PlayerDisplay)
            {
                controller.PlayerDisplay.TryChangeState(Player.State.Waiting);
                _availablePlayerDisplays.Add(controller.PlayerDisplay);
                controller.PlayerDisplay = null;
            }
        }

        public void OnScoreChanged(int player, float score)
        {
            _playerDisplays[(int)player].OnScoreChanged(score);
        }       
    }
}