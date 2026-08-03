BEGIN TRY
    -- Drop temp tables if they exist (clean start)
    IF OBJECT_ID('tempdb..#VoucherIdMapping') IS NOT NULL 
        DROP TABLE #VoucherIdMapping
    
    IF OBJECT_ID('tempdb..#SourceVouchers') IS NOT NULL 
        DROP TABLE #SourceVouchers
    
    IF OBJECT_ID('tempdb..#LineItemMapping') IS NOT NULL 
        DROP TABLE #LineItemMapping

    IF OBJECT_ID('tempdb..#SourceLineItems') IS NOT NULL 
        DROP TABLE #SourceLineItems

    -- Batch token to uniquely identify rows inserted by this run
    DECLARE @BatchToken UNIQUEIDENTIFIER = NEWID()

    -- Create temporary tables for ID mappings
    CREATE TABLE #VoucherIdMapping (
        OldId INT,
        NewId INT
    )
    
    CREATE TABLE #LineItemMapping (
        OldId INT,
        NewId INT
    )

    -- First, get the source vouchers into a temp table
    SELECT * 
    INTO #SourceVouchers
    FROM [transaction].Voucher 
    WHERE cast(issuedDate as date) >= '2026-07-08'

    PRINT 'Source vouchers loaded: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'

    -- Insert vouchers and capture new IDs using a cursor
    DECLARE @OldId INT, @NewId INT
    DECLARE @VoucherCount INT = 0
    
    DECLARE voucher_cursor CURSOR FOR
    SELECT id FROM #SourceVouchers
    
    OPEN voucher_cursor
    FETCH NEXT FROM voucher_cursor INTO @OldId
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Insert single voucher
        INSERT INTO [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].Voucher (
            code, type, definition, originConsigneeUnit, destinationConsigneeUnit, 
            period, shift, consignee1, consignee2, consignee3, consignee4, consignee5, consignee6,
            consigneeUnit1, consigneeUnit2, consigneeUnit3, consigneeUnit4, consigneeUnit5, consigneeUnit6,
            article, issuedDate, isIssued, createdOn, lastModified, isVoid, day, month, year,
            subTotal, discount, addCharge, grandTotal, paymentMethod, paymentProcessor, payer,
            isIncoming, paymentAmount, paymentIssueDate, paymentMaturityDate, paymentRefNumber,
            paymentStatus, currency, exchangeRate, tender, note, purpose, fsNumber, mrc,
            cart, extension1, extension2, extension3, extension4, extension5, extension6,
            startDate, endDate, sourceStore, destinationStore, hasEffect, sourceBankAccount,
            destinationBankAccount, lastActivity, deliveryMethod, count, space, contactPerson,
            lastUser, lastDevice, lastState, latitiude, longitude, locked, defaultImageUrl,
            remark
        )
        SELECT 
            code + '.', type, definition, originConsigneeUnit, destinationConsigneeUnit, 
            period, shift, consignee1, consignee2, consignee3, consignee4, consignee5, consignee6,
            consigneeUnit1, consigneeUnit2, consigneeUnit3, consigneeUnit4, consigneeUnit5, consigneeUnit6,
            article, issuedDate, isIssued, createdOn, lastModified, isVoid, day, month, year,
            subTotal, discount, addCharge, grandTotal, paymentMethod, paymentProcessor, payer,
            isIncoming, paymentAmount, paymentIssueDate, paymentMaturityDate, paymentRefNumber,
            paymentStatus, currency, exchangeRate, tender, note, purpose, fsNumber, mrc,
            cart, extension1, extension2, extension3, extension4, extension5, extension6,
            startDate, endDate, sourceStore, destinationStore, hasEffect, sourceBankAccount,
            destinationBankAccount, 7041267 as lastActivity, deliveryMethod, count, space, contactPerson,
            lastUser, lastDevice, lastState, latitiude, longitude, locked, defaultImageUrl,
            'MIGBATCH:' + CAST(@BatchToken AS VARCHAR(36)) + ' | Migrated from old system on ' + CAST(GETDATE() AS VARCHAR) + ' | Original Voucher ID: ' + CAST(@OldId AS VARCHAR) as remark
        FROM #SourceVouchers
        WHERE id = @OldId
        
        -- Get the new ID
        SET @NewId = SCOPE_IDENTITY()
        
        -- Store mapping
        INSERT INTO #VoucherIdMapping (OldId, NewId) VALUES (@OldId, @NewId)
        
        SET @VoucherCount = @VoucherCount + 1
        FETCH NEXT FROM voucher_cursor INTO @OldId
    END
    
    CLOSE voucher_cursor
    DEALLOCATE voucher_cursor

    PRINT 'Vouchers inserted successfully: ' + CAST(@VoucherCount AS VARCHAR) + ' records'

    -- Verify mapping exists
    IF (SELECT COUNT(*) FROM #VoucherIdMapping) = 0
    BEGIN
        RAISERROR('No voucher mappings were created. Check the source data.', 16, 1)
    END

    -- Display mapping for debugging
    PRINT 'Voucher ID Mapping:'
    SELECT OldId, NewId FROM #VoucherIdMapping

    -- Get source line items with their vouchers
    SELECT * 
    INTO #SourceLineItems
    FROM [transaction].LineItem 
    WHERE voucher IN (SELECT OldId FROM #VoucherIdMapping)

    PRINT 'Source line items loaded: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'

    -- Insert LineItems (NOTE: OUTPUT INTO is not supported when the target
    -- is a remote/linked-server table, so we tag rows with the batch token
    -- and original id inside remark, then rebuild the mapping afterward
    -- with a plain SELECT, which linked servers do support).
    INSERT INTO [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].LineItem (
        [index], voucher, article, note, override, unitAmount, uom, quantity, 
        totalAmount, discount, addCharge, tax, taxableAmount, taxAmount, 
        calculatedCost, startDate, endDate, serialCode1, serialCode2, serialCode3,
        extension1, extension2, size1, size2, isVoid, cart, createdOn, 
        lastModified, remark, objectState, parentId, scheduleHeader
    )
    SELECT 
        src.[index], 
        map.NewId,  -- Use the NEW voucher ID from mapping
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM [BILOS_SERVER19].[BILOS PASTRY 2019].article.article 
                WHERE id = src.article
            ) 
            THEN src.article
            ELSE (SELECT TOP 1 id FROM [BILOS_SERVER19].[BILOS PASTRY 2019].article.article WHERE localCode = '8856976000023')
        END as article,
        src.note, src.override, src.unitAmount, src.uom, src.quantity, 
        src.totalAmount, src.discount, src.addCharge, src.tax, src.taxableAmount, src.taxAmount, 
        src.calculatedCost, src.startDate, src.endDate, src.serialCode1, src.serialCode2, src.serialCode3,
        src.extension1, src.extension2, src.size1, src.size2, src.isVoid, src.cart, src.createdOn, 
        src.lastModified, 
        'MIGBATCH:' + CAST(@BatchToken AS VARCHAR(36)) + ' | OrigLineItemId:' + CAST(src.id AS VARCHAR) +
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM [BILOS_SERVER19].[BILOS PASTRY 2019].article.article 
                WHERE id = src.article
            ) 
            THEN ' | Migrated from old system on ' + CAST(GETDATE() AS VARCHAR) + ' | Original Article ID: ' + CAST(src.article AS VARCHAR) + ' (Found in destination) | Original Voucher ID: ' + CAST(src.voucher AS VARCHAR) + ' -> New Voucher ID: ' + CAST(map.NewId AS VARCHAR)
            ELSE ' | Migrated from old system on ' + CAST(GETDATE() AS VARCHAR) + ' | Original Article ID: ' + CAST(src.article AS VARCHAR) + ' (Mapped to default) | Original Voucher ID: ' + CAST(src.voucher AS VARCHAR) + ' -> New Voucher ID: ' + CAST(map.NewId AS VARCHAR)
        END as remark,
        src.objectState, src.parentId, src.scheduleHeader
    FROM [transaction].LineItem src
    INNER JOIN #VoucherIdMapping map ON src.voucher = map.OldId

    PRINT 'LineItems inserted successfully: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'

    -- Rebuild the OldId -> NewId mapping using a plain SELECT against the
    -- linked server (allowed, unlike OUTPUT/DML), filtered to this batch only
    INSERT INTO #LineItemMapping (OldId, NewId)
    SELECT 
        CAST(
            SUBSTRING(
                remark,
                CHARINDEX('OrigLineItemId:', remark) + LEN('OrigLineItemId:'),
                CHARINDEX(' |', remark, CHARINDEX('OrigLineItemId:', remark)) - (CHARINDEX('OrigLineItemId:', remark) + LEN('OrigLineItemId:'))
            ) AS INT
        ) as OldId,
        id as NewId
    FROM [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].LineItem
    WHERE remark LIKE 'MIGBATCH:' + CAST(@BatchToken AS VARCHAR(36)) + '%'

    PRINT 'LineItem ID Mapping rebuilt: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'

    -- Sanity check: rebuilt mapping count should equal inserted count
    IF (SELECT COUNT(*) FROM #LineItemMapping) <> (SELECT COUNT(*) FROM #SourceLineItems)
    BEGIN
        PRINT 'WARNING: LineItemMapping row count does not match SourceLineItems row count. Check remark parsing / column truncation.'
    END

    -- Display line item mapping for debugging
    PRINT 'LineItem ID Mapping:'
    SELECT OldId, NewId FROM #LineItemMapping

    -- Insert LineItemReferences using the mapping
    INSERT INTO [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].LineItemReference (
        lineItem, voucher, referingVouDfn, referencedVouDfn, referenced, value, remark
    )
    SELECT 
        ISNULL(lm.NewId, src.lineItem),  -- Use new line item ID if available
        map.NewId,  -- Use the NEW voucher ID from mapping
        src.referingVouDfn, src.referencedVouDfn, src.referenced, src.value, 
        'MIGBATCH:' + CAST(@BatchToken AS VARCHAR(36)) + ' | Migrated from old system on ' + CAST(GETDATE() AS VARCHAR) + ' | Original Voucher ID: ' + CAST(src.voucher AS VARCHAR) + ' -> New Voucher ID: ' + CAST(map.NewId AS VARCHAR)
    FROM [transaction].LineItemReference src
    INNER JOIN #VoucherIdMapping map ON src.voucher = map.OldId
    LEFT JOIN #LineItemMapping lm ON src.lineItem = lm.OldId

    PRINT 'LineItemReferences inserted successfully: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'

    -- Insert TaxTransactions using the mapping
    INSERT INTO [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].TaxTransaction (
        voucher, tax, taxableAmount, taxAmount, remark
    )
    SELECT 
        map.NewId,  -- Use the NEW voucher ID from mapping
        src.tax, src.taxableAmount, src.taxAmount, 
        'MIGBATCH:' + CAST(@BatchToken AS VARCHAR(36)) + ' | Migrated from old system on ' + CAST(GETDATE() AS VARCHAR) + ' | Original Voucher ID: ' + CAST(src.voucher AS VARCHAR) + ' -> New Voucher ID: ' + CAST(map.NewId AS VARCHAR)
    FROM [transaction].TaxTransaction src
    INNER JOIN #VoucherIdMapping map ON src.voucher = map.OldId

    PRINT 'TaxTransactions inserted successfully: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' records'
    PRINT 'All data migrated successfully with remarks added'
    PRINT 'Batch token for this run: ' + CAST(@BatchToken AS VARCHAR(36))
    
    -- Clean up
    IF OBJECT_ID('tempdb..#SourceVouchers') IS NOT NULL 
        DROP TABLE #SourceVouchers
    
    IF OBJECT_ID('tempdb..#SourceLineItems') IS NOT NULL 
        DROP TABLE #SourceLineItems
    
    IF OBJECT_ID('tempdb..#VoucherIdMapping') IS NOT NULL 
        DROP TABLE #VoucherIdMapping
    
    IF OBJECT_ID('tempdb..#LineItemMapping') IS NOT NULL 
        DROP TABLE #LineItemMapping
    
END TRY
BEGIN CATCH
    PRINT 'Error occurred: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR)
    PRINT 'Error Procedure: ' + ISNULL(ERROR_PROCEDURE(), 'N/A')
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR)
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR)
    
    -- Display current mapping for debugging
    IF OBJECT_ID('tempdb..#VoucherIdMapping') IS NOT NULL
    BEGIN
        PRINT 'Current Voucher ID Mapping:'
        SELECT * FROM #VoucherIdMapping
    END

    IF OBJECT_ID('tempdb..#LineItemMapping') IS NOT NULL
    BEGIN
        PRINT 'Current LineItem ID Mapping:'
        SELECT * FROM #LineItemMapping
    END
    
    -- Clean up on error
    IF OBJECT_ID('tempdb..#SourceVouchers') IS NOT NULL 
        DROP TABLE #SourceVouchers
    
    IF OBJECT_ID('tempdb..#SourceLineItems') IS NOT NULL 
        DROP TABLE #SourceLineItems
    
    IF OBJECT_ID('tempdb..#VoucherIdMapping') IS NOT NULL 
        DROP TABLE #VoucherIdMapping
    
    IF OBJECT_ID('tempdb..#LineItemMapping') IS NOT NULL 
        DROP TABLE #LineItemMapping
