namespace OrderApp.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string? Sku { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public int BaseUomId { get; set; }
        public decimal CurrentPrice { get; set; }
        public int? TaxRateId { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public string? CategoryName { get; set; }
        public string? BaseUomCode { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public string? TaxType { get; set; }
        public List<ItemUom> AltUoms { get; set; } = new();
    }

    public class ItemUom
    {
        public int UomId { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public decimal ConversionFactor { get; set; }
        public decimal? PriceOverride { get; set; }
        public bool IsDefault { get; set; }
    }
}
