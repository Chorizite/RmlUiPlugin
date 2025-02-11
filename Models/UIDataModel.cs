using RmlUi;
using Autofac;
using RmlUi.Lib.Serialization;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace RmlUi.Models {
    public abstract class UIDataModel : IDisposable {
        [JsonIgnore]
        protected DataModelConstructor _modelConstructor;

        public string? Name { get; private set; }

        private event PropertyChangedEventHandler? PropertyChanged;

        protected void InvokePropertyChange([System.Runtime.CompilerServices.CallerMemberName] string memberName = "") {
            if (!string.IsNullOrEmpty(memberName)) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
            }
        }

        public UIDataModel() {

        }

        public virtual void Init(string name) {
            if (RmlUiPlugin.RmlContext is null) {
                return;
            }

            Name = name;
            _modelConstructor = RmlUiPlugin.RmlContext.CreateDataModel(name);
            BuildBindings();
            //PropertyChanged += HandlePropertyChanged;
        }

        protected void BindAction(string name, Action<Event, IEnumerable<Variant>> action) {
            _modelConstructor.BindEventCallback(name, (evt, variants) => {
                try {
                    action(evt, variants);
                }
                catch (Exception ex) {
                    RmlUiPlugin.Log?.LogError(ex, $"Error in data model action ({Name}.{name}):");
                }
            });
        }

        internal IEnumerable<PropertyInfo> GetPropsToBind() {
            return GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsAssignableTo(typeof(DataVariable)));
        }

        private void BuildBindings() {
            var propsToBind = GetPropsToBind();
            foreach (var p in propsToBind) {
                if (p.GetValue(this) is DataVariable dv) {
                    _modelConstructor.BindVariable(p.Name, dv.NativePtr);
                }
                if (p.GetValue(this) is INotifyPropertyChanged notifyPropertyChanged) {
                    notifyPropertyChanged.PropertyChanged += HandlePropertyChanged;
                }
            }
        }

        protected void TriggerPropertyChange(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e) {
            if (!string.IsNullOrEmpty(e.PropertyName)) {
                _modelConstructor.Handle.DirtyVariable(e.PropertyName);
            }
            else {
                _modelConstructor.Handle.DirtyAllVariables();
            }
        }

        public virtual void Dispose() {
            if (!string.IsNullOrEmpty(Name)) {
                RmlUiPlugin.RmlContext?.RemoveDataModel(Name);
            }
            _modelConstructor?.Dispose();
        }
    }
}