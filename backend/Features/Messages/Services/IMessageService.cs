namespace backend.Features.Messages.Services;

using backend.Features.Messages.Dtos;

public interface IMessageService
{
	Task<List<ConversationDto>> GetConversationsAsync(string auth0Id);

	Task<List<MessageDto>> GetConversationAsync(string auth0Id, Guid otherUserId);

	Task<MessageDto> SendAsync(string auth0Id, SendMessageDto dto);

	Task MarkAsReadAsync(string auth0Id, Guid messageId);
	Task SendSystemMessageAsync(
	Guid receiverUserId,
	string title,
	string body);
}