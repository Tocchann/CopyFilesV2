using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CopyFiles.Core.Models;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Extensions.UI.WPF;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using Windows.Security.EnterpriseData;
using static CopyFiles.ViewModels.TargetFileInformationItem;

namespace CopyFiles.ViewModels;

public abstract partial class MainWorkBaseViewModel : ObservableObject
{

	public ObservableCollection<string> ProjectFiles { get; init; }

	[ObservableProperty]
	[NotifyPropertyChangedFor( nameof( IsSelectedProjectFile ) )]
	string? selectedProjectFile;
	public bool IsSelectedProjectFile => !string.IsNullOrEmpty( SelectedProjectFile );

	public ObservableCollection<TargetFileInformationItem> TargetFiles { get; init; }

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

	[ObservableProperty]
	bool isCheckedNeedCopy;

	partial void OnIsCheckedNeedCopyChanged( bool value )
	{
		App.ProjectSettingManager.CurrentSetting.IsNeedCopy = value;
		ListupTargetFiles();
	}

	[ObservableProperty]
	bool isReadyCopy;

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
		var result = App.DispAlert.Show( "選択されたプロジェクトファイルを削除しますか？", IDispAlert.Buttons.YesNo, IDispAlert.Icon.Question );
		if( result == IDispAlert.Result.Yes )
		{
			var selFile = SelectedProjectFile!; //	 ここは選択されているので null にはなっていない
			App.ProjectSettingManager.CurrentSetting.ProjectFiles.Remove( selFile );
			ProjectFiles.Remove( selFile );
		}
	}
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

	public MainWorkBaseViewModel()
	{
		TargetFiles = new();
		if( App.ProjectSettingManager.ProjectName != null )
		{
			ProjectFiles = [.. App.ProjectSettingManager.CurrentSetting.ProjectFiles];
			IsCheckedNeedCopy = App.ProjectSettingManager.CurrentSetting.IsNeedCopy;
		}
		else
		{
			ProjectFiles = new();
		}
	}
	protected abstract void ListupTargetFiles();
	protected void OnIsCheckedChanged()
	{
		// コピー対象になっているものが一つ以上あったらコピーを許可する
		IsReadyCopy = TargetFiles.Any( info => info.IsCopy );
	}

	protected IEnumerable<TargetFileInformation>? TargetFilesList { get; set; }

	protected CancellationTokenSource? m_cts;
}
