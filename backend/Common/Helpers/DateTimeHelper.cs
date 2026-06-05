namespace backend.Common.Helpers;

public static class DateTimeHelper
{
	public static DateTime ToUtc(DateTime value, string? timeZoneId)
	{
		if (value.Kind == DateTimeKind.Utc)
			return value;

		var timeZone = GetTimeZone(timeZoneId);

		var unspecifiedDate = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

		return TimeZoneInfo.ConvertTimeToUtc(
			unspecifiedDate,
			timeZone
		);
	}

	public static DateTime? ToUtc(DateTime? value, string? timeZoneId)
	{
		if (!value.HasValue)
			return null;

		return ToUtc(value.Value, timeZoneId);
	}

	private static TimeZoneInfo GetTimeZone(string? timeZoneId)
	{
		if (string.IsNullOrWhiteSpace(timeZoneId))
			return TimeZoneInfo.Utc;

		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
		}
		catch
		{
			return TimeZoneInfo.Utc;
		}
	}
}