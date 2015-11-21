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
using Jamcast.Extensibility.ContentDirectory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Utilities.Base32;

namespace Jamcast.Plugins.GoogleMusic
{

    #region Track

    [DataContract]
    internal class PersistedTrack : IPersisted, IComparable<PersistedTrack>
    { 
        private Track _track;

        public PersistedTrack() { }

        public PersistedTrack(Track track) : this()
        {
            id = track.id;
            _track = track;
        }

        [DataMember(Order = 1)]
        public string id { get; set; }
        public Track track
        {
            get
            {
                if (_track == null)
                {
                    PersistedTrack t = GoogleMusicAPI.Instance.GetTrack(id);
                    _track = t == null ? null : t.track;
                }

                return _track;
            }
            protected set
            {
                _track = value;
            }
        }

        public int GetPersistenceHash()
        {
            return this.id.GetHashCode();
        }

        public override string ToString()
        {
            return track.ToString();
        }

        public int CompareTo(PersistedTrack other)
        {
            return track.CompareTo(other.track);
        }

        public static Comparison<PersistedTrack> CompareByArtist = delegate(PersistedTrack t1, PersistedTrack t2) { return Track.CompareByArtist(t1.track, t2.track); };
        public static Comparison<PersistedTrack> CompareByAlbumArtist = delegate(PersistedTrack t1, PersistedTrack t2) { return Track.CompareByAlbumArtist(t1.track, t2.track); };
        public static Comparison<PersistedTrack> CompareByAlbum = delegate(PersistedTrack t1, PersistedTrack t2) { return Track.CompareByAlbum(t1.track, t2.track); };
    }


    internal class PersistedTracklist : List<PersistedTrack>
    {
        public PersistedTracklist() : base() { }

        public PersistedTracklist(IEnumerable<PersistedTrack> tracks) : this()
        {
            this.AddRange(tracks);
            if (tracks is PersistedTracklist) lastUpdatedTimestamp = (tracks as PersistedTracklist).lastUpdatedTimestamp;
        }

        public PersistedTracklist(IEnumerable<Track> tracks) : this()
        {
            foreach (Track track in tracks)
            {
                this.Add(new PersistedTrack(track));
            }
            if (tracks is Tracklist) lastUpdatedTimestamp = (tracks as Tracklist).lastUpdatedTimestamp;
        }

        public PersistedTrack this[string id]
        {
            get { return this.Find(t => t.id == id); }
        }

        public DateTime lastUpdatedTimestamp { get; set; }

        public void SortByArtist() { this.Sort(PersistedTrack.CompareByArtist); }
        public void SortByAlbumArtist() { this.Sort(PersistedTrack.CompareByAlbumArtist); }
        public void SortByAlbum() { this.Sort(PersistedTrack.CompareByAlbum); }
    }

    #endregion


    #region Playlist

    [DataContract]
    internal class PersistedPlaylistEntry : PersistedTrack, IComparable<PersistedPlaylistEntry>
    {
        public PersistedPlaylistEntry(PlaylistEntry entry) : base()
        {
            if (entry.playlistId == null)
                id = entry.trackId;
            else
                id = entry.id;

            if (entry.track == null)
            {
                PersistedTrack track = GoogleMusicAPI.Instance.GetTrack(entry.trackId);
                if (track != null)
                    base.track = track.track;
                else
                    base.track = null;
            }
            else
            {
                base.track = entry.track;
            }

            absolutePosition = entry.absolutePosition;
        }

        public long absolutePosition { get; protected set; }

        public int CompareTo(PersistedPlaylistEntry other)
        {
            return absolutePosition.CompareTo(other.absolutePosition);
        }
    }


    internal class PersistedPlaylistEntrylist : List<PersistedPlaylistEntry>
    {
        public PersistedPlaylistEntrylist() : base() { }

        public PersistedPlaylistEntrylist(IEnumerable<PersistedPlaylistEntry> entries) : this()
        {
            this.AddRange(entries);
        }

        public PersistedPlaylistEntry this[string id]
        {
            get { return this.Find(t => t.id == id); }
        }
    }


    [DataContract]
    internal class PersistedPlaylist : IPersisted, IComparable<PersistedPlaylist>
    {
        private string _name;
        private PersistedPlaylistEntrylist _entries;

        public PersistedPlaylist() { }

        public PersistedPlaylist(Playlist playlist) : this()
        {
            id = playlist.id;
            _name = playlist.name;
            _entries = new PersistedPlaylistEntrylist();
            foreach (PlaylistEntry entry in playlist.entries)
                _entries.Add(new PersistedPlaylistEntry(entry));
        }

        [DataMember(Order = 1)]
        public string id { get; set; }
        public string name
        {
            get
            {
                if (_name == null) Refresh();
                return _name;
            } 
        }
        public PersistedPlaylistEntrylist tracks
        {
            get
            {
                if (_entries == null) Refresh();
                return _entries;
            }
            set
            {
                _entries = value;
            }
        }

