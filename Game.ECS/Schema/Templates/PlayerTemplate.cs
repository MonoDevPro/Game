namespace Game.ECS.Schema.Templates;

public record PlayerTemplate(
    int Id,
    IdentityTemplate IdentityTemplate,
    DirectionTemplate DirectionTemplate,
    VitalsTemplate VitalsTemplate,
    StatsTemplate StatsTemplate
);