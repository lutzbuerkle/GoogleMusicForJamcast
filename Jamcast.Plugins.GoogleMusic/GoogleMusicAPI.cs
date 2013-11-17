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
using System;
using System.Threading;
using Jamcast.Extensibility;
using System.Net;
using System.IO;

namespace Jamcast.Plugins.GoogleMusic
{
    class GoogleMusicAPI
    {
        private static int DELAY_CONN_ATTEMPTS = 10000;
        private static int MAX_CONN_ATTEMPTS = 12;

        private static WebProxy proxy;
        private static GoogleMusicClient GMClient;
        private static Playlist tracklist;
        private static Playlists playlists;

        static GoogleMusicAPI()
        {
            proxy = null;
            GMClient = new GoogleMusicClient();
            GMClient.ErrorHandler += ErrorHandler;
            GMClient.Proxy = proxy;
            tracklist = new Playlist();
            playlists = new Playlists();
        }

        public static void Login(string login, string passwd)
        {
            ThreadPool.QueueUserWorkItem(y =>
            {
                bool connected = false;

                for (int i = 0; i < MAX_CONN_ATTEMPTS; i++)
                {
                    Log.Info(Plugin.LOG_MODULE, "Checking internet connection", null);
                    if (connected = CheckForInternetConnection()) break;
                    Thread.Sleep(DELAY_CONN_ATTEMPTS);
                }

                if (!connected)
                {
                    Log.Info(Plugin.LOG_MODULE, "Logging into Google Music failed. No connection to the internet", null);
                }
                else
                {
                    GMClient.Login(login, passwd);
                    if (GMClient.LoginStatus)
                    {
                        Log.Info(Plugin.LOG_MODULE, "Logged into Google Music", null);
                        tracklist = GMClient.GetAllTracks();
                        tracklist.Sort();
                        Log.Info(Plugin.LOG_MODULE, String.Format("Tracklist containing {0} tracks obtained from Google Music", tracklist.tracks.Count), null);
                        playlists = (Playlists)GMClient.GetPlaylist();
                        Log.Info(Plugin.LOG_MODULE, String.Format("{0} playlists obtained from Google Music", playlists.playlists.Count), null);
                    }
                    else
                    {
                        Log.Info(Plugin.LOG_MODULE, "Logging into Google Music failed", null);
                    }
                }
            });
        }

        public static bool LoggedIn { get { return GMClient.LoginStatus; } }

        public static Playlist Tracklist { get { return tracklist; } }

        public static Playlists Playlists { get { return playlists; } }

        public static Albumlist Albumlist { get { return new Albumlist(tracklist); } }

        public static AlbumArtistlist AlbumArtistlist { get { return new AlbumArtistlist(tracklist); } }

        public static string GetStreamUrl(string song_id)
        {
            StreamUrl url = GMClient.GetStreamUrl(song_id);
            return (url == null) ? null : url.url;
        }

        private static bool CheckForInternetConnection()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Proxy = proxy;
                    using (Stream stream = client.OpenRead("http://www.google.com/"))
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void ErrorHandler(string message, Exception error)
        {
            Log.Error(Plugin.LOG_MODULE, message, error);
        }

    }
}
