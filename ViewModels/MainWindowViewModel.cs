using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using Avalonia.Controls.Shapes;
using ReactiveUI;
using System.Reactive;
using combiningclient.Models;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.IO;


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
                    StartDate = StartDate.DateTime,  // Исправлено: DateTimeOffset → DateTime
                    EndDate = EndDate.DateTime,
                    CurrencyCodes = currencies
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                //var test = $"{_settings.BaseApiUrl}/api/exchangerates/sync";
                var response = await _httpClient.PostAsync(
                    $"{_settings.BaseApiUrl}/api/exchangerates/sync",
                    content);

                response.EnsureSuccessStatusCode();  // Генерирует исключение при ошибке HTTP
                ReportResult = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                ReportResult = $"Ошибка сети: {ex.Message}";
            }
            catch (Exception ex)
            {
                ReportResult = $"Ошибка: {ex.Message}";
            }
        }

        private async Task GetReport()
        {
            try
            {
                // 1. Подготовка данных
                var currencies = CurrencyCodes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToUpper())
                    .Distinct()
                    .ToList();

                if (!currencies.Any())
                {
                    ReportResult = "Ошибка: не указаны коды валют";
                    return;
                }

                // 2. Формирование запроса
                var request = new ReportRequest
                {
                    StartDate = StartDate.Date,
                    EndDate = EndDate.Date,
                    CurrencyCodes = currencies
                };

                // 3. Запрос данных с сервера
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_settings.BaseApiUrl}/api/exchangerates/report",
                    request);

                response.EnsureSuccessStatusCode();
                var reportData = await response.Content.ReadFromJsonAsync<ReportResponse>();

                // 4. Нормализация курсов (для Amount = 1)
                var normalizedData = reportData?.Reports?
                    .Select(r => new
                    {
                        Валюта = r.CurrencyCode,
                        МинимальныйКурс = r.MinRate,
                        МаксимальныйКурс = r.MaxRate,
                        СреднийКурс = r.AvgRate
      
                    })
                    .ToList();

                // 5. Формирование JSON
                var jsonReport = new
                {
                    Метаданные = new
                    {
                        Сгенерирован = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Период = $"{StartDate:yyyy-MM-dd} - {EndDate:yyyy-MM-dd}",
                        Валюты = currencies
                    },
                    Данные = normalizedData
                };

                // 6. Сохранение в файл
                var fileName = $"Отчет_Курсы_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var downloadsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    fileName);

                await File.WriteAllTextAsync(
                    downloadsPath,
                    JsonSerializer.Serialize(jsonReport, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }));

                // 7. Формирование текста для TextBox
                var reportText = new StringBuilder();
                reportText.AppendLine($"ОТЧЕТ О КУРСАХ ВАЛЮТ");
                reportText.AppendLine($"Период: {StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}");
                reportText.AppendLine($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}");
                reportText.AppendLine(new string('=', 50));

                if (normalizedData?.Any() == true)
                {
                    foreach (var currency in normalizedData)
                    {
                        reportText.AppendLine($"{currency.Валюта}:");
                        reportText.AppendLine($"  Мин: {currency.МинимальныйКурс:N4}");
                        reportText.AppendLine($"  Макс: {currency.МаксимальныйКурс:N4}");
                        reportText.AppendLine($"  Средн: {currency.СреднийКурс:N4}");
                        reportText.AppendLine();
                    }
                    reportText.AppendLine($"Файл отчёта сохранён: {downloadsPath}");
                }
                else
                {
                    reportText.AppendLine("Нет данных за указанный период");
                }

                ReportResult = reportText.ToString();
            }
            catch (Exception ex)
            {
                ReportResult = $"Ошибка при формировании отчёта: {ex.Message}";
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
    