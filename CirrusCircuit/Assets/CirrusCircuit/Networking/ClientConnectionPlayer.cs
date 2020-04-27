﻿using Cirrus.Circuit.Controls;
using Cirrus.Circuit.UI;
using Mirror;
using UnityEngine;


namespace Cirrus.Circuit.Networking
{
    // Serves to sync the connection
    public class ClientConnectionPlayer : NetworkBehaviour
    {
        public static ClientConnectionPlayer Instance;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Instance = this;
        }        


        [TargetRpc]
        public void TargetReceiveResponse(ServerResponseMessage response)
        {
            switch (response.Id)
            {
                case ServerMessageId.ServerId:

                    if (response.LocalPlayerId < 0)
                    {
                        Debug.Log("invalid local player id connected");
                        return;
                    }


                    if (response.ServerPlayerId < 0)
                    {
                        Debug.Log("invalid server player id received");
                        return;
                    }

                    Debug.Log("Assigned server id with success: " + response.ServerPlayerId);
                    Game.Instance._localPlayers.Add(LocalPlayerManager.Instance.Players[response.LocalPlayerId]);
                    LocalPlayerManager.Instance.Players[response.LocalPlayerId]._serverId = response.ServerPlayerId;
                    LocalPlayerManager.Instance.Players[response.LocalPlayerId]._characterSlot = CharacterSelect.Instance._slots[response.ServerPlayerId];
                    return;

                default: return;
            }

        }

        [Command]
        public void CmdTryChangeState_CharacterSelectSlot(GameObject obj, CharacterSelectSlot.State target)
        {            
            CharacterSelectSlot slot;
            if ((slot = obj.GetComponent<CharacterSelectSlot>()) != null) slot.RpcTryChangeState(target);          
        }

        public void TryChangeState_Game(Game.State transition, params object[] args)
        {
            CmdTryChangeState_Game(transition, args);
        }

        [Command]
        public void CmdTryChangeState_Game(Game.State transition, object[] args)
        {
            RpcTryChangeState_Game(transition, args);
        }

        [ClientRpc]
        public void RpcTryChangeState_Game(Game.State transition, object[] args)
        {            
            Game.Instance.TryChangeState(transition, args);
        }

        [Command]
        public void CmdScroll_CharacterSelectSlot(GameObject obj, bool scroll)
        {
            CharacterSelectSlot slot;
            if ((slot = obj.GetComponent<CharacterSelectSlot>()) != null) slot.RpcScroll(scroll);
        }
    }
}
