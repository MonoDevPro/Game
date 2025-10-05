using Arch.LowLevel;
using Game.Domain.Entities;

namespace Game.ECS.Components;

// Outros componentes gerenciados que não podem ser structs
public struct Name { public Handle<string> Value; }
public struct Description { public Handle<string> Value; }
public struct Inventory { public List<Handle<Item>> Items; }