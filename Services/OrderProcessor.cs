using OrderApp.Data;
using OrderApp.Models;

namespace OrderApp.Services
{
    public class OrderProcessor
    {
        private readonly DBManager _db;

        public OrderProcessor(DBManager db)
        {
            _db = db;
        }

        public async Task<OrderItem> CalculateLineItemAsync(int itemId, decimal quantity, int uomId)
        {
            var item = await _db.GetItemAsync(itemId);
            if (item == null)
                throw new Exception($"Item {itemId} not found");

            var unitPrice = item.CurrentPrice; // Simplified - would use UOM converter in full implementation
            var taxRatePercent = item.TaxRatePercent ?? 0;
            var taxType = item.TaxType ?? "exclusive";

            var grossLineAmount = unitPrice * quantity;
            decimal lineSubtotal, lineTax;

            if (taxType == "inclusive")
            {
                // unitPrice already includes tax -> back it out
                lineSubtotal = grossLineAmount / (1 + taxRatePercent / 100);
                lineTax = grossLineAmount - lineSubtotal;
            }
            else
            {
                // exclusive -> tax added on top
                lineSubtotal = grossLineAmount;
                lineTax = grossLineAmount * (taxRatePercent / 100);
            }

            return new OrderItem
            {
                ItemId = itemId,
                ItemNameSnapshot = item.Name,
                ItemSkuSnapshot = item.Sku,
                UomSnapshot = item.BaseUomCode ?? uomId.ToString(),
                UnitPriceSnapshot = Math.Round(unitPrice, 2),
                TaxRateSnapshot = taxRatePercent,
                TaxTypeSnapshot = taxType,
                Quantity = quantity,
                LineSubtotal = Math.Round(lineSubtotal, 2),
                LineTaxAmount = Math.Round(lineTax, 2),
                LineDiscountAmount = 0,
                LineTotal = Math.Round(lineSubtotal + lineTax, 2)
            };
        }

        public async Task<List<OrderItem>> CalculateLineItemsAsync(List<(int ItemId, decimal Quantity, int UomId)> lines)
        {
            var calculated = new List<OrderItem>();
            foreach (var (itemId, quantity, uomId) in lines)
            {
                calculated.Add(await CalculateLineItemAsync(itemId, quantity, uomId));
            }
            return calculated;
        }

        public async Task<(bool Valid, string? Reason, Voucher? Voucher)> ValidateVoucherAsync(string code, decimal subtotal, int? customerId = null)
        {
            var voucher = await _db.GetVoucherByCodeAsync(code);
            if (voucher == null)
                return (false, "Voucher code not found or inactive", null);

            var now = DateTime.Now;
            if (now < voucher.ValidFrom)
                return (false, "Voucher is not yet valid", null);
            if (now > voucher.ValidUntil)
                return (false, "Voucher has expired", null);

            if (subtotal < voucher.MinOrderAmount)
                return (false, $"Order must be at least {voucher.MinOrderAmount} to use this voucher", null);

            if (voucher.MaxUses != null && voucher.UsedCount >= voucher.MaxUses)
                return (false, "Voucher usage limit reached", null);

            return (true, null, voucher);
        }

        public decimal CalculateDiscount(Voucher voucher, decimal subtotal)
        {
            decimal discount;
            if (voucher.DiscountType == "percentage")
            {
                discount = subtotal * (voucher.DiscountValue / 100);
                if (voucher.MaxDiscountAmount != null)
                    discount = Math.Min(discount, voucher.MaxDiscountAmount.Value);
            }
            else
            {
                discount = voucher.DiscountValue;
            }
            // never discount more than the subtotal itself
            discount = Math.Min(discount, subtotal);
            return Math.Round(discount, 2);
        }

        public async Task<OrderCalculationResult> CalculateOrderAsync(List<(int ItemId, decimal Quantity, int UomId)> lines, string? voucherCode = null, int? customerId = null)
        {
            var calculatedLines = await CalculateLineItemsAsync(lines);

            var subtotal = Math.Round(calculatedLines.Sum(l => l.LineSubtotal), 2);
            var taxAmount = Math.Round(calculatedLines.Sum(l => l.LineTaxAmount), 2);

            decimal discountAmount = 0;
            Voucher? appliedVoucher = null;
            string? voucherError = null;

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var validation = await ValidateVoucherAsync(voucherCode, subtotal, customerId);
                if (validation.Valid)
                {
                    appliedVoucher = validation.Voucher;
                    discountAmount = CalculateDiscount(appliedVoucher, subtotal);
                }
                else
                {
                    voucherError = validation.Reason;
                }
            }

            var totalAmount = Math.Round(subtotal + taxAmount - discountAmount, 2);

