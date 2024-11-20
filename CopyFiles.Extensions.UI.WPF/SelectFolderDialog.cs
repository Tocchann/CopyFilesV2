using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.UI.Abstractions;
using CopyFiles.Extensions.UI.WPF.Interops;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Collections;
using System.Diagnostics;

namespace CopyFiles.Extensions.UI.WPF;

public class SelectFolderDialog( ILogger<SelectFolderDialog> m_logger ) : ISelectFolderDialog
{
	public string? InitialFolder { get; set; }
	public string? SelectedPath {get; set; }
	public string? Title { get; set; }

	public void AddPlace( string folder, ISelectFolderDialog.FDAP fdap )
	{
		m_places.Add( (folder, fdap) );
	}
	public bool? ShowDialog()
	{
		var ownerWindow = Utilities.GetOwnerWindow();
		return ShowDialog( NativeMethods.GetSafeOwnerWindow( ownerWindow ) );
	}
	public int LastError { get; private set; }

	private bool? ShowDialog( IntPtr ownerWindow )
	{
		FileOpenDialog? coclass = null;
		try
		{
			coclass = new FileOpenDialog();
			if( coclass is IFileOpenDialog dlg ) // そもそもここでキャストに失敗することはない(失敗するなら実装バグ)
			{
				dlg.SetOptions( IFileOpenDialog.FOS.FORCEFILESYSTEM | IFileOpenDialog.FOS.PICKFOLDERS );
				//	以前選択されていたフォルダを指定
				bool setFolder = false;
				var item = CreateItem( SelectedPath );
				if( item is not null )
				{
					dlg.SetFolder( item );
					Marshal.ReleaseComObject( item );
					setFolder = true;
				}
				//	まだフォルダを設定していない場合は初期フォルダを設定する
				if( !setFolder )
				{
					item = CreateItem( InitialFolder );
					if( item is not null )
					{
						dlg.SetFolder( item );
						Marshal.ReleaseComObject( item );
					}
				}
				//	タイトル
				if( !string.IsNullOrWhiteSpace( Title ) )
				{
					dlg.SetTitle( Title );
				}
				//	ショートカット追加
				foreach( var place in m_places )
				{
					item = CreateItem( place.folder );
					if( item is not null )
					{
						dlg.AddPlace( item, place.fdap );
						Marshal.ReleaseComObject( item );
					}
				}
				//	ダイアログを表示
				var hRes = dlg.Show( ownerWindow );
				if( NativeMethods.SUCCEEDED( hRes ) )
				{
					item = dlg.GetResult();
					SelectedPath = item.GetName( IShellItem.SIGDN.FILESYSPATH );
					Marshal.ReleaseComObject( item );
					return true;
				}
				// キャンセル以外のエラーはエラーコードを伝搬して、nullリターンする
				else if( hRes == NativeMethods.HRESULT_FROM_WIN32( NativeMethods.Win32Error.Cancelled ) )
				{
					return false;
				}
				LastError = hRes;
				return null;
			}
			else
			{
				throw new InvalidCastException( "IFileOpenDialog へのキャストに失敗しました。" );
			}
		}
		catch( Exception ex )
		{
			m_logger.LogError( ex, "SelectFolderDialog.ShowDialog() で例外が発生しました。" );
			return null;
		}
		finally
		{
			if( coclass != null )
			{
				// インターフェースをQueryしているのでカウントが２になってるかもしれないので全解除
				Marshal.FinalReleaseComObject( coclass );
			}
		}
	}
	/// <summary>
	/// SHCreateItemFromParseName() のラッパー。
	/// ファイルパスから、IShellItem を作成する専用メソッドとして用意。複数で参照するようなら外だしする(今のところ予定なし)
	/// </summary>
	private static IShellItem? CreateItem( string? folder )
	{
		if( !string.IsNullOrWhiteSpace( folder ) &&
			NativeMethods.SUCCEEDED(
				NativeMethods.SHCreateItemFromParsingName( folder,
					IntPtr.Zero, typeof( IShellItem ).GUID, out var item ) ) )
		{
			return item;
		}
		return null;
	}
	private List<(string folder, ISelectFolderDialog.FDAP fdap)> m_places = new();
}

