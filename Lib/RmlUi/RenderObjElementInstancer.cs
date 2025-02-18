using Chorizite.Core.Backend;
using Chorizite.Core.Dats;
using RmlUi.Lib.RmlUi.Elements;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RmlUi.Lib.RmlUi {
    internal class RenderObjElementInstancer : ElementInstancer {
        private readonly Dictionary<IntPtr, RenderObjElement> _elements = [];
        private ILogger _log;
        private readonly IChoriziteBackend _backend;

        public RenderObjElementInstancer(IChoriziteBackend backend, ILogger logger) : base("renderobj") {
            _log = logger;
            _backend = backend;
        }
        internal void Update() {
            var renderEls = _elements.Values.ToArray();
            foreach (var renderEl in renderEls) {
                renderEl.Update();
            }
        }

        public override IntPtr OnInstanceElement(Element parent, string tag, XMLAttributes attributes) {
            var document = new RenderObjElement(_backend, _log, attributes);
            _elements.Add(document.NativePtr, document);
            return document.NativePtr;
        }

        public override void OnReleaseElement(Element element) {
            if (_elements.TryGetValue(element.NativePtr, out var renderEl)) {
                _elements.Remove(element.NativePtr);
                renderEl.Dispose();
            }
        }

        public override void Dispose() {
            base.Dispose();
        }
    }
}
