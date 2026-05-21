# GestionPortefeuille

Application web .NET 8 / Blazor Server pour la gestion d'un portefeuille d'investissement
(actions et crypto-monnaies). Architecture en 4 couches : Core / Infrastructure / Services / Web.

## Demarrage rapide

```bash
dotnet restore
dotnet ef database update --project GestionPortefeuille.Infrastructure --startup-project GestionPortefeuille.Web
dotnet run --project GestionPortefeuille.Web
```

L'application est disponible sur `https://localhost:5001`.

## Configuration des cles d'API (User Secrets)

Les cles d'API ne sont JAMAIS stockees dans `appsettings.json`. Utilisez User Secrets :

```bash
dotnet user-secrets set "PriceData:FinnhubApiKey" "votre-cle-finnhub" \
  --project GestionPortefeuille.Web
```

Pour basculer en mode API (au lieu du mode simule), modifiez `appsettings.json` :

```json
"PriceData": {
  "Mode": "Api",
  "CacheMinutes": 15
}
```

> CoinGecko (crypto) ne necessite pas de cle. Finnhub (actions) en demande une (compte gratuit sur finnhub.io).

## Tests

```bash
dotnet test
```

## Structure

- **GestionPortefeuille.Core** : entites, interfaces, enums, options, modeles
- **GestionPortefeuille.Infrastructure** : EF Core, AppDbContext, migrations, seed
- **GestionPortefeuille.Services** : logique metier (portefeuille, transactions, analytics, prix)
- **GestionPortefeuille.Web** : UI Blazor Server, pages, composants partages
- **GestionPortefeuille.Tests** : tests unitaires xUnit
