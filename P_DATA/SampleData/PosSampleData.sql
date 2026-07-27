-- =============================================
-- TEST DATA INSERTION SCRIPT (Single Record Per Table)
-- AUTO-GENERATED IDs - NO MANUAL ID INSERTION
-- =============================================

-- =============================================
-- 1. PREREQUISITE DATA (Reference Tables)
-- =============================================

-- Insert UOMs (Units of Measure)
INSERT INTO [dbo].[uom] ([code], [name], [is_base_unit], [created_at])
VALUES ('PCS', 'Pieces', 1, GETDATE());
GO

-- Insert Tax Rates
INSERT INTO [dbo].[tax_rates] ([name], [rate_percent], [tax_type], [is_active], [created_at])
VALUES ('Standard VAT', 15.0000, 'exclusive', 1, GETDATE());
GO

-- Insert Category
INSERT INTO [dbo].[categories] ([name], [description], [parent_id], [is_active], [created_at], [updated_at])
VALUES ('TEST Category', 'This is a test category for development purposes', NULL, 1, GETDATE(), GETDATE());
GO

-- =============================================
-- 2. INSERT TEST CUSTOMER
-- =============================================

INSERT INTO [dbo].[customers] (
    [name], 
    [phone], 
    [email], 
    [address], 
    [tax_id], 
    [notes], 
    [is_active], 
    [created_at], 
    [updated_at]
)
VALUES (
    'TEST Customer', 
    '+1234567890', 
    'test.customer@email.com', 
    '123 Test Street, Test City, TC 12345', 
    'TAX-TEST-001', 
    'Test customer for development', 
    1, 
    GETDATE(), 
    GETDATE()
);
GO

-- =============================================
-- 3. INSERT TEST COMPANY
-- =============================================

INSERT INTO [dbo].[Company] (
    [CompanyName],
    [TradeName],
    [TinNumber],
    [VatNumber],
    [AddressLine1],
    [AddressLine2],
    [City],
    [StateProvince],
    [Country],
    [PostalCode],
    [Telephone],
    [Mobile],
    [Email],
    [Website],
    [Logo],
    [TaxRate],
    [CurrencyCode],
    [IsActive],
    [CreatedDate],
    [ModifiedDate]
)
VALUES (
    'TEST Company Ltd.',
    'TEST Corp',
    '0000000000-01',
    'FDK-001',
    '123 Business Park',
    'Suite 100',
    'Test City',
    'Test State',
    'Test Country',
    '12345',
    '+1234567890',
    '+1234567891',
    'info@testcompany.com',
    'www.testcompany.com',
    NULL,
    15.00,
    'USD',
    1,
    GETDATE(),
    GETDATE()
);
GO

-- =============================================
-- 4. INSERT TEST ITEM (ITM-001)
-- =============================================

INSERT INTO [dbo].[items] (
    [sku], 
    [name], 
    [description], 
    [category_id], 
    [base_uom_id], 
    [current_price], 
    [tax_rate_id], 
    [stock_quantity], 
    [reorder_level], 
    [is_active], 
    [created_at], 
    [updated_at]
)
VALUES (
    'ITM-001', 
    'TEST Item', 
    'This is a test item for development and testing purposes', 
    (SELECT TOP 1 id FROM [dbo].[categories] WHERE name = 'TEST Category'),
    (SELECT TOP 1 id FROM [dbo].[uom] WHERE code = 'PCS'),
    99.9900,
    (SELECT TOP 1 id FROM [dbo].[tax_rates] WHERE name = 'Standard VAT'),
    100.0000,
    10.0000,
    1,
    GETDATE(),
    GETDATE()
);
GO

-- =============================================
-- 5. INSERT ITEM UOM CONVERSION
-- =============================================

INSERT INTO [dbo].[item_uoms] (
    [item_id], 
    [uom_id], 
    [conversion_factor], 
    [price_override], 
    [is_default]
)
VALUES (
    (SELECT TOP 1 id FROM [dbo].[items] WHERE sku = 'ITM-001'),
    (SELECT TOP 1 id FROM [dbo].[uom] WHERE code = 'PCS'),
    1.0000,
    99.9900,
    1
);
GO

-- =============================================
-- 6. INSERT ITEM PRICE HISTORY
-- =============================================

