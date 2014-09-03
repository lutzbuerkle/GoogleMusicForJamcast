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


using GoogleMusic;
using Jamcast.Extensibility;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace Jamcast.Plugins.GoogleMusic
{
    internal class GoogleMusicAPI
    {
        private const int DELAY_CONN_ATTEMPTS = 10000;
        private const int MAX_CONN_ATTEMPTS = 12;

        private static GoogleMusicAPI _instance;

        private GoogleMusicMobileClient _MobileClient;
        private GoogleMusicWebClient _WebClient;
        private Tracklist _tracklist;
        private Playlists _playlists;
        private ulong _deviceId;

        static GoogleMusicAPI()
        {
            Proxy = null;
            _instance = new GoogleMusicAPI();
        }

        private GoogleMusicAPI()
        {
            _MobileClient = new GoogleMusicMobileClient();
            _MobileClient.ErrorHandler += ErrorHandler;
            _MobileClient.Proxy = Proxy;
            _WebClient = new GoogleMusicWebClient();
            _WebClient.ErrorHandler += ErrorHandler;
            _WebClient.Proxy = Proxy;
            _tracklist = new Tracklist();
            _playlists = new Playlists();
        }

        internal static WebProxy Proxy { get; set; }

        internal static GoogleMusicAPI Instance { get { return _instance; } }

        internal void Login(string login, string passwd)
        {
            ThreadPool.QueueUserWorkItem(y =>
            {
                bool connected = false;

                for (int i = 0; i < MAX_CONN_ATTEMPTS; i++)
                {
                    Log.Info(Plugin.LOG_MODULE, "Checking for internet connection", null);
                    if (connected = CheckForInternetConnection()) break;
                    Thread.Sleep(DELAY_CONN_ATTEMPTS);
                }

                if (!connected)
                {
                    Log.Info(Plugin.LOG_MODULE, "Logging into Google Music failed. No connection to the internet", null);
                }
                else
                {
                    _MobileClient.Login(login, passwd);
                    if (_MobileClient.LoginStatus)
                    {
                        Log.Info(Plugin.LOG_MODULE, "Logged into Google Music", null);
                        _WebClient.Login(_MobileClient);
                        if (_WebClient.LoginStatus)
                            _deviceId = GetDeviceId();
                        Log.Info(Plugin.LOG_MODULE, String.Format("Using device id {0}", _deviceId), null);
                        _tracklist = _MobileClient.GetAllTracks();
                        _tracklist.Sort();
                        Log.Info(Plugin.LOG_MODULE, String.Format("Tracklist containing {0} tracks obtained from Google Music", _tracklist.Count), null);
                        _playlists = _MobileClient.GetPlaylists();
                        foreach (Playlist p in _playlists)
                        {
                            for (int i = 0; i < p.tracks.Count; i++)
                            {
                                Track t = p.tracks[i];
                                if (t.ToString() == null) p.tracks[i] = _tracklist[t.id];
                            }
                        }
                        Log.Info(Plugin.LOG_MODULE, String.Format("{0} playlists obtained from Google Music", _playlists.Count), null);
                    }
                    else
                    {
                        Log.Info(Plugin.LOG_MODULE, "Logging into Google Music failed", null);
                    }
                }
            });
        }

        internal bool LoggedIn { get { return _MobileClient.LoginStatus; } }

        internal Tracklist Tracklist { get { return _tracklist; } }

        internal Playlists Playlists { get { return _playlists; } }

        internal Albumlist Albumlist { get { return new Albumlist(_tracklist); } }

        internal AlbumArtistlist AlbumArtistlist { get { return new AlbumArtistlist(_tracklist); } }

        internal string GetStreamUrl(string song_id)
        {
            StreamUrl url;

            if (_deviceId == 0)
            {
                url = _WebClient.GetStreamUrl(song_id);
            }
            else
            {
                url = _MobileClient.GetStreamUrl(song_id, _deviceId);
            }
            Log.Debug(Plugin.LOG_MODULE, String.Format("Url obtained for song id {0}: {1}", song_id, url == null ? "NULL" : url.url), null);

            return (url == null) ? null : url.url;
        }

        private ulong GetDeviceId()
        {
            ulong deviceId = 0;

            Settings settings = _WebClient.GetSettings();
            if (settings != null)
            {
                string id;

                var devices = settings.devices.FindAll(device => device.type.ToUpperInvariant() == "PHONE")
                                      .OrderByDescending(device => device.lastUsed).ToArray();

                if (devices.Length > 0)
                {
                    id = devices.First().id;

                    if (!String.IsNullOrEmpty(id))
                    {
                        if (id.StartsWith("0x"))
                            id = id.Substring(2);

                        UInt64.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out deviceId);
                    }
                }
            }

            return deviceId;
        }

        private bool CheckForInternetConnection()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Proxy = Proxy;
                    using (Stream stream = client.OpenRead("https://www.google.com/"))
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ErrorHandler(string message, Exception error)
        {
            Log.Error(Plugin.LOG_MODULE, message, error);
        }

    }
}
