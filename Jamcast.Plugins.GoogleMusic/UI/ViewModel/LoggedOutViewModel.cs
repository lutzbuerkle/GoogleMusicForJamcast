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


using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Jamcast.Plugins.GoogleMusic.UI.ViewModel
{
    class LoggedOutViewModel : ObservableObject
    {
        private IGoogleMusicWCFServices _pipeProxy;
        private RelayCommand _logInCommand;
        private bool _isLoggingIn;
        private string _loginError;

        public string Login { get; set; }

        public event Action OnLoginSuccess;
        
        private LoggedOutViewModel()
        {            
            _logInCommand = new RelayCommand(logIn);            
        }

        public LoggedOutViewModel(IGoogleMusicWCFServices pipeProxy) : this()
        {
            _pipeProxy = pipeProxy;
        }

        public bool IsLoggingIn
        {
            get { return _isLoggingIn; }
            set
            {
                _isLoggingIn = value;
                this.OnPropertyChanged("IsLoggingIn");
            }
        }

        public string LoginError
        {
            get { return _loginError; }
            set
            {
                _loginError = value;
                this.OnPropertyChanged("LoginError");
            }
        }
        
        public ICommand LogInCommand
        {
            get
            {
                return _logInCommand;
            }
        }

        private void logIn(object password)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += worker_logIn;
            worker.RunWorkerAsync(password);
        }

        void worker_logIn(object sender, DoWorkEventArgs e)
        {
            var password = e.Argument as PasswordBox;
            this.IsLoggingIn = true;

            try
            {
                string login = this.Login.Trim();
                string passwd = password.Password;

                if (String.IsNullOrWhiteSpace(login))
                {
                    this.LoginError = "Email field cannot be left blank";
                    return;
                }

                if (String.IsNullOrWhiteSpace(passwd))
                {
                    this.LoginError = "Password field cannot be left blank";
                    return;
                }

                this.LoginError = String.Empty;
                int status = _pipeProxy.Login(login, passwd);
                if (status != GoogleMusicAPI.LOGIN_SUCCESS)
                {
                    if (status == GoogleMusicAPI.LOGIN_FAILURE__NO_INTERNET_CONNECTION)
                        this.LoginError = "No connection to the internet";
                    else
                        this.LoginError = "Google Music login failed";
                    Configuration.Instance.Password = null;
                    return;
                }

                Configuration.Instance.Login = login;
                Configuration.Instance.Password = passwd;
                Configuration.Instance.Save();
                this.OnLoginSuccess();
            }
            catch (Exception ex)
            {
                Configuration.Instance.Password = null;                
                this.LoginError = ex.Message;
                return;
            }
            finally
            {
                this.IsLoggingIn = false;
            }
        }
    }
}