INSERT INTO [dbo].[item_price_history] (
    [item_id], 
    [price], 
    [uom_id], 
    [effective_from], 
    [changed_by], 
    [reason]
)
VALUES (
    (SELECT TOP 1 id FROM [dbo].[items] WHERE sku = 'ITM-001'),
    99.9900,
    (SELECT TOP 1 id FROM [dbo].[uom] WHERE code = 'PCS'),
    GETDATE(),
    'System',
    'Initial price setup'
);
GO

-- =============================================
-- 7. INSERT UOM CONVERSION
-- =============================================

INSERT INTO [dbo].[uom_conversions] (
    [from_uom_id], 
    [to_uom_id], 
    [factor]
)
VALUES (
    (SELECT TOP 1 id FROM [dbo].[uom] WHERE code = 'PCS'),
    (SELECT TOP 1 id FROM [dbo].[uom] WHERE code = 'PCS'),
    1.0000
);
GO

-- =============================================
-- 8. INSERT TEST VOUCHER
-- =============================================

INSERT INTO [dbo].[vouchers] (
    [code],
    [description],
    [discount_type],
    [discount_value],
    [max_discount_amount],
    [min_order_amount],
    [valid_from],
    [valid_until],
    [max_uses],
    [used_count],
    [max_uses_per_customer],
    [is_active],
    [created_at]
)
VALUES (
    'TESTVOUCHER001',
    'Test voucher for development',
    'percentage',
    10.0000,
    50.0000,
    0.0000,
    GETDATE(),
    DATEADD(month, 12, GETDATE()),
    100,
    0,
    5,
    1,
    GETDATE()
);
GO

-- =============================================
-- 9. INSERT TEST ORDER
-- =============================================

INSERT INTO [dbo].[orders] (
    [order_number],
    [customer_id],
    [customer_name_snapshot],
    [customer_phone_snapshot],
    [customer_address_snapshot],
    [status],
    [subtotal],
    [tax_amount],
    [discount_amount],
    [total_amount],
    [voucher_id],
    [voucher_code_snapshot],
    [notes],
    [order_date],
    [confirmed_at],
    [delivered_at],
    [cancelled_at],
    [created_by],
    [created_at],
    [updated_at],
    [fs]
)
SELECT 
    'ORD-TEST-001',
    c.id,
    c.name,
    c.phone,
    c.address,
    'confirmed',
    99.9900,
    14.9985,
    0.0000,
    114.9885,
    v.id,
    v.code,
    'Test order for development',
    GETDATE(),
    GETDATE(),
    NULL,
    NULL,
    'System',
    GETDATE(),
    GETDATE(),
    NULL
FROM [dbo].[customers] c
CROSS JOIN [dbo].[vouchers] v
WHERE c.name = 'TEST Customer'
  AND v.code = 'TESTVOUCHER001';
GO

-- =============================================
-- 10. INSERT TEST ORDER ITEMS
-- =============================================

INSERT INTO [dbo].[order_items] (
    [order_id],
    [item_id],
    [item_name_snapshot],
    [item_sku_snapshot],
    [uom_snapshot],
    [unit_price_snapshot],
    [tax_rate_snapshot],
    [tax_type_snapshot],
    [quantity],
    [line_subtotal],
    [line_tax_amount],
    [line_discount_amount],
    [line_total],
    [sort_order],
    [created_at]
)
SELECT 
    o.id,
    i.id,
    i.name,
    i.sku,
    'PCS',
    i.current_price,
    t.rate_percent,
    t.tax_type,
    1.0000,
    i.current_price,
    i.current_price * (t.rate_percent / 100),
    0.0000,
    i.current_price + (i.current_price * (t.rate_percent / 100)),
    1,
    GETDATE()
FROM [dbo].[orders] o
CROSS JOIN [dbo].[items] i
LEFT JOIN [dbo].[tax_rates] t ON i.tax_rate_id = t.id
WHERE o.order_number = 'ORD-TEST-001' 
  AND i.sku = 'ITM-001';
GO

-- =============================================
-- 11. INSERT TEST VOUCHER REDEMPTION
-- =============================================

INSERT INTO [dbo].[voucher_redemptions] (
    [voucher_id],
    [order_id],
    [customer_id],
    [discount_applied],
    [redeemed_at]
)
SELECT 
    v.id,
    o.id,
    o.customer_id,
    9.9990,
    GETDATE()
FROM [dbo].[vouchers] v
CROSS JOIN [dbo].[orders] o
WHERE v.code = 'TESTVOUCHER001'
  AND o.order_number = 'ORD-TEST-001';
GO

-- =============================================
-- 12. VERIFICATION QUERIES
-- =============================================

