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
using Jamcast.Extensibility.ContentDirectory;
using System;
using System.ServiceModel;
using System.Threading;

namespace Jamcast.Plugins.GoogleMusic
{

    public class Plugin : SimplePlugin
    {
        internal static string LOG_MODULE = "GoogleMusic";

        private const int MAX_CONN_ATTEMPTS = 12;
        private ServiceHost _host;

        public override Type ConfigurationPanelType
        {
            get { return typeof(Jamcast.Plugins.GoogleMusic.UI.View.GoogleMusicPanel); }
        }

        public string DisplayName {
            get { return "Google Music"; }
        }

        public override Type RootObjectRendererType
        {
            get { return typeof(Root); }
        }

        public ObjectRenderInfo[] GetPromotedPlaylists()
        {
            return null;
        }

        public override bool Startup()
        {
            string login = Configuration.Instance.Login;
            string masterToken = Configuration.Instance.MasterToken;

            try
            {
                _host = new ServiceHost(typeof(GoogleMusicWCFServices), new Uri[] { new Uri("net.pipe://localhost") });
                _host.AddServiceEndpoint(typeof(IGoogleMusicWCFServices), new NetNamedPipeBinding(), "PipeGoogleMusicForJamcast");
                _host.Open();

                GoogleMusicAPI.OnLogin += GoogleMusicWCFServices.OnLoginCallback;

                Log.Debug(Plugin.LOG_MODULE, "WCF service established.", null);
            }
            catch (Exception error)
            {
                Log.Error(Plugin.LOG_MODULE, "WCF service could not be established.", error);
                return false;
            }

            Log.Info(Plugin.LOG_MODULE, "Google Music plugin initialized successfully.", null);

            if (String.IsNullOrWhiteSpace(login) || String.IsNullOrWhiteSpace(masterToken))
            {
                Log.Debug(Plugin.LOG_MODULE, "No valid login credentials available.", null);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    int status = GoogleMusicAPI.Instance.Login(MAX_CONN_ATTEMPTS);
                    if (status == GoogleMusicAPI.LOGIN_SUCCESS)
                    {
                        Log.Info(LOG_MODULE, "Logged into Google Music. Retrieving music data.", null);
                        GoogleMusicAPI.Instance.GetMusicData();
                    }
                    else
                    {
                        if (status == GoogleMusicAPI.LOGIN_FAILURE__NO_INTERNET_CONNECTION)
                        {
                            Log.Info(LOG_MODULE, "Google Music login failed. No connection to the internet.", null);
                        }
                        else
                        {
                            Configuration.Instance.MasterToken = null;
                            Log.Info(LOG_MODULE, String.Format("Google Music login failed for user {0}.", Configuration.Instance.Login), null);
                        }
                    }
                });
            }

            return true;
        }

        public override void Shutdown()
        {
            _host.Close();
            GoogleMusicAPI.Instance.Logout();
            Log.Info(Plugin.LOG_MODULE, "Google Music plugin was shut down successfully", null);
        }

        public override void OnPreRender()
        {
            if (!GoogleMusicAPI.Instance.LoggedIn)
                throw new PluginNotLoggedInException("Channel not logged into Google Music. Please log in using the Jamcast Server Manager.");
        }

    }

}
