using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using VetMS.Data;
using VetMS.Services;

namespace VetMS;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            Database.Initialize();
            Database.SeedData();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText("db_error.txt", ex.ToString());
            MessageBox.Show(
                $"Failed to connect to the database:\n\n{ex.Message}\n\nCheck appsettings.json and make sure PostgreSQL is running.",
                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddSingleton<AppStateService>();
        services.AddSingleton<ToastService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<ICbcService, CbcService>();
        services.AddScoped<ISpeciesService, SpeciesService>();
        services.AddScoped<IBreedService, BreedService>();
        services.AddScoped<IServiceTypeService, ServiceTypeService>();
        services.AddScoped<IMedicationService, MedicationService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IClinicSettingsService, ClinicSettingsService>();

        var serviceProvider = services.BuildServiceProvider();

        var form = new MainWindow(serviceProvider);
        Application.Run(form);
    }
}
