-- =============================================
-- Database Schema for POS/Inventory Management System
-- Version: 1.0
-- Created: 2026-07-15
-- =============================================

-- =============================================
-- 1. REFERENCE TABLES
-- =============================================

-- ------------------------------
-- 1.1 Unit of Measure (UOM)
-- ------------------------------
CREATE TABLE [dbo].[uom](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [code] [nvarchar](50) NOT NULL,
    [name] [nvarchar](255) NOT NULL,
    [is_base_unit] [int] NOT NULL DEFAULT 0,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_uom] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_uom_code] UNIQUE NONCLUSTERED ([code] ASC)
) ON [PRIMARY];
GO

-- ------------------------------
-- 1.2 UOM Conversions
-- ------------------------------
CREATE TABLE [dbo].[uom_conversions](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [from_uom_id] [int] NOT NULL,
    [to_uom_id] [int] NOT NULL,
    [factor] [decimal](18, 4) NOT NULL,
    CONSTRAINT [PK_uom_conversions] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_uom_conversions] UNIQUE NONCLUSTERED ([from_uom_id] ASC, [to_uom_id] ASC),
    CONSTRAINT [CK_uom_conversions_factor] CHECK ([factor] > 0),
    CONSTRAINT [FK_uom_conversions_from_uom] FOREIGN KEY ([from_uom_id]) REFERENCES [dbo].[uom]([id]),
    CONSTRAINT [FK_uom_conversions_to_uom] FOREIGN KEY ([to_uom_id]) REFERENCES [dbo].[uom]([id])
) ON [PRIMARY];
GO

-- ------------------------------
-- 1.3 Tax Rates
-- ------------------------------
CREATE TABLE [dbo].[tax_rates](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [name] [nvarchar](255) NOT NULL,
    [rate_percent] [decimal](18, 4) NOT NULL,
    [tax_type] [nvarchar](50) NOT NULL,
    [is_active] [int] NOT NULL DEFAULT 1,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_tax_rates] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_tax_rates_rate_percent] CHECK ([rate_percent] >= 0),
    CONSTRAINT [CK_tax_rates_tax_type] CHECK ([tax_type] IN ('exclusive', 'inclusive'))
) ON [PRIMARY];
GO

-- ------------------------------
-- 1.4 Categories (Hierarchical)
-- ------------------------------
CREATE TABLE [dbo].[categories](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [name] [nvarchar](255) NOT NULL,
    [description] [nvarchar](max) NULL,
    [parent_id] [int] NULL,
    [is_active] [int] NOT NULL DEFAULT 1,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    [updated_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_categories] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_categories_name] UNIQUE NONCLUSTERED ([name] ASC),
    CONSTRAINT [FK_categories_parent] FOREIGN KEY ([parent_id]) REFERENCES [dbo].[categories]([id])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- =============================================
-- 2. BUSINESS ENTITY TABLES
-- =============================================

