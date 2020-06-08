﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cirrus.Circuit.Networking;

namespace Cirrus.Circuit.World.Objects
{
    // Goop, Sink Liquid

    public class Quicksand : BaseObject
    {
        [Header("----------------------------", order = 0)]
        [Header("Quicksand",order=1)]
        [Header("----------------------------", order = 2)]
        [SerializeField]
        private float _sinkTime = 2f;

        [SerializeField]
        private bool _isStruggleAllowed = true;

        [SerializeField]
        private int _numStruggleRequired = 3;

        private int _struggleCount = 0;

        [SerializeField]
        public float _struggleTimeLimit = 0.5f;

        public override ObjectType Type => ObjectType.Quicksand;

        public const float BaseOffset = .4f * Level.CellSize;
       
        public override bool IsSolid => false;

        private Timer _timer;
        private Timer _struggleTimer;        

        public bool _doesDestroyObjects = false;

        // struggling out

        public override ReturnType GetMoveResults(
            Move move,
            out IEnumerable<MoveResult> result,
            bool isRecursiveCall = false,
            bool lockResults = true)
        {            
            result = null;
            return ReturnType.Failed;
        }

        public override void Awake()
        {
            base.Awake();

            _timer = new Timer(_sinkTime, start: false, repeat: false);

            _struggleTimer = new Timer(_sinkTime, start: false, repeat: false);            

            if (CustomNetworkManager.IsServer) _timer.OnTimeLimitHandler += OnTimeout;
        }

        public override bool GetExitResult(
            Move move,
            out ExitResult exitResult,
            out IEnumerable<MoveResult> moveResults)
        {
            moveResults = new MoveResult[0];
            if (base.GetExitResult(
                move,
                out exitResult,
                out moveResults
                ))
            {
                exitResult.Offset = Vector3.zero;

                if (
                    _struggleTimer.IsActive &&
                    _struggleCount + 1 == _numStruggleRequired)
                {
                    exitResult.Step = move.Step + Vector3Int.up;
                    exitResult.MoveType = MoveType.Moving;
                    exitResult.Offset = Vector3.zero;
                }
                else
                {
                    exitResult.Step = Vector3Int.zero;
                    exitResult.MoveType = MoveType.Struggle;
                    exitResult.Entered = this;
                    exitResult.Destination = _levelPosition;
                    exitResult.Moved = null;                    
                }


                return true;
            }

            return false;
        }

        public override ReturnType GetEnterResults(
            Move move,
            out EnterResult enterResult,
            out IEnumerable<MoveResult> moveResults
            )
        {            
            if (_visitor != null &&
                !_struggleTimer.IsActive)
            {
                moveResults = new MoveResult[0];
                enterResult = new EnterResult
                {
                    Destination = _levelPosition + Vector3Int.up,
                    Moved = null,
                    Entered = null,
                    PitchAngle = 0f,
                    MoveType = MoveType.Moving,
                    Offset = Vector3.zero,
                    Position = move.Position,
                    Scale = 1,
                    Step = move.Step.SetY(0)
                };

                return ReturnType.Succeeded_Result_Move;
            }
            else return base.GetEnterResults(
                move,
                out enterResult,
                out moveResults
                );
            
        }

        public override void ReenterVisitor()
        {
            base.ReenterVisitor();            

            _struggleCount++;            
        }



        public void OnTimeout()
        {
            if (_visitor == null) return;

            // TODO
            _visitor.Server_Move(new Move
            {
                Destination = LevelSession.Instance.GetFallPosition(true),
                Type = MoveType.Teleport,
                Source = null,
                Position = _levelPosition,
                Entered = _entered,
                User = _visitor
            });        
        }


        public void OnStruggleTimeout()
        {
            // TODO

        }

        public override void EnterVisitor(BaseObject visitor)
        {
            base.EnterVisitor(visitor);
            _struggleCount = 0;
            _timer.Start(_sinkTime);
            _struggleTimer.Start(_struggleTimeLimit);
        }                

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_visitor != null)
            {
                _visitor._offset = 
                    BaseOffset * Vector3.up -
                    (_timer.Time / _sinkTime) * (Level.CellSize) * Vector3.up;
            }
        }


    }

}
