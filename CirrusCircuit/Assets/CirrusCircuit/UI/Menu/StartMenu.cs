﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit.UI
{
    public class StartMenu : MonoBehaviour
    {
        //[SerializeField]
        //private UnityEngine.UI.Button _playButton;

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
            //_playButton.onClick.AddListener(() => Game.Instance.StartLocal());
            _joinButton.onClick.AddListener(OnJoinClicked);
            _hostButton.onClick.AddListener(OnHostClicked);

            GameSession.OnStartClientStaticHandler += OnSessionStart;
        }

        public void OnHostClicked()
        {
            if(CustomNetworkManager.Instance.TryStartHost(_joinInput.text))
                Game.Instance.StartSession();

            else
                Debug.Log("Unable to host");
        }


        public void OnJoinClicked()
        {
            // TODO erro
            if (_joinInput == null) return;
            if (string.IsNullOrEmpty(_joinInput.text)) return;

            if (CustomNetworkManager.Instance.TryStartClient(_joinInput.text))
                Game.Instance.StartSession();            
            else           
                // TODO log ero
                Debug.Log("Unable to join");
            
        }
        
        public void OnExitClick()
        {

        }

        public void OnSessionStart(bool enabled)
        {
            Enabled = !enabled;
        }

    }
}
