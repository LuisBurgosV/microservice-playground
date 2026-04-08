#!/bin/bash

echo "🚀 Event-Driven Architecture - Quick Start"
echo "==========================================="
echo ""

# Check if docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker no está en ejecución. Por favor inicia Docker."
    exit 1
fi

echo "📦 Iniciando servicios con docker-compose..."
docker-compose up -d

echo ""
echo "⏳ Esperando a que RabbitMQ y Redis estén listos..."
sleep 5

echo ""
echo "✅ Servicios iniciados:"
echo "   - RabbitMQ: localhost:5672"
echo "   - RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "   - Redis: localhost:6379"
echo ""

echo "🔨 Compilando solución..."
dotnet build

echo ""
echo "📝 Próximos pasos:"
echo "1. Abre 2 terminales para los consumers:"
echo "   Terminal 1: cd Consumer.EmailNotification && dotnet run"
echo "   Terminal 2: cd Consumer.Logging && dotnet run"
echo ""
echo "2. En una 3ª terminal, ejecuta el producer:"
echo "   Terminal 3: cd Producer && dotnet run"
echo ""
echo "3. Observa cómo los eventos fluyen a través de la arquitectura"
echo ""