        public int GetPersistenceHash()
        {
            return this.id.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }

        public int CompareTo(PersistedPlaylist other)
        {
            return String.Compare(name, other.name, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);
        }

        private void Refresh()
        {
            PersistedPlaylist playlist = GoogleMusicAPI.Instance.GetPlaylist(id);
            if (playlist != null)
            {
                _name = playlist.name;
                _entries = playlist.tracks;
            }
        }
    }


    internal class PersistedPlaylists : List<PersistedPlaylist>
    {
        public PersistedPlaylists() : base() { }

        public PersistedPlaylists(Playlists playlists) : this()
        {
            foreach (Playlist playlist in playlists)
            {
                this.Add(new PersistedPlaylist(playlist));
            }
            lastUpdatedTimestamp = playlists.lastUpdatedTimestamp;
        }

        public PersistedPlaylists(IEnumerable<PersistedPlaylist> playlists) : this()
        {
            this.AddRange(playlists);
            if (playlists is PersistedPlaylists) lastUpdatedTimestamp = (playlists as PersistedPlaylists).lastUpdatedTimestamp;
        }

        public PersistedPlaylist this[string id]
        {
            get { return this.Find(t => t.id == id); }
        }

        public DateTime lastUpdatedTimestamp { get; set; }
    }

    #endregion


    #region Album

    [DataContract]
    internal class PersistedAlbum : IPersisted, IComparable<PersistedAlbum>
    {
        private string _id;
        private string _album;
        private string _albumArtist;
        private string _albumArtistSort;
        private PersistedTracklist _tracks;

        public PersistedAlbum() { }

        [DataMember(Order = 1)]
        public string id
        {
            get {
                if (_id == null)
                {
                    MD5 md5 = MD5.Create();
                    string idString = String.Format("{0}___{1}", album.ToLower(), albumArtistSort);
                    byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(idString));
                    _id = "B" + Base32.ToBase32String(hash).ToLower();
                }

                return _id;
            }
            set { _id = value; }
        }
        public string album
        {
            get { _album = _album == null && tracks != null && tracks.Count > 0 ? tracks.First().track.album : _album; return _album; }
            set { _album = value; }
        }
        public string albumUnique { get; set; }
        public string albumArtist
        {
            get { _albumArtist = _albumArtist == null && tracks != null && tracks.Count > 0 ? tracks.First().track.albumArtist : _albumArtist; return _albumArtist; }
            set { _albumArtist = value; }
        }
        public string albumArtistSort
        {
            get { _albumArtistSort = _albumArtistSort == null && tracks != null && tracks.Count > 0 ? tracks.First().track.albumArtistNorm : _albumArtistSort; return _albumArtistSort; }
            set { _albumArtistSort = value; }
        }
        public string albumArtUrl
        {
            get { return tracks != null && tracks.Count > 0 ? GoogleMusicAPI.Instance.GetAlbumArtUrl(tracks.First().track) : null; }
        }
        public PersistedTracklist tracks
        {
            get
            {
                if (_tracks == null) Refresh();
                return _tracks;
            }
            set { _tracks = value; }
        }

        public int GetPersistenceHash()
        {
            return this.id.GetHashCode();
        }

        public override string ToString()
        {
            return album;
        }

        public int CompareTo(PersistedAlbum other)
        {
            int result = String.Compare(album, other.album, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);
            if (result == 0)
                result = String.Compare(albumArtistSort, other.albumArtistSort, CultureInfo.CurrentCulture, CompareOptions.IgnoreSymbols);

            return result;
        }

        public static Comparison<PersistedAlbum> CompareByAlbumArtist = delegate(PersistedAlbum a1, PersistedAlbum a2) 
        {
            int result = String.Compare(a1.albumArtistSort, a2.albumArtistSort, CultureInfo.CurrentCulture, CompareOptions.IgnoreSymbols);
            if (result == 0)
                result = String.Compare(a1.ToString(), a2.ToString(), CultureInfo.CurrentCulture, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols);

            return result;
        };

