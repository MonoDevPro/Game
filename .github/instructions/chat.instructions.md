## Objetivo
Desenvolver um sistema de chat multiplayer integrado ao fluxo do jogo para comunicação entre jogadores no servidor e cliente (Godot).

### Escopo obrigatório:
- Permitir envio e recebimento de mensagens de texto entre jogadores conectados.
- Exibir mensagens em tempo real no cliente assim que enviadas.
- Integrar o sistema ao ECS e sistema de pacotes já existentes.
- Persistência temporária das últimas mensagens do chat (na memória do servidor).
- Mensagens limitadas a tamanho razoável (ex: 200 caracteres).
- Suporte a múltiplos clientes conectados (broadcast).
- Controles básicos contra spam/injeção (simples blacklist ou rate-limit por IP/Player).

### Detalhamento Técnico
#### Pacotes:
- Criar um novo pacote `ChatMessagePacket` para o Game.Network/Packets/Game;
- Cliente envia ChatMessagePacket ao servidor ao enviar mensagem;
- Servidor valida, processa e faz broadcast para todos os clients conectados;
- Mensagens devem ser associadas a um identificador de jogador presente no packet;
- Pacote deve incluir: PlayerId/NetworkId, mensagem, timestamp opcional;

#### Servidor:
- Nova rotina para processar e validar mensagens recebidas via ChatMessagePacket;
- Broadcast das mensagens válidas para todos os jogadores conectados;
- Armazenar apenas últimas X mensagens para consulta de novos clientes, se necessário;

#### Cliente Godot:
- Nova UI básica para chat (área de histórico + campo de input);
- Receber e exibir mensagens via ChatMessagePacket;
- Permitir envio de mensagens pelo input local para o servidor;
- UI deve não bloquear interação principal do jogo;
- Visual separar evento de sistema (ex: entrada/saída de jogador) caso implementado posteriormente.

#### Extras desejáveis (não obrigatório neste MVP):
- Comandos de chat (/help, /nick, etc);
- Suporte a mensagens privadas;  
- Persistência das mensagens no banco (atualmente só em memória).

### Referências técnicas e exemplos
- Seguir o padrão de registro de packets e handlers em GameServer.cs;
- Estrutura dos packets em Game.Network/Packets/Game;
- Client: usar padrão autoload/Scripts como GameClient do Godot.

---
**Prioridade:** MVP Jogável
**Resultado esperado:** Jogadores podem trocar mensagens de texto simples entre si em tempo real durante uma sessão multiplayer.

---
Caso restem dúvidas, discutir arquitetura no PR.