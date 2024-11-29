using CopyFiles.Contract.Views;
using CopyFiles.Extensions.UI.WPF;
using CopyFiles.ViewModels;

namespace CopyFiles.Views;

/// <summary>
/// CollectTargetFilesView.xaml の相互作用ロジック
/// </summary>
public partial class CollectTargetFilesView : ICollectTargetFilesView
{
	public CollectTargetFilesView( CollectTargetFilesViewModel vm )
	{
		InitializeComponent();
		DataContext = vm;
		ViewModel = vm;
	}
	public CollectTargetFilesViewModel ViewModel { get; init; }
	public bool? ShowWindow()
	{
		Owner = Utilities.GetOwnerWindow();
		return ShowDialog();
	}
}
