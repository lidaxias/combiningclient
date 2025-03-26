using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace combiningclient.Models
{
    public class SyncRequest
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public List<string> CurrencyCodes { get; set; }
        public string BaseUrl { get; set; }
    }

    public class ReportRequest
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public List<string> CurrencyCodes { get; set; }
        public string BaseUrl { get; set; }
    }

    public class ReportResponse
    {
        public List<CurrencyReport> Reports { get; set; }
    }
    public class CurrencyReport
    {
        public string CurrencyCode { get; set; }
        public decimal MinRate { get; set; }
        public decimal MaxRate { get; set; }
        public decimal AvgRate { get; set; }
    }
    public class Settings
    {
        public string BaseApiUrl { get; set; } = "http://localhost:5000";
        public TimeSpan SyncTime { get; set; } = TimeSpan.FromHours(0).Add(TimeSpan.FromMinutes(1));
        public List<string> DefaultCurrencies { get; set; } = new() { "USD", "EUR", "GBP" };

        public void Load()
        {
            // Здесь можно добавить загрузку из файла конфигурации
        }

        public void Save()
        {
            // Здесь можно добавить сохранение в файл конфигурации
        }
    }


}
