# Pidamg.Expressions — Moteur d'expressions pour .NET

[English](README.md) | **Français**

Moteur d'expressions .NET intégrable et indépendant de l'application hôte. Une expression peut
être analysée une fois, puis évaluée dans autant de contextes que nécessaire.

> [!IMPORTANT]
> Le projet est actuellement en préversion et cible .NET 10. L'API publique peut encore évoluer
> avant la version stable `1.0.0`.

## Fonctionnalités

- Opérateurs arithmétiques, logiques, de comparaison, de coalescence et ternaires
- Évaluation typée ou dynamique
- Arbres d'expressions analysés et réutilisables
- Portées de variables parentes/enfants avec masquage
- Accès aux membres, appels de méthodes et indexation null-safe
- Accès par réflexion aux propriétés et champs publics
- Accès aux membres et clés des dictionnaires
- Paramètres de méthodes facultatifs et `params`
- Interpolation `${expression}` avec conservation de la valeur native
- Comparaisons null-safe et promotion numérique
- Aucune dépendance d'exécution

## Installation

Les préversions sont publiées dans
[GitHub Packages](https://github.com/pidamg/expressions-dotnet/packages) et jointes aux
[GitHub Releases](https://github.com/pidamg/expressions-dotnet/releases).

GitHub Packages nécessite une source NuGet authentifiée, y compris pour les packages publics.
Après avoir [configuré l'authentification à GitHub Packages](https://docs.github.com/fr/packages/working-with-the-nuget-registry),
installez la préversion avec :

```bash
dotnet add package Pidamg.Expressions --prerelease
```

Les versions stables seront également publiées sur NuGet.org.

## Démarrage rapide

```csharp
using Pidamg.Expressions;

var expression = ExpressionParser.Parse<int>("quantity * unitPrice");

var context = new EvaluationContext();
context.Set("quantity", 3);
context.Set("unitPrice", 12);

Console.WriteLine(expression.Evaluate(context)); // 36
```

`ExpressionParser.Parse<T>()` renvoie un `IExpression<T>`. Utilisez la méthode `Parse()` non
générique lorsque le type du résultat doit rester dynamique :

```csharp
IEvaluable expression = ExpressionParser.Parse("enabled ? name : null");
object? result = expression.Evaluate(context);
```

## Expressions prises en charge

| Catégorie | Syntaxe |
|---|---|
| Littéraux | `null`, `true`, `false`, `42`, `3.14`, `"texte"`, `'texte'` |
| Arithmétique | `+`, `-`, `*`, `/` |
| Comparaison | `==`, `!=`, `<`, `<=`, `>`, `>=` |
| Logique | `!`, `&&`, `||` |
| Condition | `condition ? siVrai : siFaux` |
| Coalescence | `valeur ?? valeurParDefaut` |
| Accès aux membres | `client.Adresse.Ville` |
| Indexation | `elements[0]`, `valeurs["cle"]` |
| Appels | `service.Find(nom)`, `callback(valeur)` |

L'accès aux membres, l'indexation et les appels de méthodes renvoient `null` lorsque leur cible
est `null`. Une clé de dictionnaire absente ou un indice de liste hors limites renvoie également
`null`.

## Contextes avec portées

```csharp
var root = new EvaluationContext();
root.Set("environment", "production");
root.Set("retries", 3);

var child = root.CreateChild(new Dictionary<string, object?>
{
    ["retries"] = 5,
});

var expression = ExpressionParser.Parse<string>("environment + \":\" + retries");
Console.WriteLine(expression.Evaluate(child)); // production:5
```

Un contexte enfant hérite des valeurs de son parent et peut les masquer localement. `Add()` refuse
un nom déjà présent dans la portée courante ; `Set()` crée ou remplace une valeur locale.

## Interpolation de chaînes

```csharp
var context = new EvaluationContext(values: new Dictionary<string, object?>
{
    ["name"] = "api",
    ["port"] = 8080,
});

object? endpoint = Interpolator.Evaluate("https://${name}:${port}", context);
object? nativePort = Interpolator.Evaluate("${port}", context);
```

Un texte mixte produit toujours une chaîne. Un modèle constitué d'un seul placeholder conserve
la valeur native de l'expression : `nativePort` est donc un `int`.

L'interpolation typée convertit également un texte littéral ou le résultat d'un placeholder :

```csharp
var timeout = Interpolator.Parse<int>("30").Evaluate(context);
var port = Interpolator.Parse<int>("${port}").Evaluate(context);
```

## Conversion des valeurs

`ValueCoercion` expose les mêmes règles de conversion et de comparaison que l'évaluateur. Les
types numériques sont promus lors des comparaisons, les comparaisons d'entiers conservent leur
précision, l'ordonnancement avec `null` est faux et l'égalité est null-safe. Les dépassements
arithmétiques entiers sont contrôlés. La conversion des nombres en chaînes utilise la culture
invariante.

## Frontière de confiance

Pidamg.Expressions est un moteur d'expressions, pas une sandbox de sécurité. Une expression peut
lire les propriétés et champs publics, appeler les méthodes d'instance publiques et invoquer les
délégués exposés dans son contexte d'évaluation. Ces opérations peuvent avoir des effets de bord.

N'évaluez que des expressions de confiance lorsque le contexte contient des objets privilégiés.
Pour une entrée non fiable, exposez des objets de données immuables dédiés et ne fournissez pas
de services, de handles de fichiers ou de processus, de collections modifiables ni de délégués.

## Feuille de route potentielle

Les fonctionnalités suivantes sont envisagées pour de futures versions et ne sont pas prises en
charge actuellement :

- Opérateur modulo `%`
- Opérateurs d'appartenance `in` et `not in` pour les collections et dictionnaires
- Littéraux de listes et de dictionnaires
- Littéraux pour les grands entiers, les nombres décimaux et la notation scientifique
- Séquence d'échappement permettant d'insérer `${` littéralement dans une chaîne interpolée
- Positions précises dans le texte source et diagnostics de parsing améliorés
- Mode d'évaluation restreint avec listes explicites de membres et méthodes autorisés
- Limites configurables pour la profondeur, le nombre d'arguments et la complexité d'évaluation

Les évolutions doivent préserver une évaluation déterministe, indépendante de la culture et sans
ajouter de dépendance d'exécution.

## Développement

```bash
dotnet restore Pidamg.Expressions.slnx
dotnet build Pidamg.Expressions.slnx
dotnet test Pidamg.Expressions.slnx
dotnet format Pidamg.Expressions.slnx --verify-no-changes
```

Les règles de contribution, de compatibilité d'API et de publication sont décrites dans
[`CONTRIBUTING.md`](CONTRIBUTING.md).

## Licence

Ce projet est distribué sous [licence MIT](LICENSE).
