-- Script to convert payment status values from string to integer
-- This should be executed manually before applying the main migration

UPDATE Payments 
SET Status = CASE Status
    WHEN 'PENDING' THEN '1'
    WHEN 'SUCCESS' THEN '3'
    WHEN 'FAILED' THEN '4'
    WHEN 'EXPIRED' THEN '6'
    WHEN 'CANCELLED' THEN '5'
    ELSE '0'  -- Default to Created
END
WHERE Status IN ('PENDING', 'SUCCESS', 'FAILED', 'EXPIRED', 'CANCELLED');

-- Update Purpose to PaymentType mapping
UPDATE Payments 
SET Purpose = CASE Purpose
    WHEN 'SHOP_CREATION' THEN 'SHOP_REGISTRATION'
    ELSE Purpose
END
WHERE Purpose IS NOT NULL;