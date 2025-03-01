﻿using Microsoft.Extensions.Logging;
using RmlUi.Lib.RmlUi;
using RmlUiNet;
using System;
using System.IO;

namespace RmlUi.Lib {
    /// <summary>
    /// Represents a single screen. Only one screen can be loaded and displayed at a single time.
    /// </summary>
    public class Screen : UIDocument {
        internal Screen(string name, string filename, Context context, ACSystemInterface rmlSystemInterface, ILogger log) : base(name, filename, context, rmlSystemInterface, log) {
            Show();
        }
    }
}
