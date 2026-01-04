using DocumentOrganizer.ViewModel;
using Microsoft.Maui.Controls;

namespace DocumentOrganizer.Views;

public partial class RuleEditorView : ContentPage
{
    public RuleEditorView(RuleEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public RuleEditorView()
    {
        InitializeComponent();
    }

    private void SfDataGrid_ChildAdded(object sender, ElementEventArgs e)
    {

    }
}