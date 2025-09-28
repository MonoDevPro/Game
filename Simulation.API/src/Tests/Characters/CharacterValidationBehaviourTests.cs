using GameWeb.Application.Common.Behaviours;
using GameWeb.Application.Common.Security;
using Moq;

namespace GameWeb.Tests.Characters;

public class CharacterValidationBehaviourTests
{
    private sealed class DummyRequestWithCharacter : IRequest<Unit> { }

    [CharacterRequired]
    private sealed class DummyCharacterRequest : IRequest<Unit> { }

    [Fact(Skip="Placeholder - implement real test with in-memory context")]
    public async Task Should_Throw_When_No_Selected_Character()
    {
        // Arrange
        // Act
        // Assert
    }
}
