using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace LegacyMigrator;

/// <summary>
/// One-time migration tool: reads legacy data from xDB2.mdb (Jet OLEDB 4.0, 32-bit)
/// and writes it into the b-pro-max PostgreSQL database.
/// </summary>
internal static class Program
{
    private const string MdbPath = @"F:\b\b-pro-max\xDB2.mdb";
    private const string PgConnectionString =
        "Host=localhost;Port=5432;Database=bpro_max;Username=postgres;Password=;SSL Mode=Disable;Trust Server Certificate=true";

    private static int Main(string[] args)
    {
        Console.WriteLine("=== Legacy MDB to PostgreSQL Migration ===");
        Console.WriteLine($"MDB: {MdbPath}");
        Console.WriteLine();

        if (!File.Exists(MdbPath))
        {
            Console.Error.WriteLine($"ERROR: MDB file not found: {MdbPath}");
            return 1;
        }

        try
        {
            using var pgConn = new NpgsqlConnection(PgConnectionString);
            pgConn.Open();
            Console.WriteLine("Connected to PostgreSQL.");

            EnsureDatabase(pgConn);
            CreateTables(pgConn);
            SeedReferenceData(pgConn);

            var agentIdMap = MigrateAgents(pgConn);
            var userIdMap = MigrateUsers(pgConn);
            var drawIdMap = MigrateDraws(pgConn);
            MigrateSales(pgConn, agentIdMap, drawIdMap);

            Console.WriteLine();
            Console.WriteLine("=== Migration complete ===");
            Console.WriteLine($"Agents migrated: {agentIdMap.Count}");
            Console.WriteLine($"Users migrated: {userIdMap.Count}");
            Console.WriteLine($"Draws migrated: {drawIdMap.Count}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: {ex}");
            return 1;
        }
    }

