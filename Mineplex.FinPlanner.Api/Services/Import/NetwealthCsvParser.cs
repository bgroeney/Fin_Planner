using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Mineplex.FinPlanner.Api.Models.Import;

namespace Mineplex.FinPlanner.Api.Services.Import
{
    public class NetwealthCsvParser : INetwealthCsvParser
    {
        public ImportPreviewDto Parse(string csvContent)
        {
            if (csvContent.Contains("Portfolio Valuation - Detail"))
            {
                return ParsePortfolioValuation(csvContent);
            }
            if (csvContent.Contains("Cash Transaction Listing - Detail"))
            {
                return ParseTransactionListing(csvContent);
            }

            throw new ArgumentException("Unknown file format. Please upload a valid Netwealth CSV.");
        }

        private ImportPreviewDto ParsePortfolioValuation(string csvContent)
        {
            var reader = new StringReader(csvContent);
            var line = "";
            var startLine = 0;
            var accountNum = "";
            var accountName = "";

            // Scan for metadata and header
            while ((line = reader.ReadLine()) != null)
            {
                startLine++;
                // Rudimentary metadata parsing
                if (line.StartsWith("\"Account name\""))
                {
                    // "Account name","Start date"..., "Client name", ... "Account number"
                    // Line 4 has the values
                    var valuesLine = reader.ReadLine();
                    startLine++;

                    if (valuesLine == null) continue;

                    var parts = valuesLine.Split("\",\"");
                    if (parts.Length > 6)
                    {
                        accountName = parts[0].Trim('"');
                        accountNum = parts[6].Trim('"');
                    }

                    // Try to parse valuation date from End Date (typically 3rd column)
                    if (parts.Length > 2 && DateTime.TryParseExact(parts[2].Trim('"'), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var valDate))
                    {
                        // Use this as the valuation date for the file
                    }
                }

                if (line.StartsWith("\"Asset\",\"Code\""))
                {
                    break;
                }
            }

            // Reset reader to just before the header
            reader = new StringReader(csvContent);
            for (int i = 0; i < startLine - 1; i++) reader.ReadLine();

            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

            var records = new List<ImportRecordDto>();
            if (csv.Read() && csv.ReadHeader())
            {
                while (csv.Read())
                {
                    var asset = csv.GetField("Asset");
                    if (string.IsNullOrWhiteSpace(asset) || asset == "Total Portfolio") continue; // Skip totals or empty lines

                    var currentUnits = csv.GetField<decimal?>("Current units") ?? 0;
                    var value = csv.GetField<decimal?>("Value $") ?? 0;

                    records.Add(new ImportRecordDto
                    {
                        Asset = asset ?? "Unknown",
                        Code = csv.GetField("Code") ?? string.Empty,
                        Units = currentUnits,
                        Amount = value,
                        AssetClass = csv.GetField("Asset class") ?? string.Empty,
                        Type = "Holding"
                    });
                }
            }

            return new ImportPreviewDto
            {
                FileType = "PortfolioValuation",
                AccountNumber = accountNum,
                AccountName = accountName,
                TotalRecords = records.Count,
                PreviewRecords = records,
                // We'll set the date in ConfirmImportAsync or via updated parsing logic if needed
            };
        }

        private ImportPreviewDto ParseTransactionListing(string csvContent)
        {
            var reader = new StringReader(csvContent);
            var line = "";
            var startLine = 0;
            var accountNum = "";
            var accountName = "";

            while ((line = reader.ReadLine()) != null)
            {
                startLine++;
                if (line.StartsWith("\"Client name\"")) accountName = line.Split(',')[1].Trim('"');
                if (line.StartsWith("\"Account number\"")) accountNum = line.Split(',')[1].Trim('"');

                if (line.StartsWith("\"Effective Date\",\"Description\""))
                {
                    break;
                }
            }

            // Re-read from header
            reader = new StringReader(csvContent);
            for (int i = 0; i < startLine - 1; i++) reader.ReadLine();

            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

            var records = new List<ImportRecordDto>();
            if (csv.Read() && csv.ReadHeader())
            {
                while (csv.Read())
                {
                    var dateStr = csv.GetField("Effective Date");
                    if (!DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                    var debits = csv.GetField<decimal?>("Debits") ?? 0;
                    var credits = csv.GetField<decimal?>("Credits") ?? 0;

                    records.Add(new ImportRecordDto
                    {
                        Date = date,
                        Type = csv.GetField("TransactionListing Summary Group") ?? "Unknown",
                        Asset = csv.GetField("Asset") ?? "Unknown Asset",
                        Code = csv.GetField("Code") ?? "",
                        Units = csv.GetField<decimal?>("Units") ?? 0,
                        Amount = credits - debits, // Positive for inflow, negative for outflow roughly
                        AssetClass = "" // Not in this file mostly
                    });
                }
            }

            return new ImportPreviewDto
            {
                FileType = "TransactionListing",
                AccountNumber = accountNum,
                AccountName = accountName,
                TotalRecords = records.Count,
                PreviewRecords = records
            };
        }
    }
}
