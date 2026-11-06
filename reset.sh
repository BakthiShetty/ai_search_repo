#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; BOLD='\033[1m'; NC='\033[0m'
info()  { echo -e "${GREEN}[+]${NC} $*"; }
warn()  { echo -e "${YELLOW}[!]${NC} $*"; }
die()   { echo -e "${RED}[x]${NC} $*" >&2; exit 1; }

MODE="${1:-soft}"

usage() {
  echo -e "${BOLD}Usage:${NC} $0 [soft|hard]"
  echo ""
  echo "  soft  (default)  Truncate the search index tables only."
  echo "                   Keeps the Ollama model (~270 MB download avoided)."
  echo "                   Containers stay up; re-index runs immediately."
  echo ""
  echo "  hard             Destroy all Docker volumes (pgdata + ollama_models)."
  echo "                   Containers are stopped and removed."
  echo "                   The Ollama model must be re-downloaded on next start."
  exit 0
}

[[ "$MODE" == "-h" || "$MODE" == "--help" ]] && usage
[[ "$MODE" != "soft" && "$MODE" != "hard" ]] && die "Unknown mode '$MODE'. Use 'soft' or 'hard'."

# ── Soft reset ────────────────────────────────────────────────────────────────
if [[ "$MODE" == "soft" ]]; then
  info "Soft reset: truncating index tables (vectors + file metadata)..."

  # Ensure postgres is running
  if ! docker compose -f "$ROOT/docker-compose.yml" ps postgres 2>/dev/null | grep -q "running"; then
    info "Postgres not running — starting infrastructure..."
    docker compose -f "$ROOT/docker-compose.yml" up -d postgres
    for i in $(seq 1 30); do
      docker exec semantic-search-poc-postgres-1 pg_isready -U postgres -q 2>/dev/null && break
      [[ $i -eq 30 ]] && die "Postgres did not become ready in time."
      sleep 1
    done
  fi

  docker exec semantic-search-poc-postgres-1 \
    psql -U postgres -d semantic_search \
    -c "TRUNCATE file_chunks, file_metadata;" \
    -c "SELECT 'file_chunks' AS table, COUNT(*) AS rows FROM file_chunks UNION ALL SELECT 'file_metadata', COUNT(*) FROM file_metadata;"

  info "Index cleared. Run the worker to re-index:"
  echo ""
  echo "    dotnet run --project src/SemanticSearch.Worker"
  echo ""
  echo "  or restart everything with:  ./start.sh"
fi

# ── Hard reset ────────────────────────────────────────────────────────────────
if [[ "$MODE" == "hard" ]]; then
  warn "Hard reset: this will destroy pgdata and ollama_models volumes."
  warn "The Ollama model (~270 MB) must be re-downloaded on next start."
  echo ""
  read -r -p "Continue? [y/N] " confirm
  [[ "${confirm,,}" == "y" ]] || { info "Aborted."; exit 0; }

  info "Stopping and removing containers..."
  docker compose -f "$ROOT/docker-compose.yml" down

  info "Removing volumes..."
  docker volume rm semantic-search-poc_pgdata semantic-search-poc_ollama_models 2>/dev/null || \
    warn "One or more volumes were not found (may already be removed)."

  info "Hard reset complete. Start fresh with:"
  echo ""
  echo "    ./start.sh"
  echo ""
fi
