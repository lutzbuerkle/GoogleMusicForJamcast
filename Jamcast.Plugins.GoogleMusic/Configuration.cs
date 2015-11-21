/*
Copyright (c) 2015, Lutz Bürkle
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the copyright holders nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


using Jamcast.Extensibility;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Jamcast.Plugins.GoogleMusic
{

    [Serializable]
    public class Configuration {

        internal const string CONFIGURATION_KEY = "GoogleMusicConfiguration";

        private static Configuration _instance;
        private string _masterToken;

        private Configuration() { }

        public string Login { get; set; }
        public string MasterToken
        {
            get { return _masterToken; }
            set { _masterToken = value; }
        }
        public string DeviceId { get; set; }

        internal static Configuration Instance
        {
            get {
                if (_instance == null) {
                    _instance = PluginDataProvider.XmlDeserialize<Configuration>(CONFIGURATION_KEY);

                    if (_instance == null) {
                        _instance = new Configuration();
                    }
                }

                return _instance;
            }
        }

        internal void Save()
        {
            PluginDataProvider.XmlSerialize<Configuration>(CONFIGURATION_KEY, _instance);
        }

        public static string Encrypt(string plainText)
        {
            if (String.IsNullOrEmpty(plainText))
                return null;

            byte[] data = Encoding.Unicode.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string cipher)
        {
            if (String.IsNullOrEmpty(cipher))
                return null;

            byte[] data = Convert.FromBase64String(cipher);
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

            return Encoding.Unicode.GetString(decrypted);
        }
    }

}
