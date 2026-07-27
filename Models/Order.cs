namespace OrderApp.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerNameSnapshot { get; set; }
        public string? CustomerPhoneSnapshot { get; set; }
        public string? CustomerAddressSnapshot { get; set; }
        public string Status { get; set; } = "draft";
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int? VoucherId { get; set; }
        public string? VoucherCodeSnapshot { get; set; }
        public string? Notes { get; set; }
        public string? Fs { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? ItemId { get; set; }
        public string ItemNameSnapshot { get; set; } = string.Empty;
        public string? ItemSkuSnapshot { get; set; }
        public string UomSnapshot { get; set; } = string.Empty;
        public decimal UnitPriceSnapshot { get; set; }
        public decimal TaxRateSnapshot { get; set; }
        public string TaxTypeSnapshot { get; set; } = "exclusive";
        public decimal Quantity { get; set; }
        public decimal LineSubtotal { get; set; }
        public decimal LineTaxAmount { get; set; }
        public decimal LineDiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "percentage";
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public int? MaxUsesPerCustomer { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class TopSellingItem
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class OrderStatusSummary
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalSales { get; set; }
    }
}
