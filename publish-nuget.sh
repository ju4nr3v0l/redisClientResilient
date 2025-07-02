#!/bin/bash

# Script para publicar el NuGet package
# Uso: ./publish-nuget.sh [version] [api-key]

VERSION=${1:-"1.0.0"}
API_KEY=${2:-""}

echo "🚀 Publicando Azure Redis Resilient Client v$VERSION"

# Limpiar builds anteriores
echo "🧹 Limpiando builds anteriores..."
dotnet clean
rm -rf bin/
rm -rf obj/

# Restaurar dependencias
echo "📦 Restaurando dependencias..."
dotnet restore

# Compilar en Release
echo "🔨 Compilando en modo Release..."
dotnet build --configuration Release --no-restore

# Ejecutar tests (si existen)
if [ -d "Tests" ]; then
    echo "🧪 Ejecutando tests..."
    dotnet test --configuration Release --no-build
fi

# Crear el package
echo "📦 Creando NuGet package..."
dotnet pack --configuration Release --no-build --output ./nupkg

# Verificar que el package se creó
PACKAGE_FILE="./nupkg/Azure.Redis.Resilient.Client.$VERSION.nupkg"
if [ ! -f "$PACKAGE_FILE" ]; then
    echo "❌ Error: No se pudo crear el package $PACKAGE_FILE"
    exit 1
fi

echo "✅ Package creado exitosamente: $PACKAGE_FILE"

# Publicar si se proporciona API key
if [ -n "$API_KEY" ]; then
    echo "🚀 Publicando a NuGet.org..."
    dotnet nuget push "$PACKAGE_FILE" --api-key "$API_KEY" --source https://api.nuget.org/v3/index.json
    
    if [ $? -eq 0 ]; then
        echo "✅ Package publicado exitosamente en NuGet.org"
    else
        echo "❌ Error al publicar el package"
        exit 1
    fi
else
    echo "ℹ️  Para publicar en NuGet.org, ejecuta:"
    echo "   dotnet nuget push \"$PACKAGE_FILE\" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json"
fi

echo "🎉 Proceso completado!"
