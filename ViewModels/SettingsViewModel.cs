using combiningclient.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace combiningclient.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly Settings _settings;

        public Settings Settings => _settings;

        public string SyncTimeString
        {
            get => _settings.SyncTime.ToString(@"hh\:mm");
            set
            {
                if (TimeSpan.TryParse(value, out var time))
                {
                    _settings.SyncTime = time;
                }
            }
        }
        public string DefaultCurrenciesString
        {
            get => string.Join(",", _settings.DefaultCurrencies);
            set => _settings.DefaultCurrencies = value.Split(',')
                .Select(c => c.Trim())
                .ToList();
        }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public SettingsViewModel(Settings settings)
        {
            _settings = settings;
            SaveCommand = ReactiveCommand.Create(SaveSettings);
        }

        private void SaveSettings()
        {
            _settings.Save();
        }
    }
}
