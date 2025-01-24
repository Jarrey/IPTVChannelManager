using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IPTVChannelManager.Common
{
    public interface ISettings
    {
        string Name { get; }
        string SettingScope { get; }
        void LoadSetting(string scope = null);
        void SaveSetting(string scope = null);

        event EventHandler<(string, object)> SettingChanged;
    }

    public abstract class AbstractSettings : ISettings
    {
        private static string SettingDirectoryPath(string scope) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Constants.AppName, scope);

        private static string SettingPath(string scope, string setting) => Path.Combine(SettingDirectoryPath(scope), $"{setting}.settings");

        public AbstractSettings()
        {
            InitSetting();
        }

        #region Properties
        public abstract string Name { get; }

        public virtual string SettingScope { get; protected set; }

        private ConcurrentDictionary<string, object> Settings { get; set; } = new ConcurrentDictionary<string, object>();

        public ICollection<string> SettingNames => Settings.Keys;
        #endregion Properties

        public virtual void LoadSetting(string scope = null)
        {
            if (string.IsNullOrWhiteSpace(SettingScope))
            {
                SettingScope = scope ?? string.Empty;
            }
            try
            {
                CreateDirectory(SettingScope);
                string settingPath = SettingPath(SettingScope, Name);
                if (File.Exists(settingPath))
                {
                    string settingContent = File.ReadAllText(settingPath);
                    Console.WriteLine($"Load setting {Name} from {settingPath}");
                    Dictionary<string, object> settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(settingContent);
                    if (settings != null)
                    {
                        foreach (KeyValuePair<string, object> s in settings)
                        {
                            // convert the de-serialized object to target setting type
                            Set(s.Key, SettingValueConverter(s.Key, s.Value), true);
                        }
                    }
                }
                // load and merge the user setting to current applciation settings, then save settings for persistence
                SaveSetting(SettingScope);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        public void SaveSetting(string scope = null)
        {
            if (string.IsNullOrWhiteSpace(SettingScope))
            {
                SettingScope = scope ?? string.Empty;
            }
            try
            {
                string settingPath = SettingPath(SettingScope, Name);
                File.WriteAllText(settingPath, JsonConvert.SerializeObject(Settings));
                Console.WriteLine($"Save setting {Name} to {settingPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex}");
            }
        }

        public event EventHandler<(string, object)> SettingChanged;

        protected virtual object SettingValueConverter(string _, object setting) => setting;

        protected abstract void InitSetting();

        #region Getter / Setter
        public T Get<T>(string key, T @default = default)
        {
            return Settings.TryGetValue(key, out object value) ? (T)(value?.ConvertTo<T>() ?? @default) : @default;
        }

        public object GetObject(string key, object @default = default)
        {
            return Settings.TryGetValue(key, out object value) ? value ?? @default : @default;
        }

        public IEnumerable<T> GetList<T>(string key, IEnumerable<T> @default = default)
        {
            if (Settings.TryGetValue(key, out object value))
            {
                if (value is IEnumerable<T> result)
                {
                    return result;
                }
                else
                {
                    var jtokens = value as IEnumerable<JToken>;
                    return jtokens?.Select(j => (T)(j?.ConvertTo<T>())) ?? @default;
                }
            }
            return @default;
        }

        public string Get(string key, string @default = "") => Get<string>(key, @default);

        public void Set(string key, object value, bool skipSave = false)
        {
            if (Settings.TryGetValue(key, out object exitValue) && Settings.TryUpdate(key, value, exitValue))
            {
                Console.WriteLine($"Set setting {key} = {value}");
                SettingChanged?.Invoke(this, (key, value));
                if (!skipSave)
                {
                    SaveSetting();
                }
            }
        }

        protected object this[string key]
        {
            set => Settings.AddOrUpdate(key, value, (k, v) => v);
        }
        #endregion

        private static void CreateDirectory(string scope)
        {
            string settingDirectoryPath = SettingDirectoryPath(scope);
            if (!Directory.Exists(settingDirectoryPath))
            {
                Directory.CreateDirectory(settingDirectoryPath);
            }
        }
    }
}
