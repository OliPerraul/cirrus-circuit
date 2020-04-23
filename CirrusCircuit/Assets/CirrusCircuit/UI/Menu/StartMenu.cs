﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit.UI
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.UI.Button _playButton;

        [SerializeField]
        private UnityEngine.UI.Button _exitButton;

        [SerializeField]
        private UnityEngine.UI.Button _hostButton;

        [SerializeField]
        private UnityEngine.UI.Button _joinButton;

        [SerializeField]
        private UnityEngine.UI.InputField _joinInput;


        public void OnValidate()
        {

        }

        private bool _enabled = false;

        public bool Enabled
        {
            get => _enabled;

            set
            {
                _enabled = value;
                transform.GetChild(0).gameObject.SetActive(_enabled);
            }
        }


        public void Awake()
        {
            _exitButton.onClick.AddListener(OnExitClick);
            _playButton.onClick.AddListener(() => Game.Instance.StartLocal());
            _joinButton.onClick.AddListener(OnJoinClicked);
            _hostButton.onClick.AddListener(OnHostClicked);

            Game.Instance.OnCharacterSelectHandler += OnCharacterSelect;
        }

        public void OnHostClicked()
        {
            if (NetworkManagerWrapper.Instance.TryServerHost())
            {
                Game.Instance.StartOnline();
            }
            else
            {
                // TODO erro
                Debug.Log("Unable to host");
            }
        }


        public void OnJoinClicked()
        {
            // TODO erro
            if (_joinInput == null) return;
            if (string.IsNullOrEmpty(_joinInput.text)) return;
            if (NetworkManagerWrapper.Instance.TryClientJoin(_joinInput.text))
            {

            }
            else
            {
                // TODO log ero
                Debug.Log("Unable to connect");
            }

        }


        public void OnExitClick()
        {

        }


        public void OnCharacterSelect(bool enabled)
        {
            Enabled = false;
        }

    }
}
