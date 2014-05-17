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


using Jamcast.Extensibility.MediaServer;
using Jamcast.Extensibility.Metadata;
using System;
using System.IO;
using System.Net;

namespace Jamcast.Plugins.GoogleMusic
{

    [MediaRequestHandler("553A4801-A2D7-49C6-A78E-9847290D381E")]
    public class TrackHandler : MediaRequestHandler
    {
        public override RequestInitializationResult Initialize()
        {
            AudioRequestInitializationResult result = new AudioRequestInitializationResult();
            result.InputProperties = new AudioStreamProperties(MediaFormats.MP3);
            result.CanProceed = true;
            result.IsConversion = this.Context.Format != MediaFormats.MP3;
            result.SupportsSeeking = false;

            return result;
        }

        public override DataPipeBase RetrieveMedia()
        {
            string song_id = Context.Data[0];
            string url;

            url = GoogleMusicAPI.Instance.GetStreamUrl(song_id);

            if (String.IsNullOrEmpty(url))
                throw new BadMediaRequestException(String.Format("Track is unavailable (song_id: {0}).", song_id));

            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "GET";
                request.Proxy = null;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();

                return new StreamDataPipe("Google Music Stream", stream);
            }
            catch
            {
                throw new BadMediaRequestException(String.Format("Track is unavailable (song_id: {0}).", song_id));
            }
        }
    }

}
