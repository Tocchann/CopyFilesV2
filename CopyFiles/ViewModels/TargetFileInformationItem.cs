using CommunityToolkit.Mvvm.ComponentModel;
using CopyFiles.Core.Const;
using CopyFiles.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Cryptography;

namespace CopyFiles.ViewModels;

public partial class TargetFileInformationItem : ObservableObject
{
	[ObservableProperty]
	bool isCopy;

	partial void OnIsCopyChanged( bool value )
	{
		// 状態が変わったら、処理するかどうかを再判定する
		m_onIsCheckedChanged?.Invoke();
	}
	[ObservableProperty]
	bool isSelected;

	[ObservableProperty]
	CompareStatus status;

	[ObservableProperty]
	string sourceFilePath;

	[ObservableProperty]
	string sourceFileVersion;

	[ObservableProperty]
	string sourceLastWriteTime;

	[ObservableProperty]
	string destinationFilePath;

	[ObservableProperty]
	string destinationFileVersion;

	[ObservableProperty]
	string destinationLastWriteTime;

	public void UpdateStatus( bool updateAllData, bool copyMode )
	{
		IsCopy = TargetFileInformation.NeedCopy;
		Status = TargetFileInformation.CompareStatus;
		// 日付とバージョンはコピーなどを行っていると変わるので、要求に応じて更新する
		if( updateAllData )
		{
			if( copyMode )
			{
				SourceFileVersion = TargetFileInformation.ReferFileInfo.FileVersion?.ToString() ?? string.Empty;
				SourceLastWriteTime = TargetFileInformation.ReferFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
				DestinationFileVersion = TargetFileInformation.BaseFileInfo.FileVersion?.ToString() ?? string.Empty;
				DestinationLastWriteTime = TargetFileInformation.BaseFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
			}
			else
			{
				SourceFileVersion = TargetFileInformation.BaseFileInfo.FileVersion?.ToString() ?? string.Empty;
				SourceLastWriteTime = TargetFileInformation.BaseFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
			}
		}
	}

	public TargetFileInformation TargetFileInformation { get; init; }

	public delegate void OnIsCheckedChanged();

	private OnIsCheckedChanged m_onIsCheckedChanged;
	public TargetFileInformationItem( List<ReferFolder> copySettings, TargetFileInformation targetInfo, OnIsCheckedChanged onIsCheckedChanged )
	{
		TargetFileInformation = targetInfo;
		SourceFilePath = targetInfo.ReferFileInfo.FilePath;
		var referFolder = copySettings.First( info => SourceFilePath.StartsWith( info.ReferenceFolder ) );
		SourceFilePath = Path.GetRelativePath( referFolder.ReferenceFolder, SourceFilePath );
		DestinationFilePath = Path.GetRelativePath( referFolder.BaseFolder, targetInfo.BaseFileInfo.FilePath );
		SourceFileVersion = string.Empty;
		SourceLastWriteTime = string.Empty;
		DestinationFileVersion = string.Empty;
		DestinationLastWriteTime = string.Empty;
		IsSelected = false;
		UpdateStatus( true, true );
		m_onIsCheckedChanged = onIsCheckedChanged;
	}
	public TargetFileInformationItem( ReferFolder setting, TargetFileInformation targetInfo, OnIsCheckedChanged onIsCheckedChanged )
	{
		TargetFileInformation = targetInfo;
		SourceFilePath = Path.GetRelativePath( setting.BaseFolder, targetInfo.BaseFileInfo.FilePath );
		DestinationFilePath = targetInfo.ReferFileInfo.FilePath;
		SourceFileVersion = string.Empty;
		SourceLastWriteTime = string.Empty;
		DestinationFileVersion = string.Empty;
		DestinationLastWriteTime = string.Empty;
		IsSelected = false;
		UpdateStatus( true, false );
		m_onIsCheckedChanged = onIsCheckedChanged;
	}
}
