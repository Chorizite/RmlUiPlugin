using RmlUi.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RmlUi {
    [JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(UIState))]
    internal partial class SourceGenerationContext : JsonSerializerContext {
    }
}
