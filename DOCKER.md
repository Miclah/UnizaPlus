# Running UnizaPlus in Docker

This image packages `UnizaPlus.Web` only, running in `Csv` (demo) mode. It reads
the bundled `sample-data/schedule.csv` and needs no browser, no login, and no
network access. `UnizaPlusBackEnd` (the Selenium scraper used by `Live` mode)
is not part of this image; see the [readme](readme.md) for how the two data
sources differ.

*Slovenská verzia: [DOCKER.sk.md](DOCKER.sk.md)*

## Prerequisites

- Docker Engine 24+ (or Docker Desktop) with Compose v2.

## Quick start (Docker Compose)

```bash
cp .env.example .env
docker compose up --build
```

The app is now at [http://localhost:8080](http://localhost:8080). Stop it with
`docker compose down`.

## Quick start (plain Docker)

```bash
docker build -t unizaplus-web .
docker run --rm -p 8080:8080 unizaplus-web
```

## Configuration

Set these as environment variables (`docker run -e ...` or in `.env` for
Compose):

| Variable | Default | Purpose |
|---|---|---|
| `HOST_PORT` | `8080` | Host-side port Compose publishes the container's 8080 on. |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Use `Development` only for local debugging: it enables the developer exception page. |
| `UnizaPlus__DataSource` (`UNIZAPLUS_DATASOURCE` in `.env`) | `Csv` | This image only supports `Csv`. Setting `Live` will not work, since that mode needs `UnizaPlusBackEnd`, Chrome, and network access to `vzdelavanie.uniza.sk`, none of which this image ships. |

## Swapping the demo schedule

`docker-compose.yml` mounts `./sample-data` into the container read-only, so
you can replace `sample-data/schedule.csv` on the host (same format described
in the readme's [CSV format](readme.md#csv-format-demo-data--upload-csv)
section) and restart the container. No rebuild needed:

```bash
docker compose restart app
```

## Image details

- **Build stage**: `mcr.microsoft.com/dotnet/sdk:10.0`, publishes
  `UnizaPlus.Web` (and its `UnizaPlus.Models` reference) in `Release`
  configuration.
- **Runtime stage**: `mcr.microsoft.com/dotnet/aspnet:10.0`, no build tools,
  no Chrome/chromedriver, runs as a non-root user.
- Listens on port `8080` (`ASPNETCORE_URLS=http://+:8080`).

## Data and state

Every visitor's edits live in server memory for the duration of their session
(30-minute sliding timeout) and are never written to disk. Restarting the
container resets everyone's in-progress changes but never touches
`schedule.csv` itself.

## Stopping, uninstalling, and removing Docker

Everything above assumes Docker Desktop (or Docker Engine) is already
running. If you're done with it entirely, here's how to shut it down or take
it off your machine.

### Stop the running app

```bash
docker compose down
```

With plain Docker instead of Compose:

```bash
docker stop <container>
```

Either way this stops the UnizaPlus container without touching Docker
itself.

### Stop Docker Desktop / Docker Engine

- **Windows/macOS (Docker Desktop)**: quit it from the whale icon in the
  system tray or menu bar (right-click → Quit Docker Desktop). On Windows
  this also stops the WSL2 VM it runs on.
- **Linux (Docker Engine)**:

  ```bash
  sudo systemctl stop docker
  ```

  To stop it from starting on boot as well:

  ```bash
  sudo systemctl disable docker
  ```

### Uninstall Docker Desktop

- **Windows**: Settings → Apps → Installed apps → Docker Desktop →
  Uninstall.
- **macOS**: open Docker Desktop → Troubleshoot (bug icon) → Uninstall, or
  drag Docker.app to the Trash.
- **Linux**: remove the packages with your package manager. On
  Debian/Ubuntu:

  ```bash
  sudo apt purge docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  ```

### Delete everything Docker has stored

Uninstalling the app doesn't remove images, containers, or volumes on its
own. This deletes every stopped container, unused image, unused volume, and
build cache entry, freeing all the disk space Docker was using:

```bash
docker system prune -a --volumes
```

To remove just this project's image:

```bash
docker rmi unizaplus-web
```

On Windows, Docker Desktop also keeps a multi-gigabyte WSL2 virtual disk
(`docker-desktop-data`). To reclaim that space after uninstalling:

```bash
wsl --unregister docker-desktop-data
```
