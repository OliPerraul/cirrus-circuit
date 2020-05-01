﻿using Cirrus.Circuit.Controls;
using Cirrus.Circuit.UI;
using Cirrus.MirrorExt;
using Mirror;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using Cirrus.Circuit.World;
using System;
using Cirrus.Circuit.World.Objects;

namespace Cirrus.Circuit.Networking
{
    public class ObjectSession : NetworkBehaviour
    {
        [SerializeField]
        public World.Objects.BaseObject _object;

        [SyncVar]
        [SerializeField]        
        public int _index = -1;

        private Mutex _mutex = new Mutex();

        public int Index {
            get => _index;
            set {
                _index = value;
                CommandClient.Instance.Cmd_ObjectSession_SetIndex(gameObject, _index);
            }
        }

        [ClientRpc]
        public void Rpc_Interact(GameObject sourceObject)
        {
            ObjectSession sourceSession = null;
            if ((sourceSession = sourceObject.GetComponent<ObjectSession>()) != null)
            {
                _mutex.WaitOne();

                _object._Interact(sourceSession._object);

                _mutex.ReleaseMutex();
            }            
        }

        [ClientRpc]
        public void Rpc_TryFall()
        {
            _mutex.WaitOne();

            _object._TryFall();

            _mutex.ReleaseMutex();
        }

        [ClientRpc]
        public void Rpc_TryMove(Vector3Int step)
        {
            _mutex.WaitOne();

            _object._TryMove(step, null);

            _mutex.ReleaseMutex();
        }

        public void TryMove(Vector3Int step)
        {
            CommandClient.Instance.Cmd_ObjectSession_TryMove(gameObject, step);
        }

        public void TryFall()
        {
            CommandClient.Instance.Cmd_ObjectSession_TryFall(gameObject);
        }

        public void Interact(BaseObject source)
        {
            CommandClient.Instance.Cmd_ObjectSession_Interact(gameObject, source._session.gameObject);
        }

        public bool IsMoveAllowed(Vector3Int step)
        {
            return _object.IsMoveAllowed(step);
        }

        public bool IsFallAllowed()
        {
            return _object.IsFallAllowed();
        }
    }
}
