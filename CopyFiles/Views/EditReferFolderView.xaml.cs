using CopyFiles.Contract.Views;
using CopyFiles.Extensions.UI.WPF;
using CopyFiles.ViewModels;
using System.Windows;

namespace CopyFiles.Views;

/// <summary>
/// EditReferFolderView.xaml の相互作用ロジック
/// </summary>
public partial class EditReferFolderView : IEditReferFolderView
{
	public EditReferFolderView( EditReferFolderViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
		ViewModel = vm;
		vm.PropertyChanged += ( _, e ) =>
		{
			if( e.PropertyName == nameof( EditReferFolderViewModel.DialogResult ) )
			{
				DialogResult = vm.DialogResult;
			}
		};
	}

	public EditReferFolderViewModel ViewModel { get; init; }

	public bool? ShowWindow()
	{
		Owner = Utilities.GetOwnerWindow();
		return ShowDialog();
	}
}
