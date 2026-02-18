# Publicar BindMapper no GitHub Packages

## Opção 1: Publicação Automática via GitHub Actions (Recomendado)

O repositório já está configurado para publicar automaticamente no GitHub Packages usando o workflow `.github/workflows/publish-github-packages.yml`.

### Como publicar:

**Método 1 - Via Tag:**
```bash
git tag v1.0.2-preview
git push origin v1.0.2-preview
```

**Método 2 - Manualmente no GitHub:**
1. Acesse: https://github.com/djesusnet/BindMapper/actions/workflows/publish-github-packages.yml
2. Clique em **"Run workflow"**
3. Escolha o branch `main`
4. Clique em **"Run workflow"**

O workflow usa o `GITHUB_TOKEN` automático (já tem permissão `write:packages`), portanto **não precisa criar novos tokens**! ✅

---

## Opção 2: Publicação Manual Local

Se preferir publicar manualmente da sua máquina:

### 1. Criar Personal Access Token (PAT)

1. Acesse: https://github.com/settings/tokens
2. Clique em **"Generate new token (classic)"**
3. Selecione os escopos:
   - ✅ `write:packages` - Para publicar pacotes
   - ✅ `read:packages` - Para instalar pacotes
4. Copie o token gerado

### 2. Configurar autenticação local

Execute este comando substituindo `YOUR_GITHUB_PAT` pelo token criado:

```powershell
dotnet nuget add source --username djesusnet --password YOUR_GITHUB_PAT --store-password-in-clear-text --name github "https://nuget.pkg.github.com/djesusnet/index.json"
```

Ou edite o arquivo `nuget.config` e adicione seu PAT no campo `ClearTextPassword`.

## 3. Empacotar o projeto

```powershell
cd "c:\repo\projects\Open Source\BindMapper\src\BindMapper"
dotnet pack --configuration Release
```

## 4. Publicar no GitHub Packages

```powershell
dotnet nuget push "bin/Release/BindMapper.1.0.2-preview.nupkg" --api-key YOUR_GITHUB_PAT --source "github"
```

## 5. Verificar pacote publicado

Acesse: https://github.com/djesusnet?tab=packages

---

## Instalar o pacote em outros projetos

### Configurar fonte do GitHub Packages

```powershell
dotnet nuget add source --username djesusnet --password YOUR_GITHUB_PAT --store-password-in-clear-text --name github "https://nuget.pkg.github.com/djesusnet/index.json"
```

### Adicionar ao projeto

```powershell
dotnet add package BindMapper --version 1.0.2-preview --source github
```

Ou adicione ao `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="BindMapper" Version="1.0.2-preview" />
</ItemGroup>
```

---

## Publicar via GitHub Actions

Adicione ao seu workflow `.github/workflows/publish.yml`:

```yaml
name: Publish to GitHub Packages

on:
  release:
    types: [created]

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      packages: write
      contents: read
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Pack
        run: dotnet pack src/BindMapper/BindMapper.csproj --configuration Release --no-build --output .
      
      - name: Push to GitHub Packages
        run: dotnet nuget push "*.nupkg" --api-key ${{ secrets.GITHUB_TOKEN }} --source "https://nuget.pkg.github.com/djesusnet/index.json" --skip-duplicate
```

**Nota:** O `GITHUB_TOKEN` é fornecido automaticamente pelo GitHub Actions com permissão `write:packages`.

---

## Comandos rápidos

### Publicar nova versão
```powershell
# Atualizar versão no .csproj
# <Version>1.0.3-preview</Version>

# Empacotar e publicar
cd "c:\repo\projects\Open Source\BindMapper\src\BindMapper"
dotnet pack -c Release
dotnet nuget push "bin/Release/BindMapper.1.0.3-preview.nupkg" --api-key YOUR_GITHUB_PAT --source "github"
```

### Publicar também no NuGet.org (dual publishing)
```powershell
# GitHub Packages
dotnet nuget push "bin/Release/BindMapper.1.0.2-preview.nupkg" --api-key YOUR_GITHUB_PAT --source "github"

# NuGet.org
dotnet nuget push "bin/Release/BindMapper.1.0.2-preview.nupkg" --api-key YOUR_NUGET_API_KEY --source "https://api.nuget.org/v3/index.json"
```