            return new OrderCalculationResult
            {
                Lines = calculatedLines,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Voucher = appliedVoucher,
                VoucherError = voucherError
            };
        }

        public async Task<(int OrderId, string OrderNumber, OrderCalculationResult Calculation)> CreateOrderAsync(
            Order orderMeta, List<(int ItemId, decimal Quantity, int UomId)> lines, string? voucherCode = null)
        {
            // Skip voucher validation for auto-generated codes (starting with "VOUCHER-")
            bool isAutoGeneratedVoucher = !string.IsNullOrEmpty(voucherCode) && voucherCode.StartsWith("VOUCHER-");
            
            var calc = await CalculateOrderAsync(lines, isAutoGeneratedVoucher ? null : voucherCode, orderMeta.CustomerId);
            
            if (!isAutoGeneratedVoucher && !string.IsNullOrEmpty(voucherCode) && !string.IsNullOrEmpty(calc.VoucherError))
            {
                throw new Exception($"Voucher error: {calc.VoucherError}");
            }

            var orderData = new Order
            {
                CustomerId = orderMeta.CustomerId,
                CustomerNameSnapshot = orderMeta.CustomerNameSnapshot,
                CustomerPhoneSnapshot = orderMeta.CustomerPhoneSnapshot,
                CustomerAddressSnapshot = orderMeta.CustomerAddressSnapshot,
                Status = orderMeta.Status,
                Subtotal = calc.Subtotal,
                TaxAmount = calc.TaxAmount,
                DiscountAmount = calc.DiscountAmount,
                TotalAmount = calc.TotalAmount,
                VoucherId = calc.Voucher?.Id,
                VoucherCodeSnapshot = isAutoGeneratedVoucher ? voucherCode : calc.Voucher?.Code,
                Notes = orderMeta.Notes,
                CreatedBy = orderMeta.CreatedBy
            };

            var (orderId, orderNumber) = await _db.CreateOrderAsync(orderData, calc.Lines);
            return (orderId: orderId, orderNumber: orderNumber, calc: calc);
        }

        public async Task<(int OrderId, string OrderNumber, OrderCalculationResult Calculation, string Fs)> CreateOrderFromCartAsync(
            Order orderMeta, List<OrderItem> cartLines, string? voucherCode = null)
        {
            // Skip voucher validation for auto-generated codes (starting with "VOUCHER-")
            bool isAutoGeneratedVoucher = !string.IsNullOrEmpty(voucherCode) && voucherCode.StartsWith("VOUCHER-");
            var lastFs = await _db.GetlastFsNUmber();

            lastFs = lastFs + 1 ?? 0;
            string newFs = $"{lastFs:D9}";

            var subtotal = Math.Round(cartLines.Sum(l => l.LineSubtotal), 2);
            var taxAmount = Math.Round(cartLines.Sum(l => l.LineTaxAmount), 2);
            var totalAmount = Math.Round(subtotal + taxAmount, 2);

            decimal discountAmount = 0;
            Voucher? appliedVoucher = null;
            string? voucherError = null;

            if (!string.IsNullOrEmpty(voucherCode) && !isAutoGeneratedVoucher)
            {
                var validation = await ValidateVoucherAsync(voucherCode, subtotal, orderMeta.CustomerId);
                if (validation.Valid)
                {
                    appliedVoucher = validation.Voucher;
                    discountAmount = CalculateDiscount(appliedVoucher, subtotal);
                }
                else
                {
                    voucherError = validation.Reason;
                }
            }

            totalAmount = Math.Round(subtotal + taxAmount - discountAmount, 2);

            var orderData = new Order
            {
                CustomerId = orderMeta.CustomerId,
                CustomerNameSnapshot = orderMeta.CustomerNameSnapshot,
                CustomerPhoneSnapshot = orderMeta.CustomerPhoneSnapshot,
                CustomerAddressSnapshot = orderMeta.CustomerAddressSnapshot,
                Status = orderMeta.Status,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                VoucherId = appliedVoucher?.Id,
                VoucherCodeSnapshot = isAutoGeneratedVoucher ? voucherCode : appliedVoucher?.Code,
                Notes = orderMeta.Notes,
                CreatedBy = orderMeta.CreatedBy,
                Fs = newFs
            };

            var (orderId, orderNumber) = await _db.CreateOrderAsync(orderData, cartLines);

            var calc = new OrderCalculationResult
            {
                Lines = cartLines,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Voucher = appliedVoucher,
                VoucherError = voucherError
            };

       
            return (orderId: orderId, orderNumber: orderNumber, calc: calc, fs: newFs);
        }
    }

    public class OrderCalculationResult
    {
        public List<OrderItem> Lines { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public Voucher? Voucher { get; set; }
        public string? VoucherError { get; set; }
    }
}
