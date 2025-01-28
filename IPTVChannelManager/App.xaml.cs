using LibVLCSharp.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace IPTVChannelManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string[] _resourceNames;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings.Instance.LoadSetting();
        }

        public static string[] ResourceNames
        {
            get
            {
                if (_resourceNames == null)
                {
                    _resourceNames = GetResourceNames();
                }
                return _resourceNames;
            }
        }

        private static string[] GetResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resName = assembly.GetName().Name + ".g.resources";
            using (var stream = assembly.GetManifestResourceStream(resName))
            {
                using (var reader = new System.Resources.ResourceReader(stream))
                {
                    return reader.Cast<DictionaryEntry>().Select(entry =>
                             (string)entry.Key).ToArray();
                }
            }
        }
    }
}
