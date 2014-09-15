using Jamcast.Plugins.GoogleMusic.UI.ViewModel;
using Jamcast.Extensibility.UI;

namespace Jamcast.Plugins.GoogleMusic.UI.View
{
    /// <summary>
    /// Interaction logic for GoogleMusicPanel.xaml
    /// </summary>
    public partial class GoogleMusicPanel : ConfigurationPanel
    {
        public GoogleMusicPanel()
        {
            InitializeComponent();
            MainViewModel model = new MainViewModel();
            Callbacks.CurrentModel = model;
            this.DataContext = model;
        }
    }
}
