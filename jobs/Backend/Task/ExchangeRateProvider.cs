using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace ExchangeRateUpdater
{
    public class ExchangeRateProvider
    {
        /// <summary>
        /// Should return exchange rates among the specified currencies that are defined by the source. But only those defined
        /// by the source, do not return calculated exchange rates. E.g. if the source contains "CZK/USD" but not "USD/CZK",
        /// do not return exchange rate "USD/CZK" with value calculated as 1 / "CZK/USD". If the source does not provide
        /// some of the currencies, ignore them.
        /// </summary>
        public async Task<IEnumerable<ExchangeRate>> GetExchangeRatesAsync(IEnumerable<Currency> currencies)
        {
            string baseUrl = "https://www.cnb.cz/en/financial-markets/foreign-exchange-market/central-bank-exchange-rate-fixing/central-bank-exchange-rate-fixing/selected.txt";

            var exchangeRates = new List<ExchangeRate>();

            var now = DateTime.Today;
            string from = now.AddMonths(-1).ToString("dd.MM.yyyy"); //last 30 days rates
            string to = now.ToString("dd.MM.yyyy");
            Currency sourceCurrency = new Currency("CZK");

            using (HttpClient httpClient = new HttpClient())
            {
                foreach (var currency in currencies)
                {

                    string url = $"{baseUrl}?from={from}&to={to}&currency={currency.Code}&format=text"; 

                    try
                    {
                        string resp = await httpClient.GetStringAsync(url);

                        if (string.IsNullOrWhiteSpace(resp))
                            continue;

                        Currency targetCurrency = new Currency(currency.Code);

                        var rawData = resp.Split(new[] { '\n', '|' }, StringSplitOptions.RemoveEmptyEntries); 

                        string amountString = rawData.Single(x => x.Contains("Amount")).Split(':').Last().Trim(); //Format is "Amount: 100"

                        decimal value = decimal.Parse(rawData.Last(), CultureInfo.InvariantCulture) / int.Parse(amountString); //rawData.Last() is most recent value

                        exchangeRates.Add(new ExchangeRate(sourceCurrency, targetCurrency, value));

                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"HTTP error for {currency.Code}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Generic error for {currency.Code}: {ex.Message}");
                    }

                }

            }

            return exchangeRates;
        }
    }
}