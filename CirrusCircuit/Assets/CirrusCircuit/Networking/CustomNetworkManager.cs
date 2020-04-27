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

    public class NetworkManagerClientHandler : NetworkManagerHandler
    {
        public NetworkConnection _conn;

        public const int ServerResponseTimeout = 1000;

        public NetworkManagerClientHandler(CustomNetworkManager net) : base(net)
        {

        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            _conn = conn;            
            _conn.Send(new ClientConnectedMessage());            
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            _conn = conn;
        }

        public override bool TryPlayerJoin(int localId)
        {
            _conn.Send(new ClientPlayerMessage {
                LocalPlayerId = localId,
                Id = ClientPlayerMessageId.Join
            });

            var response = ClientConnectionPlayer.Instance.WaitResponse(ServerResponseTimeout);

            switch (response.Id)
            {
                case ServerMessageId.ServerId:
                    if (response.LocalPlayerId < 0)
                    {
                        Debug.Log("invalid local player id connected");
                        return false;
                    }

                    if (response.ServerPlayerId < 0)
                    {
                        Debug.Log("invalid server player id received");
                        return false;
                    }
                    Debug.Log("Assigned server id with success: " + response.ServerPlayerId);
                    LocalPlayerManager.Instance.Players[response.LocalPlayerId]._serverId = response.ServerPlayerId;
                    LocalPlayerManager.Instance.Players[response.LocalPlayerId]._characterSlot = CharacterSelect.Instance._slots[response.ServerPlayerId];
                    return true;
                    break;
                default: return false;
            }

            return false;
        }

        public override bool TryPlayerLeave(int localId)
        {
            _conn.Send(new ClientPlayerMessage
            {
                LocalPlayerId = localId,
                Id = ClientPlayerMessageId.Leave
            });

            return true;
        }
    }

    public class NetworkManagerServerHandler : NetworkManagerHandler
    {
        private Dictionary<int, ClientConnectionPlayer> _connections = new Dictionary<int, ClientConnectionPlayer>();
        private Dictionary<int, List<int>> _players = new Dictionary<int, List<int>>();
        private int _playerCount = 0;


        public NetworkManagerServerHandler(CustomNetworkManager net) : base(net)
        {
            NetworkServer.RegisterHandler<ClientConnectedMessage>(OnClientConnectedMessage);
            NetworkServer.RegisterHandler<ClientPlayerMessage>(OnPlayerJoinMessage);          
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            // If local connection            
            if (NetworkServer.localConnection.connectionId == conn.connectionId)
            {
                if (TryCreateClient(NetworkServer.localConnection, out NetworkBehaviour player))
                {
                    _connections.Add(conn.connectionId, (ClientConnectionPlayer)player);
                }
            }
        }

        private bool TryCreateClient(NetworkConnection conn, out NetworkBehaviour player) 
        {
            player = null;

            if (_net.NetworkClientTemplate.gameObject.GetComponent<NetworkBehaviour>() == null) return false;

            player = _net.NetworkClientTemplate.gameObject.Create().GetComponent<NetworkBehaviour>();

            if (NetworkServer.AddPlayerForConnection(conn, player.gameObject)) return true;            

            return false;
        }

        private bool TryCreateNetworkObject(
            NetworkConnection conn, 
            NetworkBehaviour template, 
            out NetworkBehaviour obj, 
            bool clientAuthority = true)
        {
            obj = null;

            if (template.gameObject.GetComponent<NetworkBehaviour>() == null) return false;

            obj = template.gameObject.Create().GetComponent<NetworkBehaviour>();

            NetworkServer.Spawn(obj.gameObject);            

            return true;
        }


        public bool DoTryPlayerJoin(NetworkConnection conn, int localPlayerId)
        {
            var response = new ServerMessage()
            {
                Id = ServerMessageId.ServerId
            };

            if (_playerCount == 4) response.Id = ServerMessageId.Failure;

            List<int> connectionPlayers = null;
            if (_players.TryGetValue(conn.connectionId, out connectionPlayers))
            {
                if (
                    connectionPlayers == null ||
                    connectionPlayers.Contains(localPlayerId))
                {
                    response.Id = ServerMessageId.Failure;
                }
            }
            else
            {
                connectionPlayers = new List<int>();
                _players.Add(conn.connectionId, connectionPlayers);
            }
 
            if (_connections.TryGetValue(conn.connectionId, out ClientConnectionPlayer clientConnection))
            {
                if (response.Id != ServerMessageId.Failure)
                {
                    connectionPlayers.Add(localPlayerId);
                    response.ServerPlayerId = _playerCount++;
                    response.LocalPlayerId = localPlayerId;
                    UI.CharacterSelect.Instance.AssignAuthority(conn, response.ServerPlayerId);
                }

                clientConnection.TargetReceive(response);                
            }                 

            return response.Id != ServerMessageId.Failure;
        }

        public override bool TryPlayerJoin(int localPlayerId)
        {
            // Debug.Log("On network player created");
            return DoTryPlayerJoin(NetworkServer.localConnection, localPlayerId);
        }

        public override bool TryPlayerLeave(int localPlayerId)
        {
            // Debug.Log("On network player created");
            return DoTryPlayerJoin(NetworkServer.localConnection, localPlayerId);
        }   

        public void OnPlayerJoinMessage(NetworkConnection conn, ClientPlayerMessage message)
        {
            DoTryPlayerJoin(conn, message.LocalPlayerId);
        }
   
        public void OnClientConnectedMessage(NetworkConnection conn, ClientConnectedMessage message)
        {         
            if (TryCreateClient(conn, out NetworkBehaviour client))
            {
                _connections.Add(conn.connectionId, (ClientConnectionPlayer)client);
            }
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

        //[SerializeField]
        //private ConnectedPlayer _clientPlayerTemplate;
        //public ConnectedPlayer ClientPlayerTemplate => _clientPlayerTemplate;

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
