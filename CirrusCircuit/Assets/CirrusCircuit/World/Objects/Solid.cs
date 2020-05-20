﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cirrus.Circuit.World.Objects
{
    public class Solid : BaseObject
    {
        public override ObjectType Type => ObjectType.Solid;

        public override bool Move(
            BaseObject source, 
            Vector3Int step)
        {
            switch (source.Type)
            {
                default:
                    return false;
            }
        }

        public override void Enter(
            BaseObject source,
            Vector3Int gridDest,
            Vector3Int step)
        //out Vector3 offset,
        //out Vector3Int gridDest,
        //out Vector3Int stepDest,
        //out BaseObject dest)
        {
            //gridDest = source._gridPosition;
            //stepDest = step;
            //offset = Vector3.zero;
            //dest = this;

            return;
        }

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
        }

        public override void Cmd_Fall()
        {
        
        }

        public override void Cmd_FallThrough(Vector3Int step)
        {
            
        }

        public override void Cmd_Interact(BaseObject source)
        {
            
        }

        public override void Cmd_Move(Vector3Int step)
        {
            
        }


        public override void FSM_FixedUpdate()
        {
            
        }

        public override void FSM_Update()
        {

        }


    }
}