﻿using Chorizite.Common;
using Chorizite.Core.Lib;
using RmlUi.Lib.RmlUi;
using RmlUi.Lib.RmlUi.Elements;
using RmlUi.Lib.RmlUi.VDom;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace RmlUi.Lib {
    public abstract class UIDocument : IDisposable {
        private readonly ILogger _log;
        private readonly string _docFile;
        private readonly FileWatcher? _fileWatcher;
        private ElementDocument? _doc;
        private DateTime _requestedReloadTime = DateTime.MinValue;
        private string? _iconUri;

        internal Context Context { get; }

        private ACSystemInterface _rmlSystemInterface;
        private Action<UIDocument>? _init;

        internal IntPtr NativePtr { get; private set; }

        private ScriptableDocumentElement? _scriptableDoc;
        internal bool NeedsReload;

        /// <summary>
        /// Whether or not the ui document should reload on file changes
        /// </summary>
        public bool LiveReload { get; set; } = true;
        public bool IsSource { get; }

        /// <summary>
        /// Whether or not the ui document should minimize to the tray
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// The filename of the ui document
        /// </summary>
        public string File => _docFile;

        /// <summary>
        /// The friendly name of this ui document.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The rendered x position of the screen
        /// </summary>
        public int X => _doc is null ? 0 : (int)_doc.GetAbsoluteLeft();

        /// <summary>
        /// The rendered y position of the screen
        /// </summary>
        public int Y => _doc is null ? 0 : (int)_doc.GetAbsoluteTop();

        /// <summary>
        /// The rendered width of the screen
        /// </summary>
        public int Width => _doc is null ? 0 : (int)_doc.GetClientWidth();

        /// <summary>
        /// The rendered height of the screen
        /// </summary>
        public int Height => _doc is null ? 0 : (int)_doc.GetClientHeight();

        public ScriptableDocumentElement? ScriptableDocument => RmlUiPlugin.Instance.ScriptableDocumentInstancer.GetDocument(NativePtr);

        /// <summary>
        /// Whether or not the document is visible
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// The uri of the icon for this document, if any
        /// </summary>
        public string? IconUri {
            get => _iconUri;
            set {
                if (value != _iconUri) {
                    _iconUri = value;
                    _OnIconChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Called before the document is rendered
        /// </summary>
        public event EventHandler<EventArgs> OnBeforeRender {
            add => _OnBeforeRender.Subscribe(value);
            remove => _OnBeforeRender.Unsubscribe(value);
        }
        protected WeakEvent<EventArgs> _OnBeforeRender = new();


        /// <summary>
        /// Called after the document is reloaded
        /// </summary>
        public event EventHandler<EventArgs> OnAfterReload {
            add => _onAfterReload.Subscribe(value);
            remove => _onAfterReload.Unsubscribe(value);
        }
        protected WeakEvent<EventArgs> _onAfterReload = new();

        /// <summary>
        /// Called when the document is hidden
        /// </summary>
        public event EventHandler<EventArgs> OnHide {
            add => _OnHide.Subscribe(value);
            remove => _OnHide.Unsubscribe(value);
        }
        protected WeakEvent<EventArgs> _OnHide = new();

        /// <summary>
        /// Called when the document is shown
        /// </summary>
        public event EventHandler<EventArgs> OnShow {
            add => _OnShow.Subscribe(value);
            remove => _OnShow.Unsubscribe(value);
        }
        protected WeakEvent<EventArgs> _OnShow = new();

        /// <summary>
        /// Called when the icon of the document changes
        /// </summary>
        public event EventHandler<EventArgs> OnIconChanged {
            add => _OnIconChanged.Subscribe(value);
            remove => _OnIconChanged.Unsubscribe(value);
        }
        protected WeakEvent<EventArgs> _OnIconChanged = new();

        internal UIDocument(string name, string filename, Context context, ACSystemInterface rmlSystemInterface, ILogger log, bool isSource = false, Action<UIDocument>? init = null) {
            Name = name;
            _log = log;
            IsSource = isSource;
            _docFile = IsSource ? filename : PathHelpers.TryMakeDevPath(filename);
            Context = context;
            _rmlSystemInterface = rmlSystemInterface;
            _init = init;

            LoadDoc();

            if (!IsSource && LiveReload) {
                _fileWatcher = new FileWatcher(Path.GetDirectoryName(_docFile), Path.GetFileName(_docFile), (file) => {
                    NeedsReload = true;
                });
            }

            OnShow += OnShowInternal;
            OnHide += OnHideInternal;
        }

        private void OnHideInternal(object? sender, EventArgs e) {
            if (_scriptableDoc?.OwnerDocument is not null) {
                _scriptableDoc?.OwnerDocument.AddClass("hidden");
                _scriptableDoc?.OwnerDocument.RemoveClass("visible");
                _log.LogDebug($"Hiding document {Name} {_docFile}");
            }
        }

        private void OnShowInternal(object? sender, EventArgs e) {
            if (_scriptableDoc?.OwnerDocument is not null) {
                _scriptableDoc?.OwnerDocument.RemoveClass("hidden");
                _scriptableDoc?.OwnerDocument.AddClass("visible");
                _log.LogDebug($"Showing document {Name} {_docFile}");
            }
        }

        public Element? GetElementById(string id) => ScriptableDocument?.GetElementById(id);
        public Element? QuerySelector(string query) => ScriptableDocument?.QuerySelector(query);

        public void Mount(Func<Func<VirtualNode>> virtualNode, string selector) {
            _scriptableDoc?.Mount(virtualNode, selector);
        }

        public void Unmount(string selector) => _scriptableDoc?.Unmount(selector);

        public MyObservable? Observable(string name = "[anonymous]", MyObservable parent = null) {
            return _scriptableDoc?.Observable(name, parent);
        }

        /// <summary>
        /// Hide the document
        /// </summary>
        public void Hide() {
            _doc?.Hide();
            IsVisible = false;
            _OnHide?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Show the document
        /// </summary>
        public void Show() {
            _doc?.Show();
            IsVisible = true;
            _OnShow?.Invoke(this, EventArgs.Empty);
        }

        public string GetTitle() => ScriptableDocument?.GetTitle() ?? "";

        private void LoadDoc() {
            if (_doc is not null) {
                UnloadDoc();
            }
            _log?.LogTrace($"Loading document {Name} {_docFile}");

            if (IsSource) {
                _doc = Context.LoadDocumentFromMemory(_docFile, $"server://TODO_SERVERNAME/{Name}.rml");
            }
            else {
                if (!System.IO.File.Exists(_docFile)) {
                    throw new Exception($"Unable to find document {_docFile}");
                }

                _doc = Context.LoadDocumentFromMemory(TransformRml(_docFile), _docFile.Replace("\\", "//"));
            }
            if (_doc is null) {
                throw new Exception($"Unable to create RmlUi document {Name} {_docFile}");
            }

            if (_rmlSystemInterface.HasNewFontsLoaded) {
                _rmlSystemInterface.HasNewFontsLoaded = false;
                _log?.LogDebug($"New fonts were loaded, reloading document {Name} {_docFile}");
                _doc?.Dispose();
                LoadDoc();
                return;
            }

            NativePtr = _doc.NativePtr;
            _scriptableDoc = RmlUiPlugin.Instance.ScriptableDocumentInstancer.GetDocument(NativePtr);
            
            _scriptableDoc.Panel = this;
            if (_init is not null) {
                _init(this);
            }
            _scriptableDoc.HandleLoad();

            //_scriptableDoc.QuerySelector("handle")?.AddEventListener("handledrag", (ev) => {
            //    _log?.LogDebug($"Drag event fired on document {Name}");
            //});

            if (IsVisible) {
                Show();
            }
        }


        private string TransformRml(string docFile) {
            var html = System.IO.File.ReadAllText(docFile);

            var metaRe = new Regex(@"<meta[^>]*name=""(?<name>[^""]*)""[^>]*content=""(?<content>[^""]*)""[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in metaRe.Matches(html)) {
                var name = m.Groups["name"].Value;
                var content = m.Groups["content"].Value;
                if (name.ToLower() == "icon") {
                    if (content.StartsWith("dat://")) {
                        IconUri = content;
                    }
                    else {
                        var iconPath = Path.Combine(Path.GetDirectoryName(docFile), content);
                        if (System.IO.File.Exists(iconPath)) {
                            IconUri = Path.GetFullPath(iconPath);
                        }
                    }
                }
            }

            return html;
        }

        private void UnloadDoc() {
            _log?.LogTrace($"Unloading document {Name} {_docFile}");
            _doc?.Close();
            _doc?.Dispose();
            _doc = null;
            NativePtr = IntPtr.Zero;
        }

        internal unsafe void Update() {
            if (NeedsReload) {
                NeedsReload = false;
                _log.LogDebug($"Reloading document {Name} {_docFile}");
                LoadDoc();
                Context.Update();
                _onAfterReload.Invoke(this, EventArgs.Empty);
            }

            _OnBeforeRender.Invoke(this, EventArgs.Empty);
        }

        internal void HandleGraphicsPreReset() {
            UnloadDoc();
        }

        internal void HandleGraphicsPostReset() {
            LoadDoc();
        }

        public virtual void Dispose() {
            OnShow -= OnShowInternal;
            OnHide -= OnHideInternal;
            _fileWatcher?.Dispose();
            UnloadDoc();
        }
    }
}
