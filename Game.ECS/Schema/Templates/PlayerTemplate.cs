namespace Game.ECS.Schema.Templates;

public record PlayerTemplate(
    int Id,
    IdentityTemplate IdentityTemplate,
    LocationTemplate LocationTemplate,
    DirectionTemplate DirectionTemplate,
    VitalsTemplate VitalsTemplate,
    StatsTemplate StatsTemplate
);