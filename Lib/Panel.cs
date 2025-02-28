using RmlUi.Lib;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RmlUi.Lib.RmlUi;
using Chorizite.Common;

namespace RmlUi.Lib {
    /// <summary>
    /// Represents a panel in the UI. Multiple panels can be loaded and displayed at the same time.
    /// </summary>
    public class Panel : UIDocument {
        private bool _showInBar = false;
        private bool _isDisposed = false;
        private bool _wantsAttention = false;

        /// <summary>
        /// ghost panels let click/mouse events pass through
        /// </summary>
        public bool IsGhost { get; set; }

        /// <summary>
        /// Show in the plugin bar. This is where plugins can be minimized to
        /// </summary>
        public bool ShowInBar {
            get => _showInBar;
            set {
                if (_showInBar == value) return;
                _showInBar = value;
                _OnShowInBarChanged.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Wether this panel currently requires user attention
        /// </summary>
        public bool WantsAttention {
            get => _wantsAttention;
            set {
                if (_wantsAttention == value) return;
                _wantsAttention = value;
                _OnIconChanged.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when ShowInBar changes
        /// </summary>
        public event EventHandler<EventArgs> OnShowInBarChanged {
            add => _OnShowInBarChanged.Subscribe(value);
            remove => _OnShowInBarChanged.Unsubscribe(value);
        }
        private WeakEvent<EventArgs> _OnShowInBarChanged = new();

        internal Panel(string name, string filename, Context context, ACSystemInterface rmlSystemInterface, ILogger log, Action<UIDocument>? init = null) : base(name, filename, context, rmlSystemInterface, log, false, init) {

        }

        internal Panel(string name, string rmlContents, Context context, ACSystemInterface rmlSystemInterface, ILogger log, bool isSource, Action<UIDocument>? init = null) : base(name, rmlContents, context, rmlSystemInterface, log, isSource, init) {

        }

        public void PullToFront() {
            ScriptableDocument?.PullToFront();
        }

        public override void Dispose() {
            if (!_isDisposed) {
                _isDisposed = true;
                RmlUiPlugin.Instance.PanelManager.DestroyPanel(Name);
                base.Dispose();
            }
        }
    }
}
