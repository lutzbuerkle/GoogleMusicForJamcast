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


using GoogleMusic;
using Jamcast.Extensibility;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Timers;

namespace Jamcast.Plugins.GoogleMusic
{
    internal class GoogleMusicAPI
    {
        public const int LOGIN_BUSY = 0;
        public const int LOGIN_SUCCESS = 1;
        public const int LOGIN_FAILURE__NO_INTERNET_CONNECTION = -1;
        public const int LOGIN_FAILURE__WRONG_CREDENTIALS = -2;

        private const int DELAY_CONN_ATTEMPTS = 10000;
        private const int UPDATE_INTERVAL = 300;

        private static readonly Regex _regex = new Regex(@"^(?<URL>.+)=", RegexOptions.Compiled);

        private static GoogleMusicAPI _instance;

        private GoogleMusicMobileClient _MobileClient;
        private PersistedTracklist _tracklist;
        private PersistedPlaylists _playlists;
        private PersistedAlbumlist _albums;
        private PersistedAlbumArtistlist _albumArtists;
        private Timer _timer;
        private ulong _ticks;
        private string _login;
        private string _masterToken;
        private string _deviceId;

        public delegate void OnLoginDelegate(int status);

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
            _tracklist = new PersistedTracklist();
            _playlists = new PersistedPlaylists();
            _albums = new PersistedAlbumlist();
            _albumArtists = new PersistedAlbumArtistlist();
            _timer = new Timer(1000);
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _login = Configuration.Instance.Login;
            _masterToken = Configuration.Decrypt(Configuration.Instance.MasterToken);
            _deviceId = Configuration.Instance.DeviceId;
        }

        public static event OnLoginDelegate OnLogin;

        public static WebProxy Proxy { get; set; }

        public static GoogleMusicAPI Instance
        { 
            get
            {
                if (_instance == null) _instance = new GoogleMusicAPI();
                return _instance;
            } 
        }

        public int MasterLogin(string login, string password, int numConnAttempts = 1)
        {
            int status = LOGIN_BUSY;
            string deviceId = _MobileClient.MACaddress ?? "123456789abcdef0";
            bool connected = false;

            IsLoggingIn = true;
            if (OnLogin != null) OnLogin(status);

            for (int i = 0; i < numConnAttempts; i++)
            {
                Log.Debug(Plugin.LOG_MODULE, "Checking for internet connection", null);
                if (connected = CheckForInternetConnection()) break;
                System.Threading.Thread.Sleep(DELAY_CONN_ATTEMPTS);
            }

            if (connected)
            {
                Tuple<string,string> token = _MobileClient.MasterLogin(login, password, deviceId);

                if (_MobileClient.LoginStatus)
                {
                    _login = token.Item1;
                    _masterToken = token.Item2;
                    _deviceId = deviceId;

                    status = LOGIN_SUCCESS;
                }
                else
                {
                    status = LOGIN_FAILURE__WRONG_CREDENTIALS;
                }
            }
            else
            {
                status = LOGIN_FAILURE__NO_INTERNET_CONNECTION;
            }

            IsLoggingIn = false;
            if (OnLogin != null) OnLogin(status);

            return status;
        }

        public int Login(int numConnAttempts = 1)
        {
            int status = LOGIN_BUSY;
            bool connected = false;

            IsLoggingIn = true;
            if (OnLogin != null) OnLogin(status);

            for (int i = 0; i < numConnAttempts; i++)
            {
                Log.Debug(Plugin.LOG_MODULE, "Checking for internet connection", null);
                if (connected = CheckForInternetConnection()) break;
                System.Threading.Thread.Sleep(DELAY_CONN_ATTEMPTS);
            }

            if (!connected)
            {
                status = LOGIN_FAILURE__NO_INTERNET_CONNECTION;
            }
            else
            {
                _MobileClient.Login(_login, _masterToken);
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

        public void GetMusicData()
        {
            if (_MobileClient.LoginStatus && CheckForInternetConnection())
            {
                Tracklist tracklist = _MobileClient.GetAllTracks();
                if (tracklist != null)
                {
                    _tracklist = new PersistedTracklist(tracklist);
                    _tracklist.Sort();
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} tracks obtained from Google Music", _tracklist.Count), null);

                    _albums = new PersistedAlbumlist(_tracklist);
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} albums in database", _albums.Count), null);

                    _albumArtists = new PersistedAlbumArtistlist(_albums);
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} album artists in database", _albumArtists.Count), null);
                }
                else
                {
                    Log.Info(Plugin.LOG_MODULE, String.Format("Failed to update tracklist from Google Music"), null);
                }

                Playlists playlists = _MobileClient.GetAllPlaylists();
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

                _timer.Enabled = true;
            }
            else
            {
                Log.Info(Plugin.LOG_MODULE, String.Format("Failed to update music data from Google Music"), null);
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (++_ticks % UPDATE_INTERVAL == 0)
            {
                if (_MobileClient.LoginStatus && CheckForInternetConnection())
                {
                    if (UpdateTracks(ref _tracklist))
                    {
                        _tracklist.Sort();

                        _albums = new PersistedAlbumlist(_tracklist);

                        _albumArtists = new PersistedAlbumArtistlist(_albums);
                    }

                    UpdatePlaylists(ref _playlists);
                }
            }
        }

