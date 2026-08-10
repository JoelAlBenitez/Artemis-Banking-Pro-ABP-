namespace Artemis_Banking_Pro.Core.Application.DTOs.Messages
{
    public sealed class MessageDto
    {
        public required string To { get; set; }
        public required string Subject { get; set; }
        public required string Message { get; set; }

    }
}
