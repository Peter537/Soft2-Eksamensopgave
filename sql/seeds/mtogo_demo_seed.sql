\set ON_ERROR_STOP on

-- ===========================================
-- MToGo Demo Seed (repeatable)
-- ===========================================
--
-- Goal: safe to run repeatedly; converges to the same dataset.
-- Strategy: delete seeded ID ranges, then insert deterministic rows.
--
-- Databases:
--   - mtogo_partners (Partners + MenuItems)
--   - mtogo_agents   (Agents)
--   - mtogo_legacy   (Customers)
--   - mtogo_orders   (Orders + OrderItems)
--   - mtogo_feedback (reviews)

\echo '== MToGo demo seed (repeatable) =='

-- BCrypt hash for password: testopy123 (workFactor=12)
-- Generated via BCrypt.Net.BCrypt.HashPassword("testopy123", workFactor: 12)
\set demo_password_hash '$2a$12$D7v2.s3bTItfJzkjj.7zP.7T/UtR9k5WKJpgsbX3eh3uF.5G4lrU6'

-- ==================================================
-- Partners DB
-- ==================================================

\connect mtogo_partners

DO $$
DECLARE
  base_ts CONSTANT TIMESTAMPTZ := '2025-01-01 12:00:00+00';
  demo_hash CONSTANT TEXT := '$2a$12$D7v2.s3bTItfJzkjj.7zP.7T/UtR9k5WKJpgsbX3eh3uF.5G4lrU6';

  PARTNER_COUNT CONSTANT INT := 111;
  ITEMS_PER_PARTNER CONSTANT INT := 15;

  cities TEXT[] := ARRAY[
    'Copenhagen','Aarhus','Odense','Aalborg','Esbjerg','Helsingør','Lyngby','Herlev','Roskilde','Hvidovre','Frederiksberg','Gentofte'
  ];

  partner_kinds TEXT[] := ARRAY[
    'Pizza','Sushi','Burger','Kebab','Thai','Indian','BBQ','Wok','Pasta','Deli','Café','Bistro'
  ];

  partner_suffixes TEXT[] := ARRAY[
    'House','Kitchen','Bar & Grill','Express','Corner','Family','Street Food','Takeaway','Eatery','Spot'
  ];

  p INT;
  i INT;
  addr TEXT;
  city_name TEXT;
  partner_name TEXT;
  partner_kind TEXT;
  partner_suffix TEXT;
  item_id INT;
  item_name TEXT;
  item_price NUMERIC(10,2);
