using Artemis_Banking_Pro.Core.Application.DTOs.Messages;

namespace Artemis_Banking_Pro.Core.Application.Contracts.EmailSerives
{
    public interface IEmailServices
    {
        Task<bool> SendNotification(MessageDto message);
    }
}
