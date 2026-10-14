#!/bin/sh
set -eu

DB="${ORLEANS_DATABASE:-building_manager_orleans}"
DIR="${ORLEANS_SQL_DIR:-$(dirname "$0")}"

q() { psql -v ON_ERROR_STOP=1 -q -tA -d "$DB" -c "$1"; }

q "CREATE TABLE IF NOT EXISTS orleans_schema_scripts (name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())"

if [ "$(q "SELECT count(*) FROM orleans_schema_scripts")" = "0" ] &&
   [ "$(q "SELECT to_regclass('public.orleansquery') IS NOT NULL")" = "t" ]; then
  echo "Existing Orleans schema without a script record: recording the base scripts as applied."
  for script in "$DIR"/0[1-4]-*.sql; do
    q "INSERT INTO orleans_schema_scripts(name) VALUES ('$(basename "$script")')"
  done
fi

applied=0
for script in "$DIR"/*.sql; do
  name=$(basename "$script")
  if [ "$(q "SELECT count(*) FROM orleans_schema_scripts WHERE name = '$name'")" = "1" ]; then
    continue
  fi
  echo "Applying $name to $DB"
  if grep -qiE '^[[:space:]]*BEGIN[[:space:]]*;' "$script"; then
    psql -v ON_ERROR_STOP=1 -q -d "$DB" -f "$script"
    q "INSERT INTO orleans_schema_scripts(name) VALUES ('$name')"
  else
    psql -v ON_ERROR_STOP=1 -q --single-transaction -d "$DB" \
      -f "$script" \
      -c "INSERT INTO orleans_schema_scripts(name) VALUES ('$name')"
  fi
  applied=$((applied + 1))
done
echo "Orleans schema up to date in $DB ($applied script(s) applied)."
