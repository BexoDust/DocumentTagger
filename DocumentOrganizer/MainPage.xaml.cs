using DocumentOrganizer.ViewModel;

namespace DocumentOrganizer;

public partial class MainPage : TabbedPage
{
	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		BindingContext= viewModel;
	}
}

