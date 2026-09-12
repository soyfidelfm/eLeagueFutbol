using backend.Data;
using backend.Entities.Clubs;
using backend.Entities.Draft;
using backend.Entities.Enums;
using backend.Entities.Transfers;
using backend.Features.Draft.Dtos;
using backend.Features.Messages.Services;
using backend.Features.Realtime;
using Microsoft.EntityFrameworkCore;

namespace backend.Features.Draft.Services;

public class DraftService : IDraftService
{
	private readonly AppDbContext _context;
	private readonly IMessageService _messageService;
	private readonly IAppRealtimeService _realtime;

	private const int PickSeconds = 300;

	public DraftService(
		AppDbContext context,
		IMessageService messageService,
		IAppRealtimeService realtime)
	{
		_context = context;
		_messageService = messageService;
		_realtime = realtime;
	}
}
