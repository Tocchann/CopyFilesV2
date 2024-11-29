using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contract.Views;
using CopyFiles.Core.DataflowBlock;
using CopyFiles.Core.Tasks;
using CopyFiles.Extensions.UI.Abstractions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace CopyFiles.ViewModels;

public partial class CollectTargetFilesViewModel : MainWorkBaseViewModel
{
	public ObservableCollection<ReferFolderItem> ReferFolders { get; init; }
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( IsSelectedReferFolder ) )]
	ReferFolderItem? selectedReferFolder;

	[RelayCommand]
	void AddReferFolder()
	{
		var dlg = App.GetService<IEditReferFolderView>();
		dlg.ViewModel.IsCopySetting = true;
		dlg.ViewModel.DialogTitle = "コピーフォルダの追加";
		if( dlg.ShowWindow() == true )
		{
			var referFolder = dlg.ViewModel.ReferFolder;
			App.ProjectSettingManager.CurrentSetting.CopySettings.Add( referFolder );
			ReferFolders.Add( new ReferFolderItem( referFolder ) );
		}
	}
	[RelayCommand]
	void EditReferFolder()
	{
		var dlg = App.GetService<IEditReferFolderView>();
		dlg.ViewModel.ReferFolder = SelectedReferFolder!.ReferFolder;
		dlg.ViewModel.IsCopySetting = true;
		dlg.ViewModel.DialogTitle = "コピーフォルダの編集";
		if( dlg.ShowWindow() == true )
		{
			// 中身だけ書き換える
			var referFolder = App.ProjectSettingManager.CurrentSetting.CopySettings.First( r => r.BaseFolder == SelectedReferFolder.BaseFolder && r.ReferenceFolder == SelectedReferFolder.ReferenceFolder );
			referFolder.BaseFolder = dlg.ViewModel.BaseFolder;
			referFolder.ReferenceFolder = dlg.ViewModel.ReferenceFolder;
			var referFolderItem = ReferFolders.First( r => r.BaseFolder == SelectedReferFolder.BaseFolder && r.ReferenceFolder == SelectedReferFolder.ReferenceFolder );
			referFolderItem.BaseFolder = dlg.ViewModel.BaseFolder;
			referFolderItem.ReferenceFolder = dlg.ViewModel.ReferenceFolder;
			SelectedReferFolder = referFolderItem;
		}
	}
	[RelayCommand]
	void RemoveReferFolder()
	{
		var result = App.DispAlert.Show( "選択された参照フォルダを削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			App.ProjectSettingManager.CurrentSetting.CopySettings.Remove( SelectedReferFolder!.ReferFolder );
			ReferFolders.Remove( SelectedReferFolder );
		}
	}
	public bool IsSelectedReferFolder => SelectedReferFolder != null;

	[RelayCommand]
	async Task CheckTargetFilesAsync()
	{
		Progress<int> progressCount = new();
		progressCount.ProgressChanged += ( sender, e ) =>
		{
			ProgressValue = e;
		};
		// プロジェクトを読み込む
		try
		{
			m_cts = new();
			IsIndeterminate = true;
			IsProgressBarVisible = true;
			ProgressMessage = "プロジェクトファイルの読み込み中...";
			var files = await ListFilesTask.ListSourceFilesAsync( App.ProjectSettingManager.CurrentSetting, true, m_cts.Token );
			ProgressMessage = "コピー対象ファイルの確認中...";
			ProgressMin = 0;
			ProgressMax = files.Count();
			ProgressValue = 0;
			IsIndeterminate = false;
			TargetFilesList = await ListFilesTask.ListCopyFilesAsync( App.ProjectSettingManager.CurrentSetting, files, progressCount, m_cts.Token );
			ListupTargetFiles();
		}
		catch( OperationCanceledException )
		{
			TargetFiles.Clear();
		}
		finally
		{
			IsIndeterminate = false;
			IsProgressBarVisible = false;
			m_cts?.Dispose();
			m_cts = null;
			// ビューに位置調整をおこなわせたい…けどどうするんだ？
		}
	}
	[RelayCommand]
	async Task CopyTargetFilesAsync()
	{
		Progress<int> progressCount = new();
		progressCount.ProgressChanged += ( sender, e ) =>
		{
			ProgressValue += e;
		};
		try
		{
			m_cts = new();
			IsIndeterminate = false;
			ProgressMessage = "コピー中...";
			ProgressMin = 0;
			ProgressMax = TargetFiles.Count( item => item.IsCopy );
			IsProgressBarVisible = true;
			ProgressValue = 0;
			IsIndeterminate = false;
			// コピー処理をここで行えばよい…でしょう
			var token = m_cts.Token;
			var blockOptions = new ExecutionDataflowBlockOptions
			{
				CancellationToken = token,
				EnsureOrdered = false,
				MaxDegreeOfParallelism = -1,
			};
			var copyActionBlock = new ActionBlock<TargetFileInformationItem>( item => CopyAction( item, progressCount, token ), blockOptions );
			foreach( var item in TargetFiles.Where( i => i.IsCopy ) )
			{
				await copyActionBlock.SendAsync( item, m_cts.Token );
			}
			copyActionBlock.Complete();
			await copyActionBlock.Completion;
		}
		catch( OperationCanceledException )
		{
			//TargetFiles.Clear();
		}
		finally
		{
			IsProgressBarVisible = false;
			m_cts?.Dispose();
			m_cts = null;
		}
	}
	[ObservableProperty]
	bool isReadyCopy;

	private void CopyAction( TargetFileInformationItem item, IProgress<int> progress, CancellationToken token )
	{
		// ここでコピー処理を行う
		token.ThrowIfCancellationRequested();

		// 条件によらず呼び出されていればカウンタを上げておかないとおかしくなる
		if( item.IsCopy )
		{
			var dstDir = Path.GetDirectoryName( item.TargetFileInformation.BaseFileInfo.FilePath );
			Debug.Assert( dstDir != null ); // フルパスでセットされているのでnullになることはない
			Directory.CreateDirectory( dstDir );
			File.Copy( item.TargetFileInformation.ReferFileInfo.FilePath, item.TargetFileInformation.BaseFileInfo.FilePath, true );
		}
		// 比較しなおしだけする
		token.ThrowIfCancellationRequested();
		item.TargetFileInformation.BaseFileInfo.UpdateFileInfo();
		CompareFileInfo.CompareCopy( item.TargetFileInformation, token );
		item.UpdateStatus( true, true );
		// 進捗を進める
		progress.Report( 1 );
	}
	protected override void ListupTargetFiles()
	{
		// ここは一瞬で組み立てできるので、UIスレッドで処理する
		TargetFiles.Clear();
		if( TargetFilesList != null )
		{
			var targetInfos = TargetFilesList;
			if( IsCheckedNeedCopy )
			{
				targetInfos = targetInfos.Where( info => info.NeedCopy );
			}
			// 順番は比較条件->ファイルパスの順に比較(それ以降は特に比較しなくてもいいでしょう)
			foreach( var info in targetInfos.OrderBy( i => i.ReferFileInfo.FilePath ).ThenBy( i => i.CompareStatus ) )
			{
				TargetFiles.Add( new TargetFileInformationItem( App.ProjectSettingManager.CurrentSetting.CopySettings, info, OnIsCheckedChanged ) );
			}
		}
		IsReadyCopy = TargetFiles.Any( info => info.IsCopy );
	}

	public CollectTargetFilesViewModel()
	{
		if( App.ProjectSettingManager?.ProjectName != null )
		{
			ReferFolders = [.. App.ProjectSettingManager.CurrentSetting.CopySettings.Select( x => new ReferFolderItem( x ) )];
		}
		else
		{
			ReferFolders = new();
		}
	}
}
