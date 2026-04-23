using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using VetMS.Models;

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
                is_active   BOOLEAN NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at  TIMESTAMP,
                created_by  VARCHAR(100),
                updated_by  VARCHAR(100)
            );

            CREATE TABLE IF NOT EXISTS users (
                id            SERIAL PRIMARY KEY,
                username      VARCHAR(100) NOT NULL UNIQUE,
                password_hash VARCHAR(255) NOT NULL,
                full_name     VARCHAR(200) NOT NULL,
                email         VARCHAR(150),
                role          VARCHAR(50) NOT NULL DEFAULT 'Staff',
                is_active     BOOLEAN NOT NULL DEFAULT TRUE,
                created_at    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at    TIMESTAMP,
                created_by    VARCHAR(100),
                updated_by    VARCHAR(100),
                profile_picture BYTEA
            );

            CREATE TABLE IF NOT EXISTS breeds (
                id          SERIAL PRIMARY KEY,
                species_id  INTEGER NOT NULL REFERENCES animal_species(id),
                name        VARCHAR(100) NOT NULL,
                description TEXT NOT NULL DEFAULT '',
                is_active   BOOLEAN NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at  TIMESTAMP,
                created_by  VARCHAR(100),
                updated_by  VARCHAR(100)
            );

            CREATE TABLE IF NOT EXISTS service_types (
                id          SERIAL PRIMARY KEY,
                name        VARCHAR(100) NOT NULL,
                category    VARCHAR(100) NOT NULL DEFAULT '',
                price       NUMERIC(18,2) NOT NULL DEFAULT 0,
                description TEXT NOT NULL DEFAULT '',
                is_active   BOOLEAN NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at  TIMESTAMP,
                created_by  VARCHAR(100),
                updated_by  VARCHAR(100)
            );

            CREATE TABLE IF NOT EXISTS medications (
                id           SERIAL PRIMARY KEY,
                name         VARCHAR(100) NOT NULL,
                category     VARCHAR(100) NOT NULL DEFAULT '',
                dosage_form  VARCHAR(50)  NOT NULL DEFAULT '',
                unit         VARCHAR(20)  NOT NULL DEFAULT '',
                description  TEXT NOT NULL DEFAULT '',
                is_active    BOOLEAN NOT NULL DEFAULT TRUE,
                created_at   TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at   TIMESTAMP,
                created_by   VARCHAR(100),
                updated_by   VARCHAR(100)
            );

            CREATE TABLE IF NOT EXISTS suppliers (
                id             SERIAL PRIMARY KEY,
                company_name   VARCHAR(150) NOT NULL,
                contact_person VARCHAR(100) NOT NULL DEFAULT '',
                phone          VARCHAR(50)  NOT NULL DEFAULT '',
                email          VARCHAR(150) NOT NULL DEFAULT '',
                address        TEXT NOT NULL DEFAULT '',
                is_active      BOOLEAN NOT NULL DEFAULT TRUE,
                created_at     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at     TIMESTAMP,
                created_by     VARCHAR(100),
                updated_by     VARCHAR(100)
            );

            ALTER TABLE animal_species ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE animal_species ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ALTER TABLE animal_species ADD COLUMN IF NOT EXISTS created_by VARCHAR(100);
            ALTER TABLE animal_species ADD COLUMN IF NOT EXISTS updated_by VARCHAR(100);

            ALTER TABLE breeds ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE breeds ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ALTER TABLE breeds ADD COLUMN IF NOT EXISTS created_by VARCHAR(100);
            ALTER TABLE breeds ADD COLUMN IF NOT EXISTS updated_by VARCHAR(100);

            ALTER TABLE service_types ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE service_types ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ALTER TABLE service_types ADD COLUMN IF NOT EXISTS created_by VARCHAR(100);
            ALTER TABLE service_types ADD COLUMN IF NOT EXISTS updated_by VARCHAR(100);

            ALTER TABLE medications ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE medications ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ALTER TABLE medications ADD COLUMN IF NOT EXISTS created_by VARCHAR(100);
            ALTER TABLE medications ADD COLUMN IF NOT EXISTS updated_by VARCHAR(100);

            ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP;
            ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS created_by VARCHAR(100);
            ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS updated_by VARCHAR(100);

            ALTER TABLE animal_species ADD COLUMN IF NOT EXISTS metadata JSONB;
            ALTER TABLE breeds ADD COLUMN IF NOT EXISTS metadata JSONB;
            ALTER TABLE service_types ADD COLUMN IF NOT EXISTS metadata JSONB;
            ALTER TABLE medications ADD COLUMN IF NOT EXISTS metadata JSONB;
            ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS metadata JSONB;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS metadata JSONB;

            CREATE TABLE IF NOT EXISTS customers (
                id          SERIAL PRIMARY KEY,
                full_name   VARCHAR(200) NOT NULL,
                phone       VARCHAR(50)  NOT NULL DEFAULT '',
                email       VARCHAR(200) NOT NULL DEFAULT '',
                address     TEXT         NOT NULL DEFAULT '',
                notes       TEXT         NOT NULL DEFAULT '',
                is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at  TIMESTAMP,
                created_by  VARCHAR(100),
                updated_by  VARCHAR(100),
                metadata    JSONB
            );

            CREATE TABLE IF NOT EXISTS pets (
                id            SERIAL PRIMARY KEY,
                customer_id   INTEGER NOT NULL REFERENCES customers(id) ON DELETE CASCADE,
                customer_name VARCHAR(200) NOT NULL DEFAULT '',
                species_id    INTEGER NOT NULL REFERENCES animal_species(id),
                species_name  VARCHAR(100) NOT NULL DEFAULT '',
                breed_id      INTEGER REFERENCES breeds(id),
                breed_name    VARCHAR(100) NOT NULL DEFAULT '',
                name          VARCHAR(200) NOT NULL,
                gender        VARCHAR(20)  NOT NULL DEFAULT 'Unknown',
                date_of_birth DATE,
                weight        DECIMAL(8,2) NOT NULL DEFAULT 0,
                color         VARCHAR(100) NOT NULL DEFAULT '',
                microchip_no  VARCHAR(100) NOT NULL DEFAULT '',
                notes         TEXT         NOT NULL DEFAULT '',
                is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
                created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at    TIMESTAMP,
                created_by    VARCHAR(100),
                updated_by    VARCHAR(100),
                metadata      JSONB
            );

            ALTER TABLE users ADD COLUMN IF NOT EXISTS profile_picture BYTEA;
            ALTER TABLE pets ADD COLUMN IF NOT EXISTS profile_picture BYTEA;

            CREATE TABLE IF NOT EXISTS supplier_medications (
                medication_id INTEGER NOT NULL REFERENCES medications(id) ON DELETE CASCADE,
                supplier_id   INTEGER NOT NULL REFERENCES suppliers(id) ON DELETE CASCADE,
                created_at    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (medication_id, supplier_id)
            );

            CREATE TABLE IF NOT EXISTS appointments (
                id                SERIAL PRIMARY KEY,
                pet_id            INTEGER NOT NULL REFERENCES pets(id),
                pet_name          VARCHAR(200) NOT NULL DEFAULT '',
                customer_id       INTEGER NOT NULL REFERENCES customers(id),
                customer_name     VARCHAR(200) NOT NULL DEFAULT '',
                assigned_vet_id   INTEGER REFERENCES users(id),
                vet_name          VARCHAR(200) NOT NULL DEFAULT '',
                service_type_id   INTEGER REFERENCES service_types(id),
                service_type_name VARCHAR(200) NOT NULL DEFAULT '',
                appointment_date  TIMESTAMP    NOT NULL,
                duration          INTEGER      NOT NULL DEFAULT 30,
                status            VARCHAR(50)  NOT NULL DEFAULT 'Scheduled',
                notes             TEXT         NOT NULL DEFAULT '',
                created_at        TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at        TIMESTAMP,
                created_by        VARCHAR(100),
                updated_by        VARCHAR(100),
                metadata          JSONB
            );

            CREATE TABLE IF NOT EXISTS medical_records (
                id              SERIAL PRIMARY KEY,
                appointment_id  INTEGER NOT NULL REFERENCES appointments(id),
                pet_id          INTEGER NOT NULL REFERENCES pets(id),
                pet_name        VARCHAR(200) NOT NULL DEFAULT '',
                customer_id     INTEGER NOT NULL REFERENCES customers(id),
                customer_name   VARCHAR(200) NOT NULL DEFAULT '',
                vet_id          INTEGER REFERENCES users(id),
                vet_name        VARCHAR(200) NOT NULL DEFAULT '',
                diagnosis       TEXT NOT NULL DEFAULT '',
                treatment       TEXT NOT NULL DEFAULT '',
                notes           TEXT NOT NULL DEFAULT '',
                follow_up_date  DATE,
                created_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at      TIMESTAMP,
                created_by      VARCHAR(100),
                updated_by      VARCHAR(100),
                metadata        JSONB
            );

            CREATE TABLE IF NOT EXISTS medical_record_medications (
                record_id     INTEGER NOT NULL REFERENCES medical_records(id) ON DELETE CASCADE,
                medication_id INTEGER NOT NULL REFERENCES medications(id),
                dosage        VARCHAR(200) NOT NULL DEFAULT '',
                notes         TEXT         NOT NULL DEFAULT '',
                PRIMARY KEY (record_id, medication_id)
            );

            CREATE TABLE IF NOT EXISTS cbc_records (
                id             SERIAL PRIMARY KEY,
                pet_id         INTEGER NOT NULL REFERENCES pets(id) ON DELETE CASCADE,
                pet_name       VARCHAR(200) NOT NULL DEFAULT '',
                customer_id    INTEGER NOT NULL REFERENCES customers(id),
                customer_name  VARCHAR(200) NOT NULL DEFAULT '',
                test_date      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                rbc            NUMERIC(8,2) NOT NULL DEFAULT 0,
                hgb            NUMERIC(8,2) NOT NULL DEFAULT 0,
                hct            NUMERIC(8,2) NOT NULL DEFAULT 0,
                mcv            NUMERIC(8,2) NOT NULL DEFAULT 0,
                mch            NUMERIC(8,2) NOT NULL DEFAULT 0,
                mchc           NUMERIC(8,2) NOT NULL DEFAULT 0,
                plt            NUMERIC(8,2) NOT NULL DEFAULT 0,
                wbc            NUMERIC(8,2) NOT NULL DEFAULT 0,
                neu            NUMERIC(8,2) NOT NULL DEFAULT 0,
                lym            NUMERIC(8,2) NOT NULL DEFAULT 0,
                mon            NUMERIC(8,2) NOT NULL DEFAULT 0,
                eos            NUMERIC(8,2) NOT NULL DEFAULT 0,
                bas            NUMERIC(8,2) NOT NULL DEFAULT 0,
                remarks        TEXT NOT NULL DEFAULT '',
                created_at     TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at     TIMESTAMP,
                created_by     VARCHAR(100),
                updated_by     VARCHAR(100),
                metadata       JSONB
            );
            """;
        cmd.ExecuteNonQuery();

        // One-time fix for corrupted passwords double-hashed during the avatar upload bug
        cmd.CommandText = "UPDATE users SET password_hash = @good_pw WHERE username = 'admin' AND password_hash = @bad_pw";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("good_pw", HashPassword("admin123"));
        cmd.Parameters.AddWithValue("bad_pw", HashPassword(HashPassword("admin123")));
        cmd.ExecuteNonQuery();
    }

    public static void SeedData()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();

        // Check if data already exists (to avoid duplicate seeding)
        cmd.CommandText = "SELECT COUNT(*) FROM animal_species";
        long count = (long)cmd.ExecuteScalar()!;
        
        // Seed users regardless of other tables, but check users count first
        cmd.CommandText = "SELECT COUNT(*) FROM users";
        long userCount = (long)cmd.ExecuteScalar()!;
        if (userCount == 0)
        {
            cmd.CommandText = """
                INSERT INTO users (username, password_hash, full_name, email, role, created_by) 
                VALUES (@username, @pass, @name, @email, 'Administrator', 'System')
                """;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("username", "admin");
            cmd.Parameters.AddWithValue("pass", HashPassword("admin123"));
            cmd.Parameters.AddWithValue("name", "System Administrator");
            cmd.Parameters.AddWithValue("email", "admin@vetms.local");
            cmd.ExecuteNonQuery();
        }

        if (count == 0)
        {

        cmd.Parameters.Clear();
        // Seed Animal Species
        cmd.CommandText = """
            INSERT INTO animal_species (name, description, created_by) VALUES 
            ('Dog', 'Canine companions', 'System'),
            ('Cat', 'Feline friends', 'System'),
            ('Bird', 'Avian pets', 'System'),
            ('Rabbit', 'Small mammals', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Breeds (assuming IDs 1, 2, 3, 4 for species)
        cmd.CommandText = """
            INSERT INTO breeds (species_id, name, description, created_by) VALUES 
            (1, 'Golden Retriever', 'Friendly and energetic', 'System'),
            (1, 'German Shepherd', 'Loyal and intelligent', 'System'),
            (2, 'Persian Cat', 'Long-haired and calm', 'System'),
            (2, 'Siamese Cat', 'Vocal and social', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Service Types
        cmd.CommandText = """
            INSERT INTO service_types (name, category, price, description, created_by) VALUES 
            ('General Checkup', 'Consultation', 25.00, 'Standard health check', 'System'),
            ('Vaccination', 'Prevention', 40.00, 'Annual vaccine booster', 'System'),
            ('Dental Cleaning', 'Wellness', 120.00, 'Professional teeth cleaning', 'System'),
            ('X-Ray', 'Diagnostics', 80.00, 'Radiographic imaging', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Medications
        cmd.CommandText = """
            INSERT INTO medications (name, category, dosage_form, unit, description, created_by) VALUES 
            ('Amoxicillin', 'Antibiotic', 'Tablet', '250mg', 'Broad-spectrum antibiotic', 'System'),
            ('Meloxicam', 'Anti-inflammatory', 'Oral Suspension', '1.5mg/ml', 'Pain relief', 'System'),
            ('Flevox', 'Parasiticide', 'Spot-on', '0.5ml', 'Flea and tick treatment', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Suppliers
        cmd.CommandText = """
            INSERT INTO suppliers (company_name, contact_person, phone, email, address, created_by) VALUES 
            ('VetPharma Ltd', 'John Smith', '555-0101', 'orders@vetpharma.com', '123 Pharma St, Industry City', 'System'),
            ('Global Pet Supplies', 'Mary Jane', '555-0202', 'sales@globalpet.com', '456 Supply Ave, Pet Town', 'System');
            """;
        cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "SELECT COUNT(*) FROM customers";
        long customerCount = (long)cmd.ExecuteScalar()!;
        if (customerCount > 1000) return;

        cmd.CommandText = "TRUNCATE TABLE customers, pets, appointments, medical_records, medical_record_medications, cbc_records RESTART IDENTITY CASCADE;";
        cmd.ExecuteNonQuery();

        // Seed Customers
        cmd.CommandText = """
            INSERT INTO customers (full_name, phone, email, address, notes, created_by) VALUES 
            ('John Doe', '555-1234', 'john@example.com', '123 Main St', 'VIP Customer', 'System'),
            ('Jane Smith', '555-5678', 'jane@example.com', '456 Oak Ave', 'Regular Checkups', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Pets
        cmd.CommandText = """
            INSERT INTO pets (customer_id, customer_name, species_id, species_name, breed_id, breed_name, name, gender, date_of_birth, weight, color, microchip_no, notes, created_by) VALUES 
            (1, 'John Doe', 1, 'Dog', 1, 'Golden Retriever', 'Buddy', 'Male', '2020-05-15', 30.5, 'Golden', 'MC123456', 
            'Buddy is a very friendly and energetic Golden Retriever. 

            He loves playing fetch and going for long walks in the park.
            He has a history of mild allergies during the spring season, usually presenting as skin irritation and paw licking. 
            We have successfully managed this with seasonal antihistamines and medicated baths.

            Dietary notes: Currently on a grain-free diet due to suspected sensitivities. He is highly food-motivated but can be a fast eater, so a slow-feeder bowl is recommended.

            Behavioral notes: Excellent temperament. Great with kids and other dogs. Can be slightly anxious during thunderstorms or fireworks, for which the owner uses a thunder shirt.

            Vaccination status is generally up to date. Owner is very diligent about monthly flea and tick prevention.
            Past surgeries: Neutered at 8 months. No complications.

            Next steps for owner: Continue monitoring weight, as Golden Retrievers are prone to obesity as they age. Ensure regular dental chews are provided.', 'System'),
            (2, 'Jane Smith', 2, 'Cat', 3, 'Persian Cat', 'Luna', 'Female', '2021-08-20', 4.2, 'White', 'MC987654', 'Very shy', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Appointments
        cmd.CommandText = """
            INSERT INTO appointments (pet_id, pet_name, customer_id, customer_name, assigned_vet_id, vet_name, service_type_id, service_type_name, appointment_date, duration, status, notes, created_by) VALUES 
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 1, 'General Checkup', CURRENT_TIMESTAMP - INTERVAL '365 days', 30, 'Completed', 'Annual exam 2024', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 2, 'Vaccination', CURRENT_TIMESTAMP - INTERVAL '360 days', 15, 'Completed', 'Rabies booster', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 1, 'General Checkup', CURRENT_TIMESTAMP - INTERVAL '180 days', 30, 'Completed', 'Mid-year checkup', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 4, 'X-Ray', CURRENT_TIMESTAMP - INTERVAL '90 days', 45, 'Completed', 'Limping on right front leg', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 3, 'Dental Cleaning', CURRENT_TIMESTAMP - INTERVAL '30 days', 60, 'Completed', 'Routine cleaning', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 1, 'General Checkup', CURRENT_TIMESTAMP - INTERVAL '7 days', 30, 'Completed', 'Routine annual exam', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 2, 'Vaccination', CURRENT_TIMESTAMP + INTERVAL '1 days', 15, 'Scheduled', 'Annual boosters', 'System'),
            (1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 1, 'General Checkup', CURRENT_TIMESTAMP + INTERVAL '90 days', 30, 'Scheduled', 'Follow-up on allergies', 'System'),
            (2, 'Luna', 2, 'Jane Smith', 1, 'System Administrator', 1, 'General Checkup', CURRENT_TIMESTAMP - INTERVAL '2 days', 30, 'Completed', 'Not eating well', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Medical Records
        cmd.CommandText = """
            INSERT INTO medical_records (appointment_id, pet_id, pet_name, customer_id, customer_name, vet_id, vet_name, diagnosis, treatment, notes, follow_up_date, created_by) VALUES 
            (1, 1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 'Healthy adult dog. Slight tartar buildup.', 'Recommended dental chews.', 'Weight is stable. Heart and lungs sound good.', NULL, 'System'),
            (3, 1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 'Mild seasonal allergies (atopy).', 'Prescribed antihistamines (Cetirizine 10mg daily as needed). Medicated shampoo.', 'Owner to bathe weekly if scratching worsens.', CURRENT_TIMESTAMP - INTERVAL '170 days', 'System'),
            (4, 1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 'Mild soft tissue sprain in right front limb. No fractures on X-Ray.', 'Rest for 7-10 days. Meloxicam for 5 days.', 'Owner advised to prevent jumping on/off furniture.', CURRENT_TIMESTAMP - INTERVAL '80 days', 'System'),
            (5, 1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 'Grade 1 periodontal disease.', 'Ultrasonic scaling and polishing performed under general anesthesia.', 'Recovered well from anesthesia. Extracted no teeth.', NULL, 'System'),
            (6, 1, 'Buddy', 1, 'John Doe', 1, 'System Administrator', 'Healthy adult dog. Allergies currently well controlled.', 'None required.', 'Vaccinations due next week. Continue current diet.', NULL, 'System'),
            (9, 2, 'Luna', 2, 'Jane Smith', 1, 'System Administrator', 'Mild dehydration, suspect dietary indiscretion.', 'Subcutaneous fluids, bland diet.', 'Monitor eating habits.', CURRENT_TIMESTAMP + INTERVAL '5 days', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed CBC Records
        cmd.CommandText = """
            INSERT INTO cbc_records (pet_id, pet_name, customer_id, customer_name, test_date, rbc, hgb, hct, mcv, mch, mchc, plt, wbc, neu, lym, mon, eos, bas, remarks, created_by) VALUES 
            (1, 'Buddy', 1, 'John Doe', CURRENT_TIMESTAMP - INTERVAL '365 days', 6.8, 15.8, 47.0, 69.1, 23.2, 33.6, 310, 10.2, 63, 26, 6, 4, 1, 'Baseline annual CBC. All parameters within normal limits.', 'System'),
            (1, 'Buddy', 1, 'John Doe', CURRENT_TIMESTAMP - INTERVAL '180 days', 6.6, 15.5, 46.2, 70.0, 23.5, 33.5, 325, 11.5, 66, 23, 5, 5, 1, 'Slightly elevated eosinophils, consistent with seasonal allergies.', 'System'),
            (1, 'Buddy', 1, 'John Doe', CURRENT_TIMESTAMP - INTERVAL '30 days', 6.7, 15.4, 45.8, 68.4, 23.0, 33.6, 305, 9.8, 64, 25, 5, 5, 1, 'Pre-anesthetic bloodwork before dental cleaning. Cleared for procedure.', 'System'),
            (1, 'Buddy', 1, 'John Doe', CURRENT_TIMESTAMP - INTERVAL '7 days', 6.5, 15.2, 45.0, 69.2, 23.3, 33.7, 320, 10.5, 65, 25, 5, 4, 1, 'Normal canine CBC panel.', 'System'),
            (2, 'Luna', 2, 'Jane Smith', CURRENT_TIMESTAMP - INTERVAL '2 days', 7.8, 12.5, 38.0, 48.7, 16.0, 32.8, 410, 15.2, 70, 20, 6, 3, 1, 'Slightly elevated WBC, likely stress or mild inflammation.', 'System');
            """;
        cmd.ExecuteNonQuery();

        // Seed Medications prescribed to Buddy's medical records
        // Record 2 = allergy visit → Cetirizine (use Amoxicillin=1, Meloxicam=2, Flevox=3 from seeded meds)
        // Record 3 = sprain visit → Meloxicam
        cmd.CommandText = """
            INSERT INTO medical_record_medications (record_id, medication_id, dosage, notes) VALUES
            (2, 1, '10mg once daily as needed', 'Antihistamine for seasonal allergy management. Discontinue if vomiting occurs.'),
            (3, 2, '0.1mg/kg once daily for 5 days', 'For pain and inflammation from soft tissue sprain. Give with food.'),
            (3, 3, '0.5ml topical spot-on', 'Applied during sprain visit since flea prevention was due.'),
            (5, 1, '250mg twice daily for 7 days', 'Post-dental prophylactic antibiotic course. Complete full course.');
            """;
        cmd.ExecuteNonQuery();
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static User? AuthenticateUser(string username, string password)
    {
        try
        {
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM users WHERE username = @user";
            cmd.Parameters.AddWithValue("user", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
                string inputHash = HashPassword(password);

                if (storedHash == inputHash)
                {
                    return new User
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        PasswordHash = string.Empty, // DO NOT populate the hash back into the model to avoid re-hashing bugs on Update!
                        FullName = reader.GetString(reader.GetOrdinal("full_name")),
                        Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                        Role = reader.GetString(reader.GetOrdinal("role")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        ProfilePicture = reader.IsDBNull(reader.GetOrdinal("profile_picture")) ? null : (byte[])reader["profile_picture"]
                    };
                }
            }
        }
        catch
        {
            // Log error in a real app
        }
        return null;
    }
}
