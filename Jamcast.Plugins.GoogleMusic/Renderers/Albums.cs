/*
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


using Jamcast.Extensibility.ContentDirectory;
using Jamcast.Extensibility.Metadata;
using System;

namespace Jamcast.Plugins.GoogleMusic
{

    public class AlbumlistRenderer : ContainerRenderer
    {

        private PersistedAlbumlist albums;

        public override int PrepareGetChildren(int startIndex, int count)
        {
            albums = GoogleMusicAPI.Instance.Albums;
            return albums == null ? 0 : albums.Count;
        }

        public override ObjectRenderInfo GetChildAt(int index)
        {
            return new ObjectRenderInfo(typeof(AlbumRenderer), albums[index]);
        }

        public override ServerObject GetMetadata()
        {
            return new GenericContainer(this.ObjectData.ToString());
        }

    }


    [ContainerRenderer(ContainerType.Album)]
    public class AlbumRenderer : ContainerRenderer
    {

        private PersistedAlbum album;

        public override int PrepareGetChildren(int startIndex, int count)
        {
            album = this.ObjectData as PersistedAlbum;
            return album == null ? 0 : album.tracks.Count;
        }

        public override ObjectRenderInfo GetChildAt(int index)
        {
            return new ObjectRenderInfo(typeof(GMTrack), album.tracks[index]);
        }

        public override ServerObject GetMetadata()
        {
            album = this.ObjectData as PersistedAlbum;

            if (album == null)
            {
                return new GenericContainer(this.ObjectData.ToString());
            }
            else
            {
                string albumArtUrl = album.albumArtUrl;

                if (String.IsNullOrEmpty(albumArtUrl))
                {
                    return new AlbumContainer(album.albumUnique, album.albumArtist, null);
                }
                else
                {
                    return new AlbumContainer(album.albumUnique, album.albumArtist, new ImageResource(new UriLocation(albumArtUrl), MediaFormats.JPEG));
                }
            }
        }

    }


    [ContainerRenderer(ContainerType.Album)]
    public class AlbumArtistAlbumRenderer : AlbumRenderer
    {

        private PersistedAlbum album;

        public override ServerObject GetMetadata()
        {
            album = this.ObjectData as PersistedAlbum;

            if (album == null)
            {
                return new GenericContainer(this.ObjectData.ToString());
            }
            else
            {
                string albumArtUrl = album.albumArtUrl;

                if (String.IsNullOrEmpty(albumArtUrl))
                {
                    return new AlbumContainer(album.album, album.albumArtist, null);
                }
                else
                {
                    return new AlbumContainer(album.album, album.albumArtist, new ImageResource(new UriLocation(albumArtUrl), MediaFormats.JPEG));
                }
            }
        }
    }
}
