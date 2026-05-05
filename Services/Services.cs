using VetMS.Data;
using VetMS.Models;

namespace VetMS.Services;

public interface ISpeciesService
{
    List<AnimalSpecies> GetAll();
    void Insert(AnimalSpecies item);
    void Update(AnimalSpecies item);
    void Delete(AnimalSpecies item);
}
public class SpeciesService : ISpeciesService
{
    public List<AnimalSpecies> GetAll() => DataStore.GetAnimalSpecies();
    public void Insert(AnimalSpecies item) => DataStore.Insert(item);
    public void Update(AnimalSpecies item) => DataStore.Update(item);
    public void Delete(AnimalSpecies item) => DataStore.Delete(item);
}

public interface IBreedService
{
    List<Breed> GetAll();
    void Insert(Breed item);
    void Update(Breed item);
    void Delete(Breed item);
}
public class BreedService : IBreedService
{
    public List<Breed> GetAll() => DataStore.GetBreeds();
    public void Insert(Breed item) => DataStore.Insert(item);
    public void Update(Breed item) => DataStore.Update(item);
    public void Delete(Breed item) => DataStore.Delete(item);
}

public interface IServiceTypeService
{
    List<ServiceType> GetAll();
    void Insert(ServiceType item);
    void Update(ServiceType item);
    void Delete(ServiceType item);
}
public class ServiceTypeService : IServiceTypeService
{
    public List<ServiceType> GetAll() => DataStore.GetServiceTypes();
    public void Insert(ServiceType item) => DataStore.Insert(item);
    public void Update(ServiceType item) => DataStore.Update(item);
    public void Delete(ServiceType item) => DataStore.Delete(item);
}

public interface IMedicationService
{
    List<Medication> GetAll();
    void Insert(Medication item);
    void Update(Medication item);
    void Delete(Medication item);
}
public class MedicationService : IMedicationService
{
    public List<Medication> GetAll() => DataStore.GetMedications();
    public void Insert(Medication item) => DataStore.Insert(item);
    public void Update(Medication item) => DataStore.Update(item);
    public void Delete(Medication item) => DataStore.Delete(item);
}

public interface ISupplierService
{
    List<Supplier> GetAll();
    void Insert(Supplier item);
    void Update(Supplier item);
    void Delete(Supplier item);
}
public class SupplierService : ISupplierService
{
    public List<Supplier> GetAll() => DataStore.GetSuppliers();
    public void Insert(Supplier item) => DataStore.Insert(item);
    public void Update(Supplier item) => DataStore.Update(item);
    public void Delete(Supplier item) => DataStore.Delete(item);
}

public interface IUserService
{
    List<User> GetAll();
    void Insert(User item);
    void Update(User item);
    void Delete(User item);
}
public class UserService : IUserService
{
    public List<User> GetAll() => DataStore.GetUsers();
    public void Insert(User item) => DataStore.Insert(item);
    public void Update(User item) => DataStore.Update(item);
    public void Delete(User item) => DataStore.Delete(item);
}

public interface ICustomerService
{
    List<Customer> GetAll();
    void Insert(Customer item);
    void Update(Customer item);
    void Delete(Customer item);
}
public class CustomerService : ICustomerService
{
    public List<Customer> GetAll() => DataStore.GetCustomers();
    public void Insert(Customer item) => DataStore.Insert(item);
    public void Update(Customer item) => DataStore.Update(item);
    public void Delete(Customer item) => DataStore.Delete(item);
}

public interface IPetService
{
    List<Pet> GetAll();
    void Insert(Pet item);
    void Update(Pet item);
    void Delete(Pet item);
}
public class PetService : IPetService
{
    public List<Pet> GetAll() => DataStore.GetPets();
    public void Insert(Pet item) => DataStore.Insert(item);
    public void Update(Pet item) => DataStore.Update(item);
    public void Delete(Pet item) => DataStore.Delete(item);
}

public interface IAppointmentService
{
    List<Appointment> GetAll();
    void Insert(Appointment item);
    void Update(Appointment item);
    void Delete(Appointment item);
}
public class AppointmentService : IAppointmentService
{
    public List<Appointment> GetAll() => DataStore.GetAppointments();
    public void Insert(Appointment item) => DataStore.Insert(item);
    public void Update(Appointment item) => DataStore.Update(item);
    public void Delete(Appointment item) => DataStore.Delete(item);
}

public interface IMedicalRecordService
{
    List<MedicalRecord> GetAll();
    void Insert(MedicalRecord item);
    void Update(MedicalRecord item);
    List<RecordMedication> GetMedications(int recordId);
    void SaveMedications(int recordId, List<(int MedId, string Dosage, string Notes)> items);
}
public class MedicalRecordService : IMedicalRecordService
{
    public List<MedicalRecord> GetAll() => DataStore.GetMedicalRecords();
    public void Insert(MedicalRecord item) => DataStore.Insert(item);
    public void Update(MedicalRecord item) => DataStore.Update(item);
    public List<RecordMedication> GetMedications(int recordId) => DataStore.GetRecordMedications(recordId);
    public void SaveMedications(int recordId, List<(int MedId, string Dosage, string Notes)> items)
        => DataStore.SaveRecordMedications(recordId, items);
}

public interface ICbcService
{
    List<CbcRecord> GetAll();
    void Insert(CbcRecord item);
    void Update(CbcRecord item);
    void Delete(CbcRecord item);
}
public class CbcService : ICbcService
{
    public List<CbcRecord> GetAll() => DataStore.GetCbcRecords();
    public void Insert(CbcRecord item) => DataStore.Insert(item);
    public void Update(CbcRecord item) => DataStore.Update(item);
    public void Delete(CbcRecord item) => DataStore.Delete(item);
}

public interface IClinicSettingsService
{
    ClinicSettings Get();
    void Save(ClinicSettings settings);
}
public class ClinicSettingsService : IClinicSettingsService
{
    public ClinicSettings Get() => DataStore.GetClinicSettings();
    public void Save(ClinicSettings settings) => DataStore.SaveClinicSettings(settings);
}
