namespace backend.Features.Messages.Controllers;

using System.Security.Claims;
using backend.Features.Messages.Dtos;
using backend.Features.Messages.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/messages")]
public class MessagesController : ControllerBase
{
	private readonly IMessageService _messageService;

	public MessagesController(IMessageService messageService)
	{
		_messageService = messageService;
	}

	[HttpGet("conversations")]
	public async Task<ActionResult<List<ConversationDto>>> GetConversations()
	{
		var auth0Id = GetAuth0Id();

		var conversations = await _messageService.GetConversationsAsync(auth0Id);

		return Ok(conversations);
	}

	/// <summary>
	/// GET api/messages/conversation/{userId}?before={messageId}&amp;take=15
	/// Returns the newest page (or the page older than "before"), ordered oldest to newest.
	/// </summary>
	[HttpGet("conversation/{userId:guid}")]
	public async Task<ActionResult<MessagePageDto>> GetConversation(
		Guid userId,
		[FromQuery] Guid? before = null,
		[FromQuery] int take = 15)
	{
		var auth0Id = GetAuth0Id();

		var messages = await _messageService.GetConversationAsync(auth0Id, userId, before, take);

		return Ok(messages);
	}

	[HttpPost]
	public async Task<ActionResult<MessageDto>> Send(SendMessageDto dto)
	{
		var auth0Id = GetAuth0Id();

		var message = await _messageService.SendAsync(auth0Id, dto);

		return Ok(message);
	}

	[HttpPut("{messageId:guid}/read")]
	public async Task<IActionResult> MarkAsRead(Guid messageId)
	{
		var auth0Id = GetAuth0Id();

		await _messageService.MarkAsReadAsync(auth0Id, messageId);

		return NoContent();
	}

	private string GetAuth0Id()
	{
		return User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub")
			?? throw new UnauthorizedAccessException("Auth0 user id not found.");
	}
}