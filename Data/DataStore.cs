using Npgsql;
using NpgsqlTypes;
using VetMS.Models;

namespace VetMS.Data;

public static class DataStore
{
    // ── Animal Species ────────────────────────────────────────────────────────

    public static List<AnimalSpecies> GetAnimalSpecies()
    {
        var list = new List<AnimalSpecies>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, name, description, is_active, metadata FROM animal_species ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new AnimalSpecies
            {
                Id          = r.GetInt32(0),
                Name        = r.GetString(1),
                Description = r.GetString(2),
                IsActive    = r.GetBoolean(3),
                Metadata    = r.IsDBNull(4) ? null : r.GetString(4)
            });
        return list;
    }

    public static void Insert(AnimalSpecies item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO animal_species (name, description, is_active, metadata) VALUES (@n,@d,@a,@m) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(AnimalSpecies item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE animal_species SET name=@n, description=@d, is_active=@a, metadata=@m WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(AnimalSpecies item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM animal_species WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Breeds ────────────────────────────────────────────────────────────────

    public static List<Breed> GetBreeds()
    {
        var list = new List<Breed>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            """
            SELECT b.id, b.species_id, s.name, b.name, b.description, b.is_active, b.metadata
            FROM breeds b
            JOIN animal_species s ON s.id = b.species_id
            ORDER BY b.name
            """, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Breed
            {
                Id          = r.GetInt32(0),
                SpeciesId   = r.GetInt32(1),
                SpeciesName = r.GetString(2),
                Name        = r.GetString(3),
                Description = r.GetString(4),
                IsActive    = r.GetBoolean(5),
                Metadata    = r.IsDBNull(6) ? null : r.GetString(6)
            });
        return list;
    }

    public static void Insert(Breed item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO breeds (species_id, name, description, is_active, metadata) VALUES (@s,@n,@d,@a,@m) RETURNING id", conn);
        cmd.Parameters.AddWithValue("s", item.SpeciesId);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Breed item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE breeds SET species_id=@s, name=@n, description=@d, is_active=@a, metadata=@m WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("s",  item.SpeciesId);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Breed item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM breeds WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Service Types ─────────────────────────────────────────────────────────

    public static List<ServiceType> GetServiceTypes()
    {
        var list = new List<ServiceType>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, name, category, price, description, is_active, metadata FROM service_types ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ServiceType
            {
                Id          = r.GetInt32(0),
                Name        = r.GetString(1),
                Category    = r.GetString(2),
                Price       = r.GetDecimal(3),
                Description = r.GetString(4),
                IsActive    = r.GetBoolean(5),
                Metadata    = r.IsDBNull(6) ? null : r.GetString(6)
            });
        return list;
    }

    public static void Insert(ServiceType item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO service_types (name, category, price, description, is_active, metadata) VALUES (@n,@c,@p,@d,@a,@m) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("c", item.Category);
        cmd.Parameters.AddWithValue("p", item.Price);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(ServiceType item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE service_types SET name=@n, category=@c, price=@p, description=@d, is_active=@a, metadata=@m WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("c",  item.Category);
        cmd.Parameters.AddWithValue("p",  item.Price);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(ServiceType item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM service_types WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Medications ───────────────────────────────────────────────────────────

    public static List<Medication> GetMedications()
    {
        var list = new List<Medication>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, name, category, dosage_form, unit, description, is_active, metadata FROM medications ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Medication
            {
                Id          = r.GetInt32(0),
                Name        = r.GetString(1),
                Category    = r.GetString(2),
                DosageForm  = r.GetString(3),
                Unit        = r.GetString(4),
                Description = r.GetString(5),
                IsActive    = r.GetBoolean(6),
                Metadata    = r.IsDBNull(7) ? null : r.GetString(7)
            });
        return list;
    }

    public static void Insert(Medication item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO medications (name, category, dosage_form, unit, description, is_active, metadata) VALUES (@n,@c,@f,@u,@d,@a,@m) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("c", item.Category);
        cmd.Parameters.AddWithValue("f", item.DosageForm);
        cmd.Parameters.AddWithValue("u", item.Unit);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Medication item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE medications SET name=@n, category=@c, dosage_form=@f, unit=@u, description=@d, is_active=@a, metadata=@m WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("c",  item.Category);
        cmd.Parameters.AddWithValue("f",  item.DosageForm);
        cmd.Parameters.AddWithValue("u",  item.Unit);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Medication item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM medications WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Suppliers ─────────────────────────────────────────────────────────────

    public static List<Supplier> GetSuppliers()
    {
        var list = new List<Supplier>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, company_name, contact_person, phone, email, address, is_active, metadata FROM suppliers ORDER BY company_name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Supplier
            {
                Id            = r.GetInt32(0),
                CompanyName   = r.GetString(1),
                ContactPerson = r.GetString(2),
                Phone         = r.GetString(3),
                Email         = r.GetString(4),
                Address       = r.GetString(5),
                IsActive      = r.GetBoolean(6),
                Metadata      = r.IsDBNull(7) ? null : r.GetString(7)
            });
        return list;
    }

    public static void Insert(Supplier item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO suppliers (company_name, contact_person, phone, email, address, is_active, metadata) VALUES (@c,@p,@ph,@e,@a,@ac,@m) RETURNING id", conn);
        cmd.Parameters.AddWithValue("c",  item.CompanyName);
        cmd.Parameters.AddWithValue("p",  item.ContactPerson);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("e",  item.Email);
        cmd.Parameters.AddWithValue("a",  item.Address);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Supplier item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE suppliers SET company_name=@c, contact_person=@p, phone=@ph, email=@e, address=@a, is_active=@ac, metadata=@m WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("c",  item.CompanyName);
        cmd.Parameters.AddWithValue("p",  item.ContactPerson);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("e",  item.Email);
        cmd.Parameters.AddWithValue("a",  item.Address);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Supplier item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM suppliers WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    public static List<int> GetMedicationSuppliers(int medicationId)
    {
        var supplierIds = new List<int>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("SELECT supplier_id FROM supplier_medications WHERE medication_id = @mId", conn);
        cmd.Parameters.AddWithValue("mId", medicationId);
        using var r = cmd.ExecuteReader();
        while(r.Read()) supplierIds.Add(r.GetInt32(0));
        return supplierIds;
    }

    public static void SaveMedicationSuppliers(int medicationId, List<int> supplierIds)
    {
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmdDel = new NpgsqlCommand("DELETE FROM supplier_medications WHERE medication_id = @mId", conn, tx);
            cmdDel.Parameters.AddWithValue("mId", medicationId);
            cmdDel.ExecuteNonQuery();

            if (supplierIds.Count > 0)
            {
                using var cmdIns = new NpgsqlCommand("INSERT INTO supplier_medications (medication_id, supplier_id) VALUES (@mId, @sId)", conn, tx);
                cmdIns.Parameters.Add(new NpgsqlParameter("mId", NpgsqlDbType.Integer));
                cmdIns.Parameters.Add(new NpgsqlParameter("sId", NpgsqlDbType.Integer));

                foreach (var sId in supplierIds)
                {
                    cmdIns.Parameters["mId"].Value = medicationId;
                    cmdIns.Parameters["sId"].Value = sId;
                    cmdIns.ExecuteNonQuery();
                }
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    public static List<User> GetUsers()
    {
        var list = new List<User>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, username, full_name, email, role, is_active, profile_picture, metadata FROM users ORDER BY username", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new User
            {
                Id             = r.GetInt32(0),
                Username       = r.GetString(1),
                FullName       = r.GetString(2),
                Email          = r.IsDBNull(3) ? "" : r.GetString(3),
                Role           = r.GetString(4),
                IsActive       = r.GetBoolean(5),
                ProfilePicture = r.IsDBNull(6) ? null : (byte[])r[6],
                Metadata       = r.IsDBNull(7) ? null : r.GetString(7)
            });
        }
        return list;
    }

    public static void Insert(User item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            """
            INSERT INTO users (username, password_hash, full_name, email, role, is_active, profile_picture, metadata) 
            VALUES (@u, @p, @f, @e, @r, @a, @pic, @m) RETURNING id
            """, conn);
        cmd.Parameters.AddWithValue("u", item.Username);
        cmd.Parameters.AddWithValue("p", Database.HashPassword("password")); // Default password, or pass item.PasswordHash if you added it temporarily
        if (!string.IsNullOrEmpty(item.PasswordHash))
        {
            cmd.Parameters["p"].Value = Database.HashPassword(item.PasswordHash);
        }
        cmd.Parameters.AddWithValue("f", item.FullName);
        cmd.Parameters.AddWithValue("e", (object?)item.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("r", item.Role);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.AddWithValue("pic", (object?)item.ProfilePicture ?? DBNull.Value);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(User item)
    {
        using var conn = Database.OpenConnection();
        // Option to update password only if provided
        string sql = """
            UPDATE users SET 
                username=@u, full_name=@f, email=@e, role=@r, is_active=@a, profile_picture=@pic, metadata=@m
            """;
        
        if (!string.IsNullOrEmpty(item.PasswordHash))
        {
            sql += ", password_hash=@p";
        }
        sql += " WHERE id=@id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("u", item.Username);
        cmd.Parameters.AddWithValue("f", item.FullName);
        cmd.Parameters.AddWithValue("e", (object?)item.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("r", item.Role);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        cmd.Parameters.AddWithValue("pic", (object?)item.ProfilePicture ?? DBNull.Value);
        cmd.Parameters.Add(new NpgsqlParameter("m", NpgsqlDbType.Jsonb) { Value = (object?)item.Metadata ?? DBNull.Value });
        cmd.Parameters.AddWithValue("id", item.Id);
        
        if (!string.IsNullOrEmpty(item.PasswordHash))
        {
            cmd.Parameters.AddWithValue("p", Database.HashPassword(item.PasswordHash));
        }

        cmd.ExecuteNonQuery();
        
        // Reset the item password hash on the object so it doesn't get inadvertently saved again if passed around
        item.PasswordHash = ""; 
    }


    public static void Delete(User item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM users WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Customers ─────────────────────────────────────────────────────────────

    public static List<Customer> GetCustomers()
    {
        var list = new List<Customer>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id,full_name,phone,email,address,notes,is_active,created_at,metadata FROM customers ORDER BY full_name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Customer
            {
                Id        = r.GetInt32(0),
                FullName  = r.GetString(1),
                Phone     = r.GetString(2),
                Email     = r.GetString(3),
                Address   = r.GetString(4),
                Notes     = r.GetString(5),
                IsActive  = r.GetBoolean(6),
                CreatedAt = r.GetDateTime(7),
                Metadata  = r.IsDBNull(8) ? null : r.GetString(8)
            });
        return list;
    }

    public static void Insert(Customer item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO customers (full_name,phone,email,address,notes,is_active,metadata) VALUES (@fn,@ph,@em,@ad,@no,@ac,@mt::jsonb) RETURNING id", conn);
        cmd.Parameters.AddWithValue("fn", item.FullName);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("em", item.Email);
        cmd.Parameters.AddWithValue("ad", item.Address);
        cmd.Parameters.AddWithValue("no", item.Notes);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Customer item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE customers SET full_name=@fn,phone=@ph,email=@em,address=@ad,notes=@no,is_active=@ac,metadata=@mt::jsonb,updated_at=NOW() WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.Parameters.AddWithValue("fn", item.FullName);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("em", item.Email);
        cmd.Parameters.AddWithValue("ad", item.Address);
        cmd.Parameters.AddWithValue("no", item.Notes);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Customer item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM customers WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Pets ──────────────────────────────────────────────────────────────────

    public static List<Pet> GetPets()
    {
        var list = new List<Pet>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT id,customer_id,customer_name,species_id,species_name,breed_id,breed_name,
                     name,gender,date_of_birth,weight,color,microchip_no,notes,is_active,created_at,metadata,profile_picture
              FROM pets ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Pet
            {
                Id           = r.GetInt32(0),
                CustomerId   = r.GetInt32(1),
                CustomerName = r.GetString(2),
                SpeciesId    = r.GetInt32(3),
                SpeciesName  = r.GetString(4),
                BreedId      = r.IsDBNull(5) ? null : r.GetInt32(5),
                BreedName    = r.GetString(6),
                Name         = r.GetString(7),
                Gender       = r.GetString(8),
                DateOfBirth  = r.IsDBNull(9) ? null : r.GetDateTime(9),
                Weight       = r.GetDecimal(10),
                Color        = r.GetString(11),
                MicrochipNo  = r.GetString(12),
                Notes        = r.GetString(13),
                IsActive     = r.GetBoolean(14),
                CreatedAt    = r.GetDateTime(15),
                Metadata     = r.IsDBNull(16) ? null : r.GetString(16),
                ProfilePicture = r.IsDBNull(17) ? null : (byte[])r[17]
            });
        return list;
    }

    public static void Insert(Pet item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO pets (customer_id,customer_name,species_id,species_name,breed_id,breed_name,
                                name,gender,date_of_birth,weight,color,microchip_no,notes,is_active,metadata,profile_picture)
              VALUES (@cid,@cn,@sid,@sn,@bid,@bn,@nm,@gd,@dob,@wt,@co,@mc,@no,@ac,@mt::jsonb,@pic) RETURNING id", conn);
        cmd.Parameters.AddWithValue("cid", item.CustomerId);
        cmd.Parameters.AddWithValue("cn",  item.CustomerName);
        cmd.Parameters.AddWithValue("sid", item.SpeciesId);
        cmd.Parameters.AddWithValue("sn",  item.SpeciesName);
        cmd.Parameters.Add("bid", NpgsqlDbType.Integer).Value = (object?)item.BreedId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("bn",  item.BreedName);
        cmd.Parameters.AddWithValue("nm",  item.Name);
        cmd.Parameters.AddWithValue("gd",  item.Gender);
        cmd.Parameters.Add("dob", NpgsqlDbType.Date).Value = (object?)item.DateOfBirth ?? DBNull.Value;
        cmd.Parameters.AddWithValue("wt",  item.Weight);
        cmd.Parameters.AddWithValue("co",  item.Color);
        cmd.Parameters.AddWithValue("mc",  item.MicrochipNo);
        cmd.Parameters.AddWithValue("no",  item.Notes);
        cmd.Parameters.AddWithValue("ac",  item.IsActive);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.Parameters.AddWithValue("pic", (object?)item.ProfilePicture ?? DBNull.Value);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Pet item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"UPDATE pets SET customer_id=@cid,customer_name=@cn,species_id=@sid,species_name=@sn,
                breed_id=@bid,breed_name=@bn,name=@nm,gender=@gd,date_of_birth=@dob,weight=@wt,
                color=@co,microchip_no=@mc,notes=@no,is_active=@ac,metadata=@mt::jsonb,profile_picture=@pic,updated_at=NOW()
              WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id",  item.Id);
        cmd.Parameters.AddWithValue("cid", item.CustomerId);
        cmd.Parameters.AddWithValue("cn",  item.CustomerName);
        cmd.Parameters.AddWithValue("sid", item.SpeciesId);
        cmd.Parameters.AddWithValue("sn",  item.SpeciesName);
        cmd.Parameters.Add("bid", NpgsqlDbType.Integer).Value = (object?)item.BreedId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("bn",  item.BreedName);
        cmd.Parameters.AddWithValue("nm",  item.Name);
        cmd.Parameters.AddWithValue("gd",  item.Gender);
        cmd.Parameters.Add("dob", NpgsqlDbType.Date).Value = (object?)item.DateOfBirth ?? DBNull.Value;
        cmd.Parameters.AddWithValue("wt",  item.Weight);
        cmd.Parameters.AddWithValue("co",  item.Color);
        cmd.Parameters.AddWithValue("mc",  item.MicrochipNo);
        cmd.Parameters.AddWithValue("no",  item.Notes);
        cmd.Parameters.AddWithValue("ac",  item.IsActive);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.Parameters.AddWithValue("pic", (object?)item.ProfilePicture ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Pet item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM pets WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Appointments ──────────────────────────────────────────────────────────

    public static List<Appointment> GetAppointments()
    {
        var list = new List<Appointment>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT id,pet_id,pet_name,customer_id,customer_name,assigned_vet_id,vet_name,
                     service_type_id,service_type_name,appointment_date,duration,status,notes,created_at,metadata
              FROM appointments ORDER BY appointment_date DESC", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Appointment
            {
                Id              = r.GetInt32(0),
                PetId           = r.GetInt32(1),
                PetName         = r.GetString(2),
                CustomerId      = r.GetInt32(3),
                CustomerName    = r.GetString(4),
                AssignedVetId   = r.IsDBNull(5) ? null : r.GetInt32(5),
                VetName         = r.GetString(6),
                ServiceTypeId   = r.IsDBNull(7) ? null : r.GetInt32(7),
                ServiceTypeName = r.GetString(8),
                AppointmentDate = r.GetDateTime(9),
                Duration        = r.GetInt32(10),
                Status          = r.GetString(11),
                Notes           = r.GetString(12),
                CreatedAt       = r.GetDateTime(13),
                Metadata        = r.IsDBNull(14) ? null : r.GetString(14)
            });
        return list;
    }

    public static void Insert(Appointment item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO appointments (pet_id,pet_name,customer_id,customer_name,assigned_vet_id,vet_name,
                service_type_id,service_type_name,appointment_date,duration,status,notes,metadata)
              VALUES (@pid,@pn,@cid,@cn,@vid,@vn,@stid,@stn,@dt,@dur,@st,@no,@mt::jsonb) RETURNING id", conn);
        cmd.Parameters.AddWithValue("pid",  item.PetId);
        cmd.Parameters.AddWithValue("pn",   item.PetName);
        cmd.Parameters.AddWithValue("cid",  item.CustomerId);
        cmd.Parameters.AddWithValue("cn",   item.CustomerName);
        cmd.Parameters.Add("vid",  NpgsqlDbType.Integer).Value = (object?)item.AssignedVetId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("vn",   item.VetName);
        cmd.Parameters.Add("stid", NpgsqlDbType.Integer).Value = (object?)item.ServiceTypeId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("stn",  item.ServiceTypeName);
        cmd.Parameters.AddWithValue("dt",   item.AppointmentDate);
        cmd.Parameters.AddWithValue("dur",  item.Duration);
        cmd.Parameters.AddWithValue("st",   item.Status);
        cmd.Parameters.AddWithValue("no",   item.Notes);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Appointment item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"UPDATE appointments SET pet_id=@pid,pet_name=@pn,customer_id=@cid,customer_name=@cn,
                assigned_vet_id=@vid,vet_name=@vn,service_type_id=@stid,service_type_name=@stn,
                appointment_date=@dt,duration=@dur,status=@st,notes=@no,metadata=@mt::jsonb,updated_at=NOW()
              WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id",   item.Id);
        cmd.Parameters.AddWithValue("pid",  item.PetId);
        cmd.Parameters.AddWithValue("pn",   item.PetName);
        cmd.Parameters.AddWithValue("cid",  item.CustomerId);
        cmd.Parameters.AddWithValue("cn",   item.CustomerName);
        cmd.Parameters.Add("vid",  NpgsqlDbType.Integer).Value = (object?)item.AssignedVetId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("vn",   item.VetName);
        cmd.Parameters.Add("stid", NpgsqlDbType.Integer).Value = (object?)item.ServiceTypeId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("stn",  item.ServiceTypeName);
        cmd.Parameters.AddWithValue("dt",   item.AppointmentDate);
        cmd.Parameters.AddWithValue("dur",  item.Duration);
        cmd.Parameters.AddWithValue("st",   item.Status);
        cmd.Parameters.AddWithValue("no",   item.Notes);
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.ExecuteNonQuery();
    }

    public static void Delete(Appointment item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM appointments WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }

    // ── Medical Records ───────────────────────────────────────────────────────

    public static List<MedicalRecord> GetMedicalRecords()
    {
        var list = new List<MedicalRecord>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT id,appointment_id,pet_id,pet_name,customer_id,customer_name,vet_id,vet_name,
                     diagnosis,treatment,notes,follow_up_date,created_at,metadata
              FROM medical_records ORDER BY created_at DESC", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new MedicalRecord
            {
                Id            = r.GetInt32(0),
                AppointmentId = r.GetInt32(1),
                PetId         = r.GetInt32(2),
                PetName       = r.GetString(3),
                CustomerId    = r.GetInt32(4),
                CustomerName  = r.GetString(5),
                VetId         = r.IsDBNull(6) ? null : r.GetInt32(6),
                VetName       = r.GetString(7),
                Diagnosis     = r.GetString(8),
                Treatment     = r.GetString(9),
                Notes         = r.GetString(10),
                FollowUpDate  = r.IsDBNull(11) ? null : r.GetDateTime(11),
                CreatedAt     = r.GetDateTime(12),
                Metadata      = r.IsDBNull(13) ? null : r.GetString(13)
            });
        return list;
    }

    public static void Insert(MedicalRecord item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO medical_records (appointment_id,pet_id,pet_name,customer_id,customer_name,
                vet_id,vet_name,diagnosis,treatment,notes,follow_up_date,metadata)
              VALUES (@aid,@pid,@pn,@cid,@cn,@vid,@vn,@dg,@tr,@no,@fud,@mt::jsonb) RETURNING id", conn);
        cmd.Parameters.AddWithValue("aid", item.AppointmentId);
        cmd.Parameters.AddWithValue("pid", item.PetId);
        cmd.Parameters.AddWithValue("pn",  item.PetName);
        cmd.Parameters.AddWithValue("cid", item.CustomerId);
        cmd.Parameters.AddWithValue("cn",  item.CustomerName);
        cmd.Parameters.Add("vid", NpgsqlDbType.Integer).Value = (object?)item.VetId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("vn",  item.VetName);
        cmd.Parameters.AddWithValue("dg",  item.Diagnosis);
        cmd.Parameters.AddWithValue("tr",  item.Treatment);
        cmd.Parameters.AddWithValue("no",  item.Notes);
        cmd.Parameters.Add("fud", NpgsqlDbType.Date).Value = (object?)item.FollowUpDate ?? DBNull.Value;
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(MedicalRecord item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"UPDATE medical_records SET appointment_id=@aid,pet_id=@pid,pet_name=@pn,customer_id=@cid,
                customer_name=@cn,vet_id=@vid,vet_name=@vn,diagnosis=@dg,treatment=@tr,notes=@no,
                follow_up_date=@fud,metadata=@mt::jsonb,updated_at=NOW() WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id",  item.Id);
        cmd.Parameters.AddWithValue("aid", item.AppointmentId);
        cmd.Parameters.AddWithValue("pid", item.PetId);
        cmd.Parameters.AddWithValue("pn",  item.PetName);
        cmd.Parameters.AddWithValue("cid", item.CustomerId);
        cmd.Parameters.AddWithValue("cn",  item.CustomerName);
        cmd.Parameters.Add("vid", NpgsqlDbType.Integer).Value = (object?)item.VetId ?? DBNull.Value;
        cmd.Parameters.AddWithValue("vn",  item.VetName);
        cmd.Parameters.AddWithValue("dg",  item.Diagnosis);
        cmd.Parameters.AddWithValue("tr",  item.Treatment);
        cmd.Parameters.AddWithValue("no",  item.Notes);
        cmd.Parameters.Add("fud", NpgsqlDbType.Date).Value = (object?)item.FollowUpDate ?? DBNull.Value;
        cmd.Parameters.Add("mt", NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.ExecuteNonQuery();
    }

    public static List<RecordMedication> GetRecordMedications(int recordId)
    {
        var list = new List<RecordMedication>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT rm.medication_id, m.name, rm.dosage, rm.notes
              FROM medical_record_medications rm
              JOIN medications m ON m.id = rm.medication_id
              WHERE rm.record_id = @rid", conn);
        cmd.Parameters.AddWithValue("rid", recordId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new RecordMedication
            {
                MedicationId   = r.GetInt32(0),
                MedicationName = r.GetString(1),
                Dosage         = r.GetString(2),
                Notes          = r.GetString(3)
            });
        return list;
    }

    public static void SaveRecordMedications(int recordId, List<(int MedId, string Dosage, string Notes)> items)
    {
        using var conn = Database.OpenConnection();
        using var tx   = conn.BeginTransaction();
        using var del  = new NpgsqlCommand("DELETE FROM medical_record_medications WHERE record_id=@rid", conn, tx);
        del.Parameters.AddWithValue("rid", recordId);
        del.ExecuteNonQuery();

        foreach (var (medId, dosage, notes) in items)
        {
            using var ins = new NpgsqlCommand(
                "INSERT INTO medical_record_medications (record_id,medication_id,dosage,notes) VALUES (@rid,@mid,@dos,@no)", conn, tx);
            ins.Parameters.AddWithValue("rid", recordId);
            ins.Parameters.AddWithValue("mid", medId);
            ins.Parameters.AddWithValue("dos", dosage);
            ins.Parameters.AddWithValue("no",  notes);
            ins.ExecuteNonQuery();
        }
        tx.Commit();
    }

    // ── CBC Records ───────────────────────────────────────────────────────────

    public static List<CbcRecord> GetCbcRecords()
    {
        var list = new List<CbcRecord>();
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"SELECT id,pet_id,pet_name,customer_id,customer_name,test_date,
                     rbc,hgb,hct,mcv,mch,mchc,plt,wbc,neu,lym,mon,eos,bas,remarks,metadata
              FROM cbc_records ORDER BY test_date DESC", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new CbcRecord
            {
                Id           = r.GetInt32(0),
                PetId        = r.GetInt32(1),
                PetName      = r.GetString(2),
                CustomerId   = r.GetInt32(3),
                CustomerName = r.GetString(4),
                TestDate     = r.GetDateTime(5),
                Rbc          = r.GetDecimal(6),
                Hgb          = r.GetDecimal(7),
                Hct          = r.GetDecimal(8),
                Mcv          = r.GetDecimal(9),
                Mch          = r.GetDecimal(10),
                Mchc         = r.GetDecimal(11),
                Plt          = r.GetDecimal(12),
                Wbc          = r.GetDecimal(13),
                Neu          = r.GetDecimal(14),
                Lym          = r.GetDecimal(15),
                Mon          = r.GetDecimal(16),
                Eos          = r.GetDecimal(17),
                Bas          = r.GetDecimal(18),
                Remarks      = r.GetString(19),
                Metadata     = r.IsDBNull(20) ? null : r.GetString(20)
            });
        return list;
    }

    public static void Insert(CbcRecord item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO cbc_records (pet_id,pet_name,customer_id,customer_name,test_date,
                                        rbc,hgb,hct,mcv,mch,mchc,plt,wbc,neu,lym,mon,eos,bas,remarks,metadata)
              VALUES (@pid,@pn,@cid,@cn,@td,@rbc,@hgb,@hct,@mcv,@mch,@mchc,@plt,@wbc,@neu,@lym,@mon,@eos,@bas,@rem,@mt::jsonb) RETURNING id", conn);
        cmd.Parameters.AddWithValue("pid",  item.PetId);
        cmd.Parameters.AddWithValue("pn",   item.PetName);
        cmd.Parameters.AddWithValue("cid",  item.CustomerId);
        cmd.Parameters.AddWithValue("cn",   item.CustomerName);
        cmd.Parameters.AddWithValue("td",   item.TestDate);
        cmd.Parameters.AddWithValue("rbc",  item.Rbc);
        cmd.Parameters.AddWithValue("hgb",  item.Hgb);
        cmd.Parameters.AddWithValue("hct",  item.Hct);
        cmd.Parameters.AddWithValue("mcv",  item.Mcv);
        cmd.Parameters.AddWithValue("mch",  item.Mch);
        cmd.Parameters.AddWithValue("mchc", item.Mchc);
        cmd.Parameters.AddWithValue("plt",  item.Plt);
        cmd.Parameters.AddWithValue("wbc",  item.Wbc);
        cmd.Parameters.AddWithValue("neu",  item.Neu);
        cmd.Parameters.AddWithValue("lym",  item.Lym);
        cmd.Parameters.AddWithValue("mon",  item.Mon);
        cmd.Parameters.AddWithValue("eos",  item.Eos);
        cmd.Parameters.AddWithValue("bas",  item.Bas);
        cmd.Parameters.AddWithValue("rem",  item.Remarks);
        cmd.Parameters.Add("mt", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(CbcRecord item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"UPDATE cbc_records SET pet_id=@pid,pet_name=@pn,customer_id=@cid,customer_name=@cn,test_date=@td,
                rbc=@rbc,hgb=@hgb,hct=@hct,mcv=@mcv,mch=@mch,mchc=@mchc,plt=@plt,wbc=@wbc,
                neu=@neu,lym=@lym,mon=@mon,eos=@eos,bas=@bas,remarks=@rem,metadata=@mt::jsonb,updated_at=NOW()
              WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id",   item.Id);
        cmd.Parameters.AddWithValue("pid",  item.PetId);
        cmd.Parameters.AddWithValue("pn",   item.PetName);
        cmd.Parameters.AddWithValue("cid",  item.CustomerId);
        cmd.Parameters.AddWithValue("cn",   item.CustomerName);
        cmd.Parameters.AddWithValue("td",   item.TestDate);
        cmd.Parameters.AddWithValue("rbc",  item.Rbc);
        cmd.Parameters.AddWithValue("hgb",  item.Hgb);
        cmd.Parameters.AddWithValue("hct",  item.Hct);
        cmd.Parameters.AddWithValue("mcv",  item.Mcv);
        cmd.Parameters.AddWithValue("mch",  item.Mch);
        cmd.Parameters.AddWithValue("mchc", item.Mchc);
        cmd.Parameters.AddWithValue("plt",  item.Plt);
        cmd.Parameters.AddWithValue("wbc",  item.Wbc);
        cmd.Parameters.AddWithValue("neu",  item.Neu);
        cmd.Parameters.AddWithValue("lym",  item.Lym);
        cmd.Parameters.AddWithValue("mon",  item.Mon);
        cmd.Parameters.AddWithValue("eos",  item.Eos);
        cmd.Parameters.AddWithValue("bas",  item.Bas);
        cmd.Parameters.AddWithValue("rem",  item.Remarks);
        cmd.Parameters.Add("mt", NpgsqlTypes.NpgsqlDbType.Jsonb).Value = (object?)item.Metadata ?? DBNull.Value;
        cmd.ExecuteNonQuery();
    }

    public static void Delete(CbcRecord item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand("DELETE FROM cbc_records WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.ExecuteNonQuery();
    }
}

