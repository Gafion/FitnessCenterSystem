# FitnessCenterSystem

Member and employee administration for one single-site fitness center in Denmark (~10 staff). Also the owner's learning project.

**Phase:** scaffolding (FIT-31). Signup and login are being built (FIT-47).

## Stack

- **Frontend:** React + TypeScript SPA (Vite, TanStack Router, Tailwind, shadcn/ui), Bun (D-007, D-018)
- **Backend:** C# / .NET 10, ASP.NET Core, modular monolith (D-007, D-008, D-024)
- **Database:** PostgreSQL, one schema per module (D-014)
- **Login:** ASP.NET Core Identity + Google (D-011, D-026)
- **Local dev:** Aspire (D-009, D-027)
- **Tests:** MSTest (C#), Vitest + Playwright (frontend), all in CI (D-010, D-018, D-029)

## Setup

Written for Linux and tested on Arch (Omarchy). macOS and Windows need the same tools, installed their own way.

### 1. Install the tools

| Tool | Version | Why |
|---|---|---|
| Git | any | clone the repo |
| Docker | any recent | Aspire and the integration tests run PostgreSQL in containers |
| .NET SDK | 10.0.402 or newer 10.0 (pinned in `global.json`) | backend, Aspire |
| Aspire CLI | 17.0.0 | `aspire run` starts everything |
| Bun | 1.4.4 (pinned in `frontend/package.json`) | frontend packages and scripts |
| Node.js | 22.13 or newer | Vite and Playwright run on Node |

On Arch:

```bash
# Docker: install, start at boot, and let your user run it without sudo
sudo pacman -S docker
sudo systemctl enable --now docker.service
sudo usermod -aG docker "$USER"   # log out and in (or reboot) for this to apply

# .NET SDK, here with mise (https://mise.jdx.dev); Microsoft's install script works too
mise use -g dotnet@10

# Aspire CLI as a .NET global tool
dotnet tool install -g aspire.cli --version 17.0.0

# Bun
curl -fsSL https://bun.sh/install | bash -s "bun-v1.4.4"

# Node.js, also with mise
mise use -g node@lts
```

Then add these two lines to your shell profile (`~/.bashrc` for bash) and open a new terminal:

```bash
# Use the mise .NET SDK, not a system runtime in /usr/share/dotnet that lacks ASP.NET Core
export DOTNET_ROOT="$HOME/.local/share/mise/dotnet-root"
# Where `dotnet tool install -g` puts `aspire`
export PATH="$PATH:$HOME/.dotnet/tools"
```

Check:

```bash
docker run --rm hello-world   # works without sudo
dotnet --version              # 10.0.402 or newer
aspire --version              # 17.0.0
bun --version                 # 1.4.4
node --version                # v22.13 or newer
```

### 2. Get the code and its dependencies

```bash
git clone https://github.com/Gafion/FitnessCenterSystem.git
cd FitnessCenterSystem
dotnet tool restore                           # dotnet-ef, for migrations
bun install --cwd frontend                    # frontend packages
(cd frontend && bunx playwright install chromium)   # browser for the E2E tests
```

### 3. Google login (optional)

Without this step, the app runs with email/password login only. To turn Google on, create an OAuth client of type **Web application** in the Google Cloud Console (FIT-51). Add the redirect URI `http://localhost:5173/api/identity/signin-google`, then store its ID and secret in .NET user-secrets on your machine (never in git):

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client id>" --project src/FitnessCenter.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client secret>" --project src/FitnessCenter.Api
```

### 4. Run it

```bash
aspire run
```

The first run takes a few minutes while it builds and pulls the PostgreSQL image. Then:

- **The app:** http://localhost:5173
- **The Aspire dashboard:** the `Dashboard:` link the command prints (it includes a login token). It shows the logs and health of the API, the frontend and the database.

- **The test mailbox (Mailpit):** the `mailpit` resource's link in the dashboard. Every email the app sends lands here, such as the "Confirm your email" link after a signup. Nothing reaches a real inbox.

Stop it with `Ctrl+C`. The database survives restarts in a Docker volume.

### 5. Run the checks

The same checks CI runs (CI blocks merging when one fails):

```bash
dotnet build FitnessCenter.sln -warnaserror && dotnet test FitnessCenter.sln   # backend, needs Docker
bun run --cwd frontend lint && bun run --cwd frontend typecheck && bun run --cwd frontend test && bun run --cwd frontend knip
bun run --cwd frontend e2e
```

More commands (migrations, regenerating the API client) are in [CLAUDE.md](CLAUDE.md#commands).

### Troubleshooting

| Symptom | Fix |
|---|---|
| `aspire run` says "No frameworks were found" | `DOTNET_ROOT` isn't set, so a .NET without ASP.NET Core was picked up. See step 1. |
| `aspire: command not found` | `~/.dotnet/tools` isn't on your `PATH`. See step 1. |
| "permission denied" on the Docker socket | Your user isn't in the `docker` group yet: log out and in after `usermod`. |
| "Developer certificates may not be fully trusted" | Harmless here: the app runs on plain `http://localhost`. |
| Port 5173 is in use | Stop whatever else uses it, such as another Vite dev server. Google login needs exactly this port. |
| Google says `redirect_uri_mismatch` | The URI in the Google console must be exactly `http://localhost:5173/api/identity/signin-google`. |

## Documentation

| What | Where |
|---|---|
| Decisions (source of truth) | [docs/decisions.md](docs/decisions.md) |
| Open questions | [docs/open-questions.md](docs/open-questions.md) |
| Architecture | [docs/architecture.md](docs/architecture.md) |
| Data model draft | [docs/data-model.md](docs/data-model.md) |
| Research | [docs/research/README.md](docs/research/README.md) |
| Interview guide for the center | [docs/stakeholder-interview-guide.md](docs/stakeholder-interview-guide.md) |
| Instructions for coding agents | [CLAUDE.md](CLAUDE.md) |

Tasks are tracked in Linear (team FitnessCenter, key FIT).
