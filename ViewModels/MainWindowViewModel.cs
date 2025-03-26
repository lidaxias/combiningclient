using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Avalonia.Controls;
using System;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using System.Windows.Input;
using System.Reactive;
using combiningclient.Models;

namespace combiningclient.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly HttpClient _httpClient = new();
        private readonly Settings _settings;

        private DateTimeOffset _startDate = DateTimeOffset.Now.AddDays(-7);
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        private DateTimeOffset _endDate = DateTimeOffset.Now;
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        private string _currencyCodes = "USD,EUR,GBP";
        public string CurrencyCodes
        {
            get => _currencyCodes;
            set => this.RaiseAndSetIfChanged(ref _currencyCodes, value);
        }

        private string _reportResult = "";
        public string ReportResult
        {
            get => _reportResult;
            set => this.RaiseAndSetIfChanged(ref _reportResult, value);
        }

        // Команды
        public ReactiveCommand<Unit, Unit> SyncCommand { get; }
        public ReactiveCommand<Unit, Unit> GetReportCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

        public MainWindowViewModel()
        {
            _settings = new Settings();
            _settings.Load();

            SyncCommand = ReactiveCommand.CreateFromTask(SyncData);
            GetReportCommand = ReactiveCommand.CreateFromTask(GetReport);
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
        }

        private async Task SyncData()
        {
            try
            {
                var currencies = CurrencyCodes.Split(',')
                    .Select(c => c.Trim())
                    .ToList();

                var request = new SyncRequest
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    CurrencyCodes = currencies,
                    BaseUrl = _settings.BaseApiUrl
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_settings.BaseApiUrl}/api/exchangerates/sync",
                    content);

                ReportResult = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ReportResult = $"Ошибка синхронизации: {ex.Message}";
            }
        }

        private async Task GetReport()
        {
            try
            {
                var currencies = CurrencyCodes.Split(',')
                    .Select(c => c.Trim())
                    .ToList();

                var request = new ReportRequest
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    CurrencyCodes = currencies,
                    BaseUrl = _settings.BaseApiUrl
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_settings.BaseApiUrl}/api/exchangerates/report",
                    content);

                var result = await response.Content.ReadAsStringAsync();
                var report = JsonSerializer.Deserialize<ReportResponse>(result);

                var sb = new StringBuilder();
                sb.AppendLine($"Отчет за период с {StartDate:d} по {EndDate:d}");
                sb.AppendLine("====================================");

                foreach (var currency in report.Reports)
                {
                    sb.AppendLine($"{currency.CurrencyCode}:");
                    sb.AppendLine($"  Мин: {currency.MinRate:N2}");
                    sb.AppendLine($"  Макс: {currency.MaxRate:N2}");
                    sb.AppendLine($"  Средн: {currency.AvgRate:N2}");
                    sb.AppendLine();
                }

                ReportResult = sb.ToString();
            }
            catch (Exception ex)
            {
                ReportResult = $"Ошибка получения отчета: {ex.Message}";
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow
            {
                DataContext = new SettingsViewModel(_settings)
            };
            settingsWindow.Show();
        }
    }
}
    