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
using Jamcast.Extensibility.ContentDirectory;
using Jamcast.Extensibility.Metadata;
using System;
using System.Text.RegularExpressions;

namespace Jamcast.Plugins.GoogleMusic {

    [ObjectRenderer(ServerObjectType.Track)]
    public class GMTrack : ObjectRenderer
    {
        private static readonly Regex _regex = new Regex(@"^(?<URL>.+)=", RegexOptions.Compiled);

        public override ServerObject GetMetadata()
        {

            Track t = this.ObjectData as Track;
            string[] contextData = new string[] { t.id };

            AudioItem track = new AudioItem(new MediaServerLocation(typeof(TrackHandler), contextData), MediaFormats.MP3);
            track.Title = t.title;
            track.Genre = t.genre;
            track.AlbumArtist = t.albumArtistUnified;
            if (t.artistUnified != null)
                track.Artists.Add(t.artistUnified);
            track.Album = t.album;
            track.TrackNumber = t.track;
            track.Seconds = t.durationMillis / 1000;
            if (t.composer != null)
                track.Composers.Add(t.composer);
            if (t.albumArtRef.Count > 0)
            {
                string albumArtUrl;
                Match match = _regex.Match(t.albumArtRef[0].url);
                if (match.Success)
                    albumArtUrl = match.Groups["URL"].Value;
                else
                    albumArtUrl = t.albumArtRef[0].url;
                track.AlbumArt = new ImageResource(new UriLocation(albumArtUrl), MediaFormats.JPEG);
            }

            return track;

        }

    }
}
