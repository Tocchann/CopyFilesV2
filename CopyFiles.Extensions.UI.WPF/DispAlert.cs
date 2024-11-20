using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.UI.Abstractions;
using System;
using System.Windows;
using CopyFiles.Extensions.UI.WPF.Interops;

namespace CopyFiles.Extensions.UI.WPF;

public class DispAlert : IDispAlert
{
	/// <summary>
	/// DisplayAlert のキャプションテキスト
	/// </summary>
	public string? Title { get; set; }
	public bool UseTaskDialog { get; set; }
	public DispAlert( ILogger<DispAlert>? logger = default )
	{
		m_logger = logger;
	}
	public IDispAlert.Result Show( string message,
		IDispAlert.Buttons button = IDispAlert.Buttons.OK,
		IDispAlert.Icon icon = IDispAlert.Icon.Exclamation,
		IDispAlert.Result defaultResult = IDispAlert.Result.None,
		IDispAlert.Options options = IDispAlert.Options.None )
	{
		// タイトルが設定されていない場合はメインウィンドウのキャプションを利用する。
		string title = Title ?? string.Empty;
		if( string.IsNullOrEmpty( title ) )
		{
			// メインウィンドウの実体があってかつ表示状態(アイコンでもよい)の場合のみタイトルを取り込む
			if( Application.Current.MainWindow != null &&
				Application.Current.MainWindow.Visibility == Visibility.Visible )
			{
				title = Application.Current.MainWindow.Title;
			}
		}
		// メインウィンドウからタイトルを決められない場合やタイトルがついていない場合はモジュール名を利用する
		if( string.IsNullOrEmpty( title ) )
		{
			title = AppDomain.CurrentDomain.FriendlyName;
		}
		return Show( message, title, button, icon, defaultResult, options );
	}
	public IDispAlert.Result Show( string message, string title,
		IDispAlert.Buttons button = IDispAlert.Buttons.OK,
		IDispAlert.Icon icon = IDispAlert.Icon.Exclamation,
		IDispAlert.Result defaultResult = IDispAlert.Result.None,
		IDispAlert.Options options = IDispAlert.Options.None )
	{
		m_logger?.LogInformation( $"WPF.DispAlert.Show( message: {message}, title: {title}, button: {button}, icon: {icon}, defaultResult: {defaultResult}, options: {options})" );
		if( UseTaskDialog )
		{
			IntPtr ownerWindow = NativeMethods.GetSafeOwnerWindow( Utilities.GetOwnerWindow() );
			var commonButtons = button switch
			{
				IDispAlert.Buttons.OK => NativeMethods.TaskDialogCommonButtonFlags.Ok,
				IDispAlert.Buttons.OKCancel => NativeMethods.TaskDialogCommonButtonFlags.Ok | NativeMethods.TaskDialogCommonButtonFlags.Cancel,
				IDispAlert.Buttons.YesNo => NativeMethods.TaskDialogCommonButtonFlags.Yes | NativeMethods.TaskDialogCommonButtonFlags.No,
				IDispAlert.Buttons.YesNoCancel => NativeMethods.TaskDialogCommonButtonFlags.Yes | NativeMethods.TaskDialogCommonButtonFlags.No | NativeMethods.TaskDialogCommonButtonFlags.Cancel,
				_ => throw new NotImplementedException(),
			};
			var nativeIcon = icon switch
			{
				IDispAlert.Icon.None => IntPtr.Zero,
				IDispAlert.Icon.Error => NativeMethods.MAKEINTRESOURCE( -2 ), // == TD_ERROR_ICON
				IDispAlert.Icon.Question => NativeMethods.MAKEINTRESOURCE( 32514 ), // == IDI_QUESTION
				IDispAlert.Icon.Exclamation => NativeMethods.MAKEINTRESOURCE( -1 ), // == TD_WARNING_ICON
				IDispAlert.Icon.Asterisk => NativeMethods.MAKEINTRESOURCE( -3 ), // TD_INFORMATION_ICON
				_ => throw new NotImplementedException(),
			};
			// アイコンリソースはシステムリソースしか使わないのでインスタンスはいらない
			NativeMethods.TaskDialog( ownerWindow, IntPtr.Zero, title, string.Empty, message, commonButtons, nativeIcon, out var result );
			// Windows OS の IDOK などをそれぞれでenum化しているだけなので、キャストで済ませられる(変換処理はいらない)
			return (IDispAlert.Result)result;
		}
		else
		{
			// ここはタイトルが空でも無視して利用する
			var result = Application.Current.MainWindow != null
				? MessageBox.Show( Application.Current.MainWindow, message, title, (MessageBoxButton)button, (MessageBoxImage)icon, (MessageBoxResult)defaultResult, (MessageBoxOptions)options )
				: MessageBox.Show( message, title, (MessageBoxButton)button, (MessageBoxImage)icon, (MessageBoxResult)defaultResult, (MessageBoxOptions)options );

			return (IDispAlert.Result)result;
		}
	}
	private ILogger<DispAlert>? m_logger;
}
