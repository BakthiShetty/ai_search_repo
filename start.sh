#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="$ROOT/.logs"
mkdir -p "$LOG_DIR"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${GREEN}[+]${NC} $*"; }
warn()  { echo -e "${YELLOW}[!]${NC} $*"; }
die()   { echo -e "${RED}[x]${NC} $*" >&2; exit 1; }

usage() {
  cat <<EOF
Usage: $(basename "$0") [mode]

Modes:
  all         Worker + API + Web UI (default)
  worker      Worker only
  api         API only
  worker-api  Worker + API
  ui-api      Web UI + API
EOF
  exit 1
}

MODE="${1:-all}"

case "$MODE" in
  all)        RUN_WORKER=1; RUN_API=1; RUN_WEB=1 ;;
  worker)     RUN_WORKER=1; RUN_API=0; RUN_WEB=0 ;;
  api)        RUN_WORKER=0; RUN_API=1; RUN_WEB=0 ;;
  worker-api) RUN_WORKER=1; RUN_API=1; RUN_WEB=0 ;;
  ui-api)     RUN_WORKER=0; RUN_API=1; RUN_WEB=1 ;;
  -h|--help)  usage ;;
  *)          warn "Unknown mode '$MODE'"; usage ;;
esac

PIDS=()

cleanup() {
  info "Shutting down services..."
  kill "${PIDS[@]}" 2>/dev/null || true
  wait "${PIDS[@]}" 2>/dev/null || true
  info "Done."
}

# ── 1. Infrastructure ────────────────────────────────────────────────────────
info "Starting Docker infrastructure (postgres + ollama)..."
docker compose -f "$ROOT/docker-compose.yml" up -d

# ── 2. Wait for Postgres ─────────────────────────────────────────────────────
info "Waiting for Postgres to be ready..."
for i in $(seq 1 30); do
  if docker exec semantic-search-poc-postgres-1 pg_isready -U postgres -q 2>/dev/null; then
    info "Postgres is ready."
    break
  fi
  [[ $i -eq 30 ]] && die "Postgres did not become ready in time."
  sleep 1
done

# ── 3. Ollama model ──────────────────────────────────────────────────────────
MODEL="nomic-embed-text"
info "Checking for Ollama model '$MODEL'..."
if ! docker exec semantic-search-poc-ollama-1 ollama list 2>/dev/null | grep -q "$MODEL"; then
  info "Pulling '$MODEL' (first time, ~270 MB)..."
  docker exec semantic-search-poc-ollama-1 ollama pull "$MODEL"
else
  info "Model '$MODEL' already present."
fi

# ── 4. Worker ────────────────────────────────────────────────────────────────
if [[ "$RUN_WORKER" -eq 1 ]]; then
  info "Starting indexing worker..."
  (cd "$ROOT/src/SemanticSearch.Worker" && dotnet run 2>&1 | tee "$LOG_DIR/worker.log") &
  WORKER_PID=$!
  PIDS+=("$WORKER_PID")
fi

# ── 5. API ───────────────────────────────────────────────────────────────────
if [[ "$RUN_API" -eq 1 ]]; then
  info "Starting API (http://localhost:5050)..."
  (cd "$ROOT/src/SemanticSearch.Api" && dotnet run 2>&1 | tee "$LOG_DIR/api.log") &
  API_PID=$!
  PIDS+=("$API_PID")
fi

# ── 6. Web UI ────────────────────────────────────────────────────────────────
if [[ "$RUN_WEB" -eq 1 ]]; then
  info "Installing web dependencies if needed..."
  (cd "$ROOT/src/SemanticSearch.Web" && npm install --silent)

  info "Starting React UI (http://localhost:5173)..."
  (cd "$ROOT/src/SemanticSearch.Web" && npm run dev 2>&1 | tee "$LOG_DIR/web.log") &
  WEB_PID=$!
  PIDS+=("$WEB_PID")
fi

# ── 7. Summary ───────────────────────────────────────────────────────────────
echo ""
info "Services started (mode: $MODE)."
[[ "$RUN_WORKER" -eq 1 ]] && echo "  Worker →  PID $WORKER_PID (log: $LOG_DIR/worker.log)"
[[ "$RUN_API" -eq 1 ]]    && echo "  API    →  http://localhost:5050 (PID $API_PID)"
[[ "$RUN_WEB" -eq 1 ]]    && echo "  UI     →  http://localhost:5173 (PID $WEB_PID)"
echo "  Logs   →  $LOG_DIR/"
echo ""
echo "Press Ctrl+C to stop all services."

trap cleanup INT TERM
wait "${PIDS[@]}"
