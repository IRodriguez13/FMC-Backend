using Fmc.Domain.Constants;
using Fmc.Domain.Entities;

namespace Fmc.Infrastructure.Persistence;

/// <summary>Reseñas demo únicas por cafetería (índices 4+ del catálogo).</summary>
internal static class CabaCatalogSeedReviews
{
    internal sealed record SeedReview(
        Guid ReviewId,
        int CafeIndex,
        int Rating,
        string Text,
        Guid AuthorUserId,
        string? PhotoStorageKey,
        int DaysAgo);

    /// <summary>Una reseña por local desde Belgrano (<see cref="CabaCatalogSeed.Cafes"/> índice 4).</summary>
    internal static readonly SeedReview[] Extra =
    [
        Review(reviewKey: 5, cafeIndex: 4, 4, "Tostado de origen impecable sobre Cabildo. Las medialunas son de las mejores de Belgrano.", ConsumerFreeId, null, 12),
        Review(6, 5, 5, "Ambiente relajado y baristas que saben. Ideal para una pausa en Villa Crespo.", ConsumerPremiumId, "seed-palermo-interior.jpg", 11),
        Review(7, 6, 4, "Cortado perfecto y mesas cómodas. Un clásico de Almagro.", ConsumerFreeId, null, 10),
        Review(8, 7, 5, "Sabor de barrio porteño, sin pretensiones. Buen lugar para leer en Boedo.", ConsumerPremiumId, null, 9),
        Review(9, 8, 5, "Vista al dique y cappuccino de autor. Una joya en Puerto Madero.", ConsumerFreeId, "seed-recoleta-frente.jpg", 8),
        Review(10, 9, 4, "Tranquilo y cerca del río. Muy buen desayuno en Núñez.", ConsumerPremiumId, null, 7),
        Review(11, 10, 5, "Filtrados impecables. Se nota el micro-tostado en Colegiales.", ConsumerFreeId, "seed-caballito-visita.jpg", 6),
        Review(12, 11, 4, "Espacio amplio y buena luz. Funciona bien para trabajar en remoto en Barracas.", ConsumerPremiumId, null, 15),
        Review(13, 12, 4, "Colorido y auténtico. El espresso en La Boca es intenso, como debe ser.", ConsumerFreeId, null, 14),
        Review(14, 13, 5, "Brunch generoso y café de calidad. Vale la pena el viaje a Villa Urquiza.", ConsumerPremiumId, "seed-recoleta-detalle.jpg", 13),
        Review(15, 14, 4, "Precio-calidad excelente en Flores. Muy atentos en mostrador.", ConsumerFreeId, null, 5),
        Review(16, 15, 4, "Silencioso y ordenado. Ideal para concentrarse con el notebook en Parque Patricios.", ConsumerPremiumId, null, 4),
        Review(17, 16, 5, "Rápido pero sin sacrificar sabor. Pastelería muy lograda en Retiro.", ConsumerFreeId, "seed-san-telmo-patio.jpg", 3),
        Review(18, 17, 5, "A dos cuadras del Obelisco. Perfecto para turistas y porteños apurados en Monserrat.", ConsumerPremiumId, null, 2),
        Review(19, 18, 4, "El cortado de referencia sobre Av. Corrientes. Sin vueltas en Balvanera.", ConsumerFreeId, null, 16),
        Review(20, 19, 5, "Terraza amplia y café suave. Muy buena opción en Saavedra.", ConsumerPremiumId, "seed-palermo-barra.jpg", 17),
        Review(21, 20, 5, "Brunch de autor bien servido. Los filtrados en Chacarita cambian cada semana.", ConsumerFreeId, null, 18),
        Review(22, 21, 4, "Pausa express en el centro. Salí con energía en cinco minutos desde San Nicolás.", ConsumerPremiumId, null, 1),
    ];

    private static Guid ConsumerFreeId => DataSeeder.ConsumerFreeId;
    private static Guid ConsumerPremiumId => DataSeeder.ConsumerPremiumId;

    private static SeedReview Review(
        int reviewKey,
        int cafeIndex,
        int rating,
        string text,
        Guid authorUserId,
        string? photoStorageKey,
        int daysAgo) =>
        new(
            Guid.Parse($"d{reviewKey:D7}-1111-4111-8111-111111111101"),
            cafeIndex,
            rating,
            text,
            authorUserId,
            photoStorageKey,
            daysAgo);
}
