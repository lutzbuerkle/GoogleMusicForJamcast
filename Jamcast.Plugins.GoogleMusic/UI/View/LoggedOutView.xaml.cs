using System;
using System.Windows.Controls;
using System.Windows.Input;
using Jamcast.Plugins.GoogleMusic.UI.ViewModel;

namespace Jamcast.Plugins.GoogleMusic.UI.View
{    
    public partial class LoggedOutView : UserControl
    {        
        public LoggedOutView()
        {
            InitializeComponent();                 
        }
                
        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            var model = this.DataContext as LoggedOutViewModel;
            model.LoginError = String.Empty;
            if (e.Key == Key.Enter)
                model.LogInCommand.Execute(this.txtPassword);
        }

        private void txtLogin_KeyDown(object sender, KeyEventArgs e)
        {
            var model = this.DataContext as LoggedOutViewModel;
            model.LoginError = String.Empty;
            if (e.Key == Key.Enter)
                model.LogInCommand.Execute(this.txtPassword);
        }
    }
}
