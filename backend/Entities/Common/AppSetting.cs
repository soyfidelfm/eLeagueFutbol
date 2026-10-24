namespace backend.Entities.Common;

/// <summary>League-wide key/value setting managed by admins (e.g. default draft pick seconds).</summary>
public class AppSetting
{
	public string Key { get; set; } = string.Empty;

	public string Value { get; set; } = string.Empty;

	public DateTime UpdatedAtUTC { get; set; }
}
