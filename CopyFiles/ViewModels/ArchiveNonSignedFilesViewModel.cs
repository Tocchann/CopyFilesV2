using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Contract.Views;
using CopyFiles.Core.Models;
using CopyFiles.Core.Tasks;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Extensions.UI.WPF;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks.Dataflow;
using Windows.Devices.WiFiDirect;
using static CopyFiles.ViewModels.TargetFileInformationItem;

namespace CopyFiles.ViewModels;

public partial class ArchiveNonSignedFilesViewModel : MainWorkBaseViewModel
{
	[ObservableProperty]
	ReferFolderItem referFolderItem;

	[ObservableProperty]
	string zipFileNamePrefix;

	partial void OnZipFileNamePrefixChanged( string value )
	{
		if( App.ProjectSettingManager?.ProjectName != null)
		{
			App.ProjectSettingManager.CurrentSetting.ZipFileNamePrefix = value;
		}
	}

	[RelayCommand]
	void EditReferFolder()
	{
		var dlg = App.GetService<IEditReferFolderView>();
		dlg.ViewModel.ReferFolder = ReferFolderItem.ReferFolder;
		dlg.ViewModel.IsCopySetting = false;
		dlg.ViewModel.DialogTitle = "圧縮対象フォルダの編集";
		if( dlg.ShowWindow() == true )
		{
			ReferFolderItem.BaseFolder = App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder = dlg.ViewModel.BaseFolder;
			ReferFolderItem.ReferenceFolder = App.ProjectSettingManager.CurrentSetting.SignerFileSetting.ReferenceFolder = dlg.ViewModel.ReferenceFolder;
			if( string.IsNullOrEmpty( ZipFileNamePrefix ) )
			{
				var lastFolder = ReferFolderItem.BaseFolder;
				var lastFolderName = Path.GetFileName( lastFolder );
				while( !string.IsNullOrEmpty( lastFolder ) && string.IsNullOrEmpty( lastFolderName ) )
				{
					lastFolder = Path.GetDirectoryName( lastFolder );
					lastFolderName = Path.GetFileName( lastFolder );
				}
				if( !string.IsNullOrEmpty( lastFolderName ) )
				{
					ZipFileNamePrefix = lastFolderName;
				}
			}
		}
	}
	[RelayCommand]
	void RemoveReferFolder()
	{
		var result = App.DispAlert.Show( "選択された参照フォルダを削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			ReferFolderItem.BaseFolder = App.ProjectSettingManager.CurrentSetting.SignerFileSetting.BaseFolder = string.Empty;
			ReferFolderItem.ReferenceFolder = App.ProjectSettingManager.CurrentSetting.SignerFileSetting.ReferenceFolder = string.Empty;
		}
	}

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
			var files = await ListFilesTask.ListSourceFilesAsync( App.ProjectSettingManager.CurrentSetting, false, m_cts.Token );
			ProgressMessage = "圧縮対象ファイルの確認中...";
			ProgressMin = 0;
			ProgressMax = files.Count();
			ProgressValue = 0;
			IsIndeterminate = false;
			TargetFilesList = await ListFilesTask.ListNotSignedFilesAsync( App.ProjectSettingManager.CurrentSetting, files, progressCount, m_cts.Token );
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
	async Task ArchiveTargetFilesAsync()
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
			ProgressMessage = "圧縮中...";
			ProgressMin = 0;
			ProgressMax = TargetFiles.Count( item => item.IsCopy );
			IsProgressBarVisible = true;
			ProgressValue = 0;
			IsIndeterminate = false;
			// コピー処理をここで行えばよい…でしょう
			var token = m_cts.Token;
			// 圧縮は一度に１ファイルしかおこなえないので直列処理にしないとダメ(順番は関係ない)
			var blockOptions = new ExecutionDataflowBlockOptions
			{
				CancellationToken = token,
				EnsureOrdered = false,
				MaxDegreeOfParallelism = 1,
			};
			var fileNode = $"{App.ProjectSettingManager.CurrentSetting.ZipFileNamePrefix}_{DateTime.Now:yyyyMMdd}";
			var filePath = Path.Combine( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.ReferenceFolder, fileNode + ".zip" );
			int sameCount = 1;
			while( File.Exists( filePath ) )
			{
				var appendFile = $"_{sameCount}";
				sameCount++;
				filePath = Path.Combine( App.ProjectSettingManager.CurrentSetting.SignerFileSetting.ReferenceFolder, fileNode + appendFile + ".zip" );
			}
			using( var archive = ZipFile.Open( filePath, ZipArchiveMode.Create ) )
			{
				var actionBlock = new ActionBlock<TargetFileInformationItem>( item => ArchiveAction( archive, item, progressCount, token ), blockOptions );
				foreach( var item in TargetFiles.Where( i => i.IsCopy ) )
				{
					await actionBlock.SendAsync( item, m_cts.Token );
				}
				actionBlock.Complete();
				await actionBlock.Completion;
			}
		}
		catch( OperationCanceledException )
		{
		}
		finally
		{
			IsProgressBarVisible = false;
			m_cts?.Dispose();
			m_cts = null;
		}
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
				TargetFiles.Add( new TargetFileInformationItem( App.ProjectSettingManager.CurrentSetting.SignerFileSetting, info, OnIsCheckedChanged ) );
			}
		}
		IsReadyCopy = TargetFiles.Any( info => info.IsCopy );
	}

	private void ArchiveAction( ZipArchive archive, TargetFileInformationItem item, IProgress<int> progress, CancellationToken token )
	{
		// 単純に圧縮処理を行えばよい
		archive.CreateEntryFromFile( item.TargetFileInformation.BaseFileInfo.FilePath, item.TargetFileInformation.ReferFileInfo.FilePath );
		progress.Report( 1 );
	}

	public ArchiveNonSignedFilesViewModel()
	{
		if( App.ProjectSettingManager?.ProjectName != null )
		{
			ReferFolderItem = new( App.ProjectSettingManager.CurrentSetting.SignerFileSetting );
			ZipFileNamePrefix = App.ProjectSettingManager.CurrentSetting.ZipFileNamePrefix;
		}
		else
		{
			ReferFolderItem = new();
			ZipFileNamePrefix = string.Empty;
		}
	}
}
