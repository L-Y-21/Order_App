using OrderApp.Data;
using OrderApp.Models;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace OrderApp.Services
{
    public class PrinterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public bool IsOnline { get; set; }
        public bool IsDefault { get; set; }
    }

    public class PrinterManager
    {
        private readonly DBManager _db;
        private List<PrinterInfo>? _cachedPrinters;
        private DateTime _lastCacheTime = DateTime.MinValue;

        public PrinterManager(DBManager db)
        {
            _db = db;
        }

        public async Task<bool> CheckPrinterByIpAsync(string ipAddress)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 2000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PrinterInfo>> GetAvailablePrintersAsync()
        {
            // Cache for 30 seconds to avoid excessive checks
            if (_cachedPrinters != null && (DateTime.Now - _lastCacheTime).TotalSeconds < 30)
            {
                return _cachedPrinters;
            }

            var printers = new List<PrinterInfo>();
            var defaultPrinter = new PrintDocument().PrinterSettings.PrinterName;

            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                var printerInfo = new PrinterInfo
                {
                    Name = printerName,
                    IsDefault = printerName == defaultPrinter
                };

                // Try to extract IP from printer name or settings
                var ip = ExtractIpFromPrinterName(printerName);
                if (!string.IsNullOrEmpty(ip))
                {
                    printerInfo.IpAddress = ip;
                    printerInfo.IsOnline = await CheckPrinterByIpAsync(ip);
                }
                else
                {
                    printerInfo.IsOnline = true; // Assume online if no IP
                }

                printers.Add(printerInfo);
            }

            _cachedPrinters = printers;
            _lastCacheTime = DateTime.Now;
            return printers;
        }

        private string? ExtractIpFromPrinterName(string printerName)
        {
            // Try to extract IP address from printer name (common patterns)
            var ipPattern = @"\b(?:\d{1,3}\.){3}\d{1,3}\b";
            var match = System.Text.RegularExpressions.Regex.Match(printerName, ipPattern);
            return match.Success ? match.Value : null;
        }

        public async Task<bool> IsPrinterAvailableAsync(string printerName)
        {
            var printers = await GetAvailablePrintersAsync();
            var printer = printers.FirstOrDefault(p => p.Name == printerName);
            
            if (printer == null)
                return false;

            if (!string.IsNullOrEmpty(printer.IpAddress))
                return printer.IsOnline;

            return true;
        }

        public string RenderTemplate(string template, Dictionary<string, object> data)
        {
            var output = template;

            // Handle {{#each array}}...{{/each}} blocks
            output = System.Text.RegularExpressions.Regex.Replace(
                output,
                @"{{#each (\w+)}}([\s\S]*?){{/each}}",
                match =>
                {
                    var arrayKey = match.Groups[1].Value;
                    var blockTemplate = match.Groups[2].Value;
                    
                    if (data.TryGetValue(arrayKey, out var value) && value is System.Collections.IEnumerable enumerable)
                    {
                        var result = new System.Text.StringBuilder();
                        foreach (var item in enumerable)
                        {
                            if (item is Dictionary<string, object> dict)
                            {
                                result.Append(RenderSimple(blockTemplate, dict));
                            }
                        }
                        return result.ToString();
                    }
                    return "";
                }
            );

            // Handle simple {{field}} at top level
            output = RenderSimple(output, data);
            return output;
        }

        private string RenderSimple(string template, Dictionary<string, object> data)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                template,
                @"{{(\w+(?:\.\w+)*)}}",
                match =>
                {
                    var path = match.Groups[1].Value;
                    var value = GetValueFromPath(data, path);
                    return value?.ToString() ?? "";
                }
            );
        }

        private object? GetValueFromPath(Dictionary<string, object> data, string path)
        {
            var parts = path.Split('.');
            object? current = data;

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue(part, out var value))
                    {
                        current = value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        public Dictionary<string, object> BuildInvoiceContext(Order order, string? operatorName = null)
        {
            return new Dictionary<string, object>
            {
                ["order_number"] = order.OrderNumber,
                ["order_date"] = order.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                ["status"] = order.Status,
                ["customer_name"] = order.CustomerNameSnapshot ?? "Walk-in Customer",
                ["customer_phone"] = order.CustomerPhoneSnapshot ?? "",
                ["customer_address"] = order.CustomerAddressSnapshot ?? "",
                ["operator"] = operatorName ?? "System",
                ["items"] = order.Items.Select(item => new Dictionary<string, object>
                {
                    ["name"] = item.ItemNameSnapshot,
                    ["uom"] = item.UomSnapshot,
                    ["quantity"] = item.Quantity,
                    ["unit_price"] = item.UnitPriceSnapshot.ToString("F2"),
                    ["line_total"] = item.LineTotal.ToString("F2")
                }).ToList(),
                ["subtotal"] = order.Subtotal.ToString("F2"),
                ["tax_amount"] = order.TaxAmount.ToString("F2"),
                ["discount_amount"] = order.DiscountAmount.ToString("F2"),
                ["total_amount"] = order.TotalAmount.ToString("F2"),
                ["voucher_code"] = order.VoucherCodeSnapshot ?? ""
            };
        }



public async Task<(bool Success, string Message)> PrintInvoiceAsync(Order order, string printerName = "", string? operatorName = null)
    {
        // Check printer availability
        if (!string.IsNullOrEmpty(printerName))
        {
            var isAvailable = await IsPrinterAvailableAsync(printerName);
            if (!isAvailable)
            {
                return (false, $"Printer '{printerName}' is not available or offline");
            }
        }

        if (string.IsNullOrEmpty(printerName))
        {
            return (false, "A printer name is required for raw ESC/POS printing");
        }

        try
        {
            // Get company info from database
            var company = await _db.GetCompanyAsync();

            byte[] receiptBytes = BuildEscPosReceipt(order, company, operatorName);

            bool sent = RawPrinterHelper.SendBytesToPrinter(printerName, receiptBytes);

            if (!sent)
            {
                return (false, "Failed to send print job to printer");
            }

            return (true, "Invoice printed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Printing failed: {ex.Message}");
        }
    }

        private byte[] BuildEscPosReceipt(Order order, Company? company, string? operatorName)
        {
            const int LineWidth = 47;
            const int DoubleWidthLineWidth = 23; // half, since double-width chars take 2x physical space
            var sb = new List<byte>();
            var enc = Encoding.ASCII; // Generic/Text-Only drivers usually pass ASCII/OEM bytes straight through

            // ---- ESC/POS command helpers ----
            void Cmd(params byte[] bytes) => sb.AddRange(bytes);
            void Text(string text) => sb.AddRange(enc.GetBytes(text));
            void Line(string text = "") { Text(text); Cmd(0x0A); } // \n

            void Init() => Cmd(0x1B, 0x40);                         // ESC @  - initialize printer
            void BoldOn() => Cmd(0x1B, 0x45, 0x01);                  // ESC E 1
            void BoldOff() => Cmd(0x1B, 0x45, 0x00);                 // ESC E 0
            void AlignLeft() => Cmd(0x1B, 0x61, 0x00);               // ESC a 0
            void AlignCenter() => Cmd(0x1B, 0x61, 0x01);             // ESC a 1
            void AlignRight() => Cmd(0x1B, 0x61, 0x02);              // ESC a 2
            void FontA() => Cmd(0x1B, 0x4D, 0x00);                   // ESC M 0 - Font A (normal)
            void FontB() => Cmd(0x1B, 0x4D, 0x01);                   // ESC M 1 - Font B (condensed)
            void NormalSize() => Cmd(0x1D, 0x21, 0x00);              // GS ! 0  - normal width/height
            void DoubleHeight() => Cmd(0x1D, 0x21, 0x01);            // GS ! 1  - double height only
            void DoubleWidthHeight() => Cmd(0x1D, 0x21, 0x11);       // GS ! 0x11 - double width + height
            void FeedLines(int n) => Cmd(0x1B, 0x64, (byte)n);       // ESC d n
            void FullCut() => Cmd(0x1D, 0x56, 0x00);                 // GS V 0

            void LightFont() => Cmd(0x1B, 0x4D, 0x01);   // ESC M 1 - Font B (lighter)
            void NormalFont() => Cmd(0x1B, 0x4D, 0x00);  // ESC M 0 - Font A (normal)
            void DoubleWidthHeight1()=> Cmd(0x1D, 0x21, 0x10);

            string Center(string text, int width = LineWidth)
            {
                if (text.Length >= width) return text.Substring(0, width);
                int totalPad = width - text.Length;
                int left = totalPad / 2;
                int right = totalPad - left;
                return new string(' ', left) + text + new string(' ', right);
            }

            string TwoCol(string left, string right, int width = LineWidth)
            {
                int spaces = width - left.Length - right.Length;
                if (spaces < 1) spaces = 1;
                return left + new string(' ', spaces) + right;
            }

            var tin = company?.TinNumber ?? "xxxxxxx";
            var companyName = company?.CompanyName ?? "Company Name Not Set";
            var address = company?.AddressLine1 ?? "";
            var address2 = company?.AddressLine2 ?? "";
            var ercaCode = company?.VatNumber ?? "FGK0011500";

            // ---- Build receipt ----
            Init();
            FontA();
            NormalSize();

            // TIN - centered, bold
            AlignCenter();
            //BoldOn();
            Line($"TIN: {tin}");
            //BoldOff();

            Line(new string('-', LineWidth));
            //Line();

            // Company info - centered
            BoldOn();
            Line(Center(companyName));
            BoldOff();

            //Line();
            if (!string.IsNullOrEmpty(address)) Line(Center(address));
            if (!string.IsNullOrEmpty(address2)) Line(Center(address2));
            Line();

            AlignLeft();

            Line(TwoCol($"FS NO.: {order.Fs}", $"DATE : {order.OrderDate:dd/MM/yyyy HH:mm}"));
            Line();

            var leftEquals = new string('=', 18);
            var rightEquals = new string('=', 15);
            Line($"{leftEquals} CASH INVOICE {rightEquals}");
            Line();

            Line($"Customer: {order.CustomerNameSnapshot ?? "Walk-in Customer"}");
            Line($"Ref.: {order.OrderNumber}");
            Line($"Operator: {operatorName ?? "System"}");
            Line();

            Line(new string('-', LineWidth));
            Line("# Description       Qty Price          Total");
            Line(new string('-', LineWidth));

            foreach (var item in order.Items)
            {
                var nameRaw = item.ItemNameSnapshot ?? "";
                var desc = nameRaw.Length > 18 ? nameRaw.Substring(0, 18) : nameRaw.PadRight(18);
                var line = $"{desc} {item.Quantity,3:F0}     {item.UnitPriceSnapshot,6:F3}      *{item.LineTotal,6:F2}";
                Line(line);
            }

            Line(new string('-', LineWidth));
            Line(TwoCol($"TAXBL {order.Items.Count()}", $"*{order.Subtotal,6:F2}"));
            Line(TwoCol("TAX1 15%", $"*{order.TaxAmount,6:F2}"));
            Line(new string('-', LineWidth));

            // TOTAL - big, same row (uses half-width column since chars are double-wide)
            DoubleWidthHeight();
            Line(TwoCol("TOTAL", $"*{order.TotalAmount,6:F2}", DoubleWidthLineWidth));
            NormalSize();
            Line();

            Line(new string('-', LineWidth));

            // CREDIT# - big, bold, same row
            //BoldOn();
            DoubleWidthHeight1();
            Line(TwoCol("CASH#", $"*{order.TotalAmount,6:F2}", DoubleWidthLineWidth));
            NormalSize();
            BoldOff();
            Line($"ITEM #");
            Line();
            Line();

            //// ERCA block - centered, bold, double-height, with symbol
            AlignCenter();
            BoldOn();
            //DoubleHeight();
            //Line("\x10 ERCA");
            NormalSize();
            BoldOff();
            //Line(ercaCode);
            Line();
            AlignLeft();

            // Feed + cut
            FeedLines(8);
            FullCut();

            return sb.ToArray();
        }
        private void SendFeedAndCut(string printerName)
        {
            try
            {
                byte[] feedAndCut = new byte[]
                {
                    0x1B, 0x64, 0x0A,   // ESC d 5  -> feed 5 lines
                    0x1D, 0x56, 0x00    // GS V 0   -> full cut
                };
                RawPrinterHelper.SendBytesToPrinter(printerName, feedAndCut);
            }
            catch
            {
                
            }
        }
        public static class RawPrinterHelper
        {
            [StructLayout(LayoutKind.Sequential)]
            public class DOCINFOA
            {
                [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
                [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
                [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
            }

            [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
            public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

            [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            IntPtr hPrinter;
            var di = new DOCINFOA
            {
                pDocName = "Raw ESC/POS Job",
                pDataType = "RAW"
            };

            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return false;

            bool success = false;
            try
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                        Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);
                        success = WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out _);
                        Marshal.FreeCoTaskMem(unmanagedBytes);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
            return success;
        }
    }

}

}

