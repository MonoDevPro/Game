using Arch.Core;
using Game.ECS.Schema.Components;
using Game.ECS.Services;
using Game.ECS.Services.Map;

namespace Game.ECS.Logic;

public static class MovementLogic
{
    

    /// <summary>
    /// Calcula o novo position e avalia se o movimento é permitido.
    /// Não realiza side-effects.
    /// </summary>
    
    
    public static (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }
}