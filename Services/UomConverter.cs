using OrderApp.Data;

namespace OrderApp.Services
{
    public class UomConverter
    {
        private readonly DBManager _db;
        private Dictionary<string, decimal>? _globalConversionCache;

        public UomConverter(DBManager db)
        {
            _db = db;
        }

        public void InvalidateCache()
        {
            _globalConversionCache = null;
        }

        private async Task<Dictionary<string, decimal>> LoadGlobalConversionsAsync()
        {
            if (_globalConversionCache != null)
                return _globalConversionCache;

            // This would need to be implemented in DBManager to fetch uom_conversions
            // For now, return empty cache
            _globalConversionCache = new Dictionary<string, decimal>();
            return _globalConversionCache;
        }

        public async Task<decimal> ConvertAsync(int itemId, decimal quantity, int fromUomId, int toUomId)
        {
            if (fromUomId == toUomId)
                return quantity;

            var item = await _db.GetItemAsync(itemId);
            if (item == null)
                throw new Exception($"Item {itemId} not found");

            // Simplified conversion - in full implementation would use item_uoms and global conversions
            // For now, assume 1:1 conversion
            return quantity;
        }

        public async Task<decimal> GetPriceForUomAsync(int itemId, int uomId)
        {
            var item = await _db.GetItemAsync(itemId);
            if (item == null)
                throw new Exception($"Item {itemId} not found");

            if (uomId == item.BaseUomId)
                return item.CurrentPrice;

            // Check for price override in alternate UOMs
            var altUom = item.AltUoms.FirstOrDefault(u => u.UomId == uomId);
            if (altUom != null)
            {
                if (altUom.PriceOverride.HasValue)
                    return altUom.PriceOverride.Value;
                if (altUom.ConversionFactor > 0)
                    return item.CurrentPrice * altUom.ConversionFactor;
            }

            throw new Exception($"Cannot resolve price for item {itemId} in uom {uomId}");
        }

        public async Task<List<SellableUom>> GetSellableUomsAsync(int itemId)
        {
            var item = await _db.GetItemAsync(itemId);
            if (item == null)
                return new List<SellableUom>();

            var results = new List<SellableUom>();

            // Add base UOM
            results.Add(new SellableUom
            {
                UomId = item.BaseUomId,
                Code = item.BaseUomCode ?? "",
                Name = item.BaseUomCode ?? "",
                Price = item.CurrentPrice,
                IsDefault = true
            });

            // Add alternate UOMs
            foreach (var alt in item.AltUoms)
            {
                var price = alt.PriceOverride ?? item.CurrentPrice * alt.ConversionFactor;
                results.Add(new SellableUom
                {
                    UomId = alt.UomId,
                    Code = alt.UomCode ?? "",
                    Name = alt.UomName ?? "",
                    Price = price,
                    IsDefault = alt.IsDefault
                });
            }

            return results;
        }
    }

    public class SellableUom
    {
        public int UomId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsDefault { get; set; }
    }
}
