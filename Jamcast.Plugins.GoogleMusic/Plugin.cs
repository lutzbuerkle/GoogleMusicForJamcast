/*
Copyright (c) 2013, Lutz Bürkle
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
using Jamcast.Extensibility.ContentDirectory;
using System;

namespace Jamcast.Plugins.GoogleMusic
{

    public class Plugin : IContentDirectoryPlugin {       

        internal static string LOG_MODULE = "GoogleMusic";

        public Type RootObjectRendererType {

            get { return typeof(Root); }

        }

        public string DisplayName {

            get { return "Google Music"; }

        }

        public Type ConfigurationPanelType {

            get { return typeof(GMPanel); }

        }

        public ObjectRenderInfo[] GetPromotedPlaylists() {

            return null;

        }

        public bool Startup() {

            string login = Configuration.Instance.Login;
            string passwd = Configuration.Instance.Password;

            if (!Configuration.Instance.IsEnabled)
            {
                Log.Info(Plugin.LOG_MODULE, "Plugin is disabled.");
                return false;
            }

            if (String.IsNullOrEmpty(login) || String.IsNullOrEmpty(passwd)) return false;

            GoogleMusicAPI.Instance.Login(login, passwd);

            return true;

        }

        public void Shutdown() {

            // do nothing
            Log.Debug(Plugin.LOG_MODULE, "Google Music plugin was shut down successfully", null);
        }

    }

}
