namespace Fmc.Domain.Entities;

/// <summary>
/// Suscripción de la cuenta Enterprise: Premium recibe mayor ponderación en listados;
/// Standard aparece sin boost artificial (solo orden por distancia).
/// </summary>
public enum EnterpriseSubscriptionTier
{
    Standard = 0,
    Premium = 1,
}
