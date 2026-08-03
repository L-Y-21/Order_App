namespace OrderApp.Models
{
    public class OrderPrinter
    {
        public int Id { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}