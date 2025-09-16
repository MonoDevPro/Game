# Melhorias Arquiteturais Propostas e Implementadas

## Objetivos
Reduzir boilerplate, aumentar coesão entre fluxo Client/Server, preparar terreno para features futuras (snapshotting, predição, interesse espacial) mantendo simplicidade operacional.

## Problemas Observados
1. Duplicação de lógica de registro de sistemas (AddSystem repetido em Client/Server builders).
2. Mistura de responsabilidades no SimulationBuilder (configuração de DI + construção de pipeline + reflexão de handlers de rede).
3. Ausência de um ponto declarativo único para a ordem dos sistemas (ordem espalhada por múltiplas chamadas AddSystem).
4. Sync genérico funcionando bem, mas sem mecanismo de batelada ou compressão futura (extensível fácil com estágios). 
5. Staging/Index/Factory coesos, porém Save/Destroy poderiam ser agrupados em uma fase de Finalização.
6. NetworkManager inicializado via NetworkSystem (bom), mas ausência de métricas básicas (latência, throughput) expostas para diagnóstico.
7. Falta de abstração para pipelines multi-fase (ex: PrePhysics, Simulation, PostSimulation, Finalize) caso crescimento continue.

## Melhorias Aplicadas (Low-Risk)
- Adicionado `SystemPipelineBuilder<T>` para permitir composição fluente e reduzir repetição.
- Refatorados `ServerSimulationBuilder` e `ClientSimulationBuilder` para usar a nova API, mantendo ordem explícita e legível.
- Pequenos ajustes de nullability/ref safety no `ClientSimulationBuilder`.
- Implementado sistema de estágios com `SystemStage` + atributo `PipelineSystemAttribute` e registrador automático `PipelineAutoRegistrar`.

### Novo Fluxo de Registro Automático
Cada sistema recebe um atributo `[PipelineSystem(SystemStage.X, offset)]`. O `ServerSimulationBuilder` e `ClientSimulationBuilder` agora chamam:
```
// Server
pipeline.RegisterAttributedSystems<float>(serviceProvider, isServer:true);
// Client
pipeline.RegisterAttributedSystems<float>(serviceProvider, isServer:false);
```
O atributo aceita flags `server` e `client` (ambos true por padrão) para controlar em qual lado o sistema é incluído.
Isso varre o assembly, ordena por `Stage` e depois por `OrderOffset`, adicionando os sistemas ao `Group` sem chamadas manuais dispersas. Sistemas gerados dinamicamente (ex: `GenericSyncSystem<T>`) continuam adicionados manualmente após o registro base.

Ordem atual (por estágio):
1. Net: `NetworkSystem`
2. Staging: `StagingProcessorSystem`
3. Logic: `EntityIndexSystem` (offset -50), `PredictionSystem` (cliente, -10), `EntityFactorySystem`
4. Movement: `MovementSystem`
5. Spatial: `SpatialIndexSystem`
6. Save: `EntitySaveSystem`
7. Destruction: `EntityDestructorSystem`
8. Rendering (cliente): `RenderSystem`
 9. Post: `SnapshotSystem` (servidor)

### Regras de Dependência Implementadas
- `EntityIndexSystem` antes de `MovementSystem` (garante índices prontos para queries de movimento/interesse).
- `NetworkSystem` antes de `StagingProcessorSystem` (garante ingestão de pacotes antes de processar staging).
- `SpatialIndexSystem` antes de `EntitySaveSystem` (futuro: salvar dados dependentes de posição consolidada).

### Placeholders Novos
- `PredictionSystem`: posicionado cedo na fase lógica para aplicar predição antes de movimento real.
- `SnapshotSystem`: executa em `Post` para futura geração de snapshots.

Benefícios imediatos: remoção de duplicação, alteração de ordem centralizada (apenas ajustar atributos), menor risco de esquecer adicionar um sistema em um lado (client/server).

## Recomendações Próximas (Mid-Term)
1. Pipeline Faseada:
   - Introduzir enum SystemStage { PreNet, Net, Staging, Logic, Movement, Spatial, Save, Destruction, Rendering }.
   - Fornecer `AddSystem<T>(SystemStage stage)` e ordenar automaticamente.
2. Sistema de Interesse Espacial (Interest Management):
   - Integrar com `QuadTreeSpatial` para filtrar envio de componentes apenas a peers relevantes.
   - Estratégia: por jogador, obter vizinhos num raio e enviar apenas deltas desses.
3. Canal de Delta Batching:
   - Criar `ChangeAccumulator<T>` para agrupar alterações de vários ticks em um pacote só quando payload total < MTU.
4. Predição de Movimento no Cliente:
   - Introduzir `PredictedPosition` e reconciliar usando `LastAuthoritativeTick` + correção suave.
5. Modularização da Sincronização:
   - Extrair `GenericSyncSystem<T>` em dois papéis: Collector (server) e Applier (client) permitindo estratégias distintas (Tick, LOD, OnDistance)
6. Métricas e Observabilidade:
   - Interface `INetworkMetrics` preenchida no `NetworkListener` (latência média, bytes enviados, pacotes dropados).
   - Opcional: exportar via EventCounters ou Prometheus.
7. Simplificação do Staging:
   - Unificar `IPlayerStagingArea` e `IMapStagingArea` em `IWorldStaging` com sub-filas nomeadas.
8. Snapshot & Replay (Futuro):
   - Criar `ISnapshotWriter` chamado após `EntitySaveSystem` para armazenar estado comprimido por intervalos configuráveis.
9. Configuração Declarativa de Sync:
   - Adicionar atributo `[Sync(Authority=Server, Trigger=OnChange)]` permitindo varrer assembly e auto-registrar em vez de chamadas manuais.
10. Hot Reload de Sistemas (Dev QoL):
    - Carregar sistemas via MEF/reflection em desenvolvimento e rebuild live do pipeline.

## Exemplo Futuro de Pipeline Declarativo
```
new SimulationPipelineDescriptor()
  .Stage(SystemStage.Net)
    .Add<NetworkSystem>()
  .Stage(SystemStage.Staging)
    .Add<StagingProcessorSystem>()
  .Stage(SystemStage.Logic)
    .Add<EntityIndexSystem>()
    .Add<MovementSystem>()
  .Stage(SystemStage.Spatial)
    .Add<SpatialIndexSystem>()
  .Stage(SystemStage.Save)
    .Add<EntitySaveSystem>()
  .Stage(SystemStage.Destroy)
    .Add<EntityDestructorSystem>()
  .Build(world, services);
```

## Benefícios Esperados
- Menos acoplamento entre Client/Server builders.
- Escalabilidade: fácil inserir novos sistemas sem replicar AddSystem.
- Base preparada para features avançadas (predição, interesse) sem reescrever fundamentos.
- Clareza operacional: pipeline mais legível e ordenada.

## Riscos
- Introdução de novos estágios exige validação de dependências (ex: Movement depende de Input já aplicado).
- Batching pode introduzir latência percebida se não for adaptativo.

## Próximo Passo Sugerido
Implementar enum de estágios + registrador automático por atributo para reduzir ainda mais boilerplate e consolidar ordem.

---
Refatoração atual é mínima e não altera comportamento de runtime.
