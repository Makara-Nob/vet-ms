using Microsoft.Extensions.Configuration;
using Npgsql;

namespace VetMS.Data;

public static class Database
{
    private static string? _connectionString;

    public static string ConnectionString
    {
        get
        {
            if (_connectionString is null)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                _connectionString = config.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Connection string 'Default' not found in appsettings.json.");
            }
            return _connectionString;
        }
    }

    public static NpgsqlConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public static void Initialize()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS animal_species (
                id          SERIAL PRIMARY KEY,
                name        VARCHAR(100) NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                is_active   BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS breeds (
                id          SERIAL PRIMARY KEY,
                species_id  INTEGER NOT NULL REFERENCES animal_species(id),
                name        VARCHAR(100) NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                is_active   BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS service_types (
                id          SERIAL PRIMARY KEY,
                name        VARCHAR(100) NOT NULL,
                category    VARCHAR(100) NOT NULL DEFAULT '',
                price       NUMERIC(18,2) NOT NULL DEFAULT 0,
                description TEXT NOT NULL DEFAULT '',
                is_active   BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS medications (
                id           SERIAL PRIMARY KEY,
                name         VARCHAR(100) NOT NULL,
                category     VARCHAR(100) NOT NULL DEFAULT '',
                dosage_form  VARCHAR(50)  NOT NULL DEFAULT '',
                unit         VARCHAR(20)  NOT NULL DEFAULT '',
                description  TEXT NOT NULL DEFAULT '',
                is_active    BOOLEAN NOT NULL DEFAULT TRUE
            );

            CREATE TABLE IF NOT EXISTS suppliers (
                id             SERIAL PRIMARY KEY,
                company_name   VARCHAR(150) NOT NULL,
                contact_person VARCHAR(100) NOT NULL DEFAULT '',
                phone          VARCHAR(50)  NOT NULL DEFAULT '',
                email          VARCHAR(150) NOT NULL DEFAULT '',
                address        TEXT NOT NULL DEFAULT '',
                is_active      BOOLEAN NOT NULL DEFAULT TRUE
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
