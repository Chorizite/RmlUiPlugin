using System;

namespace RmlUi.Lib {
    public class PanelVisibilityChangedEventArgs : EventArgs {
        public Panel Panel { get; init; }
        public bool IsVisible { get; init; }

        public PanelVisibilityChangedEventArgs(Panel panel, bool isVisible) => (Panel, IsVisible) = (panel, isVisible);
    }
}