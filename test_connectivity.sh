#!/bin/bash

echo "=== Teste de Conectividade Servidor-Cliente ==="

# Parar processos anteriores
pkill -f "Server.Console" 2>/dev/null
pkill -f "Simulation.Client" 2>/dev/null
sleep 1

echo "Iniciando servidor..."
cd /home/filipe/GameOpen/GameSimulation/Server.Console
dotnet run > server.log 2>&1 &
SERVER_PID=$!

echo "Aguardando servidor inicializar..."
sleep 3

echo "Iniciando cliente..."
cd /home/filipe/GameOpen/GameSimulation/Simulation.Client
timeout 10s dotnet run > client.log 2>&1
CLIENT_EXIT=$?

echo "=== Logs do Servidor ==="
head -20 /home/filipe/GameOpen/GameSimulation/Server.Console/server.log

echo ""
echo "=== Logs do Cliente ==="
head -20 /home/filipe/GameOpen/GameSimulation/Simulation.Client/client.log

echo ""
echo "=== Status dos Processos ==="
ps aux | grep -E "(Server\.Console|Simulation\.Client)" | grep -v grep

# Limpar processos
kill $SERVER_PID 2>/dev/null
sleep 1
pkill -f "Server.Console" 2>/dev/null

echo ""
echo "=== Teste Completo ==="