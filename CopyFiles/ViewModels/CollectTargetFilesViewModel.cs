using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CopyFiles.Contract.Views;
using CopyFiles.Core.Models;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Extensions.UI.WPF;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CopyFiles.ViewModels;

public partial class CollectTargetFilesViewModel : ObservableObject
{
	public ObservableCollection<string> ProjectFiles { get; init; }
	[ObservableProperty]
	string? selectedProjectFile;

	partial void OnSelectedProjectFileChanged( string? value )
	{
		OnPropertyChanged( nameof( IsSelectedProjectFile ) );
	}

	public ObservableCollection<ReferFolderItem> ReferFolders { get; init; }
	[ObservableProperty]
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
	void CheckTargetFiles()
	{
		// インジケータを動かしつつタスクを回す
	}
	[RelayCommand]
	void CopyTargetFiles()
	{
		// インジケータを動かしつつファイルをコピーする
	}
	[ObservableProperty]
	bool isReadyCopy;


	public CollectTargetFilesViewModel( ILogger<CollectTargetFilesViewModel> logger, IDispAlert dispAlert )
	{
		m_logger = logger;
		m_dispAlert = dispAlert;
		ProjectFiles = [.. App.ProjectSettingManager.CurrentSetting.ProjectFiles];
		ReferFolders = [.. App.ProjectSettingManager.CurrentSetting.CopySettings.Select( x => new ReferFolderItem( x ) )];
		TargetFiles = new();
	}
	[DesignOnly(true)]
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	public CollectTargetFilesViewModel()
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。'required' 修飾子を追加するか、Null 許容として宣言することを検討してください。
	{
		//Debug.Assert( false );	//	実行されないはず
	}

	private ILogger<CollectTargetFilesViewModel> m_logger;
	private IDispAlert m_dispAlert;
}
