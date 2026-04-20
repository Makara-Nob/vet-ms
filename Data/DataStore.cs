using Npgsql;
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
            "SELECT id, name, description, is_active FROM animal_species ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new AnimalSpecies
            {
                Id          = r.GetInt32(0),
                Name        = r.GetString(1),
                Description = r.GetString(2),
                IsActive    = r.GetBoolean(3)
            });
        return list;
    }

    public static void Insert(AnimalSpecies item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO animal_species (name, description, is_active) VALUES (@n,@d,@a) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(AnimalSpecies item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE animal_species SET name=@n, description=@d, is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
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
            SELECT b.id, b.species_id, s.name, b.name, b.description, b.is_active
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
                IsActive    = r.GetBoolean(5)
            });
        return list;
    }

    public static void Insert(Breed item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO breeds (species_id, name, description, is_active) VALUES (@s,@n,@d,@a) RETURNING id", conn);
        cmd.Parameters.AddWithValue("s", item.SpeciesId);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Breed item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE breeds SET species_id=@s, name=@n, description=@d, is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("s",  item.SpeciesId);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
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
            "SELECT id, name, category, price, description, is_active FROM service_types ORDER BY name", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ServiceType
            {
                Id          = r.GetInt32(0),
                Name        = r.GetString(1),
                Category    = r.GetString(2),
                Price       = r.GetDecimal(3),
                Description = r.GetString(4),
                IsActive    = r.GetBoolean(5)
            });
        return list;
    }

    public static void Insert(ServiceType item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO service_types (name, category, price, description, is_active) VALUES (@n,@c,@p,@d,@a) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("c", item.Category);
        cmd.Parameters.AddWithValue("p", item.Price);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(ServiceType item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE service_types SET name=@n, category=@c, price=@p, description=@d, is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("c",  item.Category);
        cmd.Parameters.AddWithValue("p",  item.Price);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
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
            "SELECT id, name, category, dosage_form, unit, description, is_active FROM medications ORDER BY name", conn);
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
                IsActive    = r.GetBoolean(6)
            });
        return list;
    }

    public static void Insert(Medication item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO medications (name, category, dosage_form, unit, description, is_active) VALUES (@n,@c,@f,@u,@d,@a) RETURNING id", conn);
        cmd.Parameters.AddWithValue("n", item.Name);
        cmd.Parameters.AddWithValue("c", item.Category);
        cmd.Parameters.AddWithValue("f", item.DosageForm);
        cmd.Parameters.AddWithValue("u", item.Unit);
        cmd.Parameters.AddWithValue("d", item.Description);
        cmd.Parameters.AddWithValue("a", item.IsActive);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Medication item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE medications SET name=@n, category=@c, dosage_form=@f, unit=@u, description=@d, is_active=@a WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("n",  item.Name);
        cmd.Parameters.AddWithValue("c",  item.Category);
        cmd.Parameters.AddWithValue("f",  item.DosageForm);
        cmd.Parameters.AddWithValue("u",  item.Unit);
        cmd.Parameters.AddWithValue("d",  item.Description);
        cmd.Parameters.AddWithValue("a",  item.IsActive);
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
            "SELECT id, company_name, contact_person, phone, email, address, is_active FROM suppliers ORDER BY company_name", conn);
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
                IsActive      = r.GetBoolean(6)
            });
        return list;
    }

    public static void Insert(Supplier item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO suppliers (company_name, contact_person, phone, email, address, is_active) VALUES (@c,@p,@ph,@e,@a,@ac) RETURNING id", conn);
        cmd.Parameters.AddWithValue("c",  item.CompanyName);
        cmd.Parameters.AddWithValue("p",  item.ContactPerson);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("e",  item.Email);
        cmd.Parameters.AddWithValue("a",  item.Address);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
        item.Id = (int)cmd.ExecuteScalar()!;
    }

    public static void Update(Supplier item)
    {
        using var conn = Database.OpenConnection();
        using var cmd = new NpgsqlCommand(
            "UPDATE suppliers SET company_name=@c, contact_person=@p, phone=@ph, email=@e, address=@a, is_active=@ac WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("c",  item.CompanyName);
        cmd.Parameters.AddWithValue("p",  item.ContactPerson);
        cmd.Parameters.AddWithValue("ph", item.Phone);
        cmd.Parameters.AddWithValue("e",  item.Email);
        cmd.Parameters.AddWithValue("a",  item.Address);
        cmd.Parameters.AddWithValue("ac", item.IsActive);
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
}