        private void Refresh()
        {
            PersistedAlbum album = GoogleMusicAPI.Instance.GetAlbum(id);
            if (album != null)
            {
                this.album = album.album;
                this.albumUnique = album.albumUnique;
                this.albumArtist = album.albumArtist;
                this.albumArtistSort = album.albumArtistSort;
                this.tracks = album.tracks;
            }
        }
    }


    internal class PersistedAlbumlist : List<PersistedAlbum>
    {
        public PersistedAlbumlist() : base() { }

        public PersistedAlbumlist(IEnumerable<PersistedTrack> tracks) : this()
        {
            List<PersistedAlbum> albums = tracks.OrderBy(track => track, new Comparer<PersistedTrack>(PersistedTrack.CompareByAlbum))
                                                .GroupBy(track => new { track.track.album, track.track.albumArtistNorm })
                                                .Select(groupedTracks => new PersistedAlbum { album = groupedTracks.Key.album, albumArtistSort = groupedTracks.Key.albumArtistNorm, tracks = new PersistedTracklist(groupedTracks.ToList()) }).ToList();

            for (int i = 0, j; i < albums.Count - 1; i = j)
            {
                bool flag = false;

                albums[i].albumUnique = albums[i].album;

                for (j = i + 1; j < albums.Count; j++)
                {
                    if (albums[i].album.ToLower() == albums[j].album.ToLower())
                    {
                        albums[j].albumUnique = String.Format("{0} [{1}]", albums[j].album, albums[j].albumArtist);
                        flag = true;
                    }
                    else
                    {
                        albums[j].albumUnique = albums[j].album;
                        if (flag)
                            albums[i].albumUnique = String.Format("{0} [{1}]", albums[i].album, albums[i].albumArtist);

                        break;
                    }
                }
            }

            this.AddRange(albums);
        }

        public PersistedAlbumlist(IEnumerable<PersistedAlbum> albums) : this()
        {
            this.AddRange(albums);
        }

        public PersistedAlbum this[string id]
        {
            get { return this.Find(a => a.id == id); }
        }

        public void SortByAlbumArtist() { this.Sort(PersistedAlbum.CompareByAlbumArtist); }
    }

    #endregion


    #region AlbumArtist

    [DataContract]
    internal class PersistedAlbumArtist : IPersisted
    {
        private string _id;
        private string _albumArtist;
        private string _albumArtistSort;
        private PersistedAlbumlist _albums;

        public PersistedAlbumArtist() { }

        [DataMember(Order = 1)]
        public string id
        {
            get {
                if (_id == null)
                {
                    MD5 md5 = MD5.Create();
                    string idString = albumArtistSort;
                    byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(idString));
                    _id = "A" + Base32.ToBase32String(hash).ToLower();
                }

                return _id;
            }
            set { _id = value; }
        }
        public string albumArtist
        {
            get { _albumArtist = _albumArtist == null && albums != null && albums.Count > 0 ? albums.First().albumArtist : _albumArtist; return _albumArtist; }
            set { _albumArtist = value; }
        }
        public string albumArtistSort
        {
            get { _albumArtistSort = _albumArtistSort == null && albums != null && albums.Count > 0 ? albums.First().albumArtistSort : _albumArtistSort; return _albumArtistSort; }
            set { _albumArtistSort = value; }
        }
        public PersistedAlbumlist albums
        {
            get
            {
                if (_albums == null) Refresh();
                return _albums;
            }
            set
            {
                _albums = value;

                if (_albums != null && _albums.Count > 0) {
                    PersistedAlbum alltracks = new PersistedAlbum();
                    alltracks.album = String.Format("All tracks by {0}", albumArtist);
                    alltracks.albumArtist = _albums.First().albumArtist;
                    alltracks.albumArtistSort = _albums.First().albumArtistSort;
                    alltracks.tracks = new PersistedTracklist();
                    foreach (PersistedAlbum album in _albums)
                    {
                        alltracks.tracks.AddRange(album.tracks);
                    }
                    alltracks.tracks.Sort();

                    _albums.Add(alltracks);
                }
            }
        }

        public int GetPersistenceHash()
        {
            return this.id.GetHashCode();
        }

        public override string ToString()
        {
            return albumArtist;
        }

        private void Refresh()
        {
            PersistedAlbumArtist albumArtist = GoogleMusicAPI.Instance.GetAlbumArtist(id);
            if (albumArtist != null)
            {
                this.albumArtist = albumArtist.albumArtist;
                this.albumArtistSort = albumArtist.albumArtistSort;
                this.albums = albumArtist.albums;
            }
        }
    }


    internal class PersistedAlbumArtistlist : List<PersistedAlbumArtist>
    {
        public PersistedAlbumArtistlist() : base() { }

        public PersistedAlbumArtistlist(IEnumerable<PersistedAlbum> albums) : this()
        {
            List<PersistedAlbumArtist> albumArtists = albums.OrderBy(album => album, new Comparer<PersistedAlbum>(PersistedAlbum.CompareByAlbumArtist))
                                                            .GroupBy(album => album.albumArtistSort)
                                                            .Select(groupedAlbums => new PersistedAlbumArtist { albumArtistSort = groupedAlbums.Key, albums = new PersistedAlbumlist(groupedAlbums.ToList()) }).ToList();
            this.AddRange(albumArtists);
        }

        public PersistedAlbumArtist this[string id]
        {
            get { return this.Find(aa => aa.id == id); }
        }
    }

#endregion


    internal class Comparer<T> : IComparer<T>
    {
        private readonly Comparison<T> _comparison;

        public Comparer(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public int Compare(T x, T y)
        {
            return _comparison(x, y);
        }
    }
}
