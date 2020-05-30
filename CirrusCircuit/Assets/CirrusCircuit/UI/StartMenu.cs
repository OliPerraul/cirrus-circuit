﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit.UI
{
    public class StartMenu : BaseSingleton<StartMenu>
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


        public override void OnValidate()
        {
            base.OnValidate();
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


        public override void Awake()
        {
            base.Awake();

            _exitButton.onClick.AddListener(OnExitClick);            
            _joinButton.onClick.AddListener(OnJoinClicked);
            _hostButton.onClick.AddListener(OnHostClicked);                       
        }

        public override void Start()
        {
            base.Start();

            Game.Instance.OnMenuHandler += (x) => Enabled = x;
        }

        public void OnHostClicked()
        {
            if(CustomNetworkManager.Instance.StartHost(_joinInput.text)) Game.Instance.JoinSession();
            else Debug.Log("Unable to host");
        }

        public void OnJoinClicked()
        {
            // TODO erro
            if (_joinInput == null) return;
            if (string.IsNullOrEmpty(_joinInput.text)) return;

            if (CustomNetworkManager.Instance.StartClient(_joinInput.text)) Game.Instance.JoinSession();
            else Debug.Log("Unable to join");
        }
        
        public void OnExitClick()
        {

        }

    }
}
