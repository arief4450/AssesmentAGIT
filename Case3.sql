-- ====================================================================================
-- 1. Schema and constraints
-- ====================================================================================

DROP TABLE IF EXISTS "PlanningSlots";
DROP TABLE IF EXISTS "Plannings";

CREATE TABLE "Plannings" (
    "PlanningId" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "RequestCode" VARCHAR(100) NOT NULL,
    "CandidateToken" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Status" VARCHAR(20) NOT NULL,
    CONSTRAINT "AK_Plannings_RequestCode" UNIQUE ("RequestCode")
);

CREATE TABLE "PlanningSlots" (
    "PlanningId" UUID NOT NULL,
    "SlotOrder" INT NOT NULL,
    "SlotName" VARCHAR(50) NOT NULL,
    "OriginalQuantity" NUMERIC NOT NULL CHECK ("OriginalQuantity" >= 0),
    "BalancedQuantity" NUMERIC NOT NULL CHECK ("BalancedQuantity" >= 0),
    "IsActive" BOOLEAN NOT NULL,
    PRIMARY KEY ("PlanningId", "SlotOrder"),
    CONSTRAINT "FK_PlanningSlots_Plannings" FOREIGN KEY ("PlanningId")
        REFERENCES "Plannings" ("PlanningId") ON DELETE CASCADE
);

-- ====================================================================================
-- 2. Seed data
-- ====================================================================================

CREATE OR REPLACE FUNCTION SeedPlanning(
    p_RequestCode VARCHAR, p_Status VARCHAR, 
    q1 NUMERIC, q2 NUMERIC, q3 NUMERIC, q4 NUMERIC, q5 NUMERIC, q6 NUMERIC, q7 NUMERIC,
    b1 NUMERIC, b2 NUMERIC, b3 NUMERIC, b4 NUMERIC, b5 NUMERIC, b6 NUMERIC, b7 NUMERIC
) RETURNS VOID AS $$
DECLARE
    new_id UUID := gen_random_uuid();
BEGIN
    INSERT INTO "Plannings" ("PlanningId", "RequestCode", "CandidateToken", "Status")
    VALUES (new_id, p_RequestCode, 'VEH-Arief_Achmadi', p_Status);

    INSERT INTO "PlanningSlots" ("PlanningId", "SlotOrder", "SlotName", "OriginalQuantity", "BalancedQuantity", "IsActive")
    VALUES 
    (new_id, 1, 'Senin',   q1, b1, q1 > 0 OR b1 > 0),
    (new_id, 2, 'Selasa',  q2, b2, q2 > 0 OR b2 > 0),
    (new_id, 3, 'Rabu',    q3, b3, q3 > 0 OR b3 > 0),
    (new_id, 4, 'Kamis',   q4, b4, q4 > 0 OR b4 > 0),
    (new_id, 5, 'Jumat',   q5, b5, q5 > 0 OR b5 > 0),
    (new_id, 6, 'Sabtu',   q6, b6, q6 > 0 OR b6 > 0),
    (new_id, 7, 'Minggu',  q7, b7, q7 > 0 OR b7 > 0);
END;
$$ LANGUAGE plpgsql;

DO $$ 
BEGIN
    -- 1. Normal case (4,5,1,7,6,4,0 -> 27)
    PERFORM SeedPlanning('REQ-001-NORMAL', 'Success',  4,5,1,7,6,4,0,  5,4,4,5,5,4,0);
    
    -- 2. All 0
    PERFORM SeedPlanning('REQ-002-ZERO', 'Success',    0,0,0,0,0,0,0,  0,0,0,0,0,0,0);
    
    -- 3. One slot active
    PERFORM SeedPlanning('REQ-003-ONESLOT', 'Success', 10,0,0,0,0,0,0, 10,0,0,0,0,0,0);
    
    -- 4. Tie
    PERFORM SeedPlanning('REQ-004-TIE', 'Success',     5,5,5,0,0,0,0,  5,5,5,0,0,0,0);
    
    -- 5. Total bersisa (Not perfectly divisible, 4+5+1=10 over 3 days -> 4,3,3)
    PERFORM SeedPlanning('REQ-005-REMAINDER', 'Success', 4,5,1,0,0,0,0, 4,3,3,0,0,0,0);
    
    -- 6. Large value
    PERFORM SeedPlanning('REQ-006-LARGE', 'Success',   1000,500,2000,0,0,0,0, 1167,1167,1166,0,0,0,0);
    
    -- 7. Another normal case
    PERFORM SeedPlanning('REQ-007-NORMAL2', 'Success', 10,20,30,40,50,0,0, 30,30,30,30,30,0,0);
    
    -- 8. ANOMALY: Inactive slot but balanced quantity > 0 (Intentional bad data)
    -- Setting slot 7 to inactive but having a balanced quantity of 5.
    INSERT INTO "Plannings" ("PlanningId", "RequestCode", "CandidateToken", "Status") VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'REQ-008-ANOMALY-INACTIVE', 'VEH-Arief_Achmadi', 'Error');
    INSERT INTO "PlanningSlots" VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 7, 'Minggu', 0, 5, false);

    -- 9. ANOMALY: Total mismatch (Original sum = 10, Balanced sum = 15)
    INSERT INTO "Plannings" ("PlanningId", "RequestCode", "CandidateToken", "Status") VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'REQ-009-ANOMALY-TOTAL', 'VEH-Arief_Achmadi', 'Error');
    INSERT INTO "PlanningSlots" VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, 'Senin', 10, 15, true);

    -- 10. ANOMALY: Incomplete slot details (Only 3 slots instead of 7)
    INSERT INTO "Plannings" ("PlanningId", "RequestCode", "CandidateToken", "Status") VALUES ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'REQ-010-ANOMALY-MISSING', 'VEH-Arief_Achmadi', 'Error');
    INSERT INTO "PlanningSlots" VALUES 
        ('cccccccc-cccc-cccc-cccc-cccccccccccc', 1, 'Senin', 5, 5, true),
        ('cccccccc-cccc-cccc-cccc-cccccccccccc', 2, 'Selasa', 5, 5, true),
        ('cccccccc-cccc-cccc-cccc-cccccccccccc', 3, 'Rabu', 5, 5, true);
END $$;

-- Cleanup helper function
DROP FUNCTION SeedPlanning;

-- ====================================================================================
-- 3. Total validation query
-- ====================================================================================
SELECT 
    p."PlanningId",
    COALESCE(SUM(s."OriginalQuantity"), 0) AS "OriginalTotal",
    COALESCE(SUM(s."BalancedQuantity"), 0) AS "BalancedTotal",
    (COALESCE(SUM(s."OriginalQuantity"), 0) = COALESCE(SUM(s."BalancedQuantity"), 0)) AS "IsTotalValid"
FROM "Plannings" p
LEFT JOIN "PlanningSlots" s ON p."PlanningId" = s."PlanningId"
GROUP BY p."PlanningId";

-- ====================================================================================
-- 4. History query
-- ====================================================================================
SELECT 
    p."RequestCode",
    p."CreatedAt",
    COUNT(s."SlotOrder") FILTER (WHERE s."IsActive" = true) AS "ActiveSlotCount",
    COALESCE(SUM(s."OriginalQuantity"), 0) AS "OriginalTotal",
    COALESCE(SUM(s."BalancedQuantity"), 0) AS "BalancedTotal",
    p."Status"
FROM "Plannings" p
LEFT JOIN "PlanningSlots" s ON p."PlanningId" = s."PlanningId"
GROUP BY p."PlanningId", p."RequestCode", p."CreatedAt", p."Status"
ORDER BY p."CreatedAt" DESC;

-- ====================================================================================
-- 5. Anomaly query
-- ====================================================================================
WITH PlanningTotals AS (
    SELECT 
        "PlanningId",
        SUM("OriginalQuantity") AS OrigTotal,
        SUM("BalancedQuantity") AS BalTotal,
        COUNT(*) AS SlotCount,
        COUNT(*) FILTER (WHERE "IsActive" = false AND "BalancedQuantity" > 0) AS InvalidInactiveCount
    FROM "PlanningSlots"
    GROUP BY "PlanningId"
),
DuplicateRequests AS (
    SELECT "RequestCode"
    FROM "Plannings"
    GROUP BY "RequestCode"
    HAVING COUNT(*) > 1
)
SELECT p.*
FROM "Plannings" p
LEFT JOIN PlanningTotals pt ON p."PlanningId" = pt."PlanningId"
WHERE 
    pt.InvalidInactiveCount > 0 
    OR pt.OrigTotal != pt.BalTotal 
    OR pt.SlotCount < 7 
    OR p."RequestCode" IN (SELECT "RequestCode" FROM DuplicateRequests);

