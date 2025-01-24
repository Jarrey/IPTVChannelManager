﻿using IPTVChannelManager.Common;
using Newtonsoft.Json;
using System.Linq;
using System.Web;

namespace IPTVChannelManager
{
    public class Channel : BindableBase
    {
        private string _name;
        private string _logo;
        private string _url;
        private string _group;
        private bool _ignore;

        public Channel(string name, string url, string group)
        {
            Name = name;
            Logo = name;
            Url = url;
            Group = group;
            Ignore = false;
        }

        [JsonProperty("name")]
        public string Name { get => _name; set => SetProperty(ref _name, value); }
        [JsonProperty("logo")]
        public string Logo
        {
            get => _logo;
            set
            {
                if (SetProperty(ref _logo, value))
                {
                    RaisePropertyChanged(nameof(LogoUrl));
                }
            }
        }
        [JsonProperty("url")]
        public string Url { get => _url; set => SetProperty(ref _url, value); }
        [JsonProperty("group")]
        public string Group { get => _group; set => SetProperty(ref _group, value); }
        [JsonProperty("ignore")]
        public bool Ignore { get => _ignore; set => SetProperty(ref _ignore, value); }
        [JsonIgnore]
        public string LogoUrl
        {
            get
            {
                string url = $"logos/{Logo}.png";
                if (App.ResourceNames.Any(name => HttpUtility.UrlDecode(name)?.ToLower() == HttpUtility.UrlDecode(url)?.ToLower()))
                {
                    return url;
                }
                return "logos/null.png";
            }
        }
    }
}
