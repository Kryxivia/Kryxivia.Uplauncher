﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public enum UpdaterState
    {
        Starting,
        Checking,
        Downloading,
        Breaking,
        Playing
    }
}