BEGIN
  -- Repeatable wipe (seeded ranges)
  DELETE FROM "MenuItems" WHERE "Id" BETWEEN 1000 AND (PARTNER_COUNT*1000 + ITEMS_PER_PARTNER);
  DELETE FROM "Partners" WHERE "Id" BETWEEN 1 AND PARTNER_COUNT;

  -- Partner 1: the known demo partner login
  INSERT INTO "Partners" ("Id","Name","Address","Email","Password","IsActive","IsDeleted","CreatedAt","UpdatedAt")
  VALUES (1,'Herlev McDonald''s','Herlev Bygade 1, 2730 Herlev','herlev@mcdonalds.dk',demo_hash,true,false,base_ts,NULL);

  -- Other partners
  FOR p IN 2..PARTNER_COUNT LOOP
    city_name := cities[1 + mod(abs(hashtext('pcity-' || p::TEXT)), array_length(cities,1))];
    addr := (1 + mod(abs(hashtext('paddr-' || p::TEXT)), 400))::INT::TEXT || ' Main Street, ' || city_name;

    partner_kind := partner_kinds[1 + mod(abs(hashtext('pkind-' || p::TEXT)), array_length(partner_kinds,1))];
    partner_suffix := partner_suffixes[1 + mod(abs(hashtext('psfx-' || p::TEXT)), array_length(partner_suffixes,1))];
    partner_name := left(city_name || ' ' || partner_kind || ' ' || partner_suffix, 100);

    INSERT INTO "Partners" ("Id","Name","Address","Email","Password","IsActive","IsDeleted","CreatedAt","UpdatedAt")
    VALUES (
      p,
      partner_name,
      left(addr, 500),
      left('partner-' || p::TEXT || '@mtogo.dk', 255),
      demo_hash,
      true,
      false,
      base_ts - ((p % 30)::INT || ' days')::INTERVAL,
      NULL
    );
  END LOOP;

  -- Menu items (explicit IDs so OrderItems.FoodItemId can reference them)
  -- Partner 1 has curated names/prices; others are deterministic.

  -- Partner 1 (ids 1001..1015)
  INSERT INTO "MenuItems" ("Id","PartnerId","Name","Price","IsActive","CreatedAt","UpdatedAt") VALUES
    (1001, 1, 'Big Mac',          60.00, true, base_ts, NULL),
    (1002, 1, 'McNuggets 9 pc',   40.00, true, base_ts, NULL),
    (1003, 1, 'Fries (M)',        20.00, true, base_ts, NULL),
    (1004, 1, 'McFlurry',         25.00, true, base_ts, NULL),
    (1005, 1, 'Quarter Pounder',  55.00, true, base_ts, NULL),
    (1006, 1, 'Chicken Burger',   50.00, true, base_ts, NULL),
    (1007, 1, 'Veggie Burger',    53.00, true, base_ts, NULL),
    (1008, 1, 'Happy Meal',       38.00, true, base_ts, NULL),
    (1009, 1, 'Salad',            30.00, true, base_ts, NULL),
    (1010, 1, 'Soda 330ml',       15.00, true, base_ts, NULL),
    (1011, 1, 'Coffee',           10.00, true, base_ts, NULL),
    (1012, 1, 'Apple Pie',        20.00, true, base_ts, NULL),
    (1013, 1, 'Onion Rings',      18.00, true, base_ts, NULL),
    (1014, 1, 'Milkshake',        33.00, true, base_ts, NULL),
    (1015, 1, 'Chicken Wrap',     46.00, true, base_ts, NULL);

  FOR p IN 2..PARTNER_COUNT LOOP
    FOR i IN 1..ITEMS_PER_PARTNER LOOP
      item_id := p*1000 + i;
      item_name := left('Item ' || i::TEXT || ' (P' || p::TEXT || ')', 200);
      item_price := (25 + mod(abs(hashtext('menuprice-' || p::TEXT || '-' || i::TEXT)), 101))::NUMERIC(10,2);

      INSERT INTO "MenuItems" ("Id","PartnerId","Name","Price","IsActive","CreatedAt","UpdatedAt")
      VALUES (item_id, p, item_name, item_price, true, base_ts - ((p % 30)::INT || ' days')::INTERVAL, NULL);
    END LOOP;
  END LOOP;

  PERFORM setval(pg_get_serial_sequence('"Partners"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Partners"));
  PERFORM setval(pg_get_serial_sequence('"MenuItems"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "MenuItems"));
END
$$;

-- ==================================================
-- Agents DB
-- ==================================================

\connect mtogo_agents

DO $$
DECLARE
  base_ts CONSTANT TIMESTAMPTZ := '2025-01-01 12:00:00+00';
  demo_hash CONSTANT TEXT := '$2a$12$D7v2.s3bTItfJzkjj.7zP.7T/UtR9k5WKJpgsbX3eh3uF.5G4lrU6';

  AGENT_COUNT CONSTANT INT := 121;

  first_names TEXT[] := ARRAY[
    'Oliver','William','Noah','Lucas','Emil','Oscar','Frederik','Victor','Magnus','Alexander','Elias','Theo',
    'Sofia','Emma','Ida','Freja','Clara','Laura','Alma','Anna','Isabella','Julie','Nora','Maja'
  ];

  last_names TEXT[] := ARRAY[
    'Nielsen','Jensen','Hansen','Pedersen','Andersen','Christensen','Larsen','Sørensen','Rasmussen','Jørgensen',
    'Petersen','Madsen','Kristensen','Olsen','Thomsen','Poulsen','Johansen','Knudsen','Mortensen','Christiansen'
  ];

  a INT;
  agent_name TEXT;
BEGIN
  DELETE FROM "Agents" WHERE "Id" BETWEEN 1 AND AGENT_COUNT;

  -- Agent 1: known demo login
  INSERT INTO "Agents" ("Id","Name","Email","Password","IsActive","IsDeleted","CreatedAt","UpdatedAt")
  VALUES (1,'Opy Nielsen','agent-opy@mtogo.dk',demo_hash,true,false,base_ts,NULL);

  FOR a IN 2..AGENT_COUNT LOOP
    agent_name := left(
      first_names[1 + mod(abs(hashtext('afn-' || a::TEXT)), array_length(first_names,1))]
      || ' ' ||
      last_names[1 + mod(abs(hashtext('aln-' || a::TEXT)), array_length(last_names,1))],
      100
    );

    INSERT INTO "Agents" ("Id","Name","Email","Password","IsActive","IsDeleted","CreatedAt","UpdatedAt")
    VALUES (
      a,
      agent_name,
      left('agent-' || a::TEXT || '@mtogo.dk', 255),
      demo_hash,
      true,
      false,
      base_ts - ((a % 60)::INT || ' days')::INTERVAL,
      NULL
    );
  END LOOP;

  PERFORM setval(pg_get_serial_sequence('"Agents"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Agents"));
END
$$;

-- ==================================================
-- Legacy DB (Customers)
-- ==================================================

\connect mtogo_legacy

DO $$
DECLARE
  base_ts CONSTANT TIMESTAMPTZ := '2025-01-01 12:00:00+00';
  demo_hash CONSTANT TEXT := '$2a$12$D7v2.s3bTItfJzkjj.7zP.7T/UtR9k5WKJpgsbX3eh3uF.5G4lrU6';

  CUSTOMER_COUNT CONSTANT INT := 1201;

  cities TEXT[] := ARRAY[
    'Copenhagen','Aarhus','Odense','Aalborg','Esbjerg','Helsingør','Lyngby','Herlev','Roskilde','Hvidovre','Frederiksberg','Gentofte'
  ];

  first_names TEXT[] := ARRAY[
    'Oliver','William','Noah','Lucas','Emil','Oscar','Frederik','Victor','Magnus','Alexander','Elias','Theo',
    'Sofia','Emma','Ida','Freja','Clara','Laura','Alma','Anna','Isabella','Julie','Nora','Maja'
  ];

  last_names TEXT[] := ARRAY[
    'Nielsen','Jensen','Hansen','Pedersen','Andersen','Christensen','Larsen','Sørensen','Rasmussen','Jørgensen',
    'Petersen','Madsen','Kristensen','Olsen','Thomsen','Poulsen','Johansen','Knudsen','Mortensen','Christiansen'
  ];

  c INT;
  addr TEXT;
  customer_name TEXT;
BEGIN
  DELETE FROM "Customers" WHERE "Id" BETWEEN 1 AND CUSTOMER_COUNT;

  -- Customer 1: known demo login
  INSERT INTO "Customers" (
    "Id","Name","Email","DeliveryAddress","NotificationMethod","Password","PhoneNumber","LanguagePreference","IsDeleted","DeletedAt"
  ) VALUES (
    1,
    'Opy Nielsen',
    'opy@outlook.dk',
    'Testvej 2, 2000 Copenhagen',
    0,
    demo_hash,
    NULL,
    0,
    false,
    NULL
  );

  FOR c IN 2..CUSTOMER_COUNT LOOP
    addr := (1 + mod(abs(hashtext('addr-' || c::TEXT)), 400))::INT::TEXT
      || ' Citizen Road, '
      || cities[1 + mod(abs(hashtext('city-' || c::TEXT)), array_length(cities,1))];

    customer_name := left(
      first_names[1 + mod(abs(hashtext('cfn-' || c::TEXT)), array_length(first_names,1))]
      || ' ' ||
      last_names[1 + mod(abs(hashtext('cln-' || c::TEXT)), array_length(last_names,1))],
      255
    );

    INSERT INTO "Customers" (
      "Id","Name","Email","DeliveryAddress","NotificationMethod","Password","PhoneNumber","LanguagePreference","IsDeleted","DeletedAt"
    ) VALUES (
      c,
      customer_name,
      left('customer-' || c::TEXT || '@mtogo.dk', 255),
      addr,
      0,
      demo_hash,
      NULL,
      0,
      false,
      NULL
    );
  END LOOP;

  PERFORM setval(pg_get_serial_sequence('"Customers"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Customers"));
END
$$;

-- ==================================================
-- Orders DB
-- ==================================================

\connect mtogo_orders

DO $$
DECLARE
  base_ts CONSTANT TIMESTAMPTZ := '2025-01-01 12:00:00+00';

  TOTAL_ORDERS CONSTANT INT := 1400;
  DELIVERED_MIN CONSTANT INT := 1000;
  IN_PROGRESS_MIN CONSTANT INT := 50;

  PARTNER_COUNT CONSTANT INT := 111;
  AGENT_COUNT CONSTANT INT := 121;
  CUSTOMER_COUNT CONSTANT INT := 1201;
  ITEMS_PER_PARTNER CONSTANT INT := 15;

  cities TEXT[] := ARRAY[
    'Copenhagen','Aarhus','Odense','Aalborg','Esbjerg','Helsingør','Lyngby','Herlev','Roskilde','Hvidovre','Frederiksberg','Gentofte'
  ];

  i INT;
  j INT;
  num_items INT;
  item_index INT;

  status_val INT;
  partner_id INT;
  agent_id INT;
  customer_id INT;

  fee_delivery NUMERIC(10,2);
  fee_service NUMERIC(10,2);
  minutes_est INT;
  dist_km NUMERIC(10,1);
  created_ts TIMESTAMPTZ;

  item_qty INT;
  food_item_id INT;
  unit_price NUMERIC(10,2);
  item_name TEXT;
  delivery_address TEXT;

  in_progress_count INT;
BEGIN
  -- Repeatable wipe
  DELETE FROM "OrderItems" WHERE ("OrderId" BETWEEN 1 AND TOTAL_ORDERS) OR ("OrderId" BETWEEN 20001 AND 20010);
  DELETE FROM "Orders" WHERE ("Id" BETWEEN 1 AND TOTAL_ORDERS) OR ("Id" BETWEEN 20001 AND 20010);

  -- Special deterministic demo orders for known demo logins.
  INSERT INTO "Orders" (
    "Id","CustomerId","PartnerId","AgentId","DeliveryAddress",
    "DeliveryFee","ServiceFee","TotalAmount","Distance","EstimatedMinutes",
    "Status","CreatedAt"
  ) VALUES
    -- Connected together (one is just created / Placed)
    (20001, 1, 1, NULL, 'Testvej 2, 2000 Copenhagen', 19.00, 8.00, 0.00, '2.1 km', 35, 0, base_ts - INTERVAL '12 minutes'),
    (20002, 1, 1, 1,    'Testvej 2, 2000 Copenhagen', 19.00, 8.00, 0.00, '2.1 km', 35, 3, base_ts - INTERVAL '42 minutes'),
    (20003, 1, 1, 1,    'Testvej 2, 2000 Copenhagen', 19.00, 8.00, 0.00, '2.1 km', 35, 4, base_ts - INTERVAL '58 minutes'),

    -- Extra in-progress for AgentId=1 (does not involve customer 1 or partner 1)
    (20004, 200, 2, 1,  (1 + mod(abs(hashtext('addr-' || 200::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 200::TEXT)), array_length(cities,1))], 16.50, 7.50, 0.00, '4.8 km', 44, 3, base_ts - INTERVAL '25 minutes'),
    (20005, 201, 4, 1,  (1 + mod(abs(hashtext('addr-' || 201::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 201::TEXT)), array_length(cities,1))], 22.00, 9.00, 0.00, '6.2 km', 52, 4, base_ts - INTERVAL '33 minutes'),
    (20006, 202, 5, 1,  (1 + mod(abs(hashtext('addr-' || 202::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 202::TEXT)), array_length(cities,1))], 14.00, 6.50, 0.00, '3.4 km', 38, 1, base_ts - INTERVAL '18 minutes'),

    -- Extra in-progress for PartnerId=1 (does not involve customer 1 or agent 1)
    (20007, 300, 1, NULL,(1 + mod(abs(hashtext('addr-' || 300::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 300::TEXT)), array_length(cities,1))], 18.00, 8.50, 0.00, '3.0 km', 41, 1, base_ts - INTERVAL '21 minutes'),
    (20008, 301, 1, 2,   (1 + mod(abs(hashtext('addr-' || 301::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 301::TEXT)), array_length(cities,1))], 18.00, 8.50, 0.00, '3.0 km', 41, 3, base_ts - INTERVAL '47 minutes'),
    (20009, 302, 1, 3,   (1 + mod(abs(hashtext('addr-' || 302::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 302::TEXT)), array_length(cities,1))], 18.00, 8.50, 0.00, '3.0 km', 41, 4, base_ts - INTERVAL '55 minutes'),
    (20010, 303, 1, 4,   (1 + mod(abs(hashtext('addr-' || 303::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || 303::TEXT)), array_length(cities,1))], 18.00, 8.50, 0.00, '3.0 km', 41, 1, base_ts - INTERVAL '29 minutes');

  INSERT INTO "OrderItems" ("Id","OrderId","FoodItemId","Name","Quantity","UnitPrice") VALUES
    (20001*100 + 1, 20001, 1001, 'Big Mac', 1, 60.00),
    (20001*100 + 2, 20001, 1003, 'Fries (M)', 1, 20.00),
    (20002*100 + 1, 20002, 1002, 'McNuggets 9 pc', 1, 40.00),
    (20002*100 + 2, 20002, 1010, 'Soda 330ml', 2, 15.00),
    (20003*100 + 1, 20003, 1005, 'Quarter Pounder', 1, 55.00),
    (20003*100 + 2, 20003, 1014, 'Milkshake', 1, 33.00),

    (20004*100 + 1, 20004, 2001, 'Item 1 (P2)', 1, (25 + mod(abs(hashtext('menuprice-2-1')), 101))::NUMERIC(10,2)),
    (20004*100 + 2, 20004, 2003, 'Item 3 (P2)', 2, (25 + mod(abs(hashtext('menuprice-2-3')), 101))::NUMERIC(10,2)),

    (20005*100 + 1, 20005, 4002, 'Item 2 (P4)', 1, (25 + mod(abs(hashtext('menuprice-4-2')), 101))::NUMERIC(10,2)),
    (20005*100 + 2, 20005, 4005, 'Item 5 (P4)', 1, (25 + mod(abs(hashtext('menuprice-4-5')), 101))::NUMERIC(10,2)),

    (20006*100 + 1, 20006, 5001, 'Item 1 (P5)', 2, (25 + mod(abs(hashtext('menuprice-5-1')), 101))::NUMERIC(10,2)),
    (20006*100 + 2, 20006, 5007, 'Item 7 (P5)', 1, (25 + mod(abs(hashtext('menuprice-5-7')), 101))::NUMERIC(10,2)),

    (20007*100 + 1, 20007, 1006, 'Chicken Burger', 1, 50.00),
    (20007*100 + 2, 20007, 1010, 'Soda 330ml', 1, 15.00),

    (20008*100 + 1, 20008, 1001, 'Big Mac', 1, 60.00),
    (20008*100 + 2, 20008, 1011, 'Coffee', 2, 10.00),

    (20009*100 + 1, 20009, 1002, 'McNuggets 9 pc', 1, 40.00),
    (20009*100 + 2, 20009, 1004, 'McFlurry', 1, 25.00),

    (20010*100 + 1, 20010, 1005, 'Quarter Pounder', 1, 55.00),
    (20010*100 + 2, 20010, 1003, 'Fries (M)', 1, 20.00);

  -- Base orders 1..TOTAL_ORDERS (deterministic)
  FOR i IN 1..TOTAL_ORDERS LOOP
    customer_id := 1 + mod(abs(hashtext('ocust-' || i::TEXT)), CUSTOMER_COUNT);
    partner_id := 1 + mod(abs(hashtext('opartner-' || i::TEXT)), PARTNER_COUNT);

    agent_id := CASE
      WHEN mod(abs(hashtext('oagent-null-' || i::TEXT)), 100) < 92 THEN 1 + mod(abs(hashtext('oagent-' || i::TEXT)), AGENT_COUNT)
      ELSE NULL
    END;

    delivery_address := CASE
      WHEN customer_id = 1 THEN 'Testvej 2, 2000 Copenhagen'
      ELSE (1 + mod(abs(hashtext('addr-' || customer_id::TEXT)), 400))::INT::TEXT || ' Citizen Road, ' || cities[1 + mod(abs(hashtext('city-' || customer_id::TEXT)), array_length(cities,1))]
    END;

    IF i <= DELIVERED_MIN THEN
      status_val := 5;
    ELSE
      IF mod(abs(hashtext('ostatus-' || i::TEXT)), 100) < 65 THEN
        status_val := 5;
      ELSIF mod(abs(hashtext('ostatus-' || i::TEXT)), 100) < 80 THEN
        status_val := 1;
      ELSIF mod(abs(hashtext('ostatus-' || i::TEXT)), 100) < 92 THEN
        status_val := 3;
      ELSE
        status_val := 4;
      END IF;
    END IF;

    fee_delivery := round((10 + (mod(abs(hashtext('odelivery-' || i::TEXT)), 3001)::NUMERIC / 100))::NUMERIC, 2);
    fee_service := round((5 + (mod(abs(hashtext('oservice-' || i::TEXT)), 2001)::NUMERIC / 100))::NUMERIC, 2);

    minutes_est := 15 + mod(abs(hashtext('ominutes-' || i::TEXT)), 60);
    dist_km := round((0.8 + (mod(abs(hashtext('odistance-' || i::TEXT)), 96)::NUMERIC / 10))::NUMERIC, 1);

    created_ts := base_ts
      - ((1 + mod(abs(hashtext('odays-' || i::TEXT)), 180))::INT || ' days')::INTERVAL
      - (mod(abs(hashtext('oseconds-' || i::TEXT)), 86400)::INT || ' seconds')::INTERVAL;

    INSERT INTO "Orders" (
      "Id","CustomerId","PartnerId","AgentId","DeliveryAddress",
      "DeliveryFee","ServiceFee","TotalAmount","Distance","EstimatedMinutes",
      "Status","CreatedAt"
    ) VALUES (
      i, customer_id, partner_id, agent_id, delivery_address,
      fee_delivery, fee_service, 0.00, dist_km::TEXT || ' km', minutes_est,
      status_val, created_ts
    );

    num_items := 1 + mod(abs(hashtext('onitems-' || i::TEXT)), 4);
    FOR j IN 1..num_items LOOP
      item_qty := 1 + mod(abs(hashtext('oqty-' || i::TEXT || '-' || j::TEXT)), 3);
      item_index := 1 + mod(abs(hashtext('oitem-' || i::TEXT || '-' || j::TEXT)), ITEMS_PER_PARTNER);
      food_item_id := partner_id*1000 + item_index;

      IF partner_id = 1 THEN
        unit_price := CASE item_index
          WHEN 1 THEN 60
          WHEN 2 THEN 40
          WHEN 3 THEN 20
          WHEN 4 THEN 25
          WHEN 5 THEN 55
          WHEN 6 THEN 50
          WHEN 7 THEN 53
          WHEN 8 THEN 38
          WHEN 9 THEN 30
          WHEN 10 THEN 15
          WHEN 11 THEN 10
          WHEN 12 THEN 20
          WHEN 13 THEN 18
          WHEN 14 THEN 33
          ELSE 46
        END;

        item_name := CASE item_index
          WHEN 1 THEN 'Big Mac'
          WHEN 2 THEN 'McNuggets 9 pc'
          WHEN 3 THEN 'Fries (M)'
          WHEN 4 THEN 'McFlurry'
          WHEN 5 THEN 'Quarter Pounder'
          WHEN 6 THEN 'Chicken Burger'
          WHEN 7 THEN 'Veggie Burger'
          WHEN 8 THEN 'Happy Meal'
          WHEN 9 THEN 'Salad'
          WHEN 10 THEN 'Soda 330ml'
          WHEN 11 THEN 'Coffee'
          WHEN 12 THEN 'Apple Pie'
          WHEN 13 THEN 'Onion Rings'
          WHEN 14 THEN 'Milkshake'
          ELSE 'Chicken Wrap'
        END;
      ELSE
        unit_price := (25 + mod(abs(hashtext('menuprice-' || partner_id::TEXT || '-' || item_index::TEXT)), 101));
        item_name := left('Item ' || item_index::TEXT || ' (P' || partner_id::TEXT || ')', 200);
      END IF;

      INSERT INTO "OrderItems" ("Id","OrderId","FoodItemId","Name","Quantity","UnitPrice")
      VALUES (i*100 + j, i, food_item_id, item_name, item_qty, unit_price);
    END LOOP;
  END LOOP;

  SELECT COUNT(*) INTO in_progress_count FROM "Orders" WHERE "Id" BETWEEN 1 AND TOTAL_ORDERS AND "Status" IN (1,3,4);
  IF in_progress_count < IN_PROGRESS_MIN THEN
    UPDATE "Orders"
    SET "Status" = 1
    WHERE "Id" IN (
      SELECT "Id" FROM "Orders" WHERE "Status" = 5 AND "Id" BETWEEN 1 AND TOTAL_ORDERS ORDER BY "Id" DESC LIMIT (IN_PROGRESS_MIN - in_progress_count)
    );
  END IF;

  -- Delivery fee should be a round number from a realistic set based on how expensive the order is.
  -- Use pre-delivery total (= service fee + items subtotal) to pick a fee bucket.
  UPDATE "Orders" o
  SET "DeliveryFee" = CASE
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 100 THEN 29.00
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 150 THEN 34.00
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 200 THEN 39.00
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 250 THEN 44.00
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 300 THEN 49.00
    WHEN (o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0)) < 350 THEN 54.00
    ELSE 59.00
  END
  WHERE (o."Id" BETWEEN 1 AND TOTAL_ORDERS) OR (o."Id" BETWEEN 20001 AND 20010);

  UPDATE "Orders" o
  SET "TotalAmount" = round(
    o."DeliveryFee" + o."ServiceFee" + COALESCE((
      SELECT SUM(oi."Quantity" * oi."UnitPrice") FROM "OrderItems" oi WHERE oi."OrderId" = o."Id"
    ), 0),
    2
  )
  WHERE (o."Id" BETWEEN 1 AND TOTAL_ORDERS) OR (o."Id" BETWEEN 20001 AND 20010);

  PERFORM setval(pg_get_serial_sequence('"Orders"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "Orders"));
  PERFORM setval(pg_get_serial_sequence('"OrderItems"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "OrderItems"));
END
$$;

-- ==================================================
-- Feedback DB
-- ==================================================

\connect mtogo_feedback

DO $$
DECLARE
  base_ts CONSTANT TIMESTAMPTZ := '2025-01-01 12:00:00+00';

  REVIEW_COUNT CONSTANT INT := 400;

  PARTNER_COUNT CONSTANT INT := 111;
  AGENT_COUNT CONSTANT INT := 121;
  CUSTOMER_COUNT CONSTANT INT := 1201;

  review_phrases TEXT[] := ARRAY[
    'Great food, fast delivery.',
    'Food was tasty but arrived a bit late.',
    'Excellent service and friendly delivery person.',
    'Portions were small for the price.',
    'Very fresh and well packed.',
    'Not satisfied with the temperature of the food.',
    'Will order again!',
    'Order was missing an item but support fixed it quickly.'
  ];

  i INT;
  customer_id INT;
  partner_id INT;
  agent_id INT;
  created_at TIMESTAMPTZ;

  food_rating INT;
  agent_rating INT;
  order_rating INT;
  phrase TEXT;
BEGIN
  -- Repeatable wipe for seeded review order ranges
  DELETE FROM reviews WHERE order_id BETWEEN 1 AND REVIEW_COUNT;
  DELETE FROM reviews WHERE order_id BETWEEN 20001 AND 20010;

  -- Reviews for orders 1..REVIEW_COUNT (must match the deterministic order formulas)
  FOR i IN 1..REVIEW_COUNT LOOP
    customer_id := 1 + mod(abs(hashtext('ocust-' || i::TEXT)), CUSTOMER_COUNT);
    partner_id  := 1 + mod(abs(hashtext('opartner-' || i::TEXT)), PARTNER_COUNT);

    agent_id := CASE
      WHEN mod(abs(hashtext('oagent-null-' || i::TEXT)), 100) < 92 THEN 1 + mod(abs(hashtext('oagent-' || i::TEXT)), AGENT_COUNT)
      ELSE NULL
    END;

    created_at := base_ts
      - ((1 + mod(abs(hashtext('rdays-' || i::TEXT)), 120))::INT || ' days')::INTERVAL
      - (mod(abs(hashtext('rsecs-' || i::TEXT)), 86400)::INT || ' seconds')::INTERVAL;

    food_rating  := 1 + mod(abs(hashtext('rfood-'  || i::TEXT)), 5);
    agent_rating := 1 + mod(abs(hashtext('ragent-' || i::TEXT)), 5);
    order_rating := 1 + mod(abs(hashtext('rorder-' || i::TEXT)), 5);

    phrase := review_phrases[1 + mod(abs(hashtext('rphrase-' || i::TEXT)), array_length(review_phrases,1))];

    INSERT INTO reviews (
      order_id, customer_id, partner_id, agent_id,
      created_at, food_rating, agent_rating, order_rating,
      food_comment, agent_comment, order_comment
    ) VALUES (
      i, customer_id, partner_id, agent_id,
      created_at, food_rating, agent_rating, order_rating,
      phrase, NULL, NULL
    );
  END LOOP;

  -- Reviews for the special Opy demo orders
  INSERT INTO reviews (
    order_id, customer_id, partner_id, agent_id,
    created_at, food_rating, agent_rating, order_rating,
    food_comment, agent_comment, order_comment
  ) VALUES
    (20001, 1, 1, NULL, base_ts - INTERVAL '10 minutes', 5, 5, 5, 'Just placed it, excited!', NULL, NULL),
    (20002, 1, 1, 1,    base_ts - INTERVAL '40 minutes', 4, 5, 4, 'Almost ready.', NULL, NULL),
    (20003, 1, 1, 1,    base_ts - INTERVAL '55 minutes', 5, 5, 5, 'Picked up, great so far.', NULL, NULL)
  ON CONFLICT (order_id) DO NOTHING;

  PERFORM setval(pg_get_serial_sequence('reviews', 'id'), (SELECT COALESCE(MAX(id), 1) FROM reviews));
END
$$;

\echo '== MToGo demo seed (repeatable) complete =='
