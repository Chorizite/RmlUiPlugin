﻿using Chorizite.Core.Backend;
using RmlUi.Lib.RmlUi.Elements;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RmlUi.Lib.RmlUi {
    public class ScriptableDocumentInstancer : ElementInstancer {
        private readonly Dictionary<IntPtr, ScriptableDocumentElement> _elements = [];
        private ILogger _log;
        private readonly IChoriziteBackend _backend;

        public ScriptableDocumentInstancer(IChoriziteBackend backend, ILogger logger) : base("body") {
            _log = logger;
            _backend = backend;
        }

        internal ScriptableDocumentElement? GetDocument(IntPtr ptr) {
            if (_elements.TryGetValue(ptr, out var document)) {
                return document;
            }
            return null;
        }

        internal void Update() {
            var docs = _elements.Values.ToArray();
            foreach (var doc in docs) {
                doc.Update();
            }
        }

        public override IntPtr OnInstanceElement(Element parent, string tag, XMLAttributes attributes) {
            var document = new ScriptableDocumentElement(_backend, _log);
            _elements.Add(document.NativePtr, document);
            return document.NativePtr;
        }

        public override void OnReleaseElement(Element element) {
            if (_elements.TryGetValue(element.NativePtr, out var document)) {
                _elements.Remove(element.NativePtr);
                document.Dispose();
            }
        }

        public override void Dispose() {
            base.Dispose();
        }
    }
}