-- ------------------------------
-- 2.1 Company
-- ------------------------------
CREATE TABLE [dbo].[Company](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [CompanyName] [nvarchar](200) NOT NULL,
    [TradeName] [nvarchar](200) NULL,
    [TinNumber] [nvarchar](50) NULL,
    [VatNumber] [nvarchar](50) NULL,
    [AddressLine1] [nvarchar](200) NULL,
    [AddressLine2] [nvarchar](200) NULL,
    [City] [nvarchar](100) NULL,
    [StateProvince] [nvarchar](100) NULL,
    [Country] [nvarchar](100) NULL,
    [PostalCode] [nvarchar](20) NULL,
    [Telephone] [nvarchar](100) NULL,
    [Mobile] [nvarchar](100) NULL,
    [Email] [nvarchar](200) NULL,
    [Website] [nvarchar](200) NULL,
    [Logo] [varbinary](max) NULL,
    [TaxRate] [decimal](5, 2) NULL,
    [CurrencyCode] [nvarchar](10) NULL,
    [IsActive] [bit] NOT NULL DEFAULT 1,
    [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] [datetime] NULL,
    CONSTRAINT [PK_Company] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- ------------------------------
-- 2.2 Customers
-- ------------------------------
CREATE TABLE [dbo].[customers](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [name] [nvarchar](255) NOT NULL,
    [phone] [nvarchar](50) NULL,
    [email] [nvarchar](255) NULL,
    [address] [nvarchar](max) NULL,
    [tax_id] [nvarchar](50) NULL,
    [notes] [nvarchar](max) NULL,
    [CustomerCode] [nvarchar](50) NULL,

    [is_active] [int] NOT NULL DEFAULT 1,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    [updated_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_customers] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- Create index for customer lookups
CREATE NONCLUSTERED INDEX [IX_customers_phone] ON [dbo].[customers] ([phone]);
CREATE NONCLUSTERED INDEX [IX_customers_email] ON [dbo].[customers] ([email]);
GO

-- =============================================
-- 3. INVENTORY TABLES
-- =============================================

-- ------------------------------
-- 3.1 Items (Products)
-- ------------------------------
CREATE TABLE [dbo].[items](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [sku] [nvarchar](50) NULL,
    [name] [nvarchar](255) NOT NULL,
    [description] [nvarchar](max) NULL,
    [category_id] [int] NULL,
    [base_uom_id] [int] NOT NULL,
    [current_price] [decimal](18, 4) NOT NULL DEFAULT 0,
    [tax_rate_id] [int] NULL,
    [stock_quantity] [decimal](18, 4) NOT NULL DEFAULT 0,
    [reorder_level] [decimal](18, 4) NOT NULL DEFAULT 0,
    [is_active] [int] NOT NULL DEFAULT 1,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    [updated_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_items] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_items_sku] UNIQUE NONCLUSTERED ([sku] ASC),
    CONSTRAINT [FK_items_category] FOREIGN KEY ([category_id]) REFERENCES [dbo].[categories]([id]),
    CONSTRAINT [FK_items_base_uom] FOREIGN KEY ([base_uom_id]) REFERENCES [dbo].[uom]([id]),
    CONSTRAINT [FK_items_tax_rate] FOREIGN KEY ([tax_rate_id]) REFERENCES [dbo].[tax_rates]([id])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- Create indexes for item lookups
CREATE NONCLUSTERED INDEX [IX_items_sku] ON [dbo].[items] ([sku]);
CREATE NONCLUSTERED INDEX [IX_items_category] ON [dbo].[items] ([category_id]);
CREATE NONCLUSTERED INDEX [IX_items_current_price] ON [dbo].[items] ([current_price]);
GO

-- ------------------------------
-- 3.2 Item UOMs
-- ------------------------------
CREATE TABLE [dbo].[item_uoms](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [item_id] [int] NOT NULL,
    [uom_id] [int] NOT NULL,
    [conversion_factor] [decimal](18, 4) NOT NULL,
    [price_override] [decimal](18, 4) NULL,
    [is_default] [int] NOT NULL DEFAULT 0,
    CONSTRAINT [PK_item_uoms] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_item_uoms] UNIQUE NONCLUSTERED ([item_id] ASC, [uom_id] ASC),
    CONSTRAINT [CK_item_uoms_conversion_factor] CHECK ([conversion_factor] > 0),
    CONSTRAINT [FK_item_uoms_item] FOREIGN KEY ([item_id]) REFERENCES [dbo].[items]([id]),
    CONSTRAINT [FK_item_uoms_uom] FOREIGN KEY ([uom_id]) REFERENCES [dbo].[uom]([id])
) ON [PRIMARY];
GO

-- ------------------------------
-- 3.3 Item Price History
-- ------------------------------
CREATE TABLE [dbo].[item_price_history](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [item_id] [int] NOT NULL,
    [price] [decimal](18, 4) NOT NULL,
    [uom_id] [int] NOT NULL,
    [effective_from] [datetime] NOT NULL DEFAULT GETDATE(),
    [changed_by] [nvarchar](255) NULL,
    [reason] [nvarchar](max) NULL,
    CONSTRAINT [PK_item_price_history] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_item_price_history_item] FOREIGN KEY ([item_id]) REFERENCES [dbo].[items]([id]),
    CONSTRAINT [FK_item_price_history_uom] FOREIGN KEY ([uom_id]) REFERENCES [dbo].[uom]([id])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- Create index for price history lookups
CREATE NONCLUSTERED INDEX [IX_item_price_history_item_id] ON [dbo].[item_price_history] ([item_id]);
CREATE NONCLUSTERED INDEX [IX_item_price_history_effective_from] ON [dbo].[item_price_history] ([effective_from]);
GO

-- =============================================
-- 4. ORDER TABLES
-- =============================================

-- ------------------------------
-- 4.1 Vouchers
-- ------------------------------
CREATE TABLE [dbo].[vouchers](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [code] [nvarchar](50) NOT NULL,
    [description] [nvarchar](max) NULL,
    [discount_type] [nvarchar](50) NOT NULL,
    [discount_value] [decimal](18, 4) NOT NULL,
    [max_discount_amount] [decimal](18, 4) NULL,
    [min_order_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [valid_from] [datetime] NOT NULL,
    [valid_until] [datetime] NOT NULL,
    [max_uses] [int] NULL,
    [used_count] [int] NOT NULL DEFAULT 0,
    [max_uses_per_customer] [int] NULL,
    [is_active] [int] NOT NULL DEFAULT 1,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_vouchers] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_vouchers_code] UNIQUE NONCLUSTERED ([code] ASC),
    CONSTRAINT [CK_vouchers_discount_type] CHECK ([discount_type] IN ('fixed', 'percentage')),
    CONSTRAINT [CK_vouchers_discount_value] CHECK ([discount_value] >= 0)
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- ------------------------------
-- 4.2 Orders
-- ------------------------------
CREATE TABLE [dbo].[orders](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [order_number] [nvarchar](50) NOT NULL,
    [customer_id] [int] NULL,
    [customer_name_snapshot] [nvarchar](255) NULL,
    [customer_phone_snapshot] [nvarchar](50) NULL,
    [customer_address_snapshot] [nvarchar](max) NULL,
    [status] [nvarchar](50) NOT NULL DEFAULT 'draft',
    [subtotal] [decimal](18, 4) NOT NULL DEFAULT 0,
    [tax_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [discount_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [total_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [voucher_id] [int] NULL,
    [voucher_code_snapshot] [nvarchar](50) NULL,
    [notes] [nvarchar](max) NULL,
    [order_date] [datetime] NOT NULL DEFAULT GETDATE(),
    [confirmed_at] [datetime] NULL,
    [delivered_at] [datetime] NULL,
    [cancelled_at] [datetime] NULL,
    [created_by] [nvarchar](255) NULL,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    [updated_at] [datetime] NOT NULL DEFAULT GETDATE(),
    [fs] [varchar](50) NULL,
    CONSTRAINT [PK_orders] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [UQ_orders_order_number] UNIQUE NONCLUSTERED ([order_number] ASC),
    CONSTRAINT [CK_orders_status] CHECK ([status] IN ('draft', 'confirmed', 'processing', 'delivered', 'cancelled')),
    CONSTRAINT [FK_orders_customer] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[customers]([id]),
    CONSTRAINT [FK_orders_voucher] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[vouchers]([id])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

-- Create indexes for order lookups
CREATE NONCLUSTERED INDEX [IX_orders_order_number] ON [dbo].[orders] ([order_number]);
CREATE NONCLUSTERED INDEX [IX_orders_customer_id] ON [dbo].[orders] ([customer_id]);
CREATE NONCLUSTERED INDEX [IX_orders_status] ON [dbo].[orders] ([status]);
CREATE NONCLUSTERED INDEX [IX_orders_order_date] ON [dbo].[orders] ([order_date]);
GO

-- ------------------------------
-- 4.3 Order Items
-- ------------------------------
CREATE TABLE [dbo].[order_items](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [order_id] [int] NOT NULL,
    [item_id] [int] NULL,
    [item_name_snapshot] [nvarchar](255) NOT NULL,
    [item_sku_snapshot] [nvarchar](50) NULL,
    [uom_snapshot] [nvarchar](50) NOT NULL,
    [unit_price_snapshot] [decimal](18, 4) NOT NULL,
    [tax_rate_snapshot] [decimal](18, 4) NOT NULL DEFAULT 0,
    [tax_type_snapshot] [nvarchar](50) NOT NULL DEFAULT 'exclusive',
    [quantity] [decimal](18, 4) NOT NULL,
    [line_subtotal] [decimal](18, 4) NOT NULL,
    [line_tax_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [line_discount_amount] [decimal](18, 4) NOT NULL DEFAULT 0,
    [line_total] [decimal](18, 4) NOT NULL,
    [sort_order] [int] NOT NULL DEFAULT 0,
    [created_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_order_items] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [CK_order_items_quantity] CHECK ([quantity] > 0),
    CONSTRAINT [CK_order_items_tax_type] CHECK ([tax_type_snapshot] IN ('exclusive', 'inclusive')),
    CONSTRAINT [FK_order_items_order] FOREIGN KEY ([order_id]) REFERENCES [dbo].[orders]([id]),
    CONSTRAINT [FK_order_items_item] FOREIGN KEY ([item_id]) REFERENCES [dbo].[items]([id])
) ON [PRIMARY];
GO

-- Create index for order items lookups
CREATE NONCLUSTERED INDEX [IX_order_items_order_id] ON [dbo].[order_items] ([order_id]);
CREATE NONCLUSTERED INDEX [IX_order_items_item_id] ON [dbo].[order_items] ([item_id]);
GO

-- ------------------------------
-- 4.4 Voucher Redemptions
-- ------------------------------
CREATE TABLE [dbo].[voucher_redemptions](
    [id] [int] IDENTITY(1,1) NOT NULL,
    [voucher_id] [int] NOT NULL,
    [order_id] [int] NOT NULL,
    [customer_id] [int] NULL,
    [discount_applied] [decimal](18, 4) NOT NULL,
    [redeemed_at] [datetime] NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_voucher_redemptions] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_voucher_redemptions_voucher] FOREIGN KEY ([voucher_id]) REFERENCES [dbo].[vouchers]([id]),
    CONSTRAINT [FK_voucher_redemptions_order] FOREIGN KEY ([order_id]) REFERENCES [dbo].[orders]([id]),
    CONSTRAINT [FK_voucher_redemptions_customer] FOREIGN KEY ([customer_id]) REFERENCES [dbo].[customers]([id])
) ON [PRIMARY];
GO

-- Create indexes for voucher redemption lookups
CREATE NONCLUSTERED INDEX [IX_voucher_redemptions_voucher_id] ON [dbo].[voucher_redemptions] ([voucher_id]);
CREATE NONCLUSTERED INDEX [IX_voucher_redemptions_order_id] ON [dbo].[voucher_redemptions] ([order_id]);
CREATE NONCLUSTERED INDEX [IX_voucher_redemptions_customer_id] ON [dbo].[voucher_redemptions] ([customer_id]);
GO

-- =============================================
-- 5. ADDITIONAL PERFORMANCE INDEXES
-- =============================================

-- Indexes for items table
CREATE NONCLUSTERED INDEX [IX_items_name] ON [dbo].[items] ([name]);
GO

-- Indexes for categories table
CREATE NONCLUSTERED INDEX [IX_categories_parent_id] ON [dbo].[categories] ([parent_id]);
GO

-- Indexes for customers table
CREATE NONCLUSTERED INDEX [IX_customers_name] ON [dbo].[customers] ([name]);
GO

-- Indexes for vouchers table
CREATE NONCLUSTERED INDEX [IX_vouchers_valid_from_until] ON [dbo].[vouchers] ([valid_from], [valid_until]);
CREATE NONCLUSTERED INDEX [IX_vouchers_is_active] ON [dbo].[vouchers] ([is_active]);
GO


