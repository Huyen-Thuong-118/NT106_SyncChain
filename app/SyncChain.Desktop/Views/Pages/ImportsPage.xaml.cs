using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ImportsPage : ContentPage
{
	public IReadOnlyList<ImportItem> Imports => DemoData.Imports;
	public IReadOnlyList<SupplierItem> Suppliers => DemoData.Suppliers;

	public ImportsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
