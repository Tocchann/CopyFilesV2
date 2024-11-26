using CopyFiles.Contract.Views;
using CopyFiles.Extensions.UI.WPF;
using CopyFiles.ViewModels;
using System.Windows;

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
	}

	public bool? ShowWindow()
	{
		Owner = Utilities.GetOwnerWindow();
		return ShowDialog();
	}
}
