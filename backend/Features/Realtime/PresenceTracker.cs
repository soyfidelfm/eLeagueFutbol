namespace backend.Features.Realtime;

using System.Collections.Concurrent;

public class PresenceTracker
{
	private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
	private readonly ConcurrentDictionary<Guid, PresenceUserDto> _users = new();

	public bool AddConnection(PresenceUserDto user, string connectionId)
	{
		var connections = _connections.GetOrAdd(user.UserId, _ => new HashSet<string>());
		bool becameOnline;

		lock (connections)
		{
			becameOnline = connections.Count == 0;
			connections.Add(connectionId);
		}

		_users[user.UserId] = user;
		return becameOnline;
	}

	public bool RemoveConnection(Guid userId, string connectionId)
	{
		if (!_connections.TryGetValue(userId, out var connections))
			return false;

		bool becameOffline;

		lock (connections)
		{
			connections.Remove(connectionId);
			becameOffline = connections.Count == 0;
		}

		if (becameOffline)
			_connections.TryRemove(userId, out _);

		return becameOffline;
	}

	public bool IsOnline(Guid userId)
	{
		return _connections.TryGetValue(userId, out var connections) && connections.Count > 0;
	}

	public IReadOnlyList<PresenceUserDto> GetOnlineUsers()
	{
		return _connections.Keys
			.Where(id => _users.ContainsKey(id))
			.Select(id => _users[id])
			.OrderBy(x => x.DisplayName)
			.ToList();
	}
}