-- Check all tables with TEST data
SELECT '=== REFERENCE TABLES ===' AS Info;
GO

SELECT 'UOM' AS Table_Name, COUNT(*) AS Record_Count, 
       MIN(name) AS Sample_Name FROM [dbo].[uom]
UNION ALL
SELECT 'Tax Rates', COUNT(*), MIN(name) FROM [dbo].[tax_rates]
UNION ALL
SELECT 'Categories', COUNT(*), MIN(name) FROM [dbo].[categories]
UNION ALL
SELECT 'Company', COUNT(*), MIN(CompanyName) FROM [dbo].[Company]
UNION ALL
SELECT 'Customers', COUNT(*), MIN(name) FROM [dbo].[customers]
UNION ALL
SELECT 'Items', COUNT(*), MIN(name) FROM [dbo].[items]
UNION ALL
SELECT 'Item UOMs', COUNT(*), '' FROM [dbo].[item_uoms]
UNION ALL
SELECT 'Item Price History', COUNT(*), '' FROM [dbo].[item_price_history]
UNION ALL
SELECT 'UOM Conversions', COUNT(*), '' FROM [dbo].[uom_conversions]
UNION ALL
SELECT 'Vouchers', COUNT(*), MIN(code) FROM [dbo].[vouchers]
UNION ALL
SELECT 'Orders', COUNT(*), MIN(order_number) FROM [dbo].[orders]
UNION ALL
SELECT 'Order Items', COUNT(*), '' FROM [dbo].[order_items]
UNION ALL
SELECT 'Voucher Redemptions', COUNT(*), '' FROM [dbo].[voucher_redemptions];
GO

-- Display TEST Customer
SELECT 
    id AS Customer_ID,
    name,
    phone,
    email,
    address,
    CASE WHEN is_active = 1 THEN 'Active' ELSE 'Inactive' END AS Status
FROM [dbo].[customers]
WHERE name = 'TEST Customer';
GO

-- Display TEST Item
SELECT 
    id AS Item_ID,
    sku AS Item_Code,
    name,
    description,
    (SELECT name FROM [dbo].[categories] WHERE id = i.category_id) AS Category,
    current_price,
    stock_quantity,
    reorder_level,
    CASE WHEN i.is_active = 1 THEN 'Active' ELSE 'Inactive' END AS Status
FROM [dbo].[items] i
WHERE sku = 'ITM-001';
GO

-- Display TEST Order with details
SELECT 
    o.id AS Order_ID,
    o.order_number,
    o.customer_name_snapshot,
    o.status,
    o.subtotal,
    o.tax_amount,
    o.discount_amount,
    o.total_amount,
    oi.item_name_snapshot AS Item,
    oi.quantity,
    oi.unit_price_snapshot,
    oi.line_total,
    v.code AS voucher_code,
    v.discount_type,
    v.discount_value
FROM [dbo].[orders] o
LEFT JOIN [dbo].[order_items] oi ON o.id = oi.order_id
LEFT JOIN [dbo].[vouchers] v ON o.voucher_id = v.id
WHERE o.order_number = 'ORD-TEST-001';
GO

-- Display Voucher Redemption
SELECT 
    vr.id AS Redemption_ID,
    v.code AS voucher_code,
    o.order_number,
    c.name AS customer_name,
    vr.discount_applied,
    vr.redeemed_at
FROM [dbo].[voucher_redemptions] vr
LEFT JOIN [dbo].[vouchers] v ON vr.voucher_id = v.id
LEFT JOIN [dbo].[orders] o ON vr.order_id = o.id
LEFT JOIN [dbo].[customers] c ON vr.customer_id = c.id;
GO

-- =============================================
-- 13. COMPLETE DATA SUMMARY
-- =============================================

PRINT '=============================================';
PRINT 'TEST DATA INSERTION COMPLETE';
PRINT '=============================================';
PRINT 'Customer: TEST Customer (Auto-generated ID)';
PRINT 'Item: TEST Item (ITM-001)';
PRINT 'Order: ORD-TEST-001';
PRINT 'Voucher: TESTVOUCHER001';
PRINT 'Company: TEST Company Ltd.';
PRINT '=============================================';
PRINT 'All test data has been successfully inserted!';
PRINT '=============================================';
PRINT '';
PRINT 'NOTE: All IDs are auto-generated by SQL Server';
PRINT 'Run verification queries above to see actual IDs';
PRINT '=============================================';
GO