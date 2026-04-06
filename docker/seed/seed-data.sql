INSERT INTO parts ("Id", tenant_id, "PartNumber", "Name")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'default', 'PN-100', 'Motor Housing'),
    ('22222222-2222-2222-2222-222222222222', 'default', 'PN-200', 'Cooling Plate'),
    ('33333333-3333-3333-3333-333333333333', 'default', 'PN-300', 'Drive Bracket')
ON CONFLICT DO NOTHING;

INSERT INTO documents ("Id", tenant_id, "PartId", "FileName", "ContentType", "StoragePath")
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', 'default', '11111111-1111-1111-1111-111111111111', 'summary.txt', 'text/plain', 'generated://parts/PN-100'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', 'default', '22222222-2222-2222-2222-222222222222', 'summary.txt', 'text/plain', 'generated://parts/PN-200'),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', 'default', '33333333-3333-3333-3333-333333333333', 'summary.txt', 'text/plain', 'generated://parts/PN-300')
ON CONFLICT DO NOTHING;

INSERT INTO part_features ("Id", tenant_id, "PartId", usage_count, failure_rate, lifecycle, cost)
VALUES
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1', 'default', '11111111-1111-1111-1111-111111111111', 840, 0.08, 'mature', 4200.00),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', 'default', '22222222-2222-2222-2222-222222222222', 120, 0.03, 'active', 950.00),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3', 'default', '33333333-3333-3333-3333-333333333333', 40, 0.01, 'growth', 280.00)
ON CONFLICT DO NOTHING;

INSERT INTO digital_twin_state ("Id", tenant_id, "PartId", part_health, risk_score, trends)
VALUES
    ('cccccccc-cccc-cccc-cccc-ccccccccccc1', 'default', '11111111-1111-1111-1111-111111111111', 'WATCH', 0.58, '{"usageTrend":"RISING","failureTrend":"WATCH","lifecycleTrend":"MATURE","costBand":"MEDIUM"}'),
    ('cccccccc-cccc-cccc-cccc-ccccccccccc2', 'default', '22222222-2222-2222-2222-222222222222', 'GOOD', 0.22, '{"usageTrend":"STABLE","failureTrend":"STABLE","lifecycleTrend":"ACTIVE","costBand":"LOW"}'),
    ('cccccccc-cccc-cccc-cccc-ccccccccccc3', 'default', '33333333-3333-3333-3333-333333333333', 'GOOD', 0.12, '{"usageTrend":"STABLE","failureTrend":"STABLE","lifecycleTrend":"GROWTH","costBand":"LOW"}')
ON CONFLICT DO NOTHING;

INSERT INTO embeddings ("Id", tenant_id, "DocumentId", "ChunkText", "Vector", feedback_score, usage_count)
VALUES
    (
        'dddddddd-dddd-dddd-dddd-ddddddddddd1',
        'default',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',
        'Motor Housing is used in high-heat assembly scenarios and has a mature lifecycle profile.',
        ('[' || array_to_string(array_fill(0.001, ARRAY[1536]), ',') || ']')::vector,
        0.42,
        22
    ),
    (
        'dddddddd-dddd-dddd-dddd-ddddddddddd2',
        'default',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2',
        'Cooling Plate is a lower-risk active component with moderate usage and lower replacement cost.',
        ('[' || array_to_string(array_fill(0.002, ARRAY[1536]), ',') || ']')::vector,
        0.31,
        11
    ),
    (
        'dddddddd-dddd-dddd-dddd-ddddddddddd3',
        'default',
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3',
        'Drive Bracket is in growth stage and is often recommended for lighter duty configurations.',
        ('[' || array_to_string(array_fill(0.003, ARRAY[1536]), ',') || ']')::vector,
        0.18,
        5
    )
ON CONFLICT DO NOTHING;

INSERT INTO bom ("Id", tenant_id, "ParentPartId", "ChildPartId", "Quantity")
VALUES
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1', 'default', '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 2.000000),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2', 'default', '11111111-1111-1111-1111-111111111111', '33333333-3333-3333-3333-333333333333', 1.000000)
ON CONFLICT DO NOTHING;
