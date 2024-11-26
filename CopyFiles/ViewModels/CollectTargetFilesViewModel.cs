using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CopyFiles.Contract.Views;
using CopyFiles.Core.DataflowBlock;
using CopyFiles.Core.Models;
using CopyFiles.Core.Tasks;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Extensions.UI.WPF;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace CopyFiles.ViewModels;

public partial class CollectTargetFilesViewModel : ObservableObject
{
	public ObservableCollection<string> ProjectFiles { get; init; }
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( IsSelectedProjectFile ) )]
	string? selectedProjectFile;

	public ObservableCollection<ReferFolderItem> ReferFolders { get; init; }
	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( IsSelectedReferFolder ) )]
	ReferFolderItem? selectedReferFolder;

	public ObservableCollection<TargetFileInformationItem> TargetFiles { get; init; }

	[RelayCommand]
	void AddProjectFile()
	{
		// 本当はDIにしたほうがいいけど、面倒なのでやらない
		var dlg = new OpenFileDialog();
		dlg.Multiselect = true;
		dlg.Filter = "InstallShield プロジェクトファイル|*.ism|実行可能ファイル|*.exe;*.dll|すべてのファイル|*.*";
		if( dlg.ShowDialog( Utilities.GetOwnerWindow() ) == true )
		{
			foreach( var file in dlg.FileNames )
			{
				App.ProjectSettingManager.CurrentSetting.ProjectFiles.Add( file );
				ProjectFiles.Add( file );
			}
		}
	}
	[RelayCommand]
	void RemoveProjectFile()
	{
		var result = m_dispAlert.Show( "選択されたプロジェクトファイルを削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			var selFile = SelectedProjectFile!;	//	 ここは選択されているので null にはなっていない
			App.ProjectSettingManager.CurrentSetting.ProjectFiles.Remove( selFile );
			ProjectFiles.Remove( selFile );
		}
	}
	public bool IsSelectedProjectFile => !string.IsNullOrEmpty( SelectedProjectFile );

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
		var result = m_dispAlert.Show( "選択された参照フォルダを削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			App.ProjectSettingManager.CurrentSetting.CopySettings.Remove( SelectedReferFolder!.ReferFolder );
			ReferFolders.Remove( SelectedReferFolder );
		}
	}
	public bool IsSelectedReferFolder => SelectedReferFolder != null;

	[ObservableProperty]
	bool isCheckedNeedCopy;

	partial void OnIsCheckedNeedCopyChanged( bool value )
	{
		App.ProjectSettingManager.CurrentSetting.IsNeedCopy = value;
		ListupTargetFiles();
	}

	// Progress関連処理
	[ObservableProperty]
	bool isProgressBarVisible;

	[ObservableProperty]
	bool isIndeterminate;

	[ObservableProperty]
	int progressMin;

	[ObservableProperty]
	int progressMax;

	[ObservableProperty]
	int progressValue;

	[ObservableProperty]
	string? progressMessage;

	[RelayCommand]
	void CheckIsCopy()
	{
		// どこかが選択されている場合のみ稼働する(それ以外は何もしない)
		var selItems = TargetFiles.Where( i => i.IsSelected );
		if( selItems.Any() )
		{
			var toggleFlag = selItems.First().IsCopy == false;
			foreach( var item in selItems )
			{
				item.IsCopy = toggleFlag;
			}
		}
	}
	[RelayCommand]
	async Task CancelWorkAsync()
	{
		if( m_cts != null )
		{
			await m_cts.CancelAsync();
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
		}
	}
	private void ListupTargetFiles()
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

	public CollectTargetFilesViewModel( ILogger<CollectTargetFilesViewModel> logger, IDispAlert dispAlert )
	{
		m_logger = logger;
		m_dispAlert = dispAlert;
		ProjectFiles = [.. App.ProjectSettingManager.CurrentSetting.ProjectFiles];
		ReferFolders = [.. App.ProjectSettingManager.CurrentSetting.CopySettings.Select( x => new ReferFolderItem( x ) )];
		TargetFiles = new();
		IsCheckedNeedCopy = App.ProjectSettingManager.CurrentSetting.IsNeedCopy;
	}
	[DesignOnly(true)]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	public CollectTargetFilesViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	{
		//Debug.Assert( false );	//	実行されないはず
	}

	private void OnIsCheckedChanged()
	{
		// コピー対象になっているものが一つ以上あったらコピーを許可する
		IsReadyCopy = TargetFiles.Any( info => info.IsCopy );
	}

	private ILogger<CollectTargetFilesViewModel> m_logger;
	private IDispAlert m_dispAlert;
	private CancellationTokenSource? m_cts;
	private IEnumerable<TargetFileInformation>? TargetFilesList { get; set; }
}
