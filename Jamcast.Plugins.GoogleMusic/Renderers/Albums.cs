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

namespace Jamcast.Plugins.GoogleMusic
{

    [ObjectRenderer(ServerObjectType.GenericContainer)]
    public class AlbumContainer : ContainerRenderer
    {

        public override ServerObject GetMetadata()
        {
            // set the container metadata
            return new GenericContainer(this.ObjectData.ToString());
        }

        public override void GetChildren(int startIndex, int count, out int totalMatches)
        {

            if (this.ObjectData is String)
            {
                Albumlist a = GoogleMusicAPI.Instance.Albumlist;

                if (a == null)
                {
                    totalMatches = 0;
                    return;
                }

                totalMatches = a.Count;

                count = Math.Min(count, totalMatches - startIndex);

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    this.CreateChildObject<AlbumContainer>(a[i]);
                }
            }
            else
            {
                Album a = this.ObjectData as Album;

                totalMatches = a.tracks.Count;

                count = Math.Min(count, totalMatches - startIndex);

                for (int i = startIndex; i < startIndex + count; i++)
                {
                    this.CreateChildObject<GMTrack>(a.tracks[i]);
                }
            }

        }

    }

}
