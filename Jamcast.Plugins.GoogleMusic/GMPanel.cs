﻿/*
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


using Jamcast.Extensibility;
using System;
using System.Windows.Forms;

namespace Jamcast.Plugins.GoogleMusic
{

    public partial class GMPanel : ConfigurationPanel {
        
        public GMPanel() {

            InitializeComponent();
                                                
        }

        public override string DisplayName {

            get { return "Google Music"; }

        }

        protected override void OnServiceAvailable() {

            refresh();

        }

        private void refresh() {

            lblStatus.Text = String.Format("The Google Music plugin is {0}.", Configuration.Instance.IsEnabled ? "enabled" : "disabled");
            cmdEnable.Text = Configuration.Instance.IsEnabled ? "Disable" : "Enable";

        }
            
        private void cmdEnable_Click(object sender, EventArgs e) {

            if (Configuration.Instance.IsEnabled) {

                if (MessageBox.Show("Google Music content will no longer be available.  Are you sure you want to continue?", "Confirm Disable Plugin", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                Configuration.Instance.IsEnabled = false;
                Configuration.Instance.Password = null;

            } else {

                LoginForm frm = new LoginForm();

                if (frm.ShowDialog(this) != DialogResult.OK)
                    return;

                Configuration.Instance.Login = frm.Login.Trim();
                Configuration.Instance.Password = frm.Password.Trim();
                Configuration.Instance.IsEnabled = true;
                
            }

            Configuration.Instance.Save();

            refresh();

            this.RequestRestart();

        }

    }

}
