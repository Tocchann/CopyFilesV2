namespace CopyFiles.Core.Const;

public enum CompareStatus
{
	/// <summary>
	/// Unknown - 未調査
	/// </summary>
	Unknown,
	/// <summary>
	/// NoAction - コピーなどの操作は不要
	/// </summary>
	Match,
	NoAction=Match,
	/// <summary>
	/// MatchWithoutSignature - 署名の有無が違うが内容は一致
	/// </summary>
	MatchWithoutSignature,
	/// <summary>
	/// MatchWithoutDate - 日付が異なるが内容は一致
	/// </summary>
	MatchWithoutDate,
	/// <summary>
	/// UnMatch - 不一致
	/// </summary>
	UnMatch,
	/// <summary>
	/// UnMatchSameVersion - バージョンは同じだが内容が異なる
	/// </summary>
	UnMatchSameVersion,
	/// <summary>
	/// NewFile - 新しいファイル
	/// </summary>
	NewFile,
	/// <summary>
	/// NotExistSignature - 署名が存在しない
	/// </summary>
	NotExistSignature,
	/// <summary>
	/// ExistSignature - 署名が存在する
	/// </summary>
	ExistSignature,
}
