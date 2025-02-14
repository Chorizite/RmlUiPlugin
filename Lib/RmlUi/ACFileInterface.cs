using Chorizite.ACProtocol.Types;
using Chorizite.Core.Lib;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using SixLabors.ImageSharp.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static RmlUiNet.Native.VariableDefinition;
namespace RmlUi.Lib.RmlUi {
    public class ACFileInterface : FileInterface {
        private readonly ILogger _log;
        private List<FileStream> m_Streams = new List<FileStream>();

        public ACFileInterface(ILogger log) {
            _log = log;
        }

        public override void Close(IntPtr file) {
            if (m_Streams[(int)file - 1] == null) {
                _log.LogError($"ACFileInterface.Close: Invalid FileHandle: {file}");
                return;
            }

            m_Streams[(int)file - 1].Dispose();
            m_Streams[(int)file - 1] = null;
        }

        public override ulong Length(IntPtr file) {
            if (m_Streams[(int)file - 1] == null) {
                _log.LogError($"ACFileInterface.Length: Invalid FileHandle: {file}");
                return 0;
            }

            return (ulong)m_Streams[(int)file - 1].Length;
        }

        public override string LoadFile(string path) {
            path = TransformPath(path);
            if (!File.Exists(path)) {
                _log.LogError($"ACFileInterface.LoadFile: Invalid File: {path}");
                return "";
            }

            return File.ReadAllText(path);
        }

        private string TransformPath(string path) {
            if (path.Contains("@templates")) {
                var templateName = path.Split("@templates").LastOrDefault()?.TrimStart(['\\', '/']);
                if (!string.IsNullOrEmpty(templateName) && RmlUiPlugin.Instance._templates.TryGetValue(templateName, out var templatePath)) {
                    if (File.Exists(templatePath)) {
                        path = templatePath;
                    }
                }
            }
            
            return PathHelpers.TryMakeDevPath(path);
        }

        public override IntPtr Open(string path) {
            path = TransformPath(path);
            if (!File.Exists(path)) {
                _log.LogError($"ACFileInterface.Open: Invalid File: {path}");
                return 0;
            }

            try {
                // check if we have an open position within the list
                var openIndex = m_Streams.FindIndex((e) => e == null);
                if (openIndex != -1) {
                    m_Streams[openIndex] = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return (IntPtr)openIndex + 1;
                }
                else {
                    m_Streams.Add(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
                    return (IntPtr)m_Streams.Count;
                }
            }
            catch (Exception ex) {
                _log.LogError(ex, $"ACFileInterface.Open");
            }

            return 0;
        }

        public override ulong Read(ulong size, IntPtr file, out byte[] buffer) {
            if (m_Streams[(int)file - 1] == null) {
                _log.LogError($"ACFileInterface.Read: Invalid FileHandle: {file}");
                buffer = [];
                return 0;
            }

            buffer = new byte[size];
            var fid = (int)file - 1;
            var position = (int)m_Streams[fid].Position;
            var length = m_Streams[fid].Length;
            var remaining = length - position;
            return (ulong)m_Streams[fid].Read(buffer, 0, (int)size);
        }

        public override bool Seek(IntPtr file, uint offset, int origin) {
            if (m_Streams[(int)file - 1] == null) {
                _log.LogError($"ACFileInterface.Seek: Invalid FileHandle: {file}");
                return false;
            }

            m_Streams[(int)file - 1].Seek(offset, (SeekOrigin)origin);
            return true;
        }

        public override ulong Tell(IntPtr file) {
            if (m_Streams[(int)file - 1] == null) {
                _log.LogError($"ACFileInterface.Tell: Invalid FileHandle: {file}");
                return 0;
            }

            return (ulong)m_Streams[(int)file - 1].Position;
        }
    }
}
