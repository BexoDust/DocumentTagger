namespace DocumentOrganizer;

public partial class App : Application
{
	public App()
	{
		//Register Syncfusion license
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NHaF5cWWBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWX5ecnVTRGldVkVwX0o=");

		InitializeComponent();

		MainPage = new AppShell();
	}
}