    private static void EnsureDatabase(NpgsqlConnection conn)
    {
        // The connection string targets bpro_max; ensure it exists by connecting to postgres db.
        using var adminConn = new NpgsqlConnection(
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=;SSL Mode=Disable;Trust Server Certificate=true");
        adminConn.Open();
        using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = 'bpro_max'", adminConn);
        var exists = cmd.ExecuteScalar() != null;
        if (!exists)
        {
            using var createCmd = new NpgsqlCommand("CREATE DATABASE bpro_max", adminConn);
            createCmd.ExecuteNonQuery();
            Console.WriteLine("Created database 'bpro_max'.");
        }
    }

    private static void CreateTables(NpgsqlConnection conn)
    {
        var sql = @"
CREATE TABLE IF NOT EXISTS agents (
    id SERIAL PRIMARY KEY,
    agent_name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    commission NUMERIC(12,2) DEFAULT 0,
    rate NUMERIC(12,2) DEFAULT 0,
    commission1 NUMERIC(12,2) DEFAULT 0,
    rate1 NUMERIC(12,2) DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username TEXT NOT NULL,
    password_hash BYTEA NOT NULL,
    salt BYTEA NOT NULL,
    iterations INTEGER NOT NULL DEFAULT 210000,
    failed_attempts INTEGER NOT NULL DEFAULT 0,
    locked_until TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username ON users (LOWER(username));

CREATE TABLE IF NOT EXISTS draws (
    id BIGSERIAL PRIMARY KEY,
    draw_date DATE NOT NULL,
    draw_time TIME NOT NULL,
    draw_number CHAR(2) NULL CHECK (draw_number IS NULL OR draw_number ~ '^[0-9]{2}$'),
    status TEXT NOT NULL DEFAULT 'Open' CHECK (status IN ('Open','Closed','Drawn','Settled','Cancelled')),
    created_by INTEGER NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (draw_date, draw_time)
);

CREATE TABLE IF NOT EXISTS bet_types (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name_en TEXT NOT NULL,
    name_my TEXT NOT NULL,
    rule_type TEXT NOT NULL,
    rule_config JSONB NOT NULL DEFAULT '{}'::jsonb,
    amount_mode TEXT NOT NULL CHECK (amount_mode IN ('PerNumber','TotalSplit','Total')),
    payout_mode TEXT NOT NULL CHECK (payout_mode IN ('PerWinningNumber','TotalIfHit')),
    payout_multiplier NUMERIC(10,4) NOT NULL CHECK (payout_multiplier > 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sales (
    id BIGSERIAL PRIMARY KEY,
    agent_id INTEGER NOT NULL REFERENCES agents(id) ON DELETE RESTRICT,
    sale_date TIMESTAMPTZ NOT NULL,
    amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission NUMERIC(14,2) NOT NULL DEFAULT 0,
    commission1 NUMERIC(14,2) NOT NULL DEFAULT 0,
    currency_type TEXT,
    currency_rate NUMERIC(12,4) NOT NULL DEFAULT 1,
    status TEXT NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Paid','Cancelled')),
    draw_id BIGINT NULL REFERENCES draws(id),
    created_by INTEGER NULL REFERENCES users(id),
    settlement_status TEXT NOT NULL DEFAULT 'Open' CHECK (settlement_status IN ('Open','Settled','Cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sale_items (
    id BIGSERIAL PRIMARY KEY,
    sale_id BIGINT NOT NULL REFERENCES sales(id) ON DELETE CASCADE,
    draw_id BIGINT NOT NULL REFERENCES draws(id),
    bet_type_id INTEGER NOT NULL REFERENCES bet_types(id),
    bet_key TEXT NOT NULL,
    amount NUMERIC(14,2) NOT NULL CHECK (amount > 0),
    payout_multiplier NUMERIC(10,4) NOT NULL CHECK (payout_multiplier > 0),
    payout_mode TEXT NOT NULL CHECK (payout_mode IN ('PerWinningNumber','TotalIfHit')),
    amount_mode TEXT NOT NULL CHECK (amount_mode IN ('PerNumber','TotalSplit','Total')),
    match_mode TEXT NOT NULL DEFAULT 'Exact' CHECK (match_mode IN ('Exact','UnorderedPair')),
    status TEXT NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sale_item_numbers (
    id BIGSERIAL PRIMARY KEY,
    sale_item_id BIGINT NOT NULL REFERENCES sale_items(id) ON DELETE CASCADE,
    number CHAR(2) NOT NULL CHECK (number ~ '^[0-9]{2}$'),
    stake NUMERIC(14,2) NOT NULL CHECK (stake >= 0),
    is_reverse BOOLEAN NOT NULL DEFAULT FALSE,
    status TEXT NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Won','Lost','Cancelled')),
    payout_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    UNIQUE (sale_item_id, number)
);
";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        Console.WriteLine("Tables ensured.");
    }

    private static void SeedReferenceData(NpgsqlConnection conn)
    {
        // Bet types
        foreach (var (code, nameEn, nameMy, ruleType, amountMode, payoutMode, multiplier, sortOrder) in new[]
        {
            ("DIRECT", "Direct", "တိုက်ရိုက်", "Exact", "TotalSplit", "PerWinningNumber", 80m, 0),
            ("R", "R", "R", "ExactAndReverse", "TotalSplit", "PerWinningNumber", 80m, 1),
            ("APA", "Apa", "ပါ", "DigitInEitherPosition", "TotalSplit", "PerWinningNumber", 80m, 2),
            ("BRAKE", "Brake", "ဘရိတ်", "DigitSum", "TotalSplit", "PerWinningNumber", 80m, 3),
            ("TOP", "Top", "ထိပ်", "DigitInPosition", "TotalSplit", "PerWinningNumber", 80m, 4),
            ("BOTTOM", "Bottom", "နောက်", "DigitInPosition", "TotalSplit", "PerWinningNumber", 80m, 5),
            ("EVEN_EVEN", "Even-Even", "စုံစုံ", "UnorderedPair", "TotalSplit", "PerWinningNumber", 80m, 6),
            ("ODD_ODD", "Odd-Odd", "မမ", "UnorderedPair", "TotalSplit", "PerWinningNumber", 80m, 7),
            ("MIXED", "Mixed", "စုံမ", "Parity", "TotalSplit", "PerWinningNumber", 80m, 8),
            ("POWER_GROUP", "Power Group", "ပါဝါ", "FixedGroup", "TotalSplit", "PerWinningNumber", 80m, 9),
            ("NAKKHAT", "Nakkhat", "နက္ခတ်", "FixedGroup", "TotalSplit", "PerWinningNumber", 80m, 10),
        })
        {
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO bet_types (code, name_en, name_my, rule_type, rule_config, amount_mode, payout_mode, payout_multiplier, is_active, sort_order)
                  VALUES (@code, @name_en, @name_my, @rule_type, CAST('{}' AS jsonb), @amount_mode, @payout_mode, @multiplier, TRUE, @sort_order)
                  ON CONFLICT (code) DO NOTHING", conn);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@name_en", nameEn);
            cmd.Parameters.AddWithValue("@name_my", nameMy);
            cmd.Parameters.AddWithValue("@rule_type", ruleType);
            cmd.Parameters.AddWithValue("@amount_mode", amountMode);
            cmd.Parameters.AddWithValue("@payout_mode", payoutMode);
            cmd.Parameters.AddWithValue("@multiplier", multiplier);
            cmd.Parameters.AddWithValue("@sort_order", sortOrder);
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine("Bet types seeded.");
    }

    /// <summary>
    /// Migrates AgentThai rows into agents. Returns MDB row index (1-based) -> PostgreSQL agent id.
    /// </summary>
    private static Dictionary<int, int> MigrateAgents(NpgsqlConnection conn)
    {
        var map = new Dictionary<int, int>();

        using var mdbConn = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={MdbPath};");
        mdbConn.Open();
        using var mdbCmd = new OleDbCommand("SELECT * FROM AgentThai", mdbConn);
        using var reader = mdbCmd.ExecuteReader();

        var ordinals = GetOrdinals(reader,
            "AgentName", "Address", "Phone", "Commision", "Rate", "Commision1", "Rate1", "CurType", "CurRate");

        int rowIndex = 0;
        while (reader.Read())
        {
            rowIndex++;
            var agentName = GetString(reader, ordinals.AgentName);
            if (string.IsNullOrWhiteSpace(agentName)) continue;

            var address = GetStringOrNull(reader, ordinals.Address);
            if (address == "0") address = null;

            var phone = GetStringOrNull(reader, ordinals.Phone);
            if (phone == "-") phone = null;

            var commission = ParseDecimal(GetString(reader, ordinals.Commision));
            var rate = ParseDecimal(GetString(reader, ordinals.Rate));
            var commission1 = ParseDecimal(GetString(reader, ordinals.Commision1));
            var rate1 = ParseDecimal(GetString(reader, ordinals.Rate1));
            var currencyType = GetStringOrNull(reader, ordinals.CurType);
            var currencyRate = ParseDecimal(GetString(reader, ordinals.CurRate));

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO agents (agent_name, address, phone, commission, rate, commission1, rate1, currency_type, currency_rate, updated_at)
                  VALUES (@agent_name, @address, @phone, @commission, @rate, @commission1, @rate1, @currency_type, @currency_rate, NOW())
                  RETURNING id", conn);
            cmd.Parameters.AddWithValue("@agent_name", agentName);
            cmd.Parameters.AddWithValue("@address", (object?)address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", (object?)phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@commission", commission);
            cmd.Parameters.AddWithValue("@rate", rate);
            cmd.Parameters.AddWithValue("@commission1", commission1);
            cmd.Parameters.AddWithValue("@rate1", rate1);
            cmd.Parameters.AddWithValue("@currency_type", (object?)currencyType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@currency_rate", currencyRate);

            var newId = (int)cmd.ExecuteScalar()!;
            map[rowIndex] = newId;
            Console.WriteLine($"  Agent #{rowIndex}: '{agentName}' -> agent id {newId}");
        }

        return map;
    }

    /// <summary>
    /// Migrates Login rows into users. Password is PBKDF2-HMAC-SHA256 hashed with random salt.
    /// </summary>
    private static Dictionary<string, int> MigrateUsers(NpgsqlConnection conn)
    {
        var map = new Dictionary<string, int>();

        using var mdbConn = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={MdbPath};");
        mdbConn.Open();
        using var mdbCmd = new OleDbCommand("SELECT * FROM Login", mdbConn);
        using var reader = mdbCmd.ExecuteReader();

        var ordinals = GetOrdinals(reader, "UserName", "Password");

        while (reader.Read())
        {
            var username = GetString(reader, ordinals.UserName);
            if (string.IsNullOrWhiteSpace(username)) continue;

            var password = GetString(reader, ordinals.Password);
            var (hash, salt) = HashPassword(password);

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO users (username, password_hash, salt, iterations, created_at, updated_at)
                  VALUES (@username, @password_hash, @salt, @iterations, NOW(), NOW())
                  RETURNING id", conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password_hash", hash);
            cmd.Parameters.AddWithValue("@salt", salt);
            cmd.Parameters.AddWithValue("@iterations", 210000);

            var newId = (int)cmd.ExecuteScalar()!;
            map[username] = newId;
            Console.WriteLine($"  User '{username}' -> user id {newId}");
        }

        return map;
    }

    /// <summary>
    /// Migrates LuckyNo and Hot rows into draws. Returns draw date string -> draw id.
    /// </summary>
    private static Dictionary<string, long> MigrateDraws(NpgsqlConnection conn)
    {
        var map = new Dictionary<string, long>();

        void Process(string table, string numberColumn)
        {
            using var mdbConn = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={MdbPath};");
            mdbConn.Open();
            using var mdbCmd = new OleDbCommand($"SELECT * FROM [{table}]", mdbConn);
            using var reader = mdbCmd.ExecuteReader();

            var ordinals = GetOrdinals(reader, "OnCount", numberColumn, "AMPM");

            while (reader.Read())
            {
                var onCount = GetString(reader, ordinals.OnCount);
                if (string.IsNullOrWhiteSpace(onCount)) continue;

                var drawDate = ParseMdbDate(onCount);
                if (drawDate == null) continue;

                var drawNumber = GetString(reader, ordinals[numberColumn]);
                if (string.IsNullOrWhiteSpace(drawNumber)) continue;

                // Ensure 2-digit
                drawNumber = drawNumber.PadLeft(2, '0').Substring(0, 2);

                var ampm = GetStringOrNull(reader, ordinals.AMPM);
                var drawTime = ParseAmPm(ampm) ?? TimeOnly.MinValue;

                var key = $"{drawDate.Value:yyyy-MM-dd}|{drawTime:hh\\:mm}";

                if (map.ContainsKey(key)) continue;

                using var cmd = new NpgsqlCommand(
                    @"INSERT INTO draws (draw_date, draw_time, draw_number, status, created_at, updated_at)
                      VALUES (@draw_date, @draw_time, @draw_number, 'Open', NOW(), NOW())
                      RETURNING id", conn);
                cmd.Parameters.AddWithValue("@draw_date", drawDate.Value);
                cmd.Parameters.AddWithValue("@draw_time", drawTime);
                cmd.Parameters.AddWithValue("@draw_number", drawNumber);

                var newId = (long)cmd.ExecuteScalar()!;
                map[key] = newId;
                Console.WriteLine($"  Draw {table}: {drawDate.Value:yyyy-MM-dd} {drawTime:hh\\:mm} #{drawNumber} -> draw id {newId}");
            }
        }

        Process("LuckyNo", "LNo");
        Process("Hot", "HotNum");

        return map;
    }

    /// <summary>
    /// Migrates LGSale + LGSaleDetail into sales, sale_items, sale_item_numbers.
    /// </summary>
    private static void MigrateSales(NpgsqlConnection conn, Dictionary<int, int> agentIdMap, Dictionary<string, long> drawIdMap)
    {
        // Look up DIRECT bet type id
        int betTypeId;
        using (var cmd = new NpgsqlCommand("SELECT id FROM bet_types WHERE code = 'DIRECT'", conn))
        {
            var result = cmd.ExecuteScalar();
            if (result == null)
                throw new InvalidOperationException("DIRECT bet type not found. Seed reference data first.");
            betTypeId = (int)result;
        }

        using var mdbConn = new OleDbConnection($"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={MdbPath};");
        mdbConn.Open();

        // Load all LGSale headers
        var sales = new List<SaleHeader>();
        using (var cmd = new OleDbCommand("SELECT * FROM LGSale ORDER By AgentID, SrNo", mdbConn))
        using (var reader = cmd.ExecuteReader())
        {
            var ordinals = GetOrdinals(reader,
                "OnCount", "AgentID", "SrNo", "Num", "Commision", "Rate");
            while (reader.Read())
            {
                sales.Add(new SaleHeader
                {
                    OnCount = GetString(reader, ordinals.OnCount),
                    AgentID = reader.GetInt32(ordinals.AgentID),
                    SrNo = reader.GetInt32(ordinals.SrNo),
                    Num = GetString(reader, ordinals.Num),
                    Commission = ParseDecimal(GetString(reader, ordinals.Commision)),
                    Commission1 = ParseDecimal(GetString(reader, ordinals.Rate)),
                });
            }
        }

        // Load all LGSaleDetail items
        var details = new List<SaleDetail>();
        using (var cmd = new OleDbCommand("SELECT * FROM LGSaleDetail ORDER By AgentID, SrNo", mdbConn))
        using (var reader = cmd.ExecuteReader())
        {
            var ordinals = GetOrdinals(reader,
                "OnCount", "AgentID", "SrNo", "Num1", "Value");
            while (reader.Read())
            {
                details.Add(new SaleDetail
                {
                    OnCount = GetString(reader, ordinals.OnCount),
                    AgentID = reader.GetInt32(ordinals.AgentID),
                    SrNo = reader.GetInt32(ordinals.SrNo),
                    Num1 = GetString(reader, ordinals.Num1).PadLeft(2, '0').Substring(0, 2),
                    Value = ParseDecimal(GetString(reader, ordinals.Value)),
                });
            }
        }

        // Group details by (OnCount, AgentID, SrNo)
        var detailGroups = details
            .GroupBy(d => (d.OnCount, d.AgentID, d.SrNo))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var sale in sales)
        {
            if (!agentIdMap.TryGetValue(sale.AgentID, out var agentId))
            {
                Console.WriteLine($"  SKIP sale: no agent mapping for AgentID={sale.AgentID}");
                continue;
            }

            var saleDate = ParseMdbDateTime(sale.OnCount);
            if (saleDate == null)
            {
                Console.WriteLine($"  SKIP sale: invalid date '{sale.OnCount}'");
                continue;
            }

            var totalAmount = detailGroups.TryGetValue((sale.OnCount, sale.AgentID, sale.SrNo), out var items)
                ? items.Sum(i => i.Value)
                : 0m;

            // Look up draw for this sale date
            long? drawId = null;
            var drawKey = $"{saleDate.Value:yyyy-MM-dd}|12:00"; // Default to noon if no specific draw
            // Try to match any draw on this date
            foreach (var kvp in drawIdMap)
            {
                if (kvp.Key.StartsWith($"{saleDate.Value:yyyy-MM-dd}|"))
                {
                    drawId = kvp.Value;
                    break;
                }
            }

            using var insertSale = new NpgsqlCommand(
                @"INSERT INTO sales (agent_id, sale_date, amount, commission, commission1, currency_type, currency_rate, status, draw_id, created_at, updated_at)
                  VALUES (@agent_id, @sale_date, @amount, @commission, @commission1, @currency_type, @currency_rate, 'Pending', @draw_id, NOW(), NOW())
                  RETURNING id", conn);
            insertSale.Parameters.AddWithValue("@agent_id", agentId);
            insertSale.Parameters.AddWithValue("@sale_date", saleDate.Value);
            insertSale.Parameters.AddWithValue("@amount", totalAmount);
            insertSale.Parameters.AddWithValue("@commission", sale.Commission);
            insertSale.Parameters.AddWithValue("@commission1", sale.Commission1);
            insertSale.Parameters.AddWithValue("@currency_type", "Ks");
            insertSale.Parameters.AddWithValue("@currency_rate", 1m);
            insertSale.Parameters.AddWithValue("@draw_id", (object?)drawId ?? DBNull.Value);

            var saleId = (long)insertSale.ExecuteScalar()!;
            Console.WriteLine($"  Sale: {sale.OnCount} AgentID={sale.AgentID} amount={totalAmount} -> sale id {saleId}");

            if (detailGroups.TryGetValue((sale.OnCount, sale.AgentID, sale.SrNo), out var saleItems))
            {
                foreach (var item in saleItems)
                {
                    // Insert sale_item
                    using var insertItem = new NpgsqlCommand(
                        @"INSERT INTO sale_items (sale_id, draw_id, bet_type_id, bet_key, amount, payout_multiplier, payout_mode, amount_mode, match_mode, status, created_at)
                          VALUES (@sale_id, @draw_id, @bet_type_id, @bet_key, @amount, @payout_multiplier, @payout_mode, @amount_mode, @match_mode, 'Pending', NOW())
                          RETURNING id", conn);
                    insertItem.Parameters.AddWithValue("@sale_id", saleId);
                    insertItem.Parameters.AddWithValue("@draw_id", (object?)drawId ?? DBNull.Value);
                    insertItem.Parameters.AddWithValue("@bet_type_id", betTypeId);
                    insertItem.Parameters.AddWithValue("@bet_key", item.Num1);
                    insertItem.Parameters.AddWithValue("@amount", item.Value);
                    insertItem.Parameters.AddWithValue("@payout_multiplier", 80m);
                    insertItem.Parameters.AddWithValue("@payout_mode", "PerWinningNumber");
                    insertItem.Parameters.AddWithValue("@amount_mode", "TotalSplit");
                    insertItem.Parameters.AddWithValue("@match_mode", "Exact");

                    var itemId = (long)insertItem.ExecuteScalar()!;

                    // Insert sale_item_number
                    using var insertNumber = new NpgsqlCommand(
                        @"INSERT INTO sale_item_numbers (sale_item_id, number, stake, is_reverse, status, payout_amount, created_at)
                          VALUES (@sale_item_id, @number, @stake, FALSE, 'Pending', 0, NOW())", conn);
                    insertNumber.Parameters.AddWithValue("@sale_item_id", itemId);
                    insertNumber.Parameters.AddWithValue("@number", item.Num1);
                    insertNumber.Parameters.AddWithValue("@stake", item.Value);
                    insertNumber.ExecuteNonQuery();

                    Console.WriteLine($"    Item: #{item.Num1} stake={item.Value} -> sale_item id {itemId}");
                }
            }
        }
    }

    // ── helper classes ──

    private sealed class SaleHeader
    {
        public string OnCount { get; set; } = string.Empty;
        public int AgentID { get; set; }
        public int SrNo { get; set; }
        public string Num { get; set; } = string.Empty;
        public decimal Commission { get; set; }
        public decimal Commission1 { get; set; }
    }

    private sealed class SaleDetail
    {
        public string OnCount { get; set; } = string.Empty;
        public int AgentID { get; set; }
        public int SrNo { get; set; }
        public string Num1 { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    private sealed class ColumnOrdinals
    {
        private readonly Dictionary<string, int> _map = new(StringComparer.OrdinalIgnoreCase);
        public int this[string name] => _map[name];
        public void Add(string name, int ordinal) => _map[name] = ordinal;
        public bool Contains(string name) => _map.ContainsKey(name);
    }

    private static ColumnOrdinals GetOrdinals(OleDbDataReader reader, params string[] names)
    {
        var result = new ColumnOrdinals();
        foreach (var name in names)
        {
            try
            {
                result.Add(name, reader.GetOrdinal(name));
            }
            catch
            {
                // Column may not exist; skip
            }
        }
        return result;
    }

    private static string GetString(OleDbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return string.Empty;
        return reader.GetString(ordinal);
    }

    private static string? GetStringOrNull(OleDbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        return reader.GetString(ordinal);
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        return decimal.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

    private static DateTime? ParseMdbDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        // MDB stores dates like "01-Nov-2012"
        if (DateTime.TryParse(value.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private static DateTime? ParseMdbDateTime(string value)
    {
        var date = ParseMdbDate(value);
        return date;
    }

    private static TimeOnly? ParseAmPm(string? ampm)
    {
        if (string.IsNullOrWhiteSpace(ampm)) return null;
        return ampm.Trim().ToUpperInvariant() switch
        {
            "AM" => new TimeOnly(6, 0),
            "PM" => new TimeOnly(12, 0),
            _ => null
        };
    }

    private static (byte[] hash, byte[] salt) HashPassword(string password)
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        const int iterations = 210000;
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return (hash, salt);
    }
}
