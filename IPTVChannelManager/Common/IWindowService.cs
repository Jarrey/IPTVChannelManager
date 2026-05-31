using System.Collections.ObjectModel;
using IPTVChannelManager.Models;

namespace IPTVChannelManager.Common
{
    /// <summary>
    /// Abstracts window-navigation so ViewModels can request UI transitions
    /// without depending on concrete Window types.
    /// </summary>
    public interface IWindowService
    {
        void OpenSettingWindow();
        void OpenScannerWindow(ObservableCollection<Channel> channels);
        void OpenPlayerWindow(Channel channel);
        void OpenEpgGuideWindow(ObservableCollection<Channel> channels);
    }
}
