﻿using System;
using Cirrus.Circuit.Controls;
using Cirrus.Circuit.UI;
using Mirror;
using UnityEngine;
using Cirrus.Circuit.World.Objects;
using System.Linq;
using System.Threading;

namespace Cirrus.Circuit.Networking
{
    // Serves to sync the connection
    public class CommandClient : NetworkBehaviour
    {
        public Events.Event OnUpdateHandler;

        public Events.Event OnFixedUpdateHandler;

        public static void AssertGameObjectNull(GameObject gameObject) => Utils.DebugUtils.Assert(gameObject != null, "Cmd GameObject is null. Was the object spawn?");

        public static CommandClient _instance;        

        public static CommandClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    foreach (var player in FindObjectsOfType<CommandClient>())
                    {
                        if (player.hasAuthority)
                        {
                            _instance = player;
                            break;
                        }
                    }
                }

                return _instance;
            }
        }

        public virtual void Update()
        {
            if(OnUpdateHandler != null) Cmd_Update();
        }

        public virtual void FixedUpdate()
        {
            if (OnFixedUpdateHandler != null) Cmd_FixedUpdate();
        }


        [Command]
        public void Cmd_Update()
        {
            OnUpdateHandler.Invoke();
        }

        [Command]
        public void Cmd_FixedUpdate()
        {
            OnFixedUpdateHandler.Invoke();
        }


        [TargetRpc]
        public void TargetReceiveResponse(ServerResponseMessage response)
        {
            switch (response.Id)
            {
                case ServerMessageId.ServerId:
                    return;

                default: return;
            }

        }

        #region Character Select Slot

        [Command]
        public void Cmd_CharacterSelectSlot_SetState(GameObject obj, CharacterSelectSlot.State target)
        {
            if (obj == null) return;
            
            //Debug.Log("RPC SELECT OUTER CMD");
            if (obj.TryGetComponent(out CharacterSelectSlot slot))
            {
                //Debug.Log("RPC SELECT INNER CMD");
                slot.Rpc_TrySetState(target);
            }
        }

        [Command]
        public void Cmd_CharacterSelectSlot_Scroll(GameObject obj, bool scroll)
        {
            if (obj == null) return;
            
            if (obj.TryGetComponent(out CharacterSelectSlot slot)) slot.Rpc_Scroll(scroll);
        }


        #endregion

        #region Player Session

        [Command]
        public void Cmd_PlayerSession_SetCharacterId(GameObject obj, int characterId)
        {
            if (obj == null) return;
            
            if (obj.TryGetComponent(out PlayerSession session)) session._characterId = characterId;
        }

        [Command]
        public void Cmd_PlayerSession_SetScore(GameObject obj, float score)
        {
            if (obj == null) return;
            
            if (obj.TryGetComponent(out PlayerSession session)) session._score = score;
        }

        #endregion

        #region Level Session

        // TODO security check valid spawn location
        [Command]
        public void Cmd_LevelSession_Spawn(GameObject obj, int spawnId, Vector3Int pos)
        {
            if (obj == null) return;

            if (obj.TryGetComponent(out LevelSession session))
            {
                if (MirrorExt.ServerUtils.TryCreateNetworkObject(
                    NetworkingLibrary.Instance.ObjectSession.gameObject, 
                    out GameObject gobj,
                    true))
                {                    
                    session._objectSessions.Add(gobj);
                    session.Rpc_Spawn(gobj, spawnId, pos);                    
                }
            }
        }


        [Command]
        public void Cmd_LevelSession_OnRainTimeout(GameObject obj, Vector3Int pos, int objectId)
        {
            if (obj == null) return;
           
            if (obj.TryGetComponent(out LevelSession session)) session.Rpc_OnRainTimeout(pos, objectId);
        }

        [Command]
        public void Cmd_LevelSession_SetRequiredGemCount(GameObject obj, int count)
        {
            if (obj == null) return;
            
            if (obj.TryGetComponent(out LevelSession session)) session._requiredGemCount = count;
        }

        [Command]
        public void Cmd_LevelSession_SetRequiredGems(GameObject obj, int count)
        {
            if (obj == null) return;
            
            if (obj.TryGetComponent(out LevelSession session)) session._requiredGems = count;
        }




        [Command]
        public void Cmd_LevelSession_SetObjectId(GameObject obj, int idx, BaseObject.ObjectId id)
        {
            //LevelSession session;
            //if ((session = obj.GetComponent<LevelSession>()) != null)
            //{                
            //    session._objectIds[idx] = id;
            //}
        }

        [Command]
        public void Cmd_LevelSession_InitObjectIds(GameObject obj)
        {
            //LevelSession session;
            //if ((session = obj.GetComponent<LevelSession>()) != null)
            //{
            //    Debug.Log("Init Object Ids");
            //    session._objectIds.(new BaseObject.ObjectId[GameSession.Instance.SelectedLevel.Size]);                    
            //}
        }

        #endregion
    
        #region Game Session

        [Command]
        public void Cmd_GameSession_SetPlayerCount(GameObject obj, int count)
        {
            if (obj == null) return;

            GameSession session;
            if ((session = obj.GetComponent<GameSession>()) != null) session._playerCount = count;
        }

        // TODO Character Select session
        [Command]
        public void Cmd_GameSession_SetCharacterSelectReadyCount(GameObject obj, int count)
        {
            if (obj == null) return;

            GameSession session;
            if ((session = obj.GetComponent<GameSession>()) != null) session._characterSelectReadyCount = count;
        }

        [Command]
        public void Cmd_GameSession_SetCharacterSelectOpenCount(GameObject obj, int count)
        {
            GameSession session;
            if ((session = obj.GetComponent<GameSession>()) != null) session._characterSelectOpenCount = count;
        }


        [Command]
        public void Cmd_GameSession_SetSelectedLevelIndex(GameObject obj, int index)
        {            
            GameSession session;
            if ((session = obj.GetComponent<GameSession>()) != null)
            {
                //Debug.Log("CMD Set Selected Level Index");
                session._selectedLevelIndex = index;
                Rpc_Game_SelectLevel(index);
            }
        }

        //[Command]
        //public void Cmd_GameSession_TrySetState_2(
        //    GameObject obj,
        //    GameSession.State transition)
        //{
        //    GameSession session;
        //    if ((session = obj.GetComponent<GameSession>()) != null) session.Rpc_TrySetState_2(transition);
        //}
        //


        #endregion

        #region Game


        [Command]
        public void Cmd_Game_ScrollLevel(int delta)
        {
            Rpc_Game_ScrollLevel(delta);
        }


        [Command]
        public void Cmd_Game_SetState(
            Game.State transition,
            bool transitionEffect)
        {
            Rpc_Game_SetState(transition, transitionEffect);
        }

        [ClientRpc]
        public void Rpc_Game_SetState(Game.State transition, bool transitionEffect)
        {
            Game.Instance._SetState(transition, transitionEffect);
        }

        [ClientRpc]
        public void Rpc_Game_SelectLevel(int index)
        {
            Debug.Log("RPC SELECT LEVEL: " + index);
            Game.Instance._SelectLevel(index);
        }

        [ClientRpc]
        public void Rpc_Game_ScrollLevel(int step)
        {
            Game.Instance._ScrollLevel(step);
        }

        #endregion

        #region Object Session

        private Mutex Cmd_ObjectSession_Interact_mutex = new Mutex();

        [Command]
        public void Cmd_ObjectSession_Interact(GameObject obj, GameObject sourceObj)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;

            ObjectSession session;
            if ((session = obj.GetComponent<ObjectSession>()) != null)
            {
                Cmd_ObjectSession_Interact_mutex.WaitOne();

                // Server holds the truth
                //if (session.IsFallAllowed())
                {
                    session.Rpc_Interact(sourceObj);
                }

                Cmd_ObjectSession_Interact_mutex.ReleaseMutex();
            }
        }    

        private Mutex Cmd_ObjectSession_TryFall_mutex = new Mutex();

        [Command]
        public void Cmd_ObjectSession_TryFall(GameObject obj)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;

            if (obj.TryGetComponent(out ObjectSession session))
            {
                Cmd_ObjectSession_TryFall_mutex.WaitOne();

                // Server holds the truth
                if (session.IsFallAllowed())
                {
                    session.Rpc_TryFall();
                }

                Cmd_ObjectSession_TryFall_mutex.ReleaseMutex();
            }
        }


        private Mutex Cmd_ObjectSession_TryMove_mutex = new Mutex();

        [Command]
        public void Cmd_ObjectSession_TryMove(GameObject obj, Vector3Int step)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;

            ObjectSession session;
            if ((session = obj.GetComponent<ObjectSession>()) != null)
            {
                Cmd_ObjectSession_TryMove_mutex.WaitOne();

                // Server holds the truth
                if (session.IsMoveAllowed(step))
                {
                    session.Rpc_TryMove(step);
                }

                Cmd_ObjectSession_TryMove_mutex.ReleaseMutex();
            }
        }

        [Command]
        public void Cmd_ObjectSession_SetIndex(GameObject obj, int idx)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;

            ObjectSession session;
            if ((session = obj.GetComponent<ObjectSession>()) != null)
            {
                session._index = idx;
            }
        }

        #endregion


        #region Round Session

        [Command]
        public void Cmd_OnIntermissionTimeoutBeginCountdown(GameObject obj)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;
            
            if(obj.TryGetComponent(out RoundSession session))
            {
                session.Rpc_OnIntermissionTimeoutBeginCountdown();
            }
        }

        [Command]
        public void Cmd_RoundSession_OnTimeout(GameObject obj)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;
            
            if (obj.TryGetComponent(out RoundSession session))
            {
                session.Rpc_OnTimeout();
            }
        }


        [Command]
        public void Cmd_RoundSession_OnRoundEnd(GameObject obj)
        {
            //AssertGameObjectNull(obj);
            if (obj == null) return;
            
            if (obj.TryGetComponent(out RoundSession session))
            {
                session.Rpc_OnRoundEnd();
            }
        }

        #endregion

    }
}
