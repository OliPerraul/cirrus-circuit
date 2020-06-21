﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Cirrus.Circuit.World.Objects
{
    class ConveyerBelt : BaseObject
    {

        // TODO
        //public override bool IsNetworked => false;

        public const float VisitorAngle = 20;

        public override ObjectType Type => ObjectType.ConveyerBelt;
        public bool _isStaircase = false;
        public bool IsStaircase => _isStaircase;

        public override ReturnType GetMoveResults(
            Move move, 
            out IEnumerable<MoveResult> result, 
            bool recursive=false)
            //bool lockResults = true)
        {
            result = null;
            return ReturnType.Failed;
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


                ////Same direction(Look up)
                if (exitResult.Step.SetY(0) == _direction)
                {
                    exitResult.Step = move.Step + Vector3Int.up;
                }

                return true;
            }

            return false;
        }

        // TODO
        public override ReturnType GetEnterResults(
            Move move,
            out EnterResult enterResult,
            out IEnumerable<MoveResult> moveResults
            )
        {
            if (base.GetEnterResults(
                move,
                out enterResult,
                out moveResults
                ) > 0)
            {
                var dir = move.Step.SetY(0);

                // Moving up or moving down
                if ((move.Step.y >= 0 &&
                    dir == _direction) ||
                    (move.Step.y != 0 &&
                    dir == -_direction))
                {
                    enterResult.PitchAngle = dir == _direction ? VisitorAngle : -VisitorAngle;
                    enterResult.Offset = Vector3.up * Level.CellSize / 2;
                    return ReturnType.Succeeded_Next;
                }
            }

            return ReturnType.Failed;
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
        }

        public override void FSM_FixedUpdate()
        {

        }

        public override void FSM_Update()
        {

        }

        public override void EnterVisitor(BaseObject source)
        {
            //source.RampIdle();
        }
    }
}
