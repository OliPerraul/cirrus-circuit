﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Cirrus.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using Mirror;
using System;
using System.Net.Sockets;


namespace Cirrus.Circuit.Networking
{
    public class PlayerJoinMessage : MessageBase
    {        
        public string Name;

        public int Id;
    }

    public class ClientConnectedMessage : MessageBase
    {
        public string Name;
    }
}