END CATCH





select * from [transaction].Voucher where remark like '%Migrated from old system%' 

select * from [transaction].LineItem where remark like '%No localCode match, mapped to default Article ID%' and article<>8490



select * from [transaction].LineItemReference where remark like '% Migrated from old system%' 

select * from [transaction].TaxTransaction where remark like '%Migrated from old system%' 

/*


delete [transaction].Voucher where remark like '%Migrated from old system%' 

delete [transaction].LineItem where remark like '%Migrated from old system%'



delete [transaction].LineItemReference where remark like '% Migrated from old system%' 

delete [transaction].TaxTransaction where remark like '% Migrated from old system%' 

*/



select * from article .Article where id in (
SELECT
    OriginalArticleId =
    CAST(
        LTRIM(RTRIM(
            SUBSTRING(
                remark,
                CHARINDEX('Original Article ID:', remark) + LEN('Original Article ID:'),
                CHARINDEX('->', remark, CHARINDEX('Original Article ID:', remark))
                    - (CHARINDEX('Original Article ID:', remark) + LEN('Original Article ID:'))
            )
        )) AS INT
    )
FROM [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].LineItem
where remark like  '%Mapped to default %'))






EXEC sp_addlinkedserver
    @server = 'BILOS_SERVER19',
    @srvproduct = '',
    @provider = 'MSOLEDBSQL',
    @datasrc = '192.168.1.13';


  
EXEC sp_addlinkedsrvlogin
    @rmtsrvname = 'BILOS_SERVER19',
    @useself = 'false',
    @rmtuser = 'User1',
    @rmtpassword = 'uxVRcH316j2RoJe';


select barCode,localCode,name from article .Article where id in (
  SELECT
    OriginalArticleId =
    CAST(
        LTRIM(RTRIM(
            SUBSTRING(
                remark,
                CHARINDEX('Original Article ID:', remark) + LEN('Original Article ID:'),
                CHARINDEX('->', remark, CHARINDEX('Original Article ID:', remark))
                    - (CHARINDEX('Original Article ID:', remark) + LEN('Original Article ID:'))
            )
        )) AS INT
    )
FROM [BILOS_SERVER19].[BILOS PASTRY 2019].[transaction].LineItem
WHERE Remark LIKE '%mapped to default Article ID%'
  AND Remark NOT LIKE '%Corrected Article%')  
