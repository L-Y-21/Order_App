namespace OrderApp.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? TinNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Logo { get; set; }
        public string? TradeName { get; set; }
        public string? VatNumber { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public decimal? TaxRate { get; set; }
        public string? CurrencyCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
