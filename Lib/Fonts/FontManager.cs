using Autofac.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RmlUi.Lib.Fonts {
    public class FontManager : IDisposable {
        private readonly ILogger? _log;
        private Dictionary<string, string> _availableFonts = new Dictionary<string, string>();

        public IEnumerable<string> AvailableFonts => _availableFonts.Keys;

        internal FontManager(ILogger? log) {
            _log = log;

            LoadFonts();
        }

        private void LoadFonts() {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _availableFonts = GetFontFileInfoInReg();
            //foreach (var r in res) {
            //    _log.LogWarning($"Font found in registry: {r.Key} {r.Value}");
            //}
            sw.Stop();
            _log.LogWarning($"Loaded {_availableFonts.Count} fonts in {sw.ElapsedMilliseconds} ms");

        }
        private Dictionary<string, string> GetFontFileInfoInReg() {
            Dictionary<string, string> result = new Dictionary<string, string>();

            try {
                RegistryKey localMachineKey = Registry.LocalMachine;
                RegistryKey localMachineKeySub = localMachineKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", false);

                string[] mynames = localMachineKeySub.GetValueNames();

                foreach (string name in mynames) {
                    string myvalue = localMachineKeySub.GetValue(name).ToString();

                    if (myvalue.Substring(myvalue.Length - 4).ToUpper() == ".TTF" && myvalue.Substring(1, 2).ToUpper() != @":") {
                        string val = name.Substring(0, name.Length - 11);
                        result[val] = myvalue;
                    }
                }
                localMachineKeySub.Close();
            }
            catch (Exception ex) {
                _log.LogWarning($"Failed to get fonts from registry: {ex.Message}");
            }
            return result;
        }

        public bool TryGetFontFile(string fontName, string fontStyle, [MaybeNullWhen(false)] out string fontFile) {
            fontName = fontName.ToLowerInvariant();
            fontStyle = fontStyle.ToLowerInvariant();

            fontFile = _availableFonts.FirstOrDefault(f => {
                if (fontStyle == "regular" && f.Key.ToLowerInvariant() == fontName) return true;
                if (f.Key.ToLowerInvariant().Contains(fontName) && f.Key.ToLowerInvariant().Contains(fontStyle)) return true;
                return false;
            }).Value;

            if (fontFile != null) {
                string folderFullName = System.Environment.GetEnvironmentVariable("windir") + "\\fonts\\";
                fontFile = folderFullName + fontFile;
            }
            return fontFile != null;
        }

        public void Dispose() {
            
        }
    }
}
