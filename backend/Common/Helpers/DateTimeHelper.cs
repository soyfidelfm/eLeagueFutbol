namespace backend.Common.Helpers;

public static class DateTimeHelper
{
	public static DateTime ToUtc(
		DateTime value,
		string? timeZoneId)
	{
		if (value.Kind == DateTimeKind.Utc)
		{
			return value;
		}

		var timeZone = GetTimeZone(timeZoneId);

		var unspecifiedDate =
			DateTime.SpecifyKind(
				value,
				DateTimeKind.Unspecified);

		return TimeZoneInfo.ConvertTimeToUtc(
			unspecifiedDate,
			timeZone);
	}

	public static DateTime? ToUtc(
		DateTime? value,
		string? timeZoneId)
	{
		if (!value.HasValue)
		{
			return null;
		}

		return ToUtc(
			value.Value,
			timeZoneId);
	}

	public static DateTime ToLocal(
		DateTime utcDate,
		string? timeZoneId)
	{
		var timeZone = GetTimeZone(timeZoneId);

		var safeUtcDate =
			DateTime.SpecifyKind(
				utcDate,
				DateTimeKind.Utc);

		return TimeZoneInfo.ConvertTimeFromUtc(
			safeUtcDate,
			timeZone);
	}

	public static DateTime? ToLocal(
		DateTime? utcDate,
		string? timeZoneId)
	{
		if (!utcDate.HasValue)
		{
			return null;
		}

		return ToLocal(
			utcDate.Value,
			timeZoneId);
	}

	public static string FormatLocal(
		DateTime utcDate,
		string? timeZoneId,
		string format = "ddd, h:mm tt")
	{
		var localDate =
			ToLocal(
				utcDate,
				timeZoneId);

		return localDate.ToString(format);
	}

	public static string GetRelativeTime(
		DateTime utcDate)
	{
		var safeUtcDate =
			DateTime.SpecifyKind(
				utcDate,
				DateTimeKind.Utc);

		var diff =
			DateTime.UtcNow - safeUtcDate;

		if (diff.TotalMinutes < 1)
		{
			return "now";
		}

		if (diff.TotalMinutes < 60)
		{
			return $"{(int)diff.TotalMinutes}m ago";
		}

		if (diff.TotalHours < 24)
		{
			return $"{(int)diff.TotalHours}h ago";
		}

		if (diff.TotalDays < 30)
		{
			return $"{(int)diff.TotalDays}d ago";
		}

		if (diff.TotalDays < 365)
		{
			return $"{(int)(diff.TotalDays / 30)}mo ago";
		}

		return $"{(int)(diff.TotalDays / 365)}y ago";
	}

	public static DateTime GetStartOfWeekUtc(
		string? timeZoneId)
	{
		var timeZone =
			GetTimeZone(timeZoneId);

		var nowLocal =
			TimeZoneInfo.ConvertTimeFromUtc(
				DateTime.UtcNow,
				timeZone);

		var weekStartLocal =
			nowLocal.Date.AddDays(
				-(int)nowLocal.DayOfWeek);

		return TimeZoneInfo.ConvertTimeToUtc(
			weekStartLocal,
			timeZone);
	}

	public static string GetCountdown(
		DateTime targetUtcDate,
		string? timeZoneId)
	{
		var targetLocal =
			ToLocal(
				targetUtcDate,
				timeZoneId);

		var nowLocal =
			ToLocal(
				DateTime.UtcNow,
				timeZoneId);

		var diff =
			targetLocal - nowLocal;

		if (diff.TotalSeconds <= 0)
		{
			return "Live Now";
		}

		if (diff.TotalDays >= 1)
		{
			return $"Starts in {(int)diff.TotalDays}d {diff.Hours}h";
		}

		if (diff.TotalHours >= 1)
		{
			return $"Starts in {(int)diff.TotalHours}h {diff.Minutes}m";
		}

		return $"Starts in {diff.Minutes}m";
	}

	public static TimeZoneInfo GetTimeZone(
		string? timeZoneId)
	{
		if (string.IsNullOrWhiteSpace(timeZoneId))
		{
			return TimeZoneInfo.Utc;
		}

		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(
				timeZoneId);
		}
		catch
		{
			return TimeZoneInfo.Utc;
		}
	}
}