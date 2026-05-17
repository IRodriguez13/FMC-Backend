using Fmc.Domain.Entities;

namespace Fmc.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateConsumerToken(Guid userId, string email, ConsumerTier tier);
    string CreateEnterpriseToken(Guid enterpriseUserId, string email, Guid cafeteriaId, EnterpriseSubscriptionTier subscriptionTier);
}
