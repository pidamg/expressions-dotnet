# Contribuer à Pidamg.Expressions

Merci de votre intérêt pour le projet.

## Prérequis

- SDK .NET défini dans [`global.json`](global.json)
- Git

Le repository utilise une solution SLNX et la gestion centralisée des dépendances NuGet.

```bash
dotnet restore Pidamg.Expressions.slnx
dotnet build Pidamg.Expressions.slnx
dotnet test Pidamg.Expressions.slnx
```

Les sorties de compilation sont centralisées dans le dossier `artifacts/`.

## Structure du repository

```text
src/Pidamg.Expressions/
tests/Pidamg.Expressions.Tests/
Directory.Build.props
Directory.Packages.props
Pidamg.Expressions.slnx
```

Les tests utilisent uniquement l'API publique. Ils référencent le projet source par défaut et
peuvent référencer un package construit en définissant la propriété MSBuild
`TestPackageVersion`.

## Conventions de code

Le formatage et les conventions sont définis dans [`.editorconfig`](.editorconfig).

```bash
dotnet format Pidamg.Expressions.slnx
dotnet format Pidamg.Expressions.slnx --verify-no-changes
```

La documentation XML des types et membres publics est rédigée en anglais et obligatoire. Les
avertissements `CS1591` sont traités comme des erreurs.

## Compatibilité de l'API publique

La surface publique est suivie par `Microsoft.CodeAnalysis.PublicApiAnalyzers` :

- `PublicAPI.Shipped.txt` contient l'API déjà publiée ;
- `PublicAPI.Unshipped.txt` contient les changements de la prochaine version.

Pour enregistrer une nouvelle API publique :

```bash
dotnet format src/Pidamg.Expressions/Pidamg.Expressions.csproj \
  analyzers \
  --diagnostics RS0016
```

La CI vérifie les diagnostics `RS0016` et `RS0017`.

## Tests

Toute correction ou nouvelle fonctionnalité doit être accompagnée de tests adaptés.

```bash
dotnet test Pidamg.Expressions.slnx --configuration Release
```

Pour tester un package local avec la même suite :

```bash
dotnet restore tests/Pidamg.Expressions.Tests \
  -p:TestPackageVersion=0.1.0-alpha \
  --source ./nupkgs \
  --source https://api.nuget.org/v3/index.json

dotnet test tests/Pidamg.Expressions.Tests \
  --configuration Release \
  --no-restore \
  -p:TestPackageVersion=0.1.0-alpha
```

## Création du package

```bash
dotnet pack src/Pidamg.Expressions/Pidamg.Expressions.csproj \
  --configuration Release \
  --output nupkgs/
```

Le package doit contenir la DLL, la documentation XML, le README, l'icône, les métadonnées du
repository et de la licence, ainsi qu'un package de symboles `.snupkg`.

## Versionnement et publication

Les versions suivent [Semantic Versioning](https://semver.org/). Lorsqu'un tag `v*` est poussé,
la CI compile, teste et crée les packages. Toutes les versions sont publiées sur GitHub Packages ;
seules les versions stables sont publiées sur NuGet.org. Une GitHub Release est ensuite créée avec
les notes extraites de `CHANGELOG.md`.

La publication NuGet.org utilise Trusted Publishing avec l'environnement GitHub protégé
`nuget.org` et le secret `NUGET_USER`, qui contient le nom du profil NuGet.org.

Avant de créer un tag, ajoutez une section datée correspondant exactement à sa version dans
`CHANGELOG.md`.
