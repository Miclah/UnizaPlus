# Spustenie UnizaPlus v Dockeri

Tento image balí iba `UnizaPlus.Web`, spustený v režime `Csv` (demo). Načíta
priložený súbor `sample-data/schedule.csv` a nepotrebuje prehliadač, prihlásenie
ani pripojenie na internet. `UnizaPlusBackEnd` (Selenium scraper pre režim
`Live`) súčasťou tohto image nie je; rozdiel medzi oboma zdrojmi dát je
popísaný v [readme](readme.md).

*English version: [DOCKER.md](DOCKER.md)*

## Predpoklady

- Docker Engine 24+ (alebo Docker Desktop) s Compose v2.

## Rýchly štart (Docker Compose)

```bash
cp .env.example .env
docker compose up --build
```

Aplikácia beží na [http://localhost:8080](http://localhost:8080). Zastavíte ju
príkazom `docker compose down`.

## Rýchly štart (samotný Docker)

```bash
docker build -t unizaplus-web .
docker run --rm -p 8080:8080 unizaplus-web
```

## Konfigurácia

Nastavte ako premenné prostredia (`docker run -e ...`, alebo v `.env` pri
použití Compose):

| Premenná | Predvolená hodnota | Význam |
|---|---|---|
| `HOST_PORT` | `8080` | Port na hostiteľovi, na ktorý Compose mapuje port 8080 v kontajneri. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` používajte iba na lokálne ladenie: zapína vývojársku stránku s výnimkami. |
| `UnizaPlus__DataSource` (`UNIZAPLUS_DATASOURCE` v `.env`) | `Csv` | Tento image podporuje iba `Csv`. Hodnota `Live` nebude fungovať, pretože ten režim potrebuje `UnizaPlusBackEnd`, Chrome a pripojenie na `vzdelavanie.uniza.sk`, nič z toho tento image neobsahuje. |

## Výmena demo rozvrhu

`docker-compose.yml` pripája `./sample-data` do kontajnera v režime len na
čítanie, takže stačí na hostiteľovi nahradiť `sample-data/schedule.csv`
(rovnaký formát ako je popísaný v readme, sekcia [formát CSV](readme.md#csv-format-demo-data--upload-csv))
a kontajner reštartovať. Bez nutnosti nového buildu:

```bash
docker compose restart app
```

## Detaily k image

- **Build fáza**: `mcr.microsoft.com/dotnet/sdk:10.0`, publikuje
  `UnizaPlus.Web` (a jeho referenciu na `UnizaPlus.Models`) v konfigurácii
  `Release`.
- **Runtime fáza**: `mcr.microsoft.com/dotnet/aspnet:10.0`, bez build
  nástrojov, bez Chrome/chromedriver, beží pod non-root používateľom.
- Počúva na porte `8080` (`ASPNETCORE_URLS=http://+:8080`).

## Dáta a stav

Úpravy každého návštevníka žijú iba v pamäti servera počas jeho session
(kĺzavé 30-minútové vypršanie) a nikdy sa nezapisujú na disk. Reštart
kontajnera vynuluje rozpracované zmeny všetkých návštevníkov, no súboru
`schedule.csv` sa nedotkne.

## Zastavenie, odinštalovanie a vymazanie Dockera

Všetko vyššie počíta s tým, že Docker Desktop (alebo Docker Engine) už beží.
Ak ho chcete úplne vypnúť alebo odstrániť z počítača, postupujte takto.

### Zastavenie bežiacej aplikácie

```bash
docker compose down
```

Pri samotnom Dockeri bez Compose:

```bash
docker stop <kontajner>
```

Oboje zastaví kontajner UnizaPlus bez toho, aby sa vypol samotný Docker.

### Vypnutie Docker Desktop / Docker Engine

- **Windows/macOS (Docker Desktop)**: ukončite ho cez ikonu veľryby v
  systémovej lište alebo v menu bare (pravý klik → Quit Docker Desktop). Na
  Windows sa tým vypne aj WSL2 virtuálny stroj, na ktorom beží.
- **Linux (Docker Engine)**:

  ```bash
  sudo systemctl stop docker
  ```

  Aby sa nespúšťal ani pri štarte systému:

  ```bash
  sudo systemctl disable docker
  ```

### Odinštalovanie Docker Desktop

- **Windows**: Nastavenia → Aplikácie → Nainštalované aplikácie → Docker
  Desktop → Odinštalovať.
- **macOS**: v Docker Desktop otvorte Troubleshoot (ikona chrobáka) →
  Uninstall, alebo presuňte Docker.app do Koša.
- **Linux**: odstráňte balíky správcom balíkov. Na Debiane/Ubuntu:

  ```bash
  sudo apt purge docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  ```

### Vymazanie všetkých dát, ktoré Docker uložil

Odinštalovaním aplikácie sa samé od seba nezmažú images, kontajnery ani
volumes. Toto zmaže všetky zastavené kontajnery, nepoužívané images,
nepoužívané volumes a build cache, čím sa uvoľní všetko miesto, ktoré Docker
zaberal:

```bash
docker system prune -a --volumes
```

Na odstránenie iba image tohto projektu:

```bash
docker rmi unizaplus-web
```

Na Windows si Docker Desktop navyše drží niekoľkogigabajtový virtuálny disk
WSL2 (`docker-desktop-data`). Na uvoľnenie tohto miesta po odinštalovaní:

```bash
wsl --unregister docker-desktop-data
```
