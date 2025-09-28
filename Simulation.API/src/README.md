# GameAPI (GameWeb) - Overview

## Character Flow (CQRS + Active Character)

- Create character: `POST /api/characters` (auto-select if user has none).
- Select character: `POST /api/characters/{id}/select` (deselects others).
- Delete (deactivate) character: `DELETE /api/characters/{id}` (marks inactive + deselects).
- List my characters: `GET /api/characters/me` (selected comes first).
- Get by id: `GET /api/characters/{id}`.

## Active Character Validation
Requests can require a selected character by decorating their request class with `[CharacterRequired]`. The `CharacterValidationBehaviour`:
1. Checks for the attribute.
2. Loads selected character for current user (`IsSelected && IsActive`).
3. Throws `NoCharacterSelectedException` (HTTP 409) if not present.
4. Injects claim `character_id` for downstream handlers.

## Domain Events
`Character` emits:
- `CharacterCreatedEvent` on creation.
- `CharacterLoggedEvent` / `CharacterLogoutEvent` on select/deselect.
- `CharacterActivatedEvent` / `CharacterDeactivatedEvent` on state changes.

## Markers
- `ICommand<T>`, `ICommand`, `IQuery<T>` separate write/read.
- `UnitOfWorkBehavior` only commits for commands.

## TODO (Potential Future Enhancements)
- Replace dynamic claim mutation with a dedicated `ICurrentCharacterContext` service.
- Add limit of characters per user.
- Add caching for read queries.
- Implement real tests using an EF Core InMemory/Sqlite setup.

## Running Migrations
(Generate after adding new columns or flags.)

## Testing
Placeholder test files under `Tests/Characters/` indicate recommended coverage.
