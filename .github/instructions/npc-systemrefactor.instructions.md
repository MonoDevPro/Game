Com base nos arquivos analisados (`NpcMovementSystem.cs`, `NpcData.cs`, `NpcFactory.cs`), o diagnóstico é claro: **o sistema atual sofre de "Lógica God Class" e falta de separação entre "Cérebro" (Decisão) e "Corpo" (Execução).**

O `NpcMovementSystem` está fazendo **três** coisas ao mesmo tempo:
1.  Gerencia Máquina de Estados (Idle -> Chase -> Attack).
2.  Calcula Pathfinding (A* / Waypoints).
3.  Aplica Física/Input (Move para X, Y).

Isso gera alto acoplamento e boilerplate. Se você quiser adicionar um comportamento novo (ex: "Fugir"), terá que mexer na lógica de movimento, o que é perigoso.

Aqui está a proposta de refatoração técnica focada em **Componentes Enxutos** e **Sistemas Especializados**.

---

### 1. Refatoração dos Componentes (Pattern Flyweight)

Atualmente, cada NPC carrega uma cópia de dados estáticos (`VisionRange`, `AttackRange`, `PatrolRadius`) dentro do componente `NpcBehavior`. Se você tiver 1.000 Orcs, são 1.000 cópias de dados que nunca mudam.

**Solução:** Separe o **Estado (Dinâmico)** da **Configuração (Estática)**.

#### Componentes Novos / Alterados

```csharp name=Game.ECS/Components/NpcComponents.cs
// 1. Identificador do Tipo (Leve) - Substitui dados hardcoded na entidade
public struct NpcType
{
    public string TemplateId; // "orc_warrior"
    // O System usará isso para buscar ranges e configs em um Singleton/Cache
}

// 2. Estado Mental (O "Cérebro" Dinâmico)
public struct NpcBrain
{
    public NpcState CurrentState;
    public float StateTimer;
    public Entity CurrentTarget; // Substitui NpcTarget complexo
}

public enum NpcState : byte
{
    Idle,
    Patrol,
    Chase,
    Combat,
    ReturnHome
}

// 3. Intenção de Navegação (Desacopla "Querer ir" de "Como ir")
// O BehaviorSystem define o Destination, o MovementSystem decide como chegar lá.
public struct NavigationAgent
{
    public Position? Destination; 
    public float StoppingDistance;
    public bool IsPathPending; // Flag para pedir recalculo de path
}
```

---

### 2. Refatoração dos Sistemas (Pipeline de IA)

Vamos quebrar o `NpcMovementSystem` em um **Pipeline de 3 Estágios**. Isso elimina o boilerplate de queries gigantes e torna o código extremamente coeso.

#### Estágio 1: NpcDecisionSystem (O Cérebro)
Responsável **apenas** por decidir o estado e definir o alvo. Não calcula rotas nem move o boneco.

```csharp name=Game.Server/ECS/Systems/NpcDecisionSystem.cs
[Query]
// Note a query muito mais limpa
[All<NpcBrain, Position, NpcType>] 
public void UpdateBehavior(ref NpcBrain brain, ref NavigationAgent nav, in Position pos, in NpcType type)
{
    // Busca configuração estática (Flyweight) via Singleton ou Serviço injetado
    var config = _npcConfigService.Get(type.TemplateId);

    // Máquina de Estados Simplificada
    switch (brain.CurrentState)
    {
        case NpcState.Idle:
            if (SearchForTarget(pos, config.VisionRange, out var target))
            {
                brain.CurrentTarget = target;
                brain.CurrentState = NpcState.Chase;
            }
            break;

        case NpcState.Chase:
            // Decisão Pura: "Onde eu quero estar?"
            // Não calcula pathfinding aqui, apenas define o destino final.
            if (TryGetComponent(brain.CurrentTarget, out Position targetPos))
            {
                nav.Destination = targetPos;
                nav.StoppingDistance = config.AttackRange;
            }
            else
            {
                brain.CurrentState = NpcState.ReturnHome;
            }
            break;
    }
}
```

#### Estágio 2: NpcNavigationSystem (O Navegador)
Responsável por converter uma `Destination` em `Waypoints` (Pathfinding). Ele só roda se o NPC tiver um destino e precisar de um caminho.

```csharp name=Game.Server/ECS/Systems/NpcNavigationSystem.cs
[Query]
[All<NavigationAgent, NpcPath, Position>]
public void UpdatePathfinding(ref NavigationAgent nav, ref NpcPath path, in Position pos)
{
    // Se não tem destino, limpa caminho
    if (nav.Destination == null)
    {
        if (path.HasPath) path.Clear();
        return;
    }

    // Boilerplate de recalculo reduzido:
    // Só recalcula se o alvo se moveu significativamente ou se a flag está ativa
    if (ShouldRecalculatePath(pos, nav.Destination.Value, path))
    {
        var newPath = _pathfinder.FindPath(pos, nav.Destination.Value);
        path.Update(newPath);
    }
}
```

#### Estágio 3: NpcMotorSystem (O Executor)
Antigo `NpcMovementSystem`, mas agora "burro". Ele não sabe se está caçando ou patrulhando. Ele apenas segue a linha traçada pelo navegador.

```csharp name=Game.Server/ECS/Systems/NpcMotorSystem.cs
[Query]
[All<NpcPath, Input, Position, NavigationAgent>]
public void DriveMotor(ref Input input, ref NpcPath path, in Position pos, in NavigationAgent nav)
{
    // Se já estamos perto o suficiente (ex: para atacar), pare.
    if (nav.Destination.HasValue && Distance(pos, nav.Destination.Value) <= nav.StoppingDistance)
    {
        input.InputX = 0;
        input.InputY = 0;
        return;
    }

    // Lógica pura de seguir waypoints (Steering)
    if (path.TryGetNextWaypoint(pos, out var waypoint))
    {
        (input.InputX, input.InputY) = GetDirection(pos, waypoint);
        // Marca input como sujo para rede
    }
}
```

---

### Resumo dos Ganhos com essa Refatoração

1.  **Redução de Boilerplate nas Queries:**
    *   **Antes:** `DriveMovement(ref Input, in Position, in AIState, in Target, in Patrol, in Behavior, in Info, ref Path, ref Dirty)`
    *   **Depois:** Cada sistema pede apenas 3 ou 4 componentes específicos.

2.  **Desacoplamento (Coesão):**
    *   Se o NPC parar de andar, você olha o `MotorSystem`.
    *   Se o NPC ficar "burro" e não trocar de alvo, você olha o `DecisionSystem`.
    *   Se o NPC andar através de paredes, você olha o `NavigationSystem`.

3.  **Performance de Memória:**
    *   Ao remover `NpcBehavior` (dados estáticos) das entidades e usar `NpcType` + Lookup, você economiza dezenas de bytes por entidade, o que melhora o **Cache Miss** da CPU ao iterar sobre milhares de NPCs.

4.  **Extensibilidade:**
    *   Para criar um Boss que usa habilidades especiais, você só precisa criar um `BossDecisionSystem` que altera o `NavigationAgent`. O sistema de navegação e motor continuam funcionando sem nenhuma alteração.