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


using System;
using System.ServiceModel;

namespace Jamcast.Plugins.GoogleMusic.UI.ViewModel
{
    internal class MainViewModel : ObservableObject
    {
        private static DuplexChannelFactory<IGoogleMusicWCFServices> _pipeFactory = new DuplexChannelFactory<IGoogleMusicWCFServices>(new Callbacks(),new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/PipeGoogleMusicForJamcast"));

        private object _currentView;
        private LoggedInViewModel _loggedInViewModel;
        private LoggedOutViewModel _loggedOutViewModel;
        private IGoogleMusicWCFServices _pipeProxy = _pipeFactory.CreateChannel();

        public MainViewModel()
        {
            string login = Configuration.Instance.Login;

            _loggedOutViewModel = new LoggedOutViewModel(_pipeProxy);
            _loggedOutViewModel.OnLoginSuccess += loggedOutViewModel_LoginSuccess;
            _loggedOutViewModel.IsLoggingIn = _pipeProxy.IsLoggingIn();
            _pipeProxy.SubscribeToCallback();
            if (!String.IsNullOrWhiteSpace(login))
                _loggedOutViewModel.Login = login;

            _loggedInViewModel = new LoggedInViewModel();
            _loggedInViewModel.OnLoggedOut += loggedInViewModel_LoggedOut;

            this.CurrentView = _pipeProxy.LoggedIn() ? (object)_loggedInViewModel : _loggedOutViewModel;
        }
        
        void loggedInViewModel_LoggedOut()
        {
            this.CurrentView = _loggedOutViewModel;
            _pipeProxy.Logout();
        }

        void loggedOutViewModel_LoginSuccess()
        {
            this.CurrentView = _loggedInViewModel;
            _pipeProxy.GetMusicData();
        }
                        
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged("CurrentView");
            }
        }

        public LoggedInViewModel CurrentLoggedInViewModel
        {
            get { return _loggedInViewModel; }
        }

        public LoggedOutViewModel CurrentLoggedOutViewModel
        {
            get { return _loggedOutViewModel; }
        }
    }

    internal class Callbacks : IGoogleMusicWCFCallbacks
    {
        public static MainViewModel CurrentModel { get; set; }

        public void OnLogin(int status)
        {
            if (status == GoogleMusicAPI.LOGIN_BUSY)
            {
                CurrentModel.CurrentLoggedOutViewModel.IsLoggingIn = true;
            }
            else
            {
                CurrentModel.CurrentLoggedOutViewModel.IsLoggingIn = false;
                if (status == GoogleMusicAPI.LOGIN_SUCCESS)
                    CurrentModel.CurrentView = CurrentModel.CurrentLoggedInViewModel;
                else
                    CurrentModel.CurrentView = CurrentModel.CurrentLoggedOutViewModel;
            }
        }
    }
}
