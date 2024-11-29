using CopyFiles.Contract.Views;
using CopyFiles.Extensions.UI.WPF;
using CopyFiles.ViewModels;

namespace CopyFiles.Views;

/// <summary>
/// ArchiveNonSignedFilesView.xaml の相互作用ロジック
/// </summary>
public partial class ArchiveNonSignedFilesView : IArchiveNonSignedFilesView
{
	public ArchiveNonSignedFilesView( ArchiveNonSignedFilesViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
		ViewModel = vm;
	}

	public ArchiveNonSignedFilesViewModel ViewModel { get; init; }

	public bool? ShowWindow()
	{
		Owner = Utilities.GetOwnerWindow();
		return ShowDialog();
	}
}
