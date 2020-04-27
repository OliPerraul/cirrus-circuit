﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Cirrus.Circuit.Controls;
using Cirrus.Utils;
using Mirror;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cirrus.Circuit.UI;

namespace Cirrus.Circuit.Networking
{
    public class NetworkManagerHandler
    {
        protected CustomNetworkManager _net;

        public NetworkManagerHandler(CustomNetworkManager net)
        {
            _net = net;
        }

        public virtual void OnClientConnect(NetworkConnection conn)
        {

        }

        public virtual void OnClientDisconnect(NetworkConnection conn)
        {

        }

        public virtual void OnClientError(NetworkConnection conn, int errorCode)
        {

        }

        public virtual void OnStartClient()
        {

        }

        public virtual void OnStopClient()
        {

        }

        public virtual bool TryPlayerJoin(int playerId)
        {
            return false;
        }

        public virtual bool TryPlayerLeave(int localId)
        {
            return false;
        }
    }
    
    
    public class CustomNetworkManager : NetworkManager
    {
        private TelepathyTransport Transport => (TelepathyTransport) transport;
        private NetworkManagerHandler _handler;

        public bool IsServer => _handler is NetworkManagerServerHandler;
        public NetworkManagerClientHandler Client => IsServer ? null : (NetworkManagerClientHandler)_handler;
        public NetworkManagerServerHandler Server => IsServer ? (NetworkManagerServerHandler)_handler : null;

        public static CustomNetworkManager Instance => (CustomNetworkManager) singleton;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("_networkPlayerTemplate")]
        private ClientConnectionPlayer _networkClientTemplate;
        public ClientConnectionPlayer NetworkClientTemplate => _networkClientTemplate;

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            _handler.OnClientConnect(conn);
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            _handler.OnClientDisconnect(conn);
        }
        
        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            base.OnClientError(conn, errorCode);
            _handler.OnClientError(conn, errorCode);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _handler.OnStartClient();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _handler.OnStopClient();
        }

        public override void Awake()
        {
            base.Awake();
        }

        public bool TryPlayerJoin(int playerId)
        {
            return _handler.TryPlayerJoin(playerId);
        }

        public bool TryPlayerJoin(Controls.Player player)
        {
            return TryPlayerJoin(player.LocalId);
        }

        public bool TryPlayerLeave(int playerId)
        {
            return TryPlayerLeave(playerId);
        }


        public bool TryInitHost(string port)
        {
            _handler = null;            
            ushort res = ushort.TryParse(port, out res) ? res: NetworkUtils.DefaultPort;
            _handler = new NetworkManagerServerHandler(this);
            Transport.port = res;
            StartHost();
            return true;            
        }
                
        // 25.1.149.130:4040

        public bool TryInitClient(string hostAddress)
        {
            _handler = null;
            if (NetworkUtils.TryParseAddress(hostAddress, out IPAddress adrs, out ushort port))
            {
                _handler = new NetworkManagerClientHandler(this);
                Transport.port = port;
                StartClient(NetworkUtils.ToUri(adrs, TelepathyTransport.Scheme));
                return true;
            }

            return false;
        }

    }
}
