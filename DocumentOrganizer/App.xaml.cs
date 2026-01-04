using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace DocumentOrganizer;

public partial class App : Application
{
	public App()
	{
		AppDomain.CurrentDomain.FirstChanceException += this.CurrentDomain_FirstChanceException;

		//Register Syncfusion license
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzk1MDk4NkAzMzMwMmUzMDJlMzAzYjMzMzAzYlUvRW0zTVlmTVIzNHRzODd5MGh1OVE0OXQ0N2VMUHVSNVMvZGhBb3FIUVk9");

		InitializeComponent();
	}

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
	{
		System.Diagnostics.Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {e.Exception.ToString()}");System.Diagnostics.Debug.WriteLine($"********************************** UNHANDLED EXCEPTION! Details: {e.Exception.ToString()}");
	}
}
