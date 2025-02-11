using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RmlUi.Lib.RmlUi {
    internal class ScriptableEventInstancer : EventInstancer {
        public override void OnInstanceEvent(Element element, RmlUiNet.EventId id, string name, Dictionary<string, object> parameters, bool interruptible) {
            switch (id) {
                case RmlUiNet.EventId.DragDrop:
                case RmlUiNet.EventId.DragOver:
                case RmlUiNet.EventId.DragOut:
                    if (RmlUiPlugin.Instance?.PanelManager._externalDragDropEventData is not null) {
                        foreach (var kv in RmlUiPlugin.Instance.PanelManager._externalDragDropEventData) {
                            if (!parameters.TryAdd(kv.Key, kv.Value)) {
                                parameters[kv.Key] = kv.Value;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
