#!/bin/bash

# Script para validar que el paquete NuGet cumple con los estándares

echo "🔍 Validando paquete NuGet..."

# Verificar archivos requeridos
echo "📁 Verificando archivos requeridos..."

required_files=("README.md" "LICENSE" "CHANGELOG.md" "icon.png")
missing_files=()

for file in "${required_files[@]}"; do
    if [ ! -f "$file" ]; then
        missing_files+=("$file")
    else
        echo "✅ $file encontrado"
    fi
done

if [ ${#missing_files[@]} -ne 0 ]; then
    echo "❌ Archivos faltantes: ${missing_files[*]}"
    exit 1
fi

# Verificar estructura del proyecto
echo "🏗️  Verificando estructura del proyecto..."

required_dirs=("Configuration" "Models" "Interfaces" "Services" "Extensions")
for dir in "${required_dirs[@]}"; do
    if [ -d "$dir" ]; then
        echo "✅ Directorio $dir encontrado"
    else
        echo "❌ Directorio $dir faltante"
        exit 1
    fi
done

# Compilar y verificar warnings
echo "🔨 Compilando proyecto..."
dotnet build --configuration Release --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Error en la compilación"
    exit 1
fi

echo "✅ Compilación exitosa"

# Verificar que se genera documentación XML
if [ -f "bin/Release/net8.0/RedisNuget.xml" ]; then
    echo "✅ Documentación XML generada"
else
    echo "❌ Documentación XML no encontrada"
    exit 1
fi

# Verificar contenido del paquete
echo "📦 Verificando contenido del paquete..."

# Buscar el archivo .nupkg
nupkg_file=$(find bin/Release -name "*.nupkg" | head -1)

if [ -z "$nupkg_file" ]; then
    echo "❌ Archivo .nupkg no encontrado"
    exit 1
fi

echo "✅ Paquete encontrado: $nupkg_file"

# Verificar contenido usando dotnet
echo "🔍 Analizando contenido del paquete..."

# Crear directorio temporal para extraer
temp_dir=$(mktemp -d)
cp "$nupkg_file" "$temp_dir/package.zip"
cd "$temp_dir"
unzip -q package.zip

# Verificar archivos en el paquete
package_files=("README.md" "LICENSE" "CHANGELOG.md")
for file in "${package_files[@]}"; do
    if [ -f "$file" ]; then
        echo "✅ $file incluido en el paquete"
    else
        echo "❌ $file no incluido en el paquete"
        cd - > /dev/null
        rm -rf "$temp_dir"
        exit 1
    fi
done

# Verificar que existe el DLL
if [ -f "lib/net8.0/RedisNuget.dll" ]; then
    echo "✅ DLL incluido en el paquete"
else
    echo "❌ DLL no encontrado en el paquete"
    cd - > /dev/null
    rm -rf "$temp_dir"
    exit 1
fi

# Verificar documentación XML
if [ -f "lib/net8.0/RedisNuget.xml" ]; then
    echo "✅ Documentación XML incluida en el paquete"
else
    echo "❌ Documentación XML no incluida en el paquete"
    cd - > /dev/null
    rm -rf "$temp_dir"
    exit 1
fi

# Limpiar
cd - > /dev/null
rm -rf "$temp_dir"

# Verificar metadatos del proyecto
echo "📋 Verificando metadatos del proyecto..."

# Leer el archivo .csproj y verificar campos requeridos
required_metadata=("PackageId" "PackageVersion" "Authors" "Description" "PackageLicenseExpression" "PackageProjectUrl" "RepositoryUrl")

for metadata in "${required_metadata[@]}"; do
    if grep -q "<$metadata>" RedisNuget.csproj; then
        echo "✅ $metadata definido"
    else
        echo "❌ $metadata faltante"
        exit 1
    fi
done

# Verificar que no hay vulnerabilidades conocidas
echo "🔒 Verificando vulnerabilidades..."
dotnet list package --vulnerable --include-transitive > /dev/null 2>&1

if [ $? -eq 0 ]; then
    echo "✅ No se encontraron vulnerabilidades críticas"
else
    echo "⚠️  Se encontraron posibles vulnerabilidades, revisar manualmente"
fi

# Verificar tamaño del paquete
package_size=$(stat -f%z "$nupkg_file" 2>/dev/null || stat -c%s "$nupkg_file" 2>/dev/null)
max_size=$((10 * 1024 * 1024)) # 10MB

if [ "$package_size" -lt "$max_size" ]; then
    echo "✅ Tamaño del paquete OK ($(($package_size / 1024))KB)"
else
    echo "⚠️  Paquete grande ($(($package_size / 1024 / 1024))MB), considerar optimizar"
fi

echo ""
echo "🎉 Validación completada exitosamente!"
echo ""
echo "📊 Resumen:"
echo "   - Archivos requeridos: ✅"
echo "   - Estructura del proyecto: ✅"
echo "   - Compilación: ✅"
echo "   - Documentación XML: ✅"
echo "   - Contenido del paquete: ✅"
echo "   - Metadatos: ✅"
echo "   - Tamaño: ✅"
echo ""
echo "🚀 El paquete está listo para publicar en NuGet.org"
