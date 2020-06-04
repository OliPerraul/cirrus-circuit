﻿using UnityEngine;
using System.Collections;
using Cirrus.Circuit.Controls;
using Cirrus.Circuit.UI;
using Mirror;
//using UnityEngine;
using Cirrus.MirrorExt;
using Cirrus;
//using Cirrus.Events;
using System;
using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit
{
    public class RoundSession : CustomNetworkBehaviour
    {
        public Delegate<int> OnIntermissionHandler;

        public Delegate<int> OnCountdownHandler;

        public Delegate<int> OnRoundStartHandler;

        public Delegate OnRoundEndHandler;

        [SyncVar]
        [SerializeField]
        private GameObject _timerGameObject;
        private ServerTimer _timer;
        public ServerTimer Timer
        {
            get {
                if (_timerGameObject == null) return null;
                else if (_timer == null) _timer = _timerGameObject.GetComponent<ServerTimer>();
                return _timer;
            }
        }

        [SerializeField]
        private Timer _countDownTimer;


        [SerializeField]
        private Timer _startIntermissionTimer;

        [SerializeField]
        private Timer _endIntermissionTimer;

        public float RemainingTime => Timer == null ? 0 : _remainingTime - Timer.Time;

        [SyncVar]
        [SerializeField]        
        private float _countDownTime = 1f;

        [SyncVar]
        [SerializeField]        
        private float _startIntermissionTime = 1; // Where we show the round number

        [SyncVar]
        [SerializeField]
        private float _endIntermissionTime = 1; // Where we show "Time's up!"

        [SyncVar]
        [SerializeField]
        private int _countDown = 3;

        [SyncVar]
        [SerializeField]
        private float _remainingTime;

        [SyncVar]
        [SerializeField]
        private int _index = 0;

        public int Index => _index;

        private static RoundSession _instance;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _countDownTimer.OnTimeLimitHandler += Cmd_OnCountDownTimeout;
            _startIntermissionTimer.OnTimeLimitHandler += Cmd_OnStartIntermissionTimeout;

            //_endIntermissionTimer.OnTimeLimitHandler += Cmd_OnEndIntermissionTimeout;
            _endIntermissionTimer.OnTimeLimitHandler += Cmd_OnRoundEnd;

            foreach (var player in GameSession.Instance.Players)
            {
                player.Score = 0;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }
        public override void Destroy()
        {
            base.Destroy();
            _countDownTimer.OnTimeLimitHandler -= Cmd_OnCountDownTimeout;
            _startIntermissionTimer.OnTimeLimitHandler -= Cmd_OnStartIntermissionTimeout;
            _endIntermissionTimer.OnTimeLimitHandler -= Cmd_OnRoundEnd;



            //OnIntermissionHandler -=

            //OnCountdownHandler;

            //OnRoundStartHandler;

            //OnRoundEndHandler;

            _instance = null;
        }

        public static RoundSession Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<RoundSession>();
                return _instance;
            }
        }

        public static RoundSession Create(
            int countDown, 
            float time, 
            float countDownTime, 
            float intermissionTime, 
            int index)
        {
            RoundSession session = NetworkingLibrary.Instance.RoundSession.Create(null);

            session._startIntermissionTime = intermissionTime;
            session._index = index;
            session._countDown = countDown;
            session._remainingTime = time;
            session._countDownTime = countDownTime;
            session._countDownTimer = new Timer(
                countDownTime,
                start: false,
                repeat: true);

            if (CustomNetworkManager.IsServer)
            {
                session._timerGameObject = ServerTimer.Create(
                    session._remainingTime,
                    start: false).gameObject;
                session.Timer.OnRoundTimeLimitHandler += session.Cmd_OnRoundTimeout;
            }

            session._startIntermissionTimer = new Timer(
                session._startIntermissionTime,
                start: false,
                repeat: false);

            NetworkServer.Spawn(
                session.gameObject, 
                NetworkServer.localConnection);

            return session;
        }


        #region On Start Intermission

        public void StartIntermisison()
        {
            OnIntermissionHandler?.Invoke(_index);
            if (CustomNetworkManager.IsServer) _startIntermissionTimer.Start();
        }


        public void Cmd_OnStartIntermissionTimeout()
        {
            Rpc_OnStartIntermissionTimeout();
        }


        [ClientRpc]
        public void Rpc_OnStartIntermissionTimeout()
        {
            OnStartIntermissionTimeout();
        }


        public void OnStartIntermissionTimeout()
        {
            OnCountdownHandler?.Invoke(_countDown);
            if(CustomNetworkManager.IsServer) _countDownTimer.Start();
        }


        #endregion

        #region On Round Timeout

        private void Cmd_OnRoundTimeout()
        {
            Rpc_OnRoundTimeout();
        }

        [ClientRpc]
        public void Rpc_OnRoundTimeout()
        {
            OnRoundTimeout();
        }

        public void OnRoundTimeout()
        {
            if (CustomNetworkManager.IsServer)
            {
                NetworkServer.Destroy(_timerGameObject);
                Destroy(_timerGameObject);
                _timerGameObject = null;
                _timer = null;
            }

            Announcement.Instance.Message = "Time's up!";
            if (CustomNetworkManager.IsServer) _endIntermissionTimer.Start(_endIntermissionTime);
        }

        #endregion

        #region On Countdown Timeout

        private void Cmd_OnCountDownTimeout()
        {
            Rpc_OnCountDownTimeout();
        }

        [ClientRpc]
        public void Rpc_OnCountDownTimeout()
        {
            OnCountdownTimeOut();
        }

        private void OnCountdownTimeOut()
        {
            _countDown--;

            if (_countDown < -1)
            {
                OnCountdownHandler?.Invoke(_countDown);

                if(CustomNetworkManager.IsServer) _countDownTimer.Stop();
            }
            else if (_countDown < 0)
            {
                OnCountdownHandler?.Invoke(_countDown);
                OnRoundStartHandler?.Invoke(_index);

                if (CustomNetworkManager.IsServer)
                {
                    Timer.DoStart();
                }

                return;
            }
            else OnCountdownHandler?.Invoke(_countDown);
        }

        #endregion

        #region On Round End

        public void Cmd_OnRoundEnd()
        {
            Rpc_OnRoundEnd();
        }

        [ClientRpc]
        public void Rpc_OnRoundEnd()
        {
            OnRoundEnd();
        }

        public void OnRoundEnd()
        {
            _countDownTimer.Stop();
            _startIntermissionTimer.Stop();

            OnRoundEndHandler?.Invoke();
            Game.Instance.OnRoundEnd();
        }

        #endregion

    }
}
