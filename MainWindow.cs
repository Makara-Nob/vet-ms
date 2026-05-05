using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace VetMS;

public class MainWindow : Form
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        Text = "VetMS - Veterinary Management System";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(1024, 680);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = "wwwroot/index.html",
            Services = serviceProvider,
        };
        blazorWebView.RootComponents.Add<Components.App>("#app");

        Controls.Add(blazorWebView);
    }
}
