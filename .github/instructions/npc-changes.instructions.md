# Análise Completa: Sistema de Ataque Básico por Vocação

## 📋 Resumo do Problema

Atualmente, o sistema de ataque básico trata **todos os ataques como corpo a corpo (melee)**, independentemente da vocação. A lógica no `DamageSystem.ProcessAttackDamage` busca um alvo apenas na **célula adjacente** (posição + facing), o que não funciona para vocações que deveriam atacar à distância.

## 🔍 Análise do Código Atual

### 1. **Vocações Disponíveis** (`Game.Domain/Enums/VocationType.cs`)
```csharp
public enum VocationType : byte
{
    Unknown = 0,
    Warrior = 1,  // Deveria ser Melee
    Archer = 2,   // Deveria ser Ranged
    Mage = 3      // Deveria ser Ranged (mágico)
}
```

### 2. **Tipos de Ataque** (`Game.ECS/Components/Combat.cs`)
```csharp
public enum AttackType : byte
{
    Basic = 0,    // Ataque básico (atualmente sempre melee)
    Heavy = 1,    // Ataque carregado
    Critical = 2, // Golpe crítico
    Magic = 3,    // Ataque mágico
}
```

### 3. **Range de Ataque** (`Game.ECS/Logic/Combat/CombatLogic.cs`)
O range é definido por **tipo de ataque**, não por vocação:
```csharp
private static int GetAttackRange(AttackType type) => type switch
{
    AttackType.Basic    => 1,   // ⚠️ Sempre melee!
    AttackType.Heavy    => 1,
    AttackType.Critical => 1,
    AttackType.Magic    => 10,  // Apenas Magic é ranged
    _ => 1
};
```

### 4. **Problema Principal: `DamageSystem.ProcessAttackDamage`**
O dano é aplicado sempre na célula adjacente:
```csharp
SpatialPosition targetSpatialPosition = new(
    position.X + facing.DirectionX,  // ⚠️ Só verifica 1 célula na direção
    position.Y + facing.DirectionY, 
    floor.Level);
```

### 5. **CombatSystem: Tipo de ataque é sempre `Basic`**
```csharp
const AttackType attackType = AttackType.Basic; // ⚠️ Hardcoded!
```

---

## 🎯 Especificação da Solução

### Comportamento Desejado por Vocação

| Vocação | Tipo de Ataque Básico | Range | Tipo de Dano | Comportamento |
|---------|----------------------|-------|--------------|---------------|
| **Warrior** | Melee | 1-2 tiles | Físico | Ataca célula adjacente |
| **Archer** | Ranged (Projétil) | 5-8 tiles | Físico | Dispara projétil em linha reta |
| **Mage** | Ranged (Magia) | 6-10 tiles | Mágico | Lança projétil mágico |

---

## 📝 Solicitação para Especialista

### **Título: Implementar Sistema de Ataque Básico Baseado em Vocação (Melee vs Ranged)**

### **Contexto**
O sistema de combate atual não diferencia ataques por vocação. Todas as entidades (NPCs e Players) usam `AttackType.Basic` que é sempre tratado como melee (1 tile de range). Precisamos que:

1. **Warriors** mantenham ataque corpo a corpo
2. **Archers** tenham ataque à distância com projétil físico
3. **Mages** tenham ataque à distância com projétil mágico

### **Arquivos que precisam ser modificados**

#### **Game.ECS (Shared Logic)**

1. **`Game.ECS/Components/Combat.cs`**
   - Adicionar componente `Vocation` (se não existir como ECS component)
   - Considerar novo enum `AttackStyle { Melee, Ranged, Magic }`
   - Possivelmente adicionar componente `Projectile` para ataques à distância

2. **`Game.ECS/Logic/Combat/CombatLogic.cs`**
   - Criar método `GetAttackStyleForVocation(VocationType vocation)`
   - Modificar `GetAttackRange` para considerar vocação
   - Criar lógica `GetBasicAttackTypeForVocation(VocationType) -> AttackType`

3. **Novo arquivo: `Game.ECS/Components/Projectile.cs`** (sugestão)
   ```csharp
   public struct Projectile
   {
       public Entity Source;           // Quem disparou
       public Entity Target;           // Alvo (opcional - pode ser direction-based)
       public Position TargetPosition; // Posição alvo
       public float Speed;             // Velocidade do projétil
       public int Damage;              // Dano a aplicar
       public bool IsMagical;          // Tipo de dano
       public float RemainingLifetime; // TTL do projétil
   }
   ```

#### **Game.Server (Server Systems)**

4. **`Game.Server/ECS/Systems/CombatSystem.cs`**
   - Modificar `ProcessAttack` para determinar `AttackType` baseado na vocação da entidade
   - Precisa acessar componente de vocação da entidade

5. **`Game.Server/ECS/Systems/DamageSystem.cs`**
   - Modificar `ProcessAttackDamage` para:
     - **Melee**: Manter lógica atual (célula adjacente)
     - **Ranged**: Criar projétil em vez de aplicar dano instantâneo
   - Novo método: `ProcessProjectileDamage` para projéteis

6. **Novo arquivo: `Game.Server/ECS/Systems/ProjectileSystem.cs`** (sugestão)
   - Movimenta projéteis em direção ao alvo
   - Verifica colisão com entidades/terreno
   - Aplica dano quando atinge alvo
   - Remove projétil após impacto ou timeout

7. **`Game.Server/ECS/Systems/NpcCombatSystem.cs`**
   - Atualizar para considerar range baseado em vocação
   - NPCs ranged devem manter distância do alvo

#### **Simulation.Client (Visual Feedback)**

8. **`Simulation.Client/godot-client/Scripts/`**
   - Adicionar visual de projétil
   - Sincronizar spawn/movimento de projéteis

### **Critérios de Aceitação**

- [ ] Warrior (Player/NPC) ataca apenas em melee (1-2 tiles)
- [ ] Archer (Player/NPC) dispara projétil físico (5-8 tiles de range)
- [ ] Mage (Player/NPC) dispara projétil mágico (6-10 tiles de range)
- [ ] Projéteis têm representação visual no cliente
- [ ] NPCs ranged tentam manter distância do alvo
- [ ] Sistema funciona tanto para `PlayerControlled` quanto `AIControlled`
- [ ] Dano físico usa `AttackPower.Physical` e `Defense.Physical`
- [ ] Dano mágico usa `AttackPower.Magical` e `Defense.Magical`
- [ ] Projéteis podem ser bloqueados por obstáculos (opcional)

### **Dependências Identificadas**

1. Entidades precisam ter componente de vocação acessível no ECS
2. Verificar se `NpcBehaviorData` já contém vocação ou precisa ser adicionado
3. O `NpcSpawnService` já define vocação para NPCs (ex: Orc = Warrior, Goblin = Archer)

### **Prioridade Sugerida**

1. ⭐ Primeiro: Lógica de determinação de `AttackType` por vocação
2. ⭐ Segundo: Sistema de projéteis para ataques ranged
3. ⭐ Terceiro: Ajuste da IA de NPCs ranged para manter distância
4. ⭐ Quarto: Feedback visual no cliente