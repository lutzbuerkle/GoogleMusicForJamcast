﻿/*
Copyright (c) 2014, Lutz Bürkle
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
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;

namespace Jamcast.Plugins.GoogleMusic
{
    internal class GoogleMusicAPI
    {
        internal const int LOGIN_SUCCESS = 0;
        internal const int LOGIN_FAILURE__NO_INTERNET_CONNECTION = 1;
        internal const int LOGIN_FAILURE__WRONG_CREDENTIALS = 2;
        internal const int LOGIN_BUSY = -1;

        private const int DELAY_CONN_ATTEMPTS = 10000;

        private static readonly Regex _regex = new Regex(@"^(?<URL>.+)=", RegexOptions.Compiled);

        private static GoogleMusicAPI _instance;

        private GoogleMusicMobileClient _MobileClient;
        private GoogleMusicWebClient _WebClient;
        private PersistedTracklist _tracklist;
        private PersistedPlaylists _playlists;
        private PersistedAlbumlist _albums;
        private PersistedAlbumArtistlist _albumArtists;
        private ulong _deviceId;

        internal delegate void OnLoginDelegate(int status);

        static GoogleMusicAPI()
        {
            Proxy = null;
            OnLogin = null;
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
            _tracklist = new PersistedTracklist();
            _playlists = new PersistedPlaylists();
            _albums = new PersistedAlbumlist();
            _albumArtists = new PersistedAlbumArtistlist();
        }

        internal static event OnLoginDelegate OnLogin;

        internal static WebProxy Proxy { get; set; }

        internal static GoogleMusicAPI Instance
        { 
            get
            {
                if (_instance == null) _instance = new GoogleMusicAPI();
                return _instance;
            } 
        }

        internal int Login(string login, string passwd, int numConnAttempts = 1)
        {
            int status = LOGIN_BUSY;
            bool connected = false;

            IsLoggingIn = true;
            if (OnLogin != null) OnLogin(status);

            for (int i = 0; i < numConnAttempts; i++)
            {
                Log.Debug(Plugin.LOG_MODULE, "Checking for internet connection", null);
                if (connected = CheckForInternetConnection()) break;
                Thread.Sleep(DELAY_CONN_ATTEMPTS);
            }

            if (!connected)
            {
                status = LOGIN_FAILURE__NO_INTERNET_CONNECTION;
            }
            else
            {
                _MobileClient.Login(login, passwd);
                if (_MobileClient.LoginStatus)
                {
                    status = LOGIN_SUCCESS;
                }
                else
                {
                    status = LOGIN_FAILURE__WRONG_CREDENTIALS;
                }
            }

            IsLoggingIn = false;
            if (OnLogin != null) OnLogin(status);

            return status;
        }

        internal void GetMusicData()
        {
            if (_MobileClient.LoginStatus && CheckForInternetConnection())
            {
                _WebClient.Login(_MobileClient);
                if (_WebClient.LoginStatus)
                    _deviceId = GetDeviceId();
                Log.Debug(Plugin.LOG_MODULE, String.Format("Using device id {0}", _deviceId), null);

                Tracklist tracklist = _MobileClient.GetAllTracks();
                if (tracklist != null)
                {
                    _tracklist = new PersistedTracklist(tracklist);
                    _tracklist.Sort();
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} tracks obtained from Google Music", _tracklist.Count), null);

                    _albums = new PersistedAlbumlist(_tracklist);
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} albums in database", _albums.Count), null);

                    _albumArtists = new PersistedAlbumArtistlist(_albums);
                    foreach (PersistedAlbumArtist albumArtist in _albumArtists)
                    {
                        PersistedAlbum alltracks = new PersistedAlbum();
                        alltracks.album = String.Format("All tracks by {0}", albumArtist);
                        alltracks.albumArtist = albumArtist.albumArtist;
                        alltracks.albumArtistSort = albumArtist.albumArtistSort;
                        alltracks.tracks = new PersistedTracklist();
                        foreach (PersistedAlbum album in albumArtist.albums)
                        {
                            alltracks.tracks.AddRange(album.tracks);
                        }
                        alltracks.tracks.Sort();
                        albumArtist.albums.Add(alltracks);
                    }
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} album artists in database", _albumArtists.Count), null);
                }
                else
                {
                    Log.Info(Plugin.LOG_MODULE, String.Format("Failed to update tracklist from Google Music"), null);
                }

                Playlists playlists = _MobileClient.GetPlaylists();
                if (playlists != null)
                {
                    _playlists = new PersistedPlaylists(playlists);
                    _playlists.Sort();
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} playlists obtained from Google Music", _playlists.Count), null);
                }
                else
                {
                    Log.Info(Plugin.LOG_MODULE, String.Format("Failed to update playlists from Google Music"), null);
                }
            }
            else
            {
                Log.Info(Plugin.LOG_MODULE, String.Format("Failed to update music data from Google Music"), null);
            }
        }

        internal void Logout()
        {
            _MobileClient.Logout();
            _WebClient.Logout();
            _instance = new GoogleMusicAPI();
            Log.Debug(Plugin.LOG_MODULE, "Logged out of Google Music", null);
        }

        internal bool IsLoggingIn { get; private set; }

        internal bool LoggedIn { get { return _MobileClient.LoginStatus; } }

        internal PersistedTracklist Tracks { get { return _tracklist; } }

        internal PersistedPlaylists Playlists { get { return _playlists; } }

        internal PersistedAlbumlist Albums { get { return _albums; } }

        internal PersistedAlbumArtistlist AlbumArtists { get { return _albumArtists; } }

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

        internal string GetAlbumArtUrl(Track track)
        {
            string albumArtUrl = null;

            if (track.albumArtRef != null && track.albumArtRef.Count > 0)
            {
                Match match = _regex.Match(track.albumArtRef.First().url);
                if (match.Success)
                    albumArtUrl = match.Groups["URL"].Value;
                else
                    albumArtUrl = track.albumArtRef.First().url;
            }

            return albumArtUrl;
        }

        internal PersistedTrack GetTrack(string id)
        {
            return _tracklist[id];
        }

        internal PersistedPlaylist GetPlaylist(string id)
        {
            return _playlists[id];
        }

        internal PersistedAlbum GetAlbum(string id)
        {
            return _albums[id];
        }

        internal PersistedAlbumArtist GetAlbumArtist(string id)
        {
            return _albumArtists[id];
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

    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IGoogleMusicWCFCallbacks))]
    public interface IGoogleMusicWCFServices
    {
        [OperationContract]
        int Login(string login, string passwd);
        [OperationContract]
        bool IsLoggingIn();
        [OperationContract]
        bool LoggedIn();
        [OperationContract]
        void GetMusicData();
        [OperationContract]
        void Logout();
        [OperationContract]
        void SubscribeToCallback();
    }

    public interface IGoogleMusicWCFCallbacks
    {
        [OperationContract(IsOneWay = true)]
        void OnLogin(int status);
    }

    public class GoogleMusicWCFServices : IGoogleMusicWCFServices
    {
        private static IGoogleMusicWCFCallbacks _callbacks;

        public int Login(string login, string passwd)
        {
            return GoogleMusicAPI.Instance.Login(login, passwd);
        }

        public bool IsLoggingIn()
        {
            return GoogleMusicAPI.Instance.IsLoggingIn;
        }

        public bool LoggedIn()
        {
            return GoogleMusicAPI.Instance.LoggedIn;
        }

        public void GetMusicData()
        {
            GoogleMusicAPI.Instance.GetMusicData();
        }

        public void Logout()
        {
            GoogleMusicAPI.Instance.Logout();
        }

        public void SubscribeToCallback()
        {
            _callbacks = OperationContext.Current.GetCallbackChannel<IGoogleMusicWCFCallbacks>();
        }

        public static void OnLoginCallback(int status)
        {
            if (_callbacks != null)
            {
                _callbacks.OnLogin(status);
                Log.Trace(Plugin.LOG_MODULE, "Login callback triggered", null);
            }
        }
    }
}