-- ====================================================================================
-- 6. Largest adjustments
-- ====================================================================================
SELECT 
    "PlanningId",
    "SlotOrder",
    "SlotName",
    "OriginalQuantity",
    "BalancedQuantity",
    ABS("BalancedQuantity" - "OriginalQuantity") AS "AbsoluteChange"
FROM "PlanningSlots"
ORDER BY ABS("BalancedQuantity" - "OriginalQuantity") DESC, "SlotOrder" ASC
LIMIT 3;

-- ====================================================================================
-- 7. Atomic save
-- ====================================================================================
/*
BEGIN;

INSERT INTO "Plannings" ("PlanningId", "RequestCode", "CandidateToken", "Status")
VALUES ('123e4567-e89b-12d3-a456-426614174000', 'REQ-NEW-01', 'VEH-Arief_Achmadi', 'Success');

INSERT INTO "PlanningSlots" ("PlanningId", "SlotOrder", "SlotName", "OriginalQuantity", "BalancedQuantity", "IsActive")
VALUES 
('123e4567-e89b-12d3-a456-426614174000', 1, 'Senin', 4, 4, true),
('123e4567-e89b-12d3-a456-426614174000', 2, 'Selasa', 5, 5, true);
-- (Insert all 7 slots here...)

COMMIT;
-- If any error occurs (e.g. RequestCode already exists), the transaction fails, 
-- and we issue a ROLLBACK; preventing partial data.
*/

-- ====================================================================================
-- 8. Latest processing version
-- ====================================================================================
/*
-- 1. Add the column:
ALTER TABLE "Plannings" ADD COLUMN "RebalanceRun" INT NOT NULL DEFAULT 1;

-- 2. Query to get the latest run per RequestCode using PostgreSQL DISTINCT ON:
SELECT DISTINCT ON ("RequestCode") *
FROM "Plannings"
ORDER BY "RequestCode", "RebalanceRun" DESC;
*/

-- ====================================================================================
-- 9. Index proposal
-- ====================================================================================
/*
-- Proposal 1: The RequestCode is already covered by the UNIQUE constraint ("AK_Plannings_RequestCode"),
-- which automatically creates a B-Tree index. No new index is needed for RequestCode lookups.

-- Proposal 2: For CreatedAt and Status, we often query "Show me recent successful plannings".
CREATE INDEX "IX_Plannings_Status_CreatedAt" ON "Plannings" ("Status", "CreatedAt" DESC);

-- Benefits:
-- - Drastically speeds up History queries (Query #4) that filter by status and order by newest first.
-- - Enables index-only scans if we only select these columns.

-- Costs/Write-penalty:
-- - Slightly increases the time it takes to execute INSERT operations because the DB must update the B-Tree.
-- - Consumes additional disk space.
*/

-- ====================================================================================
-- 10. Safe migration
-- ====================================================================================
/*
-- Step 1. Validation Before: 
-- Record the grand total of all old quantities.
-- SELECT SUM("SeninQty" + "SelasaQty" + "RabuQty" + "KamisQty" + "JumatQty" + "SabtuQty" + "MingguQty") FROM "OldPlannings";

-- Step 2. Migration within a Transaction:
BEGIN;

-- Insert Monday (Order 1)
INSERT INTO "PlanningSlots" ("PlanningId", "SlotOrder", "SlotName", "OriginalQuantity", "BalancedQuantity", "IsActive")
SELECT "PlanningId", 1, 'Senin', "SeninQty", "SeninBalanced", ("SeninQty" > 0) FROM "OldPlannings";

-- Insert Tuesday (Order 2)
INSERT INTO "PlanningSlots" ("PlanningId", "SlotOrder", "SlotName", "OriginalQuantity", "BalancedQuantity", "IsActive")
SELECT "PlanningId", 2, 'Selasa', "SelasaQty", "SelasaBalanced", ("SelasaQty" > 0) FROM "OldPlannings";

-- (Repeat for Wednesday to Sunday)

COMMIT;

-- Step 3. Validation After:
-- Verify the new table total matches the old table total.
-- SELECT SUM("OriginalQuantity") FROM "PlanningSlots";
-- Compare with the result from Step 1. They must be exactly equal.

-- Step 4. Cleanup:
-- Once application code is safely reading from "PlanningSlots", drop the old columns from "Plannings".
-- ALTER TABLE "OldPlannings" DROP COLUMN "SeninQty", DROP COLUMN "SeninBalanced", etc.
*/
