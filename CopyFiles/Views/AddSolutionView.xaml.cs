using CopyFiles.Contract.Views;
using CopyFiles.Extensions.UI.WPF;
using CopyFiles.ViewModels;
using System.Windows;

namespace CopyFiles.Views;

/// <summary>
/// AddSolutionView.xaml の相互作用ロジック
/// </summary>
public partial class AddSolutionView : IAddSolutionView
{
	public AddSolutionView( AddSolutionViewModel vm )
	{
		InitializeComponent();
		ViewModel = vm;
		DataContext = vm;
		vm.PropertyChanged += ( sender, e ) =>
		{
			if( e.PropertyName == nameof( AddSolutionViewModel.DialogResult ) )
			{
				DialogResult = ViewModel.DialogResult;
			}
		};
	}
	public AddSolutionViewModel ViewModel { get; }

	public bool? ShowWindow()
	{
		Owner = Utilities.GetOwnerWindow();
		return ShowDialog();
	}
}
