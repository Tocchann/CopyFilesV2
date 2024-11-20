using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace CopyFiles.Extensions.UI.WPF.Interops;

internal static partial class NativeMethods
{
	static uint FACILITY_WIN32 = 7;
	// HRESULT_FROM_WIN32用エラーコードの再定義。必要に応じて定義を追加すること
	internal enum Win32Error : int
	{
		Success,
		Cancelled = 1223,
	}

	//	Shell サポート(例外処理をしたくないので、HRESULT を受け取る)
	[DllImport( "shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true )]
	internal static extern int SHCreateItemFromParsingName(
		[In][MarshalAs( UnmanagedType.LPWStr )] string pszPath,
		[In] IntPtr pbc,
		[In][MarshalAs( UnmanagedType.LPStruct )] Guid riid,
		[Out][MarshalAs( UnmanagedType.Interface, IidParameterIndex = 2 )] out IShellItem ppv );
	//	HRESULT サポート
	internal static bool SUCCEEDED( int result ) => result >= 0;
	internal static bool FAILED( int result ) => result < 0;
	internal static int HRESULT_FROM_WIN32( int result ) =>
		result <= 0 ? result : (int)(0x80000000 | (int)(result & 0xFFFF) | (FACILITY_WIN32 << 16));
	internal static int HRESULT_FROM_WIN32( Win32Error result ) =>
		(int)(0x80000000 | ((int)result & 0xFFFF) | (FACILITY_WIN32 << 16));

}