        public void Logout()
        {
            _timer.Enabled = false;
            _MobileClient.Logout();
            _instance = new GoogleMusicAPI();
            Log.Debug(Plugin.LOG_MODULE, "Logged out of Google Music", null);
        }

        public void SaveCredentials()
        {
            Configuration.Instance.Login = _login;
            Configuration.Instance.MasterToken = Configuration.Encrypt(_masterToken);
            Configuration.Instance.DeviceId = _deviceId;
            Configuration.Instance.Save();
        }

        public bool IsLoggingIn { get; private set; }

        public bool LoggedIn { get { return _MobileClient.LoginStatus; } }

        public PersistedTracklist Tracks { get { return _tracklist; } }

        public PersistedPlaylists Playlists { get { return _playlists; } }

        public PersistedAlbumlist Albums { get { return _albums; } }

        public PersistedAlbumArtistlist AlbumArtists { get { return _albumArtists; } }


        private bool UpdateTracks(ref PersistedTracklist tracksInput)
        {
            bool updated = false;

            if (tracksInput != null)
            {
                Tracklist newTracks = _MobileClient.GetTracks(tracksInput.lastUpdatedTimestamp);

                if (newTracks != null)
                {
                    Log.Debug(Plugin.LOG_MODULE, String.Format("{0} updated tracks obtained from Google Music", newTracks.Count), null);

                    if (newTracks.Count > 0)
                    {
                        PersistedTracklist tracks = new PersistedTracklist(tracksInput);
                        foreach (Track newTrack in newTracks)
                        {
                            PersistedTrack removeTrack = tracks[newTrack.id];
                            if (removeTrack != null)
                                tracks.Remove(removeTrack);
                            if (newTrack.deleted == false)
                                tracks.Add(new PersistedTrack(newTrack));
                        }
                        tracksInput = tracks;
                        updated = true;
                    }

                    tracksInput.lastUpdatedTimestamp = DateTime.Now;
                }
            }

            return updated;
        }


        private bool UpdatePlaylists(ref PersistedPlaylists playlistsInput)
        {
            bool updated = false;

            Playlists playlists = _MobileClient.GetAllPlaylists();
            if (playlists != null)
            {
                playlists.Sort();
                playlistsInput = new PersistedPlaylists(playlists);
                Log.Debug(Plugin.LOG_MODULE, String.Format("Playlists updated. {0} playlists obtained from Google Music", playlists.Count), null);
                updated = true;
            }

            return updated;
        }


        public string GetStreamUrl(string song_id)
        {
            StreamUrl url = _MobileClient.GetStreamUrl(song_id, _deviceId);

            Log.Debug(Plugin.LOG_MODULE, String.Format("Url obtained for song id {0}: {1}", song_id, url == null ? "NULL" : url.url), null);

            return (url == null) ? null : url.url;
        }

        public string GetAlbumArtUrl(Track track)
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

        public PersistedTrack GetTrack(string id)
        {
            return _tracklist[id];
        }

        public PersistedPlaylist GetPlaylist(string id)
        {
            return _playlists[id];
        }

        public PersistedAlbum GetAlbum(string id)
        {
            return _albums[id];
        }

        public PersistedAlbumArtist GetAlbumArtist(string id)
        {
            return _albumArtists[id];
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
            int status = GoogleMusicAPI.Instance.MasterLogin(login, passwd);

            if (status == GoogleMusicAPI.LOGIN_SUCCESS) GoogleMusicAPI.Instance.SaveCredentials();

            return status;
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

