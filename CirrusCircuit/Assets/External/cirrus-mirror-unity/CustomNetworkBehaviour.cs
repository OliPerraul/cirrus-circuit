﻿using UnityEngine;
using System.Collections;
using System;
using Cirrus;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;
using Cirrus.Circuit.Networking;

namespace Cirrus.MirrorExt
{
    public class CustomNetworkBehaviour : NetworkBehaviour
    {
        public Delegate OnStopServerHandler;
        public Delegate OnStartAuthorityHandler;

        public Delegate OnStartClientHandler;
        public Delegate OnStopClientHandler;
        public Delegate OnStopAuthorityHandler;
        public Delegate OnStartServerHandler;
        public Delegate OnStartLocalPlayerHandler;

        public bool IsServerOrClient => isServer || isClient;

        private bool _isClientStarted = false;
        public bool IsClientStarted => _isClientStarted;

        private bool _isServerStarted = false;
        public bool IsServerStarted => _isServerStarted;

        private bool _isAuthorityStarted = false;
        public bool IsAuthorityStarted => _isAuthorityStarted;

        private bool _isLocalPlayerStarted = false;
        public bool IsLocalPlayerStarted => _isLocalPlayerStarted;

        public override void OnStopServer()
        {
            base.OnStopServer();

            OnStopServerHandler?.Invoke();

        }

        public virtual void Destroy()
        {
            if (gameObject != null)
            {

                if (CustomNetworkManager.IsServer) NetworkServer.Destroy(gameObject);
                Destroy(gameObject);
            }
        }


        public override void OnStartAuthority()
        {
            base.OnStartAuthority();


            OnStartAuthorityHandler?.Invoke();

        }


        public override void OnStartClient()
        {
            base.OnStartClient();

            _isClientStarted = true;

            OnStartClientHandler?.Invoke();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            OnStopClientHandler?.Invoke();
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();

            _isAuthorityStarted = true;

            OnStopAuthorityHandler?.Invoke();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _isServerStarted = true;

            OnStartServerHandler?.Invoke();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            _isLocalPlayerStarted = true;

            OnStartLocalPlayerHandler?.Invoke();

        }

    }
}
