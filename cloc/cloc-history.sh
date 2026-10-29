#!/usr/bin/env bash
# cloc-history.sh — Code evolution, commit by commit (gource style).
#
# Walks the history of a git repository, measures every commit with cloc using the
# same criteria as cloc.sh (tracked files minus the exclusions) and writes a static
# HTML report with D3: a histogram stacked by language of files, blank lines,
# comments and code, with dropdowns to choose what to show.
#
# Like gource, it does not check out each commit: it builds a change log
# (git diff-tree between consecutive commits), runs cloc only once per distinct
# version of each file and then "replays" the log, accumulating the totals.
# So the cost depends on what changes, not on commits × repository size.
#
# Requirements: bash, git, cloc (https://github.com/AlDanial/cloc), awk, tr.
# Run  ./cloc-history.sh --help  to see every option.

set -Eeuo pipefail

# Message language (en/es): --lang, or the system one (LANGUAGE, LC_ALL, LC_MESSAGES, LANG);
# English when it is not a supported one. Read before setting LC_ALL=C.
UI_LANG=en
for v in "${LANGUAGE:-}" "${LC_ALL:-}" "${LC_MESSAGES:-}" "${LANG:-}"; do
  v=${v%%:*}; v=${v%%[_.@-]*}
  case $v in en|es) UI_LANG=$v; break ;; esac
done
prev_arg=''
for a in "$@"; do
  [ "$prev_arg" != --lang ] || case $a in en|es) UI_LANG=$a ;; esac
  prev_arg=$a
done
export LC_ALL=C

# ---------------------------------------------------------------------------
# What is measured by default: the same as cloc.sh
#   git ls-files | grep -v '\.json$' | grep -v '\.sql$' | grep -v '^src/Packages/' \
#                | grep -v '^debug/' | grep -v '^ai/' | grep -v '^prompts/' | cloc --list-file=-
# They are extended regular expressions (grep -E) on the path relative to the repository root.
# --no-default-excludes turns them off and -x/--exclude adds more.
# ---------------------------------------------------------------------------
DEFAULT_EXCLUDES=(
  '\.json$'
  '\.sql$'
  '^src/Packages/'
  '^debug/'
  '^ai/'
  '^prompts/'
)

D3_DEFAULT_URL='https://cdnjs.cloudflare.com/ajax/libs/d3/7.9.0/d3.min.js'

usage() {
  case $UI_LANG in es) usage_es ;; *) usage_en ;; esac
}

usage_en() {
  cat <<'EOF'
Usage: cloc-history.sh [options] [-- <pathspec>...]

Builds an HTML report (D3) with the evolution of files, blank lines, comments, code and
size on disk per language at every commit. By default it measures the same as cloc.sh:
the versioned files except *.json, *.sql, src/Packages/, debug/, ai/ and prompts/.

It also includes what GitLab shows in «Repository analytics» and «Contributor analytics»,
without its commit limit: commits over time, % of each language according to cloc, commits
per day of month, per weekday and per hour (UTC), authors, average commits per day and the
activity of each contributor, filterable by branch. For that it uses every non-merge commit
reachable from each branch (not only the main line).

Output (next to the HTML): commits-reports.html (the page), commits-reports.json (the data),
commits-reports.json.js (the same data as a script, so the HTML can be opened from disk
without a server) and commits-reports.cache.txt (incremental cache). With --embed, also
commits-reports.embedded.html: page, data and D3 in a single file. The page is available
in English and Spanish (it follows the browser language; English by default).

Repository and history
  -r, --repo DIR            Repository to analyze (default: .)
  -o, --output FILE         Output HTML (default: ./commits-reports.html); the data goes next
                            to it with the same name (.json, .json.js, .cache.txt)
      --rev REV             Revision the history starts from (default: HEAD)
      --all-commits         Walk every commit (also those of merged branches).
                            By default only the main line (--first-parent), which also
                            follows the main line of any unrelated history (another root
                            commit) merged into it, so earlier history is not skipped.
      --strict-first-parent Pure --first-parent: do not follow unrelated histories.
      --branches MODE      Branches for the activity filter and marks: local (default),
                            all (local + remote ones that differ from their local branch),
                            remote or none (only --rev).
      --since DATE          Only commits after DATE (git log format)
      --until DATE          Only commits before DATE
  -n, --max-commits N       Only the N most recent commits
  -s, --step N              Sample 1 every N commits (the last one is always included)
  -- <pathspec>...          Limit commits and counting to those paths (e.g. -- src/)

Which files are measured (applied in this order)
  -x, --exclude REGEX       Exclude paths matching REGEX (ERE, like grep -Ev).
                            Repeatable. Added to the default exclusions.
  -X, --exclude-from FILE   Read exclusions (one REGEX per line, # = comment)
  -i, --include REGEX       Only measure paths matching some REGEX. Repeatable.
      --exclude-ext LIST    Shortcut: exclude extensions (e.g. json,sql,min.js)
      --exclude-dir LIST    Shortcut: exclude folders with that name at any depth
      --no-default-excludes Do not apply the cloc.sh exclusions
      --filter CMD          Shell filter over the path list (stdin -> stdout),
                            e.g. --filter "grep -v '^tests/'". Runs at the repository root.
      --include-lang LIST   Only those cloc languages (e.g. "C#,HLSL")
      --exclude-lang LIST   Exclude those cloc languages (e.g. "Unity-Prefab,XML")
      --count-duplicates    Count every identical file (cloc --skip-uniqueness).
                            By default, like cloc, identical content counts once.
      --list-files          Print the files that would be measured at --rev and exit.
  The files cloc-history itself generates (report, .json, .json.js, --embed copy and cache)
  are never measured even if versioned: those of this run by path and any other (generated
  with another -o, or by older versions) by content.

Execution and output
      --cloc CMD            cloc command (default: $CLOC or "cloc"),
                            e.g. --cloc "perl /path/cloc-2.10.pl"
  -j, --jobs N              Parallel cloc processes (default: cores, max. 8)
      --batch N             Files per cloc call (default: 2000)
      --title TEXT          Report title (default: one in the page language)
      --no-emails           Leave the authors' emails out of the report (data and page): the
                            contributors list shows only names. Authors are still grouped by
                            email (the local cache keeps them for that). Also with --rebuild.
      --d3 URL|FILE         D3 to use. A file is embedded (offline report).
                            Default: d3 v7.9.0 from cdnjs.
      --rebuild REPORT      Rebuild the page from the data of an existing report (its .json,
                            or the .json/.json.js directly; a report with the data inside the
                            HTML, such as an --embed copy, also works), without git or cloc:
                            applies the current template and recomputes the percentages.
                            Without -o it rewrites that same report (an --embed copy stays a
                            single self-contained file). Honors --title and --d3.
      --embed               Also write a self-contained copy, <output>.embedded.html: page,
                            data and D3 in a single HTML that opens anywhere without its
                            .json files (to send or attach it). D3 comes from --d3 FILE or is
                            downloaded once from the --d3 URL. With --rebuild it just joins an
                            existing report and its data, without git or cloc. It can be put in
                            a gist and viewed through gistpreview.github.io: whatever the GitHub
                            API cuts off from a big file, the page reads from the gist itself.
      --embed-output FILE   Path of the self-contained copy (implies --embed)
      --cache FILE          Incremental cache (txt). Default: <output>.cache.txt, e.g.
                            ./commits-reports.cache.txt. C lines = commits already analyzed
                            (date, totals, subject); V lines = cloc result per file version
                            (blob hash), so only new versions go through cloc; A lines =
                            date and author of every commit (only new ones are read from git).
                            Delete it to start from scratch.
      --no-cache            Neither read nor write the cache
      --force               Ignore the cache and measure everything again. Asks for
                            confirmation; the new cache replaces the old one only if the
                            run finishes
  -y, --yes                 Do not ask for confirmation (required by --force without a terminal)
      --keep-temp           Keep the temporary directory (debugging)
      --lang en|es          Language of these messages (default: the system one; English
                            if it is not supported)
  -v, --verbose             One line per commit as it gets measured (+ batch timings).
                            -vv adds the commands and the temporary directory.
  -q, --quiet               No progress messages
  -h, --help                This help

Progress: each phase shows which commit of the total it is at; the cloc phase (the slow one)
also shows measured versions, elapsed time and ETA.

Examples
  ./cloc-history.sh                                  # same as cloc.sh, whole history
  ./cloc-history.sh -s 10 -o /tmp/report.html        # 1 commit out of 10
  ./cloc-history.sh --since "6 months ago" --exclude-lang "Unity-Prefab"
  ./cloc-history.sh --no-default-excludes -x '^vendor/' -- src/
  ./cloc-history.sh --list-files | head              # check what gets measured
  ./cloc-history.sh -v 2>&1 | tee cloc-history.log   # per-commit detail
  ./cloc-history.sh --rebuild commits-reports.html   # redo the page without recomputing
  ./cloc-history.sh --rebuild commits-reports.html --embed   # page + data in one HTML file
  ./cloc-history.sh --rebuild commits-reports.html --embed --no-emails   # to publish it
EOF
}

usage_es() {
  cat <<'EOF'
Uso: cloc-history.sh [opciones] [-- <pathspec>...]

Genera un informe HTML (D3) con la evolución de archivos, líneas en blanco, comentarios,
código y tamaño en disco por lenguaje en cada commit. Por defecto mide lo mismo que
cloc.sh: los ficheros versionados menos *.json, *.sql, src/Packages/, debug/, ai/ y prompts/.

Incluye también lo que GitLab enseña en «Repository analytics» y «Contributor analytics»,
pero sin su límite de commits: commits en el tiempo, % de cada lenguaje según cloc, commits
por día del mes, por día de la semana y por hora (UTC), autores, media de commits por día y
la actividad de cada contribuidor, filtrable por rama. Para eso usa todos los commits sin
merges alcanzables desde cada rama (no solo la línea principal).

Salida (junto al HTML): commits-reports.html (la página), commits-reports.json (los datos),
commits-reports.json.js (los mismos datos como script, para poder abrir el HTML desde el
disco sin servidor) y commits-reports.cache.txt (caché incremental). Con --embed, además
commits-reports.embedded.html: la página, los datos y D3 en un solo fichero. La página está
en inglés y en español (sigue el idioma del navegador; inglés por defecto).

Repositorio e historia
  -r, --repo DIR            Repositorio a analizar (por defecto: .)
  -o, --output FICHERO      HTML de salida (por defecto: ./commits-reports.html); los datos
                            van al lado con el mismo nombre (.json, .json.js, .cache.txt)
      --rev REV             Revisión de la que parte la historia (por defecto: HEAD)
      --all-commits         Recorre todos los commits (también los de ramas fusionadas).
                            Por defecto solo la línea principal (--first-parent), que además
                            sigue la línea principal de cualquier historia no relacionada
                            (otro commit raíz) fusionada en ella, para no saltarse historia.
      --strict-first-parent --first-parent puro: no sigue las historias no relacionadas.
      --branches MODO      Ramas para el filtro y las marcas de la actividad: local (por
                            defecto), all (locales + remotas que no coinciden con su local),
                            remote o none (solo --rev).
      --since FECHA         Solo commits posteriores a FECHA (formato de git log)
      --until FECHA         Solo commits anteriores a FECHA
  -n, --max-commits N       Solo los N commits más recientes
  -s, --step N              Muestrea 1 de cada N commits (siempre incluye el último)
  -- <pathspec>...          Limita commits y conteo a esas rutas (p. ej. -- src/)

Qué ficheros se miden (se aplican en este orden)
  -x, --exclude REGEX       Excluye las rutas que casen con REGEX (ERE, como grep -Ev).
                            Repetible. Se suma a las exclusiones por defecto.
  -X, --exclude-from FICH   Lee exclusiones (una REGEX por línea, # = comentario)
  -i, --include REGEX       Solo mide las rutas que casen con alguna REGEX. Repetible.
      --exclude-ext LISTA   Atajo: excluye extensiones (p. ej. json,sql,min.js)
      --exclude-dir LISTA   Atajo: excluye carpetas con ese nombre a cualquier nivel
      --no-default-excludes No aplica las exclusiones de cloc.sh
      --filter CMD          Filtro de shell sobre la lista de rutas (stdin -> stdout),
                            p. ej. --filter "grep -v '^tests/'". Se ejecuta en la raíz.
      --include-lang LISTA  Solo esos lenguajes de cloc (p. ej. "C#,HLSL")
      --exclude-lang LISTA  Excluye esos lenguajes de cloc (p. ej. "Unity-Prefab,XML")
      --count-duplicates    Cuenta todos los ficheros idénticos (cloc --skip-uniqueness).
                            Por defecto, como cloc, el mismo contenido cuenta una vez.
      --list-files          Muestra los ficheros que se medirían en --rev y termina.
  Los ficheros que genera el propio cloc-history (informe, .json, .json.js, copia de --embed
  y caché) nunca se miden aunque estén versionados: los de esta ejecución por su ruta y
  cualquier otro (generado con otro -o, o de versiones antiguas) por su contenido.

Ejecución y salida
      --cloc CMD            Comando de cloc (por defecto: $CLOC o "cloc"),
                            p. ej. --cloc "perl /ruta/cloc-2.10.pl"
  -j, --jobs N              Procesos de cloc en paralelo (por defecto: núcleos, máx. 8)
      --batch N             Ficheros por llamada a cloc (por defecto: 2000)
      --title TEXTO         Título del informe (por defecto, uno en el idioma de la página)
      --no-emails           Deja fuera del informe (datos y página) los correos de los autores:
                            la lista de contribuidores solo muestra nombres. Los autores se
                            siguen agrupando por correo (la caché local los guarda para eso).
                            También con --rebuild.
      --d3 URL|FICHERO      D3 a usar. Si es un fichero se incrusta (informe offline).
                            Por defecto: d3 v7.9.0 desde cdnjs.
      --rebuild INFORME     Regenera la página con los datos de un informe ya generado
                            (su .json, o el .json/.json.js directamente; también vale un
                            informe con los datos dentro del HTML, como una copia de --embed),
                            sin git ni cloc: aplica la plantilla actual y recalcula los
                            porcentajes. Sin -o reescribe ese mismo informe (una copia de
                            --embed sigue siendo un único fichero). Respeta --title y --d3.
      --embed               Escribe además una copia autocontenida, <salida>.embedded.html:
                            la página, los datos y D3 en un único HTML que se abre en cualquier
                            sitio sin sus .json (para enviarlo o adjuntarlo). D3 sale de
                            --d3 FICHERO o se descarga una vez de la URL de --d3. Con
                            --rebuild solo junta un informe existente con sus datos, sin git
                            ni cloc. Se puede subir a un gist y verlo con gistpreview.github.io:
                            lo que la API de GitHub recorte de un fichero grande, la página lo
                            lee del propio gist.
      --embed-output FICH   Ruta de la copia autocontenida (implica --embed)
      --cache FICHERO       Caché incremental (txt). Por defecto: <salida>.cache.txt, p. ej.
                            ./commits-reports.cache.txt. Líneas C = commits ya analizados
                            (fecha, totales, asunto); líneas V = resultado de cloc por versión
                            de fichero (hash del blob), así solo pasan por cloc las nuevas;
                            líneas A = fecha y autor de cada commit (solo se leen los nuevos).
                            Bórralo para empezar de cero.
      --no-cache            Ni lee ni escribe la caché
      --force               Ignora la caché y vuelve a medir todo. Pide confirmación; la
                            caché nueva sustituye a la anterior solo si la ejecución termina
  -y, --yes                 No pide confirmación (necesario para --force sin terminal)
      --keep-temp           Conserva el directorio temporal (depuración)
      --lang en|es          Idioma de estos mensajes (por defecto, el del sistema; inglés si
                            no es uno de los soportados)
  -v, --verbose             Una línea por commit según se va midiendo (+ tiempos de lote).
                            -vv añade los comandos y el directorio temporal.
  -q, --quiet               Sin mensajes de progreso
  -h, --help                Esta ayuda

Progreso: cada fase indica por qué commit va del total; la fase de cloc (la lenta)
muestra además versiones medidas, tiempo transcurrido y ETA.

Ejemplos
  ./cloc-history.sh                                  # igual que cloc.sh, toda la historia
  ./cloc-history.sh -s 10 -o /tmp/informe.html       # 1 de cada 10 commits
  ./cloc-history.sh --since "6 months ago" --exclude-lang "Unity-Prefab"
  ./cloc-history.sh --no-default-excludes -x '^vendor/' -- src/
  ./cloc-history.sh --list-files | head              # comprobar qué se mide
  ./cloc-history.sh -v 2>&1 | tee cloc-history.log   # detalle por commit
  ./cloc-history.sh --rebuild commits-reports.html   # rehacer la página sin recalcular
  ./cloc-history.sh --rebuild commits-reports.html --embed   # página + datos en un solo HTML
  ./cloc-history.sh --rebuild commits-reports.html --embed --no-emails   # para publicarlo
EOF
}

# Console messages in the UI_LANG language: "tx KEY args..." prints that key's printf format.
# Adding a language only takes another branch in msg_format (and another usage_xx).
msg_format() {
  case $UI_LANG.$1 in
    es.warn) MSG='cloc-history: aviso: %s' ;;                en.warn) MSG='cloc-history: warning: %s' ;;
    es.need_arg) MSG='falta el valor de %s' ;;               en.need_arg) MSG='missing value for %s' ;;
    es.unknown_opt) MSG='opción desconocida: %s (usa --help)' ;; en.unknown_opt) MSG='unknown option: %s (see --help)' ;;
    es.need_uint) MSG='%s necesita un entero > 0' ;;         en.need_uint) MSG='%s needs an integer > 0' ;;
    es.bad_branches) MSG='--branches debe ser local, all, remote o none' ;; en.bad_branches) MSG='--branches must be local, all, remote or none' ;;
    es.bad_lang) MSG='--lang debe ser en o es' ;;            en.bad_lang) MSG='--lang must be en or es' ;;
    es.mkdir_out) MSG='no se puede crear la carpeta de salida %s' ;; en.mkdir_out) MSG='cannot create the output folder %s' ;;
    es.rebuild_list) MSG='--rebuild y --list-files no se pueden combinar' ;; en.rebuild_list) MSG='--rebuild and --list-files cannot be combined' ;;
    es.cant_read) MSG='no se puede leer %s' ;;               en.cant_read) MSG='cannot read %s' ;;
    es.rebuild_nodata) MSG='no encuentro %s, los datos de %s' ;; en.rebuild_nodata) MSG='cannot find %s, the data of %s' ;;
    es.not_report) MSG='%s no es un informe de cloc-history.sh' ;; en.not_report) MSG='%s is not a cloc-history.sh report' ;;
    es.no_data_in) MSG='%s no tiene datos de cloc-history.sh' ;; en.no_data_in) MSG='%s has no cloc-history.sh data' ;;
    es.no_git) MSG='git no está en el PATH' ;;               en.no_git) MSG='git is not in the PATH' ;;
    es.not_repo) MSG="'%s' no es un repositorio git" ;;      en.not_repo) MSG="'%s' is not a git repository" ;;
    es.bad_rev) MSG='revisión no válida: %s' ;;              en.bad_rev) MSG='invalid revision: %s' ;;
    es.tmp_left) MSG='no se pudo borrar la carpeta temporal %s (algún fichero sigue en uso); se puede borrar a mano' ;;
    en.tmp_left) MSG='could not delete the temporary folder %s (a file in it is still in use); it can be deleted by hand' ;;
    es.keep_temp) MSG='Temporales conservados en: %s' ;;     en.keep_temp) MSG='Temporary files kept in: %s' ;;
    es.tmp_dir) MSG='directorio temporal: %s' ;;             en.tmp_dir) MSG='temporary directory: %s' ;;
    es.bad_regex) MSG='expresión regular no válida: %s' ;;   en.bad_regex) MSG='invalid regular expression: %s' ;;
    es.filter_failed) MSG='el filtro --filter ha fallado: %s' ;; en.filter_failed) MSG='the --filter command failed: %s' ;;
    es.rebuild_read) MSG='[1/2] Leyendo los datos de %s' ;;  en.rebuild_read) MSG='[1/2] Reading the data from %s' ;;
    es.nodata_found) MSG='no se encontraron datos en %s' ;;  en.nodata_found) MSG='no data found in %s' ;;
    es.cloc_empty) MSG='--cloc vacío' ;;                     en.cloc_empty) MSG='--cloc is empty' ;;
    es.no_cloc) MSG="no se puede ejecutar cloc ('%s').\n  Instálalo (scoop install cloc · choco install cloc · winget install AlDanial.Cloc ·\n  apt install cloc · brew install cloc) o indica cuál usar con --cloc / CLOC=..." ;;
    en.no_cloc) MSG="cannot run cloc ('%s').\n  Install it (scoop install cloc · choco install cloc · winget install AlDanial.Cloc ·\n  apt install cloc · brew install cloc) or point to it with --cloc / CLOC=..." ;;
    es.show_ext) MSG="no se pudo leer 'cloc --show-ext'; se extraerán todos los ficheros" ;; en.show_ext) MSG="could not read 'cloc --show-ext'; every file will be extracted" ;;
    es.cache_loaded) MSG='Caché: %s (%s commits y %s versiones ya medidas, %s commits de actividad)' ;;
    en.cache_loaded) MSG='Cache: %s (%s commits and %s versions already measured, %s activity commits)' ;;
    es.force_prompt) MSG='--force: se ignorará la caché %s (%s commits, %s versiones de fichero, %s commits de actividad) y se volverá a medir todo con cloc. La caché nueva solo sustituye a esta si la ejecución termina. ¿Continuar? [s/N]' ;;
    en.force_prompt) MSG='--force: the cache %s (%s commits, %s file versions, %s activity commits) will be ignored and everything measured again with cloc. The new cache replaces it only if the run finishes. Continue? [y/N]' ;;
    es.force_yes) MSG='--force --yes: se ignora la caché %s y se vuelve a medir todo' ;; en.force_yes) MSG='--force --yes: ignoring the cache %s and measuring everything again' ;;
    es.force_needs_yes) MSG='--force necesita confirmación: ejecútalo en una terminal o añade --yes' ;; en.force_needs_yes) MSG='--force needs confirmation: run it in a terminal or add --yes' ;;
    es.force_cancel) MSG='Cancelado: no se ha tocado nada.' ;; en.force_cancel) MSG='Cancelled: nothing was changed.' ;;
    es.force_nocache) MSG='--force: aún no hay caché (%s); se mide todo' ;; en.force_nocache) MSG='--force: there is no cache yet (%s); measuring everything' ;;
    es.force_rebuild) MSG='--force y --rebuild no se pueden combinar (--rebuild no usa la caché)' ;; en.force_rebuild) MSG='--force and --rebuild cannot be combined (--rebuild does not use the cache)' ;;
    es.force_useless) MSG='--force no hace nada con --no-cache' ;; en.force_useless) MSG='--force has no effect with --no-cache' ;;
    es.cache_other) MSG='la caché %s es de otra configuración (versión de cloc, filtros de lenguaje o criterio de ficheros); se rehace' ;;
    en.cache_other) MSG='the cache %s belongs to another configuration (cloc version, language filters or file criteria); rebuilding it' ;;
    es.gitlog_failed) MSG='git log ha fallado' ;;            en.gitlog_failed) MSG='git log failed' ;;
    es.no_commits) MSG='no hay commits que analizar con esos criterios' ;; en.no_commits) MSG='no commits to analyze with those criteria' ;;
    es.cached_note) MSG=' · %s ya analizados (caché), %s nuevos' ;; en.cached_note) MSG=' · %s already analyzed (cache), %s new' ;;
    es.walk_all) MSG='todos los commits' ;;                  en.walk_all) MSG='all commits' ;;
    es.walk_joined) MSG='first-parent + historias no relacionadas' ;; en.walk_joined) MSG='first-parent + unrelated histories' ;;
    es.phase1) MSG='[1/5] %s commits a analizar (de %s, %s desde %s)%s' ;; en.phase1) MSG='[1/5] %s commits to analyze (of %s, %s from %s)%s' ;;
    es.pairs) MSG='git diff-tree --stdin -r -z --raw --no-renames < pares de árboles (%s)' ;; en.pairs) MSG='git diff-tree --stdin -r -z --raw --no-renames < tree pairs (%s)' ;;
    es.diff_failed) MSG='no se pudo leer el registro de cambios (git diff-tree)' ;; en.diff_failed) MSG='could not read the change log (git diff-tree)' ;;
    es.events_failed) MSG='no se pudo preparar el registro de eventos' ;; en.events_failed) MSG='could not prepare the event log' ;;
    es.sizes) MSG='git cat-file --batch-check: tamaño de %s blobs' ;; en.sizes) MSG='git cat-file --batch-check: size of %s blobs' ;;
    es.phase2) MSG='[2/5] Cambios leídos: %s/%s commits · %s rutas medibles · %s versiones distintas (%s en caché, %s para cloc) · %s' ;;
    en.phase2) MSG='[2/5] Changes read: %s/%s commits · %s measurable paths · %s distinct versions (%s cached, %s for cloc) · %s' ;;
    es.vlegend) MSG='  (por commit: ficheros medidos + añadidos ~ modificados - borrados; nuevas = versiones que pasan por cloc)' ;;
    en.vlegend) MSG='  (per commit: measured files + added ~ modified - deleted; new = versions sent to cloc)' ;;
    es.vnew) MSG='nuevas' ;;                                  en.vnew) MSG='new' ;;
    es.eta_calc) MSG='calculando…' ;;                         en.eta_calc) MSG='estimating…' ;;
    es.phase3_prog) MSG='[3/5] cloc: commit %s/%s (%s%%) · versiones %s/%s · %s · ETA %s' ;;
    en.phase3_prog) MSG='[3/5] cloc: commit %s/%s (%s%%) · versions %s/%s · %s · ETA %s' ;;
    es.batch_done) MSG='  · lote %s/%s listo: commits %s-%s, %s versiones, %s' ;; en.batch_done) MSG='  · batch %s/%s done: commits %s-%s, %s versions, %s' ;;
    es.batches) MSG='  · %s lotes de hasta %s versiones, %s en paralelo' ;; en.batches) MSG='  · %s batches of up to %s versions, %s in parallel' ;;
    es.batch_failed) MSG='falló el lote %s de cloc' ;;        en.batch_failed) MSG='cloc batch %s failed' ;;
    es.batch_checkout) MSG='lote %s: git no pudo extraer algunos ficheros (no se cuentan):' ;; en.batch_checkout) MSG='batch %s: git could not extract some files (not counted):' ;;
    es.cloc_noresults) MSG="cloc %s no devolvió resultados para %s ficheros; revisa que '%s' funciona (con --keep-temp se conservan los lotes para mirarlos)" ;;
    en.cloc_noresults) MSG="cloc %s returned no results for %s files; check that '%s' works (--keep-temp keeps the batches to look at them)" ;;
    es.phase3) MSG='[3/5] cloc: %s/%s commits medidos · %s versiones nuevas en %s lotes (%s en paralelo) · %s' ;;
    en.phase3) MSG='[3/5] cloc: %s/%s commits measured · %s new versions in %s batches (%s in parallel) · %s' ;;
    es.phase3_none) MSG='[3/5] cloc: nada nuevo que medir (todo estaba en la caché)' ;; en.phase3_none) MSG='[3/5] cloc: nothing new to measure (everything was cached)' ;;
    es.revlist_failed) MSG='git rev-list de %s ha fallado' ;; en.revlist_failed) MSG='git rev-list of %s failed' ;;
    es.merges_failed) MSG='git rev-list --merges ha fallado' ;; en.merges_failed) MSG='git rev-list --merges failed' ;;
    es.actlog_failed) MSG='git log (actividad) ha fallado' ;; en.actlog_failed) MSG='git log (activity) failed' ;;
    es.act_note) MSG='%s commits nuevos leídos de git' ;;     en.act_note) MSG='%s new commits read from git' ;;
    es.branches_failed) MSG='no se pudo calcular la pertenencia a ramas' ;; en.branches_failed) MSG='could not compute branch membership' ;;
    es.activity_failed) MSG='no se pudo calcular la actividad' ;; en.activity_failed) MSG='could not compute the activity' ;;
    es.act_commits) MSG='%s commits, %s autores' ;;            en.act_commits) MSG='%s commits, %s authors' ;;
    es.phase4) MSG='[4/5] Actividad: %s en %s · %s ramas, %s commits sin merges entre todas (%s) · %s' ;;
    en.phase4) MSG='[4/5] Activity: %s on %s · %s branches, %s non-merge commits across them (%s) · %s' ;;
    es.replay_failed) MSG='no se pudo reproducir el registro de cambios' ;; en.replay_failed) MSG='could not replay the change log' ;;
    es.phase5) MSG='%s Totales de %s commits con %% sobre el total de líneas · generando %s' ;;
    en.phase5) MSG='%s Totals for %s commits with %% of total lines · writing %s' ;;
    es.cache_saved) MSG='Caché: %s (%s commits analizados, %s versiones medidas, %s commits de actividad)' ;;
    en.cache_saved) MSG='Cache: %s (%s commits analyzed, %s versions measured, %s activity commits)' ;;
    es.embed_saved) MSG='Copia autocontenida: %s (%s; datos y D3 dentro)' ;; en.embed_saved) MSG='Self-contained copy: %s (%s; data and D3 inside)' ;;
    es.embed_saved_url) MSG='Copia con los datos dentro: %s (%s; D3 se carga de %s)' ;; en.embed_saved_url) MSG='Copy with the data inside: %s (%s; D3 is loaded from %s)' ;;
    es.embed_d3) MSG='no se pudo descargar D3 de %s: la copia autocontenida lo cargará de ahí al abrirse (usa --d3 ruta/a/d3.min.js para incrustarlo)' ;;
    en.embed_d3) MSG='could not download D3 from %s: the self-contained copy will load it from there when opened (use --d3 path/to/d3.min.js to embed it)' ;;
    es.noemail_failed) MSG='no se pudieron quitar los correos de los datos (--no-emails)' ;; en.noemail_failed) MSG='could not remove the emails from the data (--no-emails)' ;;
    es.embed_same) MSG='--embed-output no puede ser el informe, sus datos ni la caché (%s)' ;; en.embed_same) MSG='--embed-output cannot be the report, its data or the cache (%s)' ;;
    es.done_embed) MSG='Listo en %s: %s (%s) · %s commits' ;; en.done_embed) MSG='Done in %s: %s (%s) · %s commits' ;;
    es.done) MSG='Listo en %s: %s (%s) + %s (%s) + %s · %s commits' ;; en.done) MSG='Done in %s: %s (%s) + %s (%s) + %s · %s commits' ;;
    # Messages printed by awk (passed to it already translated).
    es.awk_reading) MSG='[2/5] Leyendo cambios: commit %d/%d (%d%%)' ;; en.awk_reading) MSG='[2/5] Reading changes: commit %d/%d (%d%%)' ;;
    es.awk_mismatch) MSG='cloc-history: la salida de git diff-tree no cuadra con los commits pedidos (%s)' ;;
    en.awk_mismatch) MSG='cloc-history: git diff-tree output does not match the requested commits (%s)' ;;
    es.awk_count) MSG='cloc-history: git diff-tree devolvió %d de %d commits' ;; en.awk_count) MSG='cloc-history: git diff-tree returned %d of %d commits' ;;
    es.awk_skipped) MSG='cloc-history: aviso: %d rutas con tabuladores o saltos de línea no se miden' ;;
    en.awk_skipped) MSG='cloc-history: warning: %d paths with tabs or newlines are not measured' ;;
    es.awk_replay) MSG='cloc-history: se reprodujeron %d de %d commits' ;; en.awk_replay) MSG='cloc-history: replayed %d of %d commits' ;;
    *) MSG=$1 ;;
  esac
}
tx() { local MSG; msg_format "$1"; shift; printf -- "$MSG" "$@"; }
# Unfilled format, for awk (which uses it in its own printf).
txf() { local MSG; msg_format "$1"; printf '%s' "$MSG"; }

if [ -t 2 ]; then TTY=1; else TTY=0; fi
clear_line() { if [ "$TTY" = 1 ]; then printf '\r\033[K' >&2; fi; }
die()  { clear_line; printf 'cloc-history: error: %s\n' "$*" >&2; exit 1; }
warn() { clear_line; printf '%s\n' "$(tx warn "$*")" >&2; }
log()  { [ "$QUIET" = 1 ] || { clear_line; printf '%s\n' "$*" >&2; }; }
vlog() { [ "$QUIET" = 1 ] || [ "$VERBOSE" -lt 1 ] || { clear_line; printf '%s\n' "$*" >&2; }; }
dlog() { [ "$QUIET" = 1 ] || [ "$VERBOSE" -lt 2 ] || { clear_line; printf '    $ %s\n' "$*" >&2; }; }

fmt_dur() {  # seconds -> "1h 05m", "4m 12s", "35s"
  local s=$1
  if [ "$s" -ge 3600 ]; then printf '%dh %02dm' $((s / 3600)) $((s % 3600 / 60))
  elif [ "$s" -ge 60 ]; then printf '%dm %02ds' $((s / 60)) $((s % 60))
  else printf '%ds' "$s"; fi
}
if [ "$(date +%N 2>/dev/null)" != '%N' ] && [ -n "$(date +%N 2>/dev/null)" ]; then
  now_ms() { local n; n=$(date +%s%N); printf '%s\n' $((n / 1000000)); }
else
  now_ms() { printf '%s\n' $(( $(date +%s) * 1000 )); }
fi
is_uint() { case $1 in ''|*[!0-9]*) return 1 ;; *) [ "$1" -gt 0 ] ;; esac; }
need_arg() { [ $# -ge 2 ] || die "$(tx need_arg "$1")"; }

# Escapes the ERE metacharacters of a literal.
re_quote() { printf '%s' "$1" | sed 's/[][\.*^$+?(){}|/]/\\&/g'; }

# Turns a list "a,b,c" into an ERE alternation "a|b|c" with every element escaped.
list_to_alt() {
  local item out='' IFS=','
  for item in $1; do
    item=${item#"${item%%[![:space:]]*}"}; item=${item%"${item##*[![:space:]]}"}
    [ -n "$item" ] || continue
    out+="${out:+|}$(re_quote "$item")"
  done
  printf '%s' "$out"
}

# Paths passed to git.exe on Windows (Git Bash) must be native.
native_path() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else printf '%s\n' "$1"; fi
}

json_str() {
  local s=$1
  s=${s//\\/\\\\}; s=${s//\"/\\\"}; s=${s//</\\u003c}
  s=${s//$'\t'/\\t}; s=${s//$'\n'/\\n}; s=${s//$'\r'/}
  printf '"%s"' "$s"
}

json_arr() {  # json_arr elem...
  local out='' e
  for e in "$@"; do out+="${out:+,}$(json_str "$e")"; done
  printf '[%s]' "$out"
}

html_esc() {
  local s=$1
  s=${s//&/&amp;}; s=${s//</&lt;}; s=${s//>/&gt;}; s=${s//\"/&quot;}
  printf '%s' "$s"
}

# ---------------------------------------------------------------------------
# Arguments
# ---------------------------------------------------------------------------
REPO=.
OUT=./commits-reports.html
OUT_SET=0
REV=HEAD
FIRST_PARENT=1
JOIN_ROOTS=1
SINCE=''
UNTIL=''
MAX_COMMITS=''
STEP=1
EXCLUDES=()
INCLUDES=()
EXCLUDE_FILES=()
USE_DEFAULTS=1
FILTER_CMD=''
INCLUDE_LANG=''
EXCLUDE_LANG=''
COUNT_DUPS=0
LIST_ONLY=0
CLOC_STR=${CLOC:-cloc}
JOBS=''
BATCH=2000
BATCH_SET=0
TITLE=''
D3_SRC=$D3_DEFAULT_URL
KEEP=0
CACHE=''
USE_CACHE=1
FORCE=0
ASSUME_YES=0
QUIET=0
VERBOSE=0
REBUILD=''
EMBED=0
EMBED_OUT=''
NO_EMAILS=0
PATHSPEC=()
SELF_EXCLUDES=()
BRANCHES=local

while [ $# -gt 0 ]; do
  case $1 in
    -r|--repo)            need_arg "$@"; REPO=$2; shift 2 ;;
    -o|--output)          need_arg "$@"; OUT=$2; OUT_SET=1; shift 2 ;;
    --rev)                need_arg "$@"; REV=$2; shift 2 ;;
    --all-commits)        FIRST_PARENT=0; shift ;;
    --strict-first-parent) JOIN_ROOTS=0; shift ;;
    --branches)           need_arg "$@"; BRANCHES=$2; shift 2 ;;
    --lang)               need_arg "$@"; case $2 in en|es) ;; *) die "$(tx bad_lang)" ;; esac; shift 2 ;;
    --since)              need_arg "$@"; SINCE=$2; shift 2 ;;
    --until)              need_arg "$@"; UNTIL=$2; shift 2 ;;
    -n|--max-commits)     need_arg "$@"; MAX_COMMITS=$2; shift 2 ;;
    -s|--step)            need_arg "$@"; STEP=$2; shift 2 ;;
    -x|--exclude)         need_arg "$@"; EXCLUDES+=("$2"); shift 2 ;;
    -X|--exclude-from)    need_arg "$@"; EXCLUDE_FILES+=("$2"); shift 2 ;;
    -i|--include)         need_arg "$@"; INCLUDES+=("$2"); shift 2 ;;
    --exclude-ext)        need_arg "$@"; alt=$(list_to_alt "$2"); [ -z "$alt" ] || EXCLUDES+=("\\.($alt)\$"); shift 2 ;;
    --exclude-dir)        need_arg "$@"; alt=$(list_to_alt "$2"); [ -z "$alt" ] || EXCLUDES+=("(^|/)($alt)/"); shift 2 ;;
    --no-default-excludes) USE_DEFAULTS=0; shift ;;
    --filter)             need_arg "$@"; FILTER_CMD=$2; shift 2 ;;
    --include-lang)       need_arg "$@"; INCLUDE_LANG=$2; shift 2 ;;
    --exclude-lang)       need_arg "$@"; EXCLUDE_LANG=$2; shift 2 ;;
    --count-duplicates)   COUNT_DUPS=1; shift ;;
    --list-files)         LIST_ONLY=1; shift ;;
    --cloc)               need_arg "$@"; CLOC_STR=$2; shift 2 ;;
    -j|--jobs)            need_arg "$@"; JOBS=$2; shift 2 ;;
    --batch)              need_arg "$@"; BATCH=$2; BATCH_SET=1; shift 2 ;;
    --title)              need_arg "$@"; TITLE=$2; shift 2 ;;
    --d3)                 need_arg "$@"; D3_SRC=$2; shift 2 ;;
    --rebuild)            need_arg "$@"; REBUILD=$2; shift 2 ;;
    --embed)              EMBED=1; shift ;;
    --embed-output)       need_arg "$@"; EMBED=1; EMBED_OUT=$2; shift 2 ;;
    --no-emails)          NO_EMAILS=1; shift ;;
    --cache)              need_arg "$@"; CACHE=$2; shift 2 ;;
    --no-cache)           USE_CACHE=0; shift ;;
    --force)              FORCE=1; shift ;;
    -y|--yes)             ASSUME_YES=1; shift ;;
    --keep-temp)          KEEP=1; shift ;;
    -v|--verbose)         VERBOSE=$((VERBOSE + 1)); shift ;;
    -vv)                  VERBOSE=$((VERBOSE + 2)); shift ;;
    -q|--quiet)           QUIET=1; shift ;;
    -h|--help)            usage; exit 0 ;;
    --)                   shift; PATHSPEC=("$@"); break ;;
    *)                    die "$(tx unknown_opt "$1")" ;;
  esac
done

[ -z "$MAX_COMMITS" ] || is_uint "$MAX_COMMITS" || die "$(tx need_uint --max-commits)"
is_uint "$STEP"  || die "$(tx need_uint --step)"
case $BRANCHES in local|all|remote|none) ;; *) die "$(tx bad_branches)" ;; esac
is_uint "$BATCH" || die "$(tx need_uint --batch)"
if [ -z "$JOBS" ]; then
  JOBS=$( (nproc || getconf _NPROCESSORS_ONLN || echo 2) 2>/dev/null | head -n1 )
  is_uint "$JOBS" || JOBS=2
  [ "$JOBS" -le 8 ] || JOBS=8
fi
is_uint "$JOBS" || die "$(tx need_uint --jobs)"

# --rebuild without -o rewrites the report itself (or the HTML next to the given data).
if [ -n "$REBUILD" ] && [ "$OUT_SET" = 0 ]; then
  case $REBUILD in
    *.json.js) OUT="${REBUILD%.json.js}.html" ;;
    *.json)    OUT="${REBUILD%.json}.html" ;;
    *)         OUT=$REBUILD ;;
  esac
fi

# Output files: the HTML and, next to it, the data (<report>.json and <report>.json.js).
OUT_BASE=${OUT%.html}
DATA_JSON="$OUT_BASE.json"
DATA_JS="$OUT_BASE.json.js"
DATA_JSON_NAME=$(basename "$DATA_JSON")
DATA_JS_NAME=$(basename "$DATA_JS")
# --embed: a self-contained copy (page, data and D3 in one HTML), <report>.embedded.html by default.
# Rebuilding such a copy without -o refreshes it in place and writes nothing else (SPLIT=0: no
# page + .json + .json.js).
EMBED_MARK='<!-- cloc-history.sh embedded report'
SPLIT=1
if [ -n "$REBUILD" ] && [ "$OUT_SET" = 0 ] && [ -r "$REBUILD" ] && grep -qF "$EMBED_MARK" "$REBUILD"; then
  SPLIT=0; EMBED=1; [ -n "$EMBED_OUT" ] || EMBED_OUT=$REBUILD
fi
[ "$EMBED" = 0 ] || [ -n "$EMBED_OUT" ] || EMBED_OUT="$OUT_BASE.embedded.html"
if [ "$SPLIT" = 1 ]; then MAIN_OUT=$OUT; else MAIN_OUT=$EMBED_OUT; fi

# The output folders are created now, so a missing one is not found out after the whole analysis.
for d in "$(dirname "$OUT")" ${EMBED_OUT:+"$(dirname "$EMBED_OUT")"}; do
  mkdir -p "$d" 2>/dev/null || die "$(tx mkdir_out "$d")"
done
# The copy must not overwrite the report, its data or the cache.
abs_path() { printf '%s/%s' "$(cd "$(dirname "$1")" 2>/dev/null && pwd -P)" "$(basename "$1")"; }
if [ -n "$EMBED_OUT" ] && [ "$SPLIT" = 1 ]; then
  for f in "$OUT" "$DATA_JSON" "$DATA_JS" "${CACHE:-$OUT_BASE.cache.txt}"; do
    [ "$(abs_path "$EMBED_OUT")" != "$(abs_path "$f")" ] || die "$(tx embed_same "$EMBED_OUT")"
  done
fi

if [ -n "$REBUILD" ]; then
  [ "$LIST_ONLY" = 0 ] || die "$(tx rebuild_list)"
  [ "$FORCE" = 0 ] || die "$(tx force_rebuild)"
  [ -r "$REBUILD" ] || die "$(tx cant_read "$REBUILD")"
  # Where the data comes from: a .json/.json.js, a report with its .json next to it, or an
  # old report with the data embedded in the HTML itself.
  case $REBUILD in
    *.json|*.js) REBUILD_KIND=data; REBUILD_DATA=$REBUILD ;;
    *)
      if grep -q 'id="report-data"' "$REBUILD"; then
        REBUILD_KIND=inline; REBUILD_DATA=$REBUILD
      elif grep -q 'CLOC_HISTORY_FILES' "$REBUILD"; then
        REBUILD_KIND=data
        name=$(sed -n 's/.*CLOC_HISTORY_FILES = {"json":"\([^"]*\)".*/\1/p' "$REBUILD" | head -n1)
        REBUILD_DATA="$(dirname "$REBUILD")/${name:-$(basename "${REBUILD%.html}").json}"
        [ -r "$REBUILD_DATA" ] || die "$(tx rebuild_nodata "$REBUILD_DATA" "$REBUILD")"
      else
        die "$(tx not_report "$REBUILD")"
      fi ;;
  esac
  grep -q '"commits":\[' "$REBUILD_DATA" || die "$(tx no_data_in "$REBUILD_DATA")"
  # Title: the requested one, the original report's (already escaped) or the data's.
  if [ -n "$TITLE" ]; then
    TITLE_HTML=$(html_esc "$TITLE")
  elif [ "$REBUILD_DATA" != "$REBUILD" ] || [ "$REBUILD_KIND" = inline ]; then
    TITLE_HTML=$(sed -n 's:.*<title>\(.*\)</title>.*:\1:p' "$REBUILD" | head -n1)
  else
    t=$(head -c 8192 "$REBUILD_DATA" | sed -nE 's/.*"title":"(([^"\\]|\\.)*)".*/\1/p' | head -n1)
    t=${t//\\u003c/<}; t=${t//\\\"/\"}; t=${t//\\\\/\\}
    TITLE_HTML=$(html_esc "${t:-Code evolution}")
  fi
else
  command -v git >/dev/null 2>&1 || die "$(tx no_git)"
  git -C "$REPO" rev-parse --git-dir >/dev/null 2>&1 || die "$(tx not_repo "$REPO")"
  TOP=$(git -C "$REPO" rev-parse --show-toplevel 2>/dev/null || true)
  [ -n "$TOP" ] || TOP=$(cd "$REPO" && pwd)
  # git runs from the root from here on, so a pathspec means the same from any folder (as the
  # exclusion patterns and --list-files already do), not a path under the current one.
  REPO=$TOP
  REPO_NAME=$(basename "$TOP")
  TIP=$(git -C "$REPO" rev-parse --verify -q "$REV^{commit}") || die "$(tx bad_rev "$REV")"
  # Without --title the page sets one in its language; the fixed <title> is only shown before loading.
  if [ -n "$TITLE" ]; then TITLE_HTML=$(html_esc "$TITLE"); else TITLE_HTML=$(html_esc "$REPO_NAME · cloc-history"); fi

  # The files this run writes are not measured even if they are tracked (when they fall inside
  # the repository). Generated files with another name are recognized later by their content.
  TOP_P=$(cd "$TOP" && pwd -P)
  for f in "$OUT" "$DATA_JSON" "$DATA_JS" "${CACHE:-$OUT_BASE.cache.txt}" ${EMBED_OUT:+"$EMBED_OUT"}; do
    d=$(cd "$(dirname "$f")" 2>/dev/null && pwd -P) || continue
    case "$d/" in
      "$TOP_P"/*) rel="${d#"$TOP_P"}/$(basename "$f")"; SELF_EXCLUDES+=("^$(re_quote "${rel#/}")\$") ;;
    esac
  done
fi

# git without a pager, without quoted paths and with commit output in UTF-8.
G=(git -C "$REPO" -c core.quotepath=off -c i18n.logOutputEncoding=UTF-8 -c log.showSignature=false)

# ---------------------------------------------------------------------------
# Temporary directory
# ---------------------------------------------------------------------------
WORK=$(mktemp -d "${TMPDIR:-/tmp}/cloc-history.XXXXXX")
# rm -rf of temporary files that never stops the run: on Windows an antivirus scan, or a process
# that is still closing, can keep a file or folder that was just written busy for a moment
# ("Device or resource busy"). It retries for a few seconds and then leaves it be.
rm_temp() {
  local i
  for i in 1 2 3 4 5 6; do
    rm -rf "$@" 2>/dev/null && return 0
    sleep 0.5
  done
  return 0
}
cleanup() {
  local pids
  pids=$(jobs -p 2>/dev/null || true)
  [ -z "$pids" ] || kill $pids 2>/dev/null || true
  if [ "$KEEP" = 1 ]; then log "$(tx keep_temp "$WORK")"; return; fi
  rm_temp "$WORK"
  [ ! -e "$WORK" ] || warn "$(tx tmp_left "$WORK")"
}
trap cleanup EXIT
trap 'exit 130' INT TERM
dlog "$(tx tmp_dir "$WORK")"

# Include/exclude patterns (in files: awk -v would mangle the backslashes).
{
  if [ "$USE_DEFAULTS" = 1 ]; then printf '%s\n' "${DEFAULT_EXCLUDES[@]}"; fi
  if [ ${#SELF_EXCLUDES[@]} -gt 0 ]; then printf '%s\n' "${SELF_EXCLUDES[@]}"; fi
  if [ ${#EXCLUDES[@]} -gt 0 ]; then printf '%s\n' "${EXCLUDES[@]}"; fi
  if [ ${#EXCLUDE_FILES[@]} -gt 0 ]; then
    for f in "${EXCLUDE_FILES[@]}"; do
      [ -r "$f" ] || die "$(tx cant_read "$f")"
      tr -d '\r' < "$f" | grep -v '^[[:space:]]*\(#\|$\)' || true
    done
  fi
} > "$WORK/exclude.re"
if [ ${#INCLUDES[@]} -gt 0 ]; then printf '%s\n' "${INCLUDES[@]}"; fi > "$WORK/include.re"

# Checks that every expression is valid before starting anything long.
for f in "$WORK/exclude.re" "$WORK/include.re"; do
  while IFS= read -r re; do
    rc=0; printf '' | grep -E -- "$re" >/dev/null 2>&1 || rc=$?
    [ "$rc" -le 1 ] || die "$(tx bad_regex "$re")"
  done < "$f"
done

# ---------------------------------------------------------------------------
# awk: reading the change log (git diff-tree -z, with NUL -> \036)
#   mode=list   git ls-tree input; prints the paths that pass the filters
#   mode=paths  prints every path (regular file) in the history that passes the filters
#   mode=plan   writes the event log and the list of versions to measure with cloc
# ---------------------------------------------------------------------------
AWK_DIFF=$(cat <<'AWK'
function wanted_re(p,   i) {
  # cloc always skips these folders, even when they come in --list-file
  if (p ~ /(^|\/)\.(svn|cvs|hg|git|bzr|snapshot|config|venv)\//) return 0
  for (i = 1; i <= nex; i++) if (p ~ ex[i]) return 0
  if (ninc) { for (i = 1; i <= ninc; i++) if (p ~ inc[i]) return 1; return 0 }
  return 1
}
function basename_of(p,   i) { i = match(p, /[^\/]*$/); return substr(p, i) }
# Safe name to extract the blob to (Windows, perl): cloc only looks at the name and the extension.
function safe_name(b,   stem) {
  gsub(/[^A-Za-z0-9._+-]/, "_", b)
  sub(/[. ]+$/, "", b)
  if (b == "") b = "_"
  stem = tolower(b); sub(/\..*$/, "", stem)
  if (stem ~ /^(con|prn|aux|nul|com[0-9]|lpt[0-9])$/) b = "_" b
  if (length(b) > 100) b = substr(b, length(b) - 99)
  return b
}
# Language cloc would assign by name/extension: "" no extension, "?" unknown extension.
function lang_of(b,   l, i, s) {
  l = tolower(b)
  if (l in EXT) return EXT[l]
  for (i = 2; i <= length(l); i++)
    if (substr(l, i, 1) == ".") { s = substr(l, i + 1); if (s in EXT) return EXT[s] }
  if (index(substr(l, 2), ".") == 0) return ""
  return "?"
}
function prefilter(b,   ls, n, t, i) {
  if (!next_ok) return 1
  ls = lang_of(b)
  if (ls == "") return 1            # no extension: cloc will look at the shebang
  if (ls == "?") return 0           # extension cloc does not know
  if (nexl && (tolower(ls) in EXL)) return 0
  if (ninl) {
    if (tolower(ls) in INL) return 1
    n = split(ls, t, "/"); for (i = 1; i <= n; i++) if (tolower(t[i]) in INL) return 1
    return 0
  }
  return 1
}
# Per-commit progress on stderr: one line rewritten in place on a terminal, every 10 % otherwise.
function progress(   p) {
  if (!showprog) return
  if (tty) {
    if (ci % 50 == 0 || ci == npair) {
      printf "\r\033[K" m_reading, ci, npair, int(ci * 100 / npair) > "/dev/stderr"
      fflush("/dev/stderr")
    }
  } else if ((p = int(ci * 10 / npair)) > lastp) {
    lastp = p
    printf m_reading "\n", ci, npair, p * 10 > "/dev/stderr"
    fflush("/dev/stderr")
  }
}
# Summary of each commit (for -v): added, modified, deleted and new versions.
function flush_commit() {
  if (mode == "plan" && ci > 0) print ci "\t" ca "\t" cm "\t" cd "\t" cn > pcf
  ca = cm = cd = cn = 0
}
function header(h) {
  if (ci >= npair || PAIR[ci + 1] != h) {
    printf m_mismatch "\n", h > "/dev/stderr"
    bad = 1; exit 3
  }
  flush_commit()
  ci++
  if (mode == "plan") print "@\t" ci
  if (mode == "paths") progress()
}
function onpath(p,   f, nm, ns, st, b, s, key) {
  split(meta, f, " "); nm = f[2]; ns = f[4]; st = substr(f[5], 1, 1)
  if (mode == "paths") {
    if (nm ~ /^100(644|755)$/ && !(p in SEEN)) {
      SEEN[p] = 1
      if (p ~ /[\t\n]/) { skipped++; return }
      if (wanted_re(p)) print p
    }
    return
  }
  if (!(p in KEPT)) return
  if (!(p in OK)) OK[p] = prefilter(safe_name(basename_of(p)))
  if (!OK[p]) return
  if (st == "D" || nm !~ /^100(644|755)$/) { print "-\t" p; cd++; return }
  if (st == "A") ca++; else cm++
  s = safe_name(basename_of(p)); key = ns "/" s
  # Versions are numbered in history order: each cloc batch covers a range of commits.
  if (!(key in KID)) { KID[key] = ++nk; cn++; print nk "\t" ns "\t" s "\t" ci > keysf }
  print "+\t" p "\t" KID[key] "\t" ns
}
BEGIN {
  # The auxiliary files are read line by line; RS changes at the end, for the input.
  while ((getline ln < exf) > 0) if (ln != "") ex[++nex] = ln
  while ((getline ln < incf) > 0) if (ln != "") inc[++ninc] = ln
  npair = 0
  if (mode != "list") while ((getline ln < pairsf) > 0) PAIR[++npair] = ln
  if (mode == "plan") {
    while ((getline ln < keptf) > 0) KEPT[ln] = 1
    next_ok = 0
    while ((getline ln < extf) > 0) {
      e = ln; sub(/\t.*/, "", e); lg = ln; sub(/^[^\t]*\t/, "", lg)
      e = tolower(e)
      if ((e in EXT) && EXT[e] != lg) EXT[e] = EXT[e] "/" lg; else EXT[e] = lg
      next_ok = 1
    }
    nexl = split(exl, tmp, ","); for (i = 1; i <= nexl; i++) { gsub(/^ +| +$/, "", tmp[i]); EXL[tolower(tmp[i])] = 1 }
    ninl = split(inl, tmp, ","); for (i = 1; i <= ninl; i++) { gsub(/^ +| +$/, "", tmp[i]); INL[tolower(tmp[i])] = 1 }
  }
  ci = 0; want_path = 0
  RS = "\036"
}
mode == "list" {
  t = index($0, "\t"); if (!t) next
  p = substr($0, t + 1); split(substr($0, 1, t - 1), f, " ")
  if (f[1] ~ /^100(644|755)$/ && p !~ /[\t\n]/ && wanted_re(p)) print p
  next
}
{
  if (want_path) { want_path = 0; onpath($0); next }
  n = split($0, part, "\n")
  for (j = 1; j <= n; j++) {
    if (part[j] == "") continue
    if (substr(part[j], 1, 1) == ":") { meta = part[j]; want_path = 1 }
    else header(part[j])
  }
}
END {
  if (bad) exit 3
  flush_commit()
  if (mode != "list" && ci != npairs) {
    printf m_count "\n", ci, npairs > "/dev/stderr"
    exit 3
  }
  if (skipped) printf m_skipped "\n", skipped > "/dev/stderr"
}
AWK
)

run_filter_cmd() {  # stdin -> stdout
  if [ -n "$FILTER_CMD" ]; then
    (cd "$TOP" && bash -c "$FILTER_CMD") || die "$(tx filter_failed "$FILTER_CMD")"
  else
    cat
  fi
}

# ---------------------------------------------------------------------------
# --list-files: what would be measured at the requested revision
# ---------------------------------------------------------------------------
if [ "$LIST_ONLY" = 1 ]; then
  "${G[@]}" ls-tree -r -z --full-tree "$TIP" -- ${PATHSPEC[@]+"${PATHSPEC[@]}"} \
    | tr '\0' '\036' \
    | awk -v mode=list -v exf="$WORK/exclude.re" -v incf="$WORK/include.re" "$AWK_DIFF" \
    | run_filter_cmd
  exit 0
fi

# ---------------------------------------------------------------------------
# Static percentages (computed by the script, not by the browser)
#   Each commit row is ["hash",ts,"author","subject",n,[li,files,blank,comment,code,bytes,...]]
#   (no bytes in old data, "format" 1).
#   Appended: [% of lines of each language over the commit total, in the same order]
#   and [% blank, % comments, % code]. Rows that already carry them are recomputed.
# ---------------------------------------------------------------------------
AWK_PCT=$(cat <<'AWK'
function pct(x, t) { return t > 0 ? sprintf("%.2f", x * 100 / t) : "0" }
NR == 1 { G = ($0 ~ /"format":[2-9]/) ? 6 : 5 }
/^\["/ {
  line = $0; comma = ""
  if (substr(line, length(line)) == ",") { comma = ","; line = substr(line, 1, length(line) - 1) }
  if (match(line, /,\[[0-9,]*\],\[[0-9.,]*\],\[[0-9.,]*\]\]$/)) {
    head = substr(line, 1, RSTART - 1); s = substr(line, RSTART + 2); s = substr(s, 1, index(s, "]") - 1)
  } else if (match(line, /,\[[0-9,]*\]\]$/)) {
    head = substr(line, 1, RSTART - 1); s = substr(line, RSTART + 2, RLENGTH - 4)
  } else { print; next }
  n = split(s, a, ","); tb = tc = tk = 0
  for (i = 1; i + G - 1 <= n; i += G) { tb += a[i + 2]; tc += a[i + 3]; tk += a[i + 4] }
  t = tb + tc + tk; pl = ""
  for (i = 1; i + G - 1 <= n; i += G) pl = pl (i > 1 ? "," : "") pct(a[i + 2] + a[i + 3] + a[i + 4], t)
  print head ",[" s "],[" pl "],[" pct(tb, t) "," pct(tc, t) "," pct(tk, t) "]]" comma
  next
}
{ print }
AWK
)

START_TS=$(date +%s)

if [ -n "$REBUILD" ]; then
# ---------------------------------------------------------------------------
# --rebuild: data from the existing report, without git or cloc
# ---------------------------------------------------------------------------
  log "$(tx rebuild_read "$REBUILD_DATA")"
  if [ "$REBUILD_KIND" = inline ]; then
    awk 'BEGIN { m = "<script id=\"report-data\" type=\"application/json\">" }
         !on { i = index($0, m); if (!i) next; $0 = substr($0, i + length(m)); on = 1 }
         on { j = index($0, "</script>"); if (j) $0 = substr($0, 1, j - 1); if ($0 != "") print; if (j) exit }' \
      "$REBUILD_DATA" > "$WORK/data.raw"
  else
    # .json as it is; the .json.js loses its wrapper (comment, assignment and final ';').
    tr -d '\r' < "$REBUILD_DATA" | awk 'f || /^\{/ { f = 1; if ($0 != ";") print }' > "$WORK/data.raw"
  fi
  grep -q '"commits":\[' "$WORK/data.raw" || die "$(tx nodata_found "$REBUILD_DATA")"
  awk "$AWK_PCT" "$WORK/data.raw" > "$WORK/data.json"
  NCOMMITS=$(grep -c '^\["' "$WORK/data.json" || true)
  PHASE_HTML='[2/2]'
else
# ---------------------------------------------------------------------------
# cloc
# ---------------------------------------------------------------------------
read -r -a CLOC_CMD <<< "$CLOC_STR"
[ ${#CLOC_CMD[@]} -gt 0 ] || die "$(tx cloc_empty)"
CLOC_VERSION=$("${CLOC_CMD[@]}" --version 2>/dev/null | tr -d '\r' | head -n1) || true
[ -n "$CLOC_VERSION" ] || die "$(tx no_cloc "$CLOC_STR")"
dlog "cloc $CLOC_VERSION: ${CLOC_CMD[*]}"

# Extensions and names cloc recognizes: avoids extracting binaries and files cloc ignores.
"${CLOC_CMD[@]}" --show-ext 2>/dev/null | tr -d '\r' \
  | awk 'NF >= 2 && $1 != "Extension" && $1 !~ /^-+$/ {
           e = $1; l = $0; sub(/^[ \t]*[^ \t]+[ \t]+/, "", l); print e "\t" l }' \
  > "$WORK/ext.map" || true
if [ "$(wc -l < "$WORK/ext.map")" -lt 50 ]; then
  warn "$(tx show_ext)"
  : > "$WORK/ext.map"
fi

# Plain comma CSV: cloc 1.x (e.g. 1.90, Ubuntu 22.04's package) silently drops every file when
# given --csv-delimiter. The extracted names never contain a comma (safe_name), so it is enough.
CLOC_ARGS=(--by-file --csv --quiet --hide-rate --skip-uniqueness)
[ -z "$INCLUDE_LANG" ] || CLOC_ARGS+=("--include-lang=$INCLUDE_LANG")
[ -z "$EXCLUDE_LANG" ] || CLOC_ARGS+=("--exclude-lang=$EXCLUDE_LANG")
if [ "$QUIET" = 1 ]; then SHOWPROG=0; else SHOWPROG=1; fi
# Messages printed by awk, already in the console language.
AWK_MSGS=(-v "m_reading=$(txf awk_reading)" -v "m_mismatch=$(txf awk_mismatch)" -v "m_count=$(txf awk_count)"
          -v "m_skipped=$(txf awk_skipped)" -v "m_replay=$(txf awk_replay)")

# Incremental cache (txt): versions already measured by cloc (V), commits already analyzed (C)
# and activity commits (A).
# cloc's result for a version only depends on its content, its name, the cloc version and the
# language filters: if any of these change, the cache is useless and is rebuilt.
CACHE_HEADER='# cloc-history cache v3'
CACHE_V1=0
CACHE_SIG="cloc=$CLOC_VERSION;include-lang=$INCLUDE_LANG;exclude-lang=$EXCLUDE_LANG;self-exclude=1"
[ -n "$CACHE" ] || CACHE="${OUT%.html}.cache.txt"
: > "$WORK/vcache"; : > "$WORK/ccache"; : > "$WORK/acache"
# --force: asks before discarding the cache. With --yes it does not ask and, without a terminal,
# it requires --yes (instead of waiting for an answer that will never come). The old cache is not
# touched: the new one replaces it at the end, only if the run finishes.
# "Yes" in either console language: y/yes or s/si/sí.
is_yes() { case $1 in [yY]|[yY][eE][sS]|[sS]|[sS][iI]|[sS]í|[sS]Í) return 0 ;; *) return 1 ;; esac; }
confirm_force() {
  local answer=''
  if [ "$ASSUME_YES" = 1 ]; then log "$(tx force_yes "$CACHE")"; return 0; fi
  [ -t 0 ] || die "$(tx force_needs_yes)"
  clear_line
  printf '%s ' "$(tx force_prompt "$CACHE" "$(grep -c '^C' "$CACHE" || true)" "$(grep -c '^V' "$CACHE" || true)" "$(grep -c '^A' "$CACHE" || true)")" >&2
  read -r answer || true
  is_yes "$answer" && return 0
  printf '%s\n' "$(tx force_cancel)" >&2
  exit 0
}
if [ "$FORCE" = 1 ] && [ "$USE_CACHE" = 0 ]; then
  warn "$(tx force_useless)"
elif [ "$FORCE" = 1 ] && [ -f "$CACHE" ]; then
  confirm_force
elif [ "$FORCE" = 1 ]; then
  log "$(tx force_nocache "$CACHE")"
elif [ "$USE_CACHE" = 1 ] && [ -f "$CACHE" ]; then
  CACHE_FIRST=$(head -n1 "$CACHE" | tr -d '\r')
  # v1 did not store sizes and v2 did not store the activity: whatever is there is reused and rewritten as v3.
  [ "$CACHE_FIRST" != '# cloc-history cache v1' ] || CACHE_V1=1
  if { [ "$CACHE_FIRST" = "$CACHE_HEADER" ] || [ "$CACHE_FIRST" = '# cloc-history cache v2' ] || [ "$CACHE_V1" = 1 ]; } \
     && grep -qxF "# config"$'\t'"$CACHE_SIG" "$CACHE"; then
    tr -d '\r' < "$CACHE" | awk -F'\t' -v vc="$WORK/vcache" -v cc="$WORK/ccache" -v ac="$WORK/acache" '
      $1 == "V" { print substr($0, 3) > vc }
      $1 == "C" { print > cc }
      $1 == "A" { print substr($0, 3) > ac }'
    log "$(tx cache_loaded "$CACHE" "$(wc -l < "$WORK/ccache" | tr -d ' ')" "$(wc -l < "$WORK/vcache" | tr -d ' ')" "$(wc -l < "$WORK/acache" | tr -d ' ')")"
  else
    warn "$(tx cache_other "$CACHE")"
  fi
fi

# ---------------------------------------------------------------------------
# 1. Commits
# ---------------------------------------------------------------------------
LOG_ARGS=(log --reverse --no-color '--date=format:%Y-%m-%d %H:%M'
          '--format=%H%x1f%T%x1f%ct%x1f%an%x1f%s%x1f%cd')
if [ "$FIRST_PARENT" = 1 ]; then LOG_ARGS+=(--first-parent); else LOG_ARGS+=(--topo-order); fi
[ -z "$SINCE" ] || LOG_ARGS+=("--since=$SINCE")
[ -z "$UNTIL" ] || LOG_ARGS+=("--until=$UNTIL")

# The main line (--first-parent) stops at its root commit. When a repository was assembled by
# merging an unrelated history (another root commit, e.g. a project re-created from scratch
# that later merged the old branch with --allow-unrelated-histories), everything before that
# merge hangs from the second parent and --first-parent skips it. To keep the timeline linear
# without walking every branch, the main line is made of segments, oldest first: the main line
# of the history joined by the oldest such merge (recursively) and then the main line of the
# tip from that merge on (the commits of the tip's own root before the merge are replaced).
# Each segment is a line "tip<TAB>stop" (stop = commit whose ancestors are excluded, or empty).
SEGMENTS="$WORK/segments"; : > "$SEGMENTS"
join_segments() {  # $1 = tip
  local tip=$1 chain n last r lo hi mid best=0 best_root='' m p
  chain=$(mktemp "$WORK/chain.XXXXXX")
  "${G[@]}" rev-list --first-parent "$tip" > "$chain"
  n=$(wc -l < "$chain" | tr -d ' ')
  last=$(tail -n1 "$chain")
  for r in $("${G[@]}" rev-list --max-parents=0 "$tip"); do
    [ "$r" != "$last" ] || continue
    # Commits are newest first and "contains the root r" is true up to some line: binary search
    # for the last one, which is the oldest main-line commit that already has r behind it.
    lo=1; hi=$n
    while [ "$lo" -lt "$hi" ]; do
      mid=$(( (lo + hi + 1) / 2 ))
      if "${G[@]}" merge-base --is-ancestor "$r" "$(sed -n "${mid}p" "$chain")"; then lo=$mid; else hi=$((mid - 1)); fi
    done
    if [ "$lo" -gt "$best" ]; then best=$lo; best_root=$r; fi
  done
  if [ "$best" -gt 0 ]; then
    m=$(sed -n "${best}p" "$chain")
    for p in $("${G[@]}" rev-list --parents -n1 "$m" | cut -d' ' -f3-); do
      if "${G[@]}" merge-base --is-ancestor "$best_root" "$p"; then
        join_segments "$p"
        printf '%s\t%s\n' "$tip" "$m^" >> "$SEGMENTS"
        rm -f "$chain"; return 0
      fi
    done
  fi
  printf '%s\t\n' "$tip" >> "$SEGMENTS"
  rm -f "$chain"
}
if [ "$FIRST_PARENT" = 1 ] && [ "$JOIN_ROOTS" = 1 ]; then join_segments "$TIP"; else printf '%s\t\n' "$TIP" > "$SEGMENTS"; fi

dlog "git ${LOG_ARGS[*]} <$(wc -l < "$SEGMENTS" | tr -d ' ') segments> -- ${PATHSPEC[*]:-}"
: > "$WORK/all-commits"
while IFS=$'\t' read -r seg_tip seg_stop; do
  "${G[@]}" "${LOG_ARGS[@]}" "$seg_tip" ${seg_stop:+"^$seg_stop"} -- ${PATHSPEC[@]+"${PATHSPEC[@]}"} >> "$WORK/all-commits" \
    || die "$(tx gitlog_failed)"
done < "$SEGMENTS"
# --max-count, applied to the whole walk: the N most recent commits.
if [ -n "$MAX_COMMITS" ]; then
  tail -n "$MAX_COMMITS" "$WORK/all-commits" > "$WORK/all-commits.tail" && mv -f "$WORK/all-commits.tail" "$WORK/all-commits"
fi
TOTAL_COMMITS=$(wc -l < "$WORK/all-commits" | tr -d ' ')
[ "$TOTAL_COMMITS" -gt 0 ] || die "$(tx no_commits)"

awk -v s="$STEP" -v n="$TOTAL_COMMITS" '(NR - 1) % s == 0 || NR == n' "$WORK/all-commits" > "$WORK/commits"
NCOMMITS=$(wc -l < "$WORK/commits" | tr -d ' ')

FIRST_HASH=$(head -n1 "$WORK/commits" | cut -d"$(printf '\037')" -f1)
if git -C "$REPO" rev-parse --verify -q "$FIRST_HASH^" >/dev/null 2>&1; then FIRST_IS_ROOT=false; else FIRST_IS_ROOT=true; fi

EMPTY_TREE=$(git -C "$REPO" hash-object -t tree --stdin < /dev/null)
awk -F'\037' -v e="$EMPTY_TREE" '{ print (NR == 1 ? e : prev) " " $2; prev = $2 }' "$WORK/commits" > "$WORK/pairs"

if [ "$FIRST_PARENT" != 1 ]; then WALK=$(tx walk_all)
elif [ "$(wc -l < "$SEGMENTS" | tr -d ' ')" -gt 1 ]; then WALK=$(tx walk_joined)
else WALK="first-parent"; fi
CACHED_NOTE=''
if [ -s "$WORK/ccache" ]; then
  NCACHED=$(awk -v cc="$WORK/ccache" '
    BEGIN { while ((getline ln < cc) > 0) { split(ln, a, "\t"); C[a[2]] = 1 } }
    { split($0, m, "\037"); if (m[1] in C) n++ } END { print n + 0 }' "$WORK/commits")
  CACHED_NOTE=$(tx cached_note "$NCACHED" "$((NCOMMITS - NCACHED))")
fi
log "$(tx phase1 "$NCOMMITS" "$TOTAL_COMMITS" "$WALK" "$REV" "$CACHED_NOTE")"

# ---------------------------------------------------------------------------
# 2. Change log between consecutive commits
# ---------------------------------------------------------------------------
T_PHASE=$SECONDS
dlog "$(tx pairs "$NCOMMITS")"
"${G[@]}" diff-tree --stdin -r -z --raw --no-renames --no-abbrev -- ${PATHSPEC[@]+"${PATHSPEC[@]}"} \
    < "$WORK/pairs" | tr '\0' '\036' | tee "$WORK/diff" \
  | awk -v mode=paths -v exf="$WORK/exclude.re" -v incf="$WORK/include.re" "${AWK_MSGS[@]}" \
        -v pairsf="$WORK/pairs" -v npairs="$NCOMMITS" -v tty="$TTY" -v showprog="$SHOWPROG" \
        "$AWK_DIFF" > "$WORK/candidates" \
  || die "$(tx diff_failed)"
run_filter_cmd < "$WORK/candidates" > "$WORK/kept"

: > "$WORK/keys"
awk -v mode=plan -v exf="$WORK/exclude.re" -v incf="$WORK/include.re" "${AWK_MSGS[@]}" \
    -v pairsf="$WORK/pairs" -v npairs="$NCOMMITS" -v keptf="$WORK/kept" \
    -v extf="$WORK/ext.map" -v keysf="$WORK/keys" -v pcf="$WORK/percommit" \
    -v exl="$EXCLUDE_LANG" -v inl="$INCLUDE_LANG" "$AWK_DIFF" "$WORK/diff" > "$WORK/events" \
  || die "$(tx events_failed)"
rm_temp "$WORK/diff"

NKEYS=$(wc -l < "$WORK/keys" | tr -d ' ')

# Versions already measured in the cache -> straight to stats; the rest (keys.todo) goes to cloc.
: > "$WORK/stats.cached"; : > "$WORK/keys.todo"
awk -F'\t' -v vc="$WORK/vcache" -v todo="$WORK/keys.todo" -v cst="$WORK/stats.cached" '
  BEGIN { while ((getline ln < vc) > 0) { t = index(ln, "\t"); V[substr(ln, 1, t - 1)] = substr(ln, t + 1) } }
  {
    k = $2 "/" $3
    if (k in V) { split(V[k], a, "\t"); if (a[1] != "-") print $1 "\t" V[k] > cst }
    else print > todo
  }' "$WORK/keys"
NTODO=$(wc -l < "$WORK/keys.todo" | tr -d ' ')

# Size on disk of each version: the blob bytes, from a single git call (without extracting
# anything or running cloc). Also for the versions of a v1 cache, which did not store them.
{ cut -f2 "$WORK/keys"; awk -F'\t' 'NF < 6 { sha = $1; sub(/\/.*/, "", sha); print sha }' "$WORK/vcache"; } \
  | sort -u | "${G[@]}" cat-file --batch-check="%(objectname)$(printf '\t')%(objectsize)" 2>/dev/null \
  | awk -F'\t' 'NF == 2' > "$WORK/sizes" || true
dlog "$(tx sizes "$(wc -l < "$WORK/sizes" | tr -d ' ')")"
log "$(tx phase2 "$NCOMMITS" "$NCOMMITS" "$(wc -l < "$WORK/kept" | tr -d ' ')" "$NKEYS" "$((NKEYS - NTODO))" "$NTODO" "$(fmt_dur $((SECONDS - T_PHASE)))")"

# One line per commit for -v (printed once the commit is measured).
if [ "$VERBOSE" -ge 1 ] && [ "$QUIET" = 0 ]; then
  awk -F'\037' -v pcf="$WORK/percommit" -v n="$NCOMMITS" -v m_new="$(tx vnew)" '
    BEGIN {
      w = length(n "")
      while ((getline ln < pcf) > 0) { split(ln, a, "\t"); AD[a[1]] = a[2]; MO[a[1]] = a[3]; DE[a[1]] = a[4]; NV[a[1]] = a[5] }
    }
    {
      s = $5
      if (length(s) > 70) { s = substr(s, 1, 69); sub(/[\300-\377][\200-\277]*$/, "", s); s = s "…" }
      printf "  [%" w "d/%d] %s %s  +%d ~%d -%d  %3d " m_new "  %s\n", NR, n, substr($1, 1, 10), $6, AD[NR], MO[NR], DE[NR], NV[NR], s
    }' "$WORK/commits" > "$WORK/vlines"
  vlog "$(tx vlegend)"
fi

# ---------------------------------------------------------------------------
# 3. cloc, in parallel batches, on every distinct version of every file
#    Versions are in history order, so finishing batches 0..b in order means that every
#    commit before the first one of batch b+1 is measured.
# ---------------------------------------------------------------------------
GIT_NATIVE_ARGS=(-c core.autocrlf=false -c core.safecrlf=false -c core.longpaths=true -c core.symlinks=false)

# Versions that are files generated by cloc-history (report, data or cache), whatever their
# name: they are recognized by their first line (or, in the HTML, by its internal marker) and
# are not passed to cloc. cloc-history.sh itself starts with "#!" and is not caught here.
AWK_SELF=$(cat <<'AWK'
{
  f = $0; first = ""; gen = 0
  if ((getline first < f) > 0) {
    if (first ~ /^# cloc-history cache v/) gen = 1
    else if (first ~ /^\/\* / && index(first, "(cloc-history.sh)")) gen = 1   # .json.js header (any version)
    else if (first ~ /^\{"meta":\{"repo":/ && index(first, "\"firstParent\":")) gen = 1
    else if (tolower(first) ~ /^<!doctype html>/)
      while ((getline ln < f) > 0)
        if (index(ln, "window.CLOC_HISTORY_FILES") || index(ln, "id=\"report-data\"")) { gen = 1; break }
  }
  close(f)
  if (gen) print f
}
AWK
)

run_batch() {  # $1 = batch number
  local b=$1 dir="$WORK/b$1" t0
  t0=$(now_ms)
  mkdir -p "$dir/t"
  awk -F'\t' '{ print "100644 " $2 "\t" $1 "/" $3 }' "$WORK/keys.$b" | tr '\n' '\0' > "$dir/info"
  awk -F'\t' '{ print $1 "/" $3 }' "$WORK/keys.$b" > "$dir/list"
  GIT_INDEX_FILE=$(native_path "$dir/index") git -C "$REPO" update-index -z --index-info < "$dir/info"
  GIT_INDEX_FILE=$(native_path "$dir/index") GIT_LFS_SKIP_SMUDGE=1 \
    git -C "$REPO" "${GIT_NATIVE_ARGS[@]}" --work-tree="$(native_path "$dir/t")" checkout-index -a -f \
    2> "$dir/checkout.err" || true
  (cd "$dir/t" && awk "$AWK_SELF" ../list) > "$dir/self" 2>/dev/null || true
  if [ -s "$dir/self" ]; then
    awk 'NR == FNR { G[$0] = 1; next } !($0 in G)' "$dir/self" "$dir/list" > "$dir/list.tmp"
    mv -f "$dir/list.tmp" "$dir/list"
  fi
  # A batch made only of cloc-history's own files leaves nothing for cloc.
  if [ -s "$dir/list" ]; then
    (cd "$dir/t" && "${CLOC_CMD[@]}" "${CLOC_ARGS[@]}" --list-file=../list) > "$dir/out" 2> "$dir/cloc.err" || true
  else
    : > "$dir/out"
  fi
  wc -l < "$dir/list" | tr -d ' ' > "$dir/sent"
  # language,filename,blank,comment,code: read from the right, so a comma in a language name
  # would stay in the language (the header and SUM lines have no numbered file and are skipped).
  tr -d '\r' < "$dir/out" | awk -F',' 'NF >= 5 && $1 != "SUM" {
      f = $(NF - 3); gsub(/\\/, "/", f); sub(/^\.\//, "", f); k = f; sub(/\/.*/, "", k)
      if (k !~ /^[0-9]+$/) next
      l = $1; for (i = 2; i <= NF - 4; i++) l = l "," $i
      print k "\t" l "\t" $(NF - 2) "\t" $(NF - 1) "\t" $NF
    }' > "$dir/stats"
  rm_temp "$dir/t" "$dir/index" "$dir/info"   # its results are already in stats: a busy file must not fail it
  printf '%s\n' $(( $(now_ms) - t0 )) > "$dir/done"
}

fmt_ms() { printf '%d.%ds' $(($1 / 1000)) $(($1 % 1000 / 100)); }

# ETA: average time per version in each process × versions left / processes running.
eta_text() {
  local left=$((NTODO - DONE_KEYS)) par=$JOBS
  if [ "$left" -le 0 ]; then printf '0s'; return; fi
  if [ "$DONE_KEYS" -eq 0 ]; then tx eta_calc; return; fi
  [ $((NBATCH - DONE_BATCHES)) -ge "$par" ] || par=$((NBATCH - DONE_BATCHES))
  [ "$par" -ge 1 ] || par=1
  printf '~%s' "$(fmt_dur $(( (left * DONE_MS / DONE_KEYS / par + 999) / 1000 )))"
}

p3_progress() {
  [ "$QUIET" = 1 ] && return 0
  local pct=100 line
  [ "$NCOMMITS" -gt 0 ] && pct=$((MEASURED * 100 / NCOMMITS))
  line=$(tx phase3_prog "$MEASURED" "$NCOMMITS" "$pct" "$DONE_KEYS" "$NTODO" "$(fmt_dur $((SECONDS - T_PHASE)))" "$(eta_text)")
  if [ "$TTY" = 1 ]; then printf '\r\033[K%s' "$line" >&2
  elif [ $((pct / 10)) -gt "$P3_LAST10" ]; then P3_LAST10=$((pct / 10)); printf '%s\n' "$line" >&2; fi
}

advance() {  # collects finished batches, moves the measured commit forward and refreshes the progress
  local b dur prev=$MEASURED
  for ((b = 0; b < NBATCH; b++)); do
    if [ -z "${B_SEEN[b]:-}" ] && [ -e "$WORK/b$b/done" ]; then
      B_SEEN[b]=1
      dur=$(cat "$WORK/b$b/done" 2>/dev/null || true); dur=${dur:-0}
      DONE_BATCHES=$((DONE_BATCHES + 1)); DONE_KEYS=$((DONE_KEYS + B_COUNT[b])); DONE_MS=$((DONE_MS + dur))
      vlog "$(tx batch_done "$((b + 1))" "$NBATCH" "${B_FIRST[b]}" "${B_LAST[b]}" "${B_COUNT[b]}" "$(fmt_ms "$dur")")"
    fi
  done
  while [ "$NEXT_B" -lt "$NBATCH" ] && [ -n "${B_SEEN[NEXT_B]:-}" ]; do NEXT_B=$((NEXT_B + 1)); done
  if [ "$NEXT_B" -lt "$NBATCH" ]; then MEASURED=$((B_FIRST[NEXT_B] - 1)); else MEASURED=$NCOMMITS; fi
  if [ "$VERBOSE" -ge 1 ] && [ "$QUIET" = 0 ] && [ "$MEASURED" -gt "$prev" ]; then
    clear_line; sed -n "$((prev + 1)),${MEASURED}p" "$WORK/vlines" >&2
  fi
  p3_progress
}

# Without --batch: about 4 batches per process, so the progress and the ETA move often.
if [ "$BATCH_SET" = 0 ] && [ "$NTODO" -gt 0 ]; then
  per_batch=$(( (NTODO + JOBS * 4 - 1) / (JOBS * 4) ))
  [ "$per_batch" -ge "$BATCH" ] || BATCH=$per_batch
  [ "$BATCH" -ge 100 ] || BATCH=100
fi
NBATCH=$(( (NTODO + BATCH - 1) / BATCH ))
T_PHASE=$SECONDS
if [ "$NBATCH" -gt 0 ]; then
  awk -F'\t' -v B="$BATCH" -v d="$WORK" '
    { b = int((NR - 1) / B); f = d "/keys." b
      if (f != prev) { if (prev != "") close(prev); prev = f; first[b] = $4 }
      print > f; last[b] = $4; cnt[b]++ }
    END { for (i = 0; i <= b; i++) print i "\t" first[i] "\t" last[i] "\t" cnt[i] }' "$WORK/keys.todo" > "$WORK/batches"
  B_FIRST=(); B_LAST=(); B_COUNT=(); B_SEEN=()
  while IFS=$'\t' read -r b f l c; do B_FIRST[b]=$f; B_LAST[b]=$l; B_COUNT[b]=$c; done < "$WORK/batches"
  DONE_BATCHES=0; DONE_KEYS=0; DONE_MS=0; NEXT_B=0; P3_LAST10=-1
  MEASURED=$((B_FIRST[0] - 1))
  vlog "$(tx batches "$NBATCH" "$BATCH" "$JOBS")"
  dlog "cloc ${CLOC_ARGS[*]} --list-file=<batch>"

  launched=0
  while :; do
    while [ "$launched" -lt "$NBATCH" ] && [ "$(jobs -rp | wc -l)" -lt "$JOBS" ]; do
      run_batch "$launched" &
      launched=$((launched + 1))
    done
    advance
    if [ "$launched" -ge "$NBATCH" ] && [ -z "$(jobs -rp)" ]; then break; fi
    sleep 0.5
  done
  wait || true
  advance
  for ((b = 0; b < NBATCH; b++)); do
    if [ ! -e "$WORK/b$b/done" ]; then
      cat "$WORK/b$b/checkout.err" "$WORK/b$b/cloc.err" 2>/dev/null | head -n 20 >&2 || true
      die "$(tx batch_failed "$b")"
    fi
    if [ -s "$WORK/b$b/checkout.err" ]; then
      warn "$(tx batch_checkout "$b")"
      head -n 5 "$WORK/b$b/checkout.err" >&2
    fi
    cat "$WORK/b$b/stats"
  done > "$WORK/stats.new"
  # No results at all from files that did reach cloc: cloc itself is not working.
  SENT=$(cat "$WORK"/b*/sent 2>/dev/null | awk '{ s += $1 } END { print s + 0 }')
  [ -s "$WORK/stats.new" ] || [ "$SENT" -eq 0 ] || {
    cat "$WORK/b0/cloc.err" 2>/dev/null | head -n 20 >&2 || true
    die "$(tx cloc_noresults "$CLOC_VERSION" "$SENT" "$CLOC_STR")"
  }
  log "$(tx phase3 "$NCOMMITS" "$NCOMMITS" "$NTODO" "$NBATCH" "$JOBS" "$(fmt_dur $((SECONDS - T_PHASE)))")"
else
  : > "$WORK/stats.new"
  log "$(tx phase3_none)"
fi
cat "$WORK/stats.cached" "$WORK/stats.new" > "$WORK/stats"
# New V lines for the cache ("-" = cloc ignores that version: it is not tried again).
awk -F'\t' -v sf="$WORK/stats.new" -v szf="$WORK/sizes" '
  BEGIN {
    while ((getline ln < sf) > 0) { t = index(ln, "\t"); S[substr(ln, 1, t - 1)] = substr(ln, t + 1) }
    while ((getline ln < szf) > 0) { split(ln, a, "\t"); SZ[a[1]] = a[2] }
  }
  { print "V\t" $2 "/" $3 "\t" (($1 in S) ? S[$1] : "-\t0\t0\t0") (($2 in SZ) ? "\t" SZ[$2] : "") }' "$WORK/keys.todo" > "$WORK/vnew"

# ---------------------------------------------------------------------------
# 4. Activity, like GitLab's "Repository analytics" and "Contributor analytics" but without
#    their commit limit: every commit reachable from --rev, without merges, with the commit
#    date in UTC and the author (honouring .mailmap). With the cache, git is only asked about
#    the commits that are not in it yet.
# ---------------------------------------------------------------------------
T_PHASE=$SECONDS
# --since/--until also bound the activity (git turns the date into seconds).
ACT_MIN=''; ACT_MAX=''
[ -z "$SINCE" ] || ACT_MIN=$(git -C "$REPO" rev-parse --since="$SINCE" | sed -n 's/^--max-age=//p')
[ -z "$UNTIL" ] || ACT_MAX=$(git -C "$REPO" rev-parse --until="$UNTIL" | sed -n 's/^--min-age=//p')
act_lines() { awk -F'\037' '{ gsub(/\t/, " ", $3); gsub(/\t/, " ", $4); print $1 "\t" $2 "\t" $3 "\t" $4 }'; }

# Branches: name, commit it points to, date of that commit and kind (L local, R remote, V --rev).
# Remote branches pointing to the same commit as their local branch are not repeated.
{
  case $BRANCHES in local|all) "${G[@]}" for-each-ref --format='%(refname:short)%09%(objectname)%09%(committerdate:unix)%09L' refs/heads ;; esac
  case $BRANCHES in all|remote) "${G[@]}" for-each-ref --format='%(refname:short)%09%(objectname)%09%(committerdate:unix)%09R' refs/remotes ;; esac
} | awk -F'\t' '
    $1 ~ /(^|\/)HEAD$/ { next }
    $4 == "L" { LOC[$1] = $2; print; next }
    { short = $1; sub(/^[^\/]*\//, "", short); if ((short in LOC) && LOC[short] == $2) next; print }' > "$WORK/branches.raw"
# --rev is always there: if it is one of those branches, it is marked as the current one; if not
# (a tag, a hash, a detached HEAD…), it is added under its own name.
if [ "$REV" = HEAD ]; then REV_BRANCH=$(git -C "$REPO" symbolic-ref -q --short HEAD || true)
else REV_BRANCH=$(git -C "$REPO" rev-parse -q --verify --symbolic-full-name "$REV" 2>/dev/null | sed -n 's#^refs/heads/##p; s#^refs/remotes/##p' || true); fi
REV_TS=$(git -C "$REPO" log -1 --format=%ct "$TIP")
awk -F'\t' -v OFS='\t' -v rb="$REV_BRANCH" -v rn="$REV" -v tip="$TIP" -v ts="$REV_TS" '
  { cur = ($1 == rb && $2 == tip) ? 1 : 0; if (cur) found = 1; print $1, $2, $3, $4, cur }
  END { if (!found) print (rb != "" ? rb : rn), tip, ts, "V", 1 }' "$WORK/branches.raw" > "$WORK/branches.cur"
# Creation time from the local reflog, while it still keeps it (git forgets it eventually).
: > "$WORK/branches.tsv"
while IFS=$'\t' read -r name tip ts kind cur; do
  created=''; from=''
  if [ "$kind" = L ]; then
    # %gd with --date=unix gives "branch@{seconds}": the time of the reflog entry, not of the commit.
    line=$("${G[@]}" reflog show --date=unix --format='%gd%x09%gs' "refs/heads/$name" -- 2>/dev/null | tail -n1 || true)
    case ${line#*$'\t'} in
      "branch: Created from "*)
        created=$(printf '%s' "${line%%$'\t'*}" | sed -n 's/.*@{\([0-9]*\)}$/\1/p')
        from=${line#*branch: Created from } ;;
    esac
  fi
  printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' "$name" "$tip" "$ts" "$kind" "$cur" "$created" "$from" >> "$WORK/branches.tsv"
done < "$WORK/branches.cur"
NBR=$(wc -l < "$WORK/branches.tsv" | tr -d ' ')

# Which commits each branch reaches (with merges, to know which branches contain others).
i=0
while IFS=$'\t' read -r name tip rest; do
  "${G[@]}" rev-list "$tip" -- ${PATHSPEC[@]+"${PATHSPEC[@]}"} > "$WORK/mem.$i" || die "$(tx revlist_failed "$name")"
  i=$((i + 1))
done < "$WORK/branches.tsv"
cut -f2 "$WORK/branches.tsv" | sort -u > "$WORK/tips"
"${G[@]}" rev-list --merges --stdin -- ${PATHSPEC[@]+"${PATHSPEC[@]}"} < "$WORK/tips" > "$WORK/merges" || die "$(tx merges_failed)"

# Date and author of each commit: those already in the cache are not asked to git again.
cat "$WORK"/mem.* | sort -u > "$WORK/union"
awk -F'\t' 'FILENAME == ARGV[1] { M[$1] = 1; next } FILENAME == ARGV[2] { C[$1] = 1; next } !($1 in M) && !($1 in C)' \
  "$WORK/merges" "$WORK/acache" "$WORK/union" > "$WORK/need"
: > "$WORK/meta.new"
if [ -s "$WORK/need" ]; then
  "${G[@]}" log --no-walk=unsorted --stdin --no-color '--format=%H%x1f%ct%x1f%aN%x1f%aE' < "$WORK/need" | act_lines > "$WORK/meta.new" \
    || die "$(tx actlog_failed)"
fi
awk -F'\t' 'FILENAME == ARGV[1] { U[$1] = 1; next } ($1 in U) && !s[$1]++' "$WORK/union" "$WORK/acache" "$WORK/meta.new" > "$WORK/meta"
ACT_NOTE=$(tx act_note "$(wc -l < "$WORK/meta.new" | tr -d ' ')")

# For each commit without merges: date, author and the branches it belongs to (bits, 30 per word).
# For each branch: commits, own commits (those reached by no branch that does not contain it: its
# work since the last tip it builds on) and the date of the first of them (its approximate start).
AWK_BRANCHES=$(cat <<'AWK'
function rep(s, a, b,   o, i) {
  o = ""
  while ((i = index(s, a)) > 0) { o = o substr(s, 1, i - 1) b; s = substr(s, i + length(a)) }
  return o s
}
function jesc(s) {
  s = rep(s, "\\", "\\\\"); s = rep(s, "\"", "\\\""); s = rep(s, "<", "\\u003c")
  gsub(/[\001-\037\177]/, " ", s)
  return s
}
function hasbit(h, i) { return int(MW[h, int(i / 30)] / P2[i % 30]) % 2 }
BEGIN {
  FS = "\t"
  for (k = 0; k < 30; k++) P2[k] = 2 ^ k
  nb = 0
  while ((getline ln < brf) > 0) {
    split(ln, f, "\t")
    BN[nb] = f[1]; BT[nb] = f[2]; BTS[nb] = f[3]; BK[nb] = f[4]; BC[nb] = f[5]; BCR[nb] = f[6]; BFR[nb] = f[7]
    if (f[5] == 1) REVI = nb
    nb++
  }
  W = int((nb + 29) / 30); if (W < 1) W = 1
  for (i = 0; i < nb; i++) {
    fn = memdir "/mem." i
    while ((getline h < fn) > 0) MW[h, int(i / 30)] += P2[i % 30]   # each commit comes once per branch
    close(fn)
  }
  # C "contains" B if the commit B points to is in C's history.
  for (b = 0; b < nb; b++) for (c = 0; c < nb; c++) if (c != b && !hasbit(BT[b], c)) NC[b, c] = 1
  na = 0
}
{
  h = $1; ts = $2 + 0
  if (mints != "" && ts < mints + 0) next
  if (maxts != "" && ts > maxts + 0) next
  ns = 0; words = ""
  for (w = 0; w < W; w++) words = words "\t" (MW[h, w] + 0)
  for (b = 0; b < nb; b++) if (hasbit(h, b)) S[++ns] = b
  if (!ns) next
  k = tolower($4); if (k == "") k = "\001" tolower($3)
  if (!(k in AI)) { AI[k] = na; AT[na] = -1; na++ }
  a = AI[k]
  if (ts > AT[a]) { AT[a] = ts; AN[a] = $3; AE[a] = $4 }
  print ts "\t" a words > logf
  for (x = 1; x <= ns; x++) {
    b = S[x]; TOT[b]++
    own = 1
    for (y = 1; y <= ns; y++) if ((b, S[y]) in NC) { own = 0; break }
    if (own) { OWN[b]++; if (!(b in OS) || ts < OS[b]) OS[b] = ts }
  }
  if (hasbit(h, REVI)) print > revf
}
END {
  # [name, commit, commit date, commits, own, start, created (reflog), from, in --rev, current, kind]
  printf "[" > bjf
  for (b = 0; b < nb; b++)
    printf "%s[\"%s\",\"%s\",%d,%d,%d,%s,%s,\"%s\",%d,%d,\"%s\"]", (b ? "," : ""), jesc(BN[b]), BT[b], BTS[b], TOT[b], OWN[b],
      ((b in OS) ? OS[b] : "null"), (BCR[b] != "" ? BCR[b] : "null"), jesc(BFR[b]), hasbit(BT[b], REVI), BC[b], BK[b] > bjf
  printf "]" > bjf
  printf "{\"words\":%d,\"authors\":[", W > auf
  for (a = 0; a < na; a++) printf "%s[\"%s\",\"%s\"]", (a ? "," : ""), jesc(AN[a]), jesc(AE[a]) > auf
  printf "]," > auf
}
AWK
)
: > "$WORK/log.tsv"; : > "$WORK/act.all"
awk -v brf="$WORK/branches.tsv" -v memdir="$WORK" -v mints="$ACT_MIN" -v maxts="$ACT_MAX" \
    -v logf="$WORK/log.tsv" -v revf="$WORK/act.all" -v bjf="$WORK/branches.json" -v auf="$WORK/log.head" \
    "$AWK_BRANCHES" "$WORK/meta" || die "$(tx branches_failed)"
{
  cat "$WORK/log.head"
  printf '"commits":['
  sort -n -k1,1 "$WORK/log.tsv" | tr '\t' ',' | awk '{ printf "%s%s", (NR > 1 ? "," : ""), $0 }'
  printf ']}'
} > "$WORK/log.json"

AWK_ACTIVITY=$(cat <<'AWK'
function rep(s, a, b,   o, i) {
  o = ""
  while ((i = index(s, a)) > 0) { o = o substr(s, 1, i - 1) b; s = substr(s, i + length(a)) }
  return o s
}
function jesc(s) {
  s = rep(s, "\\", "\\\\"); s = rep(s, "\"", "\\\""); s = rep(s, "<", "\\u003c")
  gsub(/[\001-\037\177]/, " ", s)
  return s
}
# Day of the month of a day counted from 1970-01-01 (civil calendar, H. Hinnant's algorithm).
function mday(z,   era, doe, yoe, doy, mp) {
  z += 719468
  era = int(z / 146097); doe = z - era * 146097
  yoe = int((doe - int(doe / 1460) + int(doe / 36524) - int(doe / 146096)) / 365)
  doy = doe - (365 * yoe + int(yoe / 4) - int(yoe / 100))
  mp = int((5 * doy + 2) / 153)
  return doy - int((153 * mp + 2) / 5) + 1
}
BEGIN { FS = "\t"; n = 0; na = 0 }
($1 in SEEN) { next }
{
  SEEN[$1] = 1; ts = $2 + 0
  if (mints != "" && ts < mints + 0) next
  if (maxts != "" && ts > maxts + 0) next
  day = int(ts / 86400); n++
  WD[(day + 3) % 7]++                      # 1970-01-01 was a Thursday; 0 = Monday
  HR[int((ts - day * 86400) / 3600)]++
  MD[mday(day)]++
  DAY[day]++
  if (n == 1 || day < d0) d0 = day
  if (n == 1 || day > d1) d1 = day
  # Authors grouped by email (by name when there is no email); the most recent name is shown.
  k = tolower($4); if (k == "") k = "\001" tolower($3)
  if (!(k in AC)) { AK[++na] = k; AF[k] = day; AL[k] = day; AT[k] = -1 }
  AC[k]++
  if (ts > AT[k]) { AT[k] = ts; AN[k] = $3; AE[k] = $4 }
  if (day < AF[k]) AF[k] = day
  if (day > AL[k]) AL[k] = day
  if ((k, day) in AD) AD[k, day]++; else { AD[k, day] = 1; ADL[k] = ADL[k] " " day }
}
END {
  printf "{\"tz\":\"UTC\",\"commits\":%d,\"authors\":%d,", n, na
  if (n) printf "\"firstDay\":%d,\"lastDay\":%d,\"avgPerDay\":%.2f,", d0, d1, n / (d1 - d0 + 1)
  else printf "\"firstDay\":null,\"lastDay\":null,\"avgPerDay\":0,"
  printf "\"weekday\":["; for (i = 0; i < 7; i++) printf "%s%d", (i ? "," : ""), WD[i]
  printf "],\"hour\":["; for (i = 0; i < 24; i++) printf "%s%d", (i ? "," : ""), HR[i]
  printf "],\"monthDay\":["; for (i = 1; i <= 31; i++) printf "%s%d", (i > 1 ? "," : ""), MD[i]
  printf "],\"days\":["; sep = ""
  if (n) for (d = d0; d <= d1; d++) if (d in DAY) { printf "%s%d,%d", sep, d, DAY[d]; sep = "," }
  printf "],"
  # One line per author, with its sort key in front (sorted outside with sort).
  for (i = 1; i <= na; i++) {
    k = AK[i]; m = split(substr(ADL[k], 2), dl, " "); v = ""
    for (j = 1; j <= m; j++) v = v (j > 1 ? "," : "") dl[j] "," AD[k, dl[j]]
    printf "%d\t%s\t  [\"%s\",\"%s\",%d,%d,%d,[%s]]\n", AC[k], tolower(AN[k]), jesc(AN[k]), jesc(AE[k]), AC[k], AF[k], AL[k], v > pf
  }
}
AWK
)
: > "$WORK/people.tsv"
awk -v mints="$ACT_MIN" -v maxts="$ACT_MAX" -v pf="$WORK/people.tsv" "$AWK_ACTIVITY" "$WORK/act.all" > "$WORK/activity.head" \
  || die "$(tx activity_failed)"
{
  cat "$WORK/activity.head"
  printf '"people":[\n'
  sort -t "$(printf '\t')" -k1,1nr -k2,2 "$WORK/people.tsv" | cut -f3- | awk '{ printf "%s%s", (NR > 1 ? ",\n" : ""), $0 }'
  printf '\n]}'
} > "$WORK/activity.json"
ACT_COMMITS=$(tx act_commits $(sed -n 's/^{"tz":"UTC","commits":\([0-9]*\),"authors":\([0-9]*\).*/\1 \2/p' "$WORK/activity.head"))
log "$(tx phase4 "$ACT_COMMITS" "${REV_BRANCH:-$REV}" "$NBR" "$(wc -l < "$WORK/log.tsv" | tr -d ' ')" "$ACT_NOTE" "$(fmt_dur $((SECONDS - T_PHASE)))")"

# ---------------------------------------------------------------------------
# 5. Replay the log: totals per language at every commit -> JSON
# ---------------------------------------------------------------------------
AWK_REPLAY=$(cat <<'AWK'
function rep(s, a, b,   o, i) {
  o = ""
  while ((i = index(s, a)) > 0) { o = o substr(s, 1, i - 1) b; s = substr(s, i + length(a)) }
  return o s
}
function jesc(s) {
  s = rep(s, "\\", "\\\\"); s = rep(s, "\"", "\\\""); s = rep(s, "<", "\\u003c")
  gsub(/[\001-\037\177]/, " ", s)
  return s
}
function acc(k, s, sg,   l) {
  l = LG[k]
  if (!(l in LI)) { LI[l] = nl; LN[nl] = l; nl++ }
  TF[l] += sg; TB[l] += sg * BL[k]; TC[l] += sg * CM[k]; TD[l] += sg * CD[k]; TZ[l] += sg * SZ[s]
}
# Like cloc: the same content (blob) at several paths counts only once.
function add(p, k, s) {
  if (p in PK) rem(p)
  if (!(k in LG)) return
  PK[p] = k; PS[p] = s
  if (dups) { acc(k, s, 1); return }
  if (CNT[s]++ == 0) { REP[s] = k; acc(k, s, 1) }
}
function rem(p,   k, s) {
  if (!(p in PK)) return
  k = PK[p]; s = PS[p]; delete PK[p]; delete PS[p]
  if (dups) { acc(k, s, -1); return }
  if (--CNT[s] == 0) { acc(REP[s], s, -1); delete REP[s]; delete CNT[s] }
}
# Each language: index, files, blank, comments, code, bytes (format 2).
function emit(   l, v, sep, f, b, c, k, z, s) {
  v = ""; sep = ""; f = b = c = k = z = 0
  for (l in TF) if (TF[l] > 0) {
    v = v sep LI[l] "," TF[l] "," TB[l] "," TC[l] "," TD[l] "," TZ[l]; sep = ","
    f += TF[l]; b += TB[l]; c += TC[l]; k += TD[l]; z += TZ[l]
  }
  printf "%s[\"%s\",%s,\"%s\",\"%s\",%d,[%s]]", (cur > 1 ? ",\n" : ""), H[cur], T[cur], jesc(A[cur]), jesc(S[cur]), nch, v
  # Cache C line: the analyzed commit with its totals.
  s = S[cur]; gsub(/[\t\r\n]/, " ", s)
  printf "C\t%s\t%s\t%d\t%d\t%d\t%d\t%d\t%s\n", H[cur], D[cur], f, b, c, k, z, s > cf
}
BEGIN {
  FS = "\t"
  while ((getline ln < statsf) > 0) {
    split(ln, a, "\t"); LG[a[1]] = a[2]; BL[a[1]] = a[3] + 0; CM[a[1]] = a[4] + 0; CD[a[1]] = a[5] + 0
  }
  while ((getline ln < sizesf) > 0) { split(ln, a, "\t"); SZ[a[1]] = a[2] + 0 }
  n = 0
  while ((getline ln < commitsf) > 0) { n++; split(ln, m, "\037"); H[n] = m[1]; T[n] = m[3] + 0; A[n] = m[4]; S[n] = m[5]; D[n] = m[6] }
  nl = 0; cur = 0
}
$1 == "@" { if (cur) emit(); cur = $2 + 0; nch = 0; next }
$1 == "+" { nch++; add($2, $3, $4); next }
$1 == "-" { nch++; rem($2); next }
END {
  if (cur) emit()
  printf "[" > langsf
  for (i = 0; i < nl; i++) printf "%s\"%s\"", (i ? "," : ""), jesc(LN[i]) > langsf
  printf "]" > langsf
  if (cur != n) { printf m_replay "\n", cur, n > "/dev/stderr"; exit 4 }
  # Languages of the last commit with their % of total lines, code and bytes (languages chart):
  # [index, files, blank, comments, code, bytes, % lines, % code, % bytes]
  tl = tk = tz = 0
  for (l in TF) if (TF[l] > 0) { tl += TB[l] + TC[l] + TD[l]; tk += TD[l]; tz += TZ[l] }
  printf "[" > lsf; sep = ""
  for (l in TF) if (TF[l] > 0) {
    printf "%s[%d,%d,%d,%d,%d,%d,%s,%s,%s]", sep, LI[l], TF[l], TB[l], TC[l], TD[l], TZ[l],
      pct(TB[l] + TC[l] + TD[l], tl), pct(TD[l], tk), pct(TZ[l], tz) > lsf
    sep = ","
  }
  printf "]" > lsf
}
function pct(x, t) { return t > 0 ? sprintf("%.2f", x * 100 / t) : "0" }
AWK
)

awk -v statsf="$WORK/stats" -v commitsf="$WORK/commits" -v langsf="$WORK/langs.json" -v cf="$WORK/clines" \
    -v sizesf="$WORK/sizes" -v lsf="$WORK/languages.json" -v dups="$COUNT_DUPS" "${AWK_MSGS[@]}" "$AWK_REPLAY" "$WORK/events" > "$WORK/rows.json" \
  || die "$(tx replay_failed)"

mapfile_compat() {  # reads a file into an array (bash 3 has no mapfile)
  local __name=$1 __file=$2 __line __i=0
  eval "$__name=()"
  while IFS= read -r __line || [ -n "$__line" ]; do eval "$__name[$__i]=\$__line"; __i=$((__i + 1)); done < "$__file"
}
mapfile_compat EXCLUDE_LIST "$WORK/exclude.re"
mapfile_compat INCLUDE_LIST "$WORK/include.re"

if [ "$COUNT_DUPS" = 1 ]; then DUPS_JSON=true; else DUPS_JSON=false; fi
if [ "$FIRST_PARENT" = 1 ]; then FP_JSON=true; else FP_JSON=false; fi
GENERATED=$(date '+%Y-%m-%dT%H:%M:%S%z')
ELAPSED=$(( $(date +%s) - START_TS ))

{
  printf '{"meta":{'
  if [ -n "$TITLE" ]; then CUSTOM_TITLE=true; else CUSTOM_TITLE=false; fi
  printf '"repo":%s,"title":%s,"customTitle":%s,"rev":%s,"tip":%s,' "$(json_str "$REPO_NAME")" "$(json_str "$TITLE")" "$CUSTOM_TITLE" "$(json_str "$REV")" "$(json_str "$TIP")"
  printf '"firstParent":%s,"step":%s,"maxCommits":%s,"since":%s,"until":%s,' "$FP_JSON" "$STEP" "${MAX_COMMITS:-null}" "$(json_str "$SINCE")" "$(json_str "$UNTIL")"
  printf '"pathspec":%s,"excludes":%s,"includes":%s,"filter":%s,' \
    "$(json_arr ${PATHSPEC[@]+"${PATHSPEC[@]}"})" "$(json_arr ${EXCLUDE_LIST[@]+"${EXCLUDE_LIST[@]}"})" \
    "$(json_arr ${INCLUDE_LIST[@]+"${INCLUDE_LIST[@]}"})" "$(json_str "$FILTER_CMD")"
  printf '"includeLang":%s,"excludeLang":%s,"countDuplicates":%s,"branches":%s,' "$(json_str "$INCLUDE_LANG")" "$(json_str "$EXCLUDE_LANG")" "$DUPS_JSON" "$(json_str "$BRANCHES")"
  printf '"format":4,"cloc":%s,"generated":%s,"elapsed":%s,"totalCommits":%s,"firstIsRoot":%s},' \
    "$(json_str "$CLOC_VERSION")" "$(json_str "$GENERATED")" "$ELAPSED" "$TOTAL_COMMITS" "$FIRST_IS_ROOT"
  printf '"langs":'; cat "$WORK/langs.json"
  printf ',"commits":[\n'; cat "$WORK/rows.json"; printf '\n],\n'
  # Blocks that are not commit rows (they do not start with '["', so AWK_PCT leaves them as they are).
  printf '"languages":'; cat "$WORK/languages.json"; printf ',\n'
  printf '"activity":'; cat "$WORK/activity.json"; printf ',\n'
  printf '"branches":'; cat "$WORK/branches.json"; printf ',\n'
  printf '"log":'; cat "$WORK/log.json"; printf '\n}'
} | awk "$AWK_PCT" > "$WORK/data.json"
PHASE_HTML='[5/5]'
fi

# --no-emails: the report keeps no author email. Authors are already grouped by email at this
# point; only the two places that carry them are blanked ("email" -> ""):
#   activity.people  one author per line:  ["name","email",commits,first,last,[days]],
#   log.authors      on the log line:      "authors":[["name","email"],...],"commits":[...]
# (commit rows carry the author's name only). Works on the data of a --rebuild as well.
AWK_NOEMAIL=$(cat <<'AWK'
# Copies ["name","email" from the start of s to OUT with the email blanked; returns the rest of s.
function cut_email(s) {
  if (!match(s, /^\["([^"\\]|\\.)*","/)) { OUT = OUT s; return "" }
  OUT = OUT substr(s, 1, RLENGTH); s = substr(s, RLENGTH + 1)
  if (!match(s, /^([^"\\]|\\.)*"/)) { OUT = OUT s; return "" }
  OUT = OUT "\""; return substr(s, RLENGTH + 1)
}
people && /^\]/ { people = 0 }
people && /^  \["/ { OUT = "  "; s = cut_email(substr($0, 3)); print OUT s; next }
/"people":\[$/ { people = 1 }
(i = index($0, "\"authors\":[")) > 0 {
  j = index(substr($0, i), "],\"commits\":")
  if (j) {
    OUT = substr($0, 1, i + 10); s = substr($0, i + 11, j - 12)   # s = what is inside "authors":[ ... ]
    while (substr(s, 1, 1) == "[") {
      s = cut_email(s)                                             # s = ] , [ ... or ]
      OUT = OUT substr(s, 1, 1); s = substr(s, 2)
      if (substr(s, 1, 1) == ",") { OUT = OUT ","; s = substr(s, 2) }
    }
    print OUT s substr($0, i + j - 1); next
  }
}
{ print }
AWK
)
if [ "$NO_EMAILS" = 1 ]; then
  awk "$AWK_NOEMAIL" "$WORK/data.json" > "$WORK/data.noemail" || die "$(tx noemail_failed)"
  mv -f "$WORK/data.noemail" "$WORK/data.json"
fi

# ---------------------------------------------------------------------------
# HTML
# ---------------------------------------------------------------------------
log "$(tx phase5 "$PHASE_HTML" "$NCOMMITS" "$MAIN_OUT")"

html_head() {
cat <<'EOF'
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
EOF
printf '<title>%s</title>\n' "$TITLE_HTML"
cat <<'EOF'
<script>
  // Theme chosen earlier in this browser (otherwise the system one), applied before painting.
  try { var t = localStorage.getItem('cloc-history.theme'); if (t === 'dark' || t === 'light') document.documentElement.dataset.theme = t; } catch (e) { /* no storage */ }

  // Published through gistpreview.github.io ("?<gist id>/<file>"): that page document.write()s the
  // file as the GitHub API returns it, and the API cuts long files (an --embed copy of a big
  // history), so the end of the page (data, D3 and the app) never arrives. Here, ahead of all that:
  // if the app has not started once that write is done and the API says the file was cut, the
  // whole file is read from its raw URL and written again (only once). The app also uses this to
  // find its .json in the same gist.
  (function () {
    var m = /^\?([0-9a-f]{20,})(?:\/([^?#]*))?/i.exec(location.search);
    if (!m) return;
    var api = null;
    var gist = window.CLOC_HISTORY_GIST = {
      file: m[2] ? decodeURIComponent(m[2]) : '',
      info: function () {
        return api || (api = fetch('https://api.github.com/gists/' + m[1]).then(function (r) {
          if (!r.ok) throw new Error('GitHub API: HTTP ' + r.status);
          return r.json();
        }));
      }
    };
    if (window.CLOC_HISTORY_FULL) return;
    setTimeout(function () {
      if (window.CLOC_HISTORY_STARTED) return;
      gist.info().then(function (info) {
        var files = info.files || {}, name = gist.file;
        if (!name) for (var k in files) if (!name || k === 'index.html') name = k;   // as gistpreview does
        var f = files[name];
        if (!f || !f.truncated || !f.raw_url) return null;
        return fetch(f.raw_url).then(function (r) {
          if (!r.ok) throw new Error(f.raw_url + ': HTTP ' + r.status);
          return r.text();
        });
      }).then(function (html) {
        if (!html || window.CLOC_HISTORY_STARTED || window.CLOC_HISTORY_FULL) return;
        window.CLOC_HISTORY_FULL = true;
        document.open();
        document.write(html.replace(/<head>/i, '<head><script>window.CLOC_HISTORY_FULL = true;<\/script>'));
        document.close();
      }).catch(function (e) { if (window.console) console.error('cloc-history: ' + e.message); });
    }, 0);
  })();
</script>
<style>
:root {
  color-scheme: light;
  --page: #f9f9f7; --surface: #fcfcfb;
  --ink: #0b0b0b; --ink-2: #52514e; --muted: #898781;
  --grid: #e1e0d9; --axis: #c3c2b7;
  --border: rgba(11, 11, 11, 0.10); --wash: rgba(11, 11, 11, 0.06);
  --s1: #2a78d6; --s2: #eb6834; --s3: #1baf7a; --s4: #eda100;
  --s5: #e87ba4; --s6: #008300; --s7: #4a3aa7; --s8: #e34948;
  --other: #b5b4ad;
  --up: #1a7f37; --down: #c62828;
  --shadow: 0 6px 24px rgba(0, 0, 0, 0.12);
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    color-scheme: dark;
    --page: #0d0d0d; --surface: #1a1a19;
    --ink: #ffffff; --ink-2: #c3c2b7; --muted: #898781;
    --grid: #2c2c2a; --axis: #383835;
    --border: rgba(255, 255, 255, 0.10); --wash: rgba(255, 255, 255, 0.08);
    --s1: #3987e5; --s2: #d95926; --s3: #199e70; --s4: #c98500;
    --s5: #d55181; --s6: #008300; --s7: #9085e9; --s8: #e66767;
    --other: #55544f;
    --up: #3fb950; --down: #f87171;
    --shadow: 0 6px 24px rgba(0, 0, 0, 0.5);
  }
}
:root[data-theme="dark"] {
  color-scheme: dark;
  --page: #0d0d0d; --surface: #1a1a19;
  --ink: #ffffff; --ink-2: #c3c2b7; --muted: #898781;
  --grid: #2c2c2a; --axis: #383835;
  --border: rgba(255, 255, 255, 0.10); --wash: rgba(255, 255, 255, 0.08);
  --s1: #3987e5; --s2: #d95926; --s3: #199e70; --s4: #c98500;
  --s5: #d55181; --s6: #008300; --s7: #9085e9; --s8: #e66767;
  --other: #55544f;
  --up: #3fb950; --down: #f87171;
  --shadow: 0 6px 24px rgba(0, 0, 0, 0.5);
}
* { box-sizing: border-box; }
html, body { margin: 0; background: var(--page); color: var(--ink); }
body { font: 14px/1.45 system-ui, -apple-system, "Segoe UI", sans-serif; }
.page { max-width: 1280px; margin: 0 auto; padding: 24px 24px 48px; }
h1 { font-size: 22px; font-weight: 600; margin: 0 0 4px; }
h2 { font-size: 15px; font-weight: 600; margin: 0; }
.sub { color: var(--ink-2); margin: 0; }
.filters { display: flex; flex-wrap: wrap; gap: 12px 16px; align-items: flex-end; margin: 20px 0 16px; }
.filters label, .filters .field { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--ink-2); }
/* Language picker: a button that opens a panel with one checkbox per language. */
.lang-pick { position: relative; }
#f-lang { display: inline-flex; align-items: center; justify-content: space-between; gap: 8px; min-width: 180px; max-width: 300px; }
#f-lang .txt { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
#f-lang .chev { color: var(--muted); font-size: 11px; }
.lang-panel {
  position: absolute; left: 0; top: calc(100% + 4px); z-index: 20; min-width: 100%; width: max-content; max-width: min(340px, 90vw);
  max-height: min(420px, 60vh); overflow-y: auto; padding: 4px; font-size: 13px; color: var(--ink);
  background: var(--surface); border: 1px solid var(--border); border-radius: 10px; box-shadow: var(--shadow);
}
.filters .lang-panel label {
  flex-direction: row; align-items: center; gap: 8px; flex: 1; min-width: 0; padding: 6px 8px; border-radius: 6px;
  font-size: 13px; color: var(--ink); cursor: pointer;
}
.filters .lang-panel label:hover { background: var(--wash); }
.lang-panel .pick-all { font-weight: 600; border-bottom: 1px solid var(--grid); border-radius: 6px 6px 0 0; margin-bottom: 4px; }
.lang-panel input[type="checkbox"] { margin: 0; accent-color: var(--s1); flex: none; }
.lang-panel input[type="checkbox"]:disabled { cursor: default; }
.lang-panel .nm { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.pick-row { display: flex; align-items: center; gap: 4px; }
.pick-row .only { border: 0; background: none; min-height: 0; padding: 2px 6px; font-size: 12px; color: var(--ink-2); visibility: hidden; }
.pick-row:hover .only, .pick-row:focus-within .only { visibility: visible; }
.pick-row .only:hover { color: var(--ink); text-decoration: underline; }
@media (hover: none) { .pick-row .only { visibility: visible; } }
select, button {
  font: inherit; color: var(--ink); background: var(--surface);
  border: 1px solid var(--border); border-radius: 8px; padding: 6px 10px; min-height: 34px;
}
button { cursor: pointer; }
select:hover, button:hover { border-color: var(--axis); }
:focus-visible { outline: 2px solid var(--s1); outline-offset: 2px; }
.kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; margin-bottom: 16px; }
.tile { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 12px 14px; }
.tile .k { font-size: 12px; color: var(--ink-2); }
.tile .v { font-size: 24px; font-weight: 600; margin-top: 2px; }
.tile .d { font-size: 12px; color: var(--ink-2); font-variant-numeric: tabular-nums; }
/* Change from the previous commit: green when it adds, red when it takes away (▲/▼ and the sign say it too). */
.tile .d.delta-up, td.delta-up, .tooltip .delta-up { color: var(--up); }
.tile .d.delta-down, td.delta-down, .tooltip .delta-down { color: var(--down); }
.card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 16px; margin-bottom: 16px; }
.hint { color: var(--muted); font-size: 12px; margin: 2px 0 0; }
.legend { display: flex; flex-wrap: wrap; gap: 4px 16px; margin: 12px 0 4px; }
.legend button {
  border: 0; background: none; padding: 2px 0; min-height: 0; border-radius: 4px;
  display: inline-flex; align-items: center; gap: 6px; color: var(--ink-2); font-size: 13px;
}
.legend button[aria-pressed="false"] { opacity: 0.45; text-decoration: line-through; }
.legend button[aria-pressed="mixed"] { opacity: 0.75; }
.sw { width: 10px; height: 10px; border-radius: 2px; flex: none; display: inline-block; }
.chart { position: relative; border-radius: 4px; }
.chart:focus { outline: none; }
.chart:focus-visible { outline: 2px solid var(--s1); outline-offset: 4px; }
.chart svg, .overview svg, #bars svg { display: block; }
.grid line { stroke: var(--grid); shape-rendering: crispEdges; }
.grid .domain { display: none; }
.grid text, .xaxis text, .ov-label { fill: var(--muted); font-size: 11px; font-variant-numeric: tabular-nums; }
.xaxis line { stroke: var(--axis); shape-rendering: crispEdges; }
.baseline { stroke: var(--axis); shape-rendering: crispEdges; }
.hover-col { fill: var(--wash); }
.sel-line { stroke: var(--ink-2); stroke-width: 1; shape-rendering: crispEdges; pointer-events: none; }
.overlay { fill: transparent; cursor: crosshair; touch-action: pan-y; }
.zoom-sel { fill: var(--s1); fill-opacity: 0.12; stroke: var(--s1); stroke-opacity: 0.6; pointer-events: none; }
.overview { margin-top: 10px; }
.ov-area { fill: var(--ink-2); fill-opacity: 0.14; }
.ov-line { fill: none; stroke: var(--ink-2); stroke-width: 1; }
.brush .selection { fill: var(--s1); fill-opacity: 0.06; stroke: var(--s1); stroke-opacity: 0.5; }
.ov-shade { fill: var(--surface); fill-opacity: 0.65; pointer-events: none; }
.ov-handles { pointer-events: none; }
.ov-handle rect { fill: var(--surface); stroke: var(--ink-2); stroke-width: 1; }
.ov-handle line { stroke: var(--ink-2); stroke-width: 1; shape-rendering: crispEdges; }
.empty { color: var(--muted); padding: 24px 0; }
.load-panel { border-style: dashed; margin-top: 16px; }
.page.waiting > :not(header):not(#load-panel) { display: none; }
.load-panel.over { border-color: var(--s1); background: var(--wash); }
.load-panel p { margin: 6px 0 10px; color: var(--ink-2); }
.load-panel code { font-family: ui-monospace, SFMono-Regular, Consolas, monospace; color: var(--ink); }
.tooltip {
  position: fixed; z-index: 10; pointer-events: none; left: 0; top: 0;
  background: var(--surface); color: var(--ink); border: 1px solid var(--border); border-radius: 10px;
  padding: 10px 12px; box-shadow: var(--shadow); font-size: 12px; width: max-content; max-width: 340px;
}
.tooltip .t-head { font-weight: 600; font-variant-numeric: tabular-nums; }
.tooltip .t-meta { color: var(--ink-2); }
.tooltip .t-subj { color: var(--ink-2); margin-bottom: 6px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.tooltip .t-row { display: grid; grid-template-columns: 12px minmax(56px, auto) 1fr; align-items: center; gap: 8px; line-height: 1.6; }
.tooltip .t-key { width: 12px; height: 2px; border-radius: 1px; }
.tooltip .t-val { font-weight: 600; text-align: right; font-variant-numeric: tabular-nums; }
.tooltip .t-name { color: var(--ink-2); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.tooltip .t-total { border-top: 1px solid var(--grid); margin-top: 4px; padding-top: 4px; }
.tooltip .t-foot { color: var(--muted); margin-top: 4px; }
.detail-head { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: flex-start; gap: 12px; }
.detail-head > div:first-child { flex: 1 1 320px; min-width: 0; }
.detail-head .meta { color: var(--ink-2); margin: 2px 0 0; font-variant-numeric: tabular-nums; }
.detail-head .subject { margin: 4px 0 0; }
.detail-head .meta, .detail-head .subject { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.detail-head .is-cut { cursor: help; }
.tooltip .t-full { overflow-wrap: anywhere; }
.player { display: flex; gap: 6px; align-items: center; flex-wrap: wrap; }
.player button { min-width: 38px; }
.player label { font-size: 12px; color: var(--ink-2); display: inline-flex; gap: 6px; align-items: center; }
.detail-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.35fr); gap: 24px; margin-top: 16px; }
#bars .lab { fill: var(--ink-2); font-size: 12px; }
#bars .val { fill: var(--ink); font-size: 12px; font-variant-numeric: tabular-nums; }
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 12.5px; font-variant-numeric: tabular-nums; }
th, td { padding: 6px 7px; border-bottom: 1px solid var(--grid); text-align: right; white-space: nowrap; }
th:first-child, td:first-child { text-align: left; }
td:first-child { max-width: 180px; overflow: hidden; text-overflow: ellipsis; }
th { color: var(--ink-2); font-weight: 600; }
/* Sortable headers: a button that does not look like one; the arrow always keeps its slot so the text does not jump. */
th button {
  font: inherit; color: inherit; background: none; border: 0; border-radius: 4px; padding: 0; min-height: 0;
  cursor: pointer; display: inline-flex; align-items: center; gap: 3px;
}
th button:hover { color: var(--ink); }
th button::after { content: ''; display: inline-block; width: 9px; font-size: 9px; color: var(--muted); }
th button:hover::after { content: '↕'; }
th[aria-sort="ascending"] button::after { content: '▲'; color: var(--ink-2); }
th[aria-sort="descending"] button::after { content: '▼'; color: var(--ink-2); }
tfoot td { font-weight: 600; border-bottom: 0; }
td .sw { margin-right: 6px; vertical-align: -1px; }
tr.focus td { background: var(--wash); }
.config summary { cursor: pointer; font-weight: 600; }
.config dl { display: grid; grid-template-columns: max-content minmax(0, 1fr); gap: 6px 16px; margin: 12px 0 0; }
.config dt { color: var(--ink-2); }
.config dd { margin: 0; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 12px; overflow-wrap: anywhere; }
.act-window { margin: 12px 0 10px; }
#branch-filter { margin: 12px 0 0; }
.toc { align-items: center; }
.toc .check { margin-left: auto; }
.check { display: inline-flex; align-items: center; gap: 6px; font-size: 12px; color: var(--ink-2); cursor: pointer; }
.branch-marks line { stroke: var(--ink-2); stroke-opacity: 0.35; shape-rendering: crispEdges; }
.branch-marks .flag { fill: var(--ink-2); fill-opacity: 0.7; }
.bmark:hover line { stroke-opacity: 0.9; }
.bmark.is-current line { stroke: var(--s1); stroke-opacity: 0.9; }
.bmark.is-current .flag { fill: var(--s1); fill-opacity: 1; }
.bmark-hit { fill: transparent; cursor: help; }
.tooltip .t-branch + .t-branch { margin-top: 6px; }
#branch-table td:first-child { max-width: none; }
#branch-table th:nth-child(3), #branch-table td:nth-child(3), #branch-table th:nth-child(7), #branch-table td:nth-child(7) { text-align: left; }
#branch-table td.rel { white-space: normal; min-width: 220px; }
#branch-table .link-btn { color: var(--ink); text-decoration-color: var(--axis); }
.tag { display: inline-block; margin-left: 6px; padding: 0 6px; border-radius: 999px; font-size: 11px; color: var(--ink-2); border: 1px solid var(--border); }
.muted { color: var(--muted); }
.link-btn { border: 0; background: none; padding: 0; min-height: 0; color: var(--s1); font: inherit; cursor: pointer; text-decoration: underline; text-underline-offset: 2px; }
.head { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px 24px; }
.head-text { min-width: 0; }
.head-tools { display: flex; align-items: center; gap: 8px; flex: none; }
#theme-btn { display: inline-flex; align-items: center; justify-content: center; width: 34px; padding: 0; color: var(--ink-2); }
#theme-btn:hover { color: var(--ink); }
#theme-btn svg { width: 16px; height: 16px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
.lang-switch { position: relative; flex: none; }
#lang-btn { display: inline-flex; align-items: center; gap: 8px; }
#lang-btn .chev { color: var(--muted); font-size: 11px; }
.flag { width: 20px; height: 15px; border-radius: 2px; object-fit: cover; box-shadow: 0 0 0 1px var(--border); flex: none; }
#lang-list {
  position: absolute; right: 0; top: calc(100% + 4px); z-index: 20; min-width: 100%; margin: 0; padding: 4px; list-style: none;
  background: var(--surface); border: 1px solid var(--border); border-radius: 10px; box-shadow: var(--shadow);
}
#lang-list:focus { outline: none; }
#lang-list:focus-visible { outline: 2px solid var(--s1); outline-offset: 2px; }
#lang-list li { display: flex; align-items: center; gap: 8px; padding: 6px 10px; border-radius: 6px; cursor: pointer; white-space: nowrap; }
#lang-list li.active { background: var(--wash); }
#lang-list li[aria-selected="true"] { font-weight: 600; }
#lang-list .tick { margin-left: auto; padding-left: 12px; color: var(--ink-2); }
.sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
.toc { display: flex; flex-wrap: wrap; gap: 6px 18px; margin: 12px 0 0; font-size: 13px; }
.toc a { color: var(--ink-2); text-decoration: none; border-bottom: 1px solid transparent; }
.toc a:hover { color: var(--ink); border-bottom-color: var(--axis); }
.section-title { font-size: 18px; margin: 36px 0 2px; scroll-margin-top: 16px; }
.section-sub { color: var(--ink-2); margin: 0 0 4px; }
.card-head { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 8px 16px; }
.card-head label { font-size: 12px; color: var(--ink-2); display: inline-flex; gap: 6px; align-items: center; }
.grid-2 { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 16px; }
.th-host, .cat-chart { margin-top: 10px; }
.th-host svg, .cat-chart svg, #lang-chart svg { display: block; }
.th-ov { margin-top: 6px; }
.xaxis .domain { stroke: var(--axis); shape-rendering: crispEdges; }
.cat-chart .val, #lang-chart .val { fill: var(--ink); font-size: 11.5px; font-variant-numeric: tabular-nums; }
#lang-chart { margin-top: 10px; }
#lang-chart .lab { fill: var(--ink-2); font-size: 12px; }
#lang-chart .row-hit { fill: transparent; }
.data-toggle { margin-top: 8px; font-size: 12px; }
.data-toggle summary { cursor: pointer; color: var(--ink-2); width: max-content; }
.data-toggle table { margin-top: 6px; max-width: 420px; }
.contrib-card { padding: 4px 16px; }
.contrib + .contrib { border-top: 1px solid var(--grid); }
.contrib summary {
  display: grid; grid-template-columns: 44px minmax(0, 1.1fr) minmax(0, 1fr) auto; gap: 2px 12px;
  align-items: baseline; padding: 10px 6px; cursor: pointer; list-style: none; border-radius: 8px;
}
.contrib summary::-webkit-details-marker { display: none; }
.contrib summary:hover { background: var(--wash); }
.c-rank { color: var(--muted); font-variant-numeric: tabular-nums; }
.c-rank::before { content: '▸'; display: inline-block; width: 14px; transition: transform 0.15s; }
.contrib[open] .c-rank::before { transform: rotate(90deg); }
.c-name { font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.c-mail { color: var(--ink-2); font-size: 12.5px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.c-n { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
#contrib-list.no-mail .contrib summary { grid-template-columns: 44px minmax(0, 1fr) auto; }
.c-meta { grid-column: 2 / -1; color: var(--muted); font-size: 12px; }
.c-body { padding: 4px 0 16px; }
@media (max-width: 900px) {
  .detail-grid, .grid-2 { grid-template-columns: minmax(0, 1fr); }
  .contrib summary, #contrib-list.no-mail .contrib summary { grid-template-columns: 36px minmax(0, 1fr) auto; }
  .c-mail { display: none; }
}
@media (max-width: 640px) {
  .page { padding: 16px; }
  .config dl { grid-template-columns: minmax(0, 1fr); }
  .config dd { margin-bottom: 6px; }
}
</style>
</head>
<body>
<div class="page">
  <header class="head">
    <div class="head-text">
      <h1 id="title"></h1>
      <p class="sub" id="subtitle"></p>
    </div>
    <div class="head-tools">
      <button type="button" id="theme-btn" aria-pressed="false"></button>
      <div class="lang-switch">
        <span class="sr-only" id="lang-label" data-i18n="ui.language">Language</span>
        <button type="button" id="lang-btn" aria-haspopup="listbox" aria-expanded="false" aria-labelledby="lang-label lang-btn"></button>
        <ul id="lang-list" role="listbox" aria-labelledby="lang-label" tabindex="-1" hidden></ul>
      </div>
    </div>
  </header>
  <nav class="toc" data-i18n-aria-label="nav.aria" aria-label="Report sections">
    <a href="#sec-code" data-i18n="nav.code">Code history</a>
    <a href="#sec-activity" data-i18n="nav.activity">Repository activity</a>
    <a href="#sec-langs" data-i18n="nav.langs">Languages</a>
    <a href="#sec-contrib" data-i18n="nav.contrib">Contributors</a>
    <a href="#sec-branches" id="toc-branches" data-i18n="nav.branches">Branches</a>
    <label class="check" id="toc-marks"><input type="checkbox" id="show-branches" checked> <span data-i18n="nav.marks">Mark when each branch appears on the charts</span></label>
  </nav>

  <section class="card load-panel" id="load-panel" hidden>
    <h2 data-i18n="load.title">Data file missing</h2>
    <p><span data-i18n="load.text1">Could not read </span><code id="load-name"></code><span data-i18n="load.text2"> next to this HTML (was it moved without it, or does the browser block local reads?). Pick it or drop it here:</span></p>
    <input type="file" id="load-input" accept=".json,.js,application/json">
    <p class="hint" id="load-error"></p>
  </section>

  <h2 class="section-title" id="sec-code" data-i18n="code.title">Code history</h2>
  <p class="section-sub" data-i18n="code.sub">Every commit of the main line measured with cloc, using the cloc.sh criteria. The filters below only affect this section.</p>
  <div class="filters" role="group" data-i18n-aria-label="code.filters" aria-label="Code history filters">
    <label><span data-i18n="f.metric">Metric</span>
      <select id="f-metric">
        <option value="code" data-i18n="metric.code">Code</option>
        <option value="comment" data-i18n="metric.comment">Comments</option>
        <option value="blank" data-i18n="metric.blank">Blank lines</option>
        <option value="lines" data-i18n="metric.lines">Total lines</option>
        <option value="files" data-i18n="metric.files">Files</option>
        <option value="size" data-i18n="metric.size">Size on disk</option>
      </select>
    </label>
    <label><span data-i18n="f.view">View</span>
      <select id="f-mode">
        <option value="total" data-i18n="view.total">Total at each commit</option>
        <option value="delta" data-i18n="view.delta">Change from the previous commit</option>
      </select>
    </label>
    <div class="field"><span id="f-lang-label" data-i18n="f.lang">Language</span>
      <div class="lang-pick">
        <button type="button" id="f-lang" aria-haspopup="true" aria-expanded="false" aria-controls="f-lang-panel" aria-labelledby="f-lang-label f-lang"><span class="txt"></span><span class="chev" aria-hidden="true">▾</span></button>
        <div id="f-lang-panel" class="lang-panel" role="group" aria-labelledby="f-lang-label" hidden>
          <label class="pick-all"><input type="checkbox" id="f-lang-all"><span data-i18n="lang.allBox">All languages</span></label>
          <div id="f-lang-items"></div>
        </div>
      </div>
    </div>
    <button id="reset-zoom" type="button" hidden data-i18n="btn.resetZoom">Show the whole history</button>
    <button id="csv" type="button" data-i18n="btn.csv">Download CSV</button>
  </div>

  <section class="kpis" id="kpis" data-i18n-aria-label="kpis.aria" aria-label="Totals of the selected commit"></section>

  <section class="card">
    <h2 id="chart-title"></h2>
    <p class="hint" data-i18n="chart.hint">One bar per commit, in order (the X axis is not to time scale). Hover to see each commit and click to pin it; drag over the chart or in the strip below to zoom (double click: whole history). With the chart focused: ← → (Shift = 10), Home, End, Space.</p>
    <div class="legend" id="legend" data-i18n-aria-label="legend.aria" aria-label="Languages (click to hide or show)"></div>
    <div id="chart" class="chart" tabindex="0" role="img" data-i18n-aria-roledescription="chart.roledesc" aria-roledescription="stacked bar chart"></div>
    <div id="overview" class="overview" data-i18n-aria-label="overview.aria" aria-label="Whole history: drag to zoom"></div>
  </section>

  <section class="card" id="detail">
    <div class="detail-head">
      <div>
        <h2 id="d-title"></h2>
        <p class="meta" id="d-meta"></p>
        <p class="subject" id="d-subject"></p>
      </div>
      <div class="player" role="group" data-i18n-aria-label="player.aria" aria-label="Playback">
        <button type="button" id="p-first" data-i18n-title="player.first" data-i18n-aria-label="player.first" title="First commit" aria-label="First commit">⏮</button>
        <button type="button" id="p-prev" data-i18n-title="player.prev" data-i18n-aria-label="player.prev" title="Previous commit" aria-label="Previous commit">◀</button>
        <button type="button" id="p-play" data-i18n-title="player.playTitle" title="Play the history" aria-label="Play">▶ Play</button>
        <button type="button" id="p-next" data-i18n-title="player.next" data-i18n-aria-label="player.next" title="Next commit" aria-label="Next commit">▶</button>
        <button type="button" id="p-last" data-i18n-title="player.last" data-i18n-aria-label="player.last" title="Last commit" aria-label="Last commit">⏭</button>
        <label><span data-i18n="player.speed">Speed</span>
          <select id="p-speed">
            <option value="2">2 commits/s</option>
            <option value="5">5 commits/s</option>
            <option value="10" selected>10 commits/s</option>
            <option value="25">25 commits/s</option>
            <option value="50">50 commits/s</option>
            <option value="100">100 commits/s</option>
          </select>
        </label>
      </div>
    </div>
    <div class="detail-grid">
      <div id="bars" aria-hidden="true"></div>
      <div class="table-wrap">
        <table id="lang-table">
          <thead><tr>
            <th scope="col" data-sort="name"><button type="button" data-i18n="th.lang">Language</button></th>
            <th scope="col" data-sort="f"><button type="button" data-i18n="th.files">Files</button></th>
            <th scope="col" data-sort="b"><button type="button" data-i18n="th.blank">Blank</button></th>
            <th scope="col" data-sort="cm"><button type="button" data-i18n="th.comment">Comments</button></th>
            <th scope="col" data-sort="cd"><button type="button" data-i18n="th.code">Code</button></th>
            <th scope="col" data-sort="ln" data-i18n-title="th.linesTitle" title="Blank + comments + code"><button type="button" data-i18n="th.lines">Lines</button></th>
            <th scope="col" data-sort="z" data-i18n-title="th.sizeTitle" title="Bytes of the measured files (versioned content, no CRLF conversion)"><button type="button" data-i18n="th.size">Size</button></th>
            <th scope="col" data-sort="p" data-i18n-title="th.pctTitle" title="Language lines (blank + comments + code) over the commit's total lines"><button type="button" data-i18n="th.pct">% lines</button></th>
            <th scope="col" data-sort="delta" id="th-delta"><button type="button">Δ</button></th>
          </tr></thead>
          <tbody></tbody>
          <tfoot></tfoot>
        </table>
      </div>
    </div>
  </section>

  <h2 class="section-title" id="sec-activity" data-i18n="act.title">Repository activity</h2>
  <p class="section-sub" id="act-sub"></p>
  <div class="filters" id="branch-filter" role="group" data-i18n-aria-label="act.filter" aria-label="Activity filter">
    <label><span data-i18n="f.branch">Branch</span>
      <select id="f-branch"></select>
    </label>
  </div>
  <div id="act-body">
    <p class="hint act-window" id="act-window" aria-live="polite"></p>
    <section class="kpis" id="act-kpis" data-i18n-aria-label="act.kpis" aria-label="Commit statistics of the visible range"></section>
    <section class="card">
      <h2 data-i18n="act.time">Commits over time</h2>
      <p class="hint" data-i18n="act.timeHint">Grouped by day, week, month or year depending on the visible range. Drag the minimap handles to narrow the range (double click: whole history).</p>
      <div id="act-time" class="th-host"></div>
    </section>
  </div>
  <section class="card" id="sec-langs">
    <div class="card-head">
      <h2 data-i18n="langs.title">Languages</h2>
      <label><span data-i18n="langs.basis">Computed over</span>
        <select id="lang-basis">
          <option value="code" data-i18n="basis.code">Lines of code</option>
          <option value="lines" data-i18n="basis.lines">Total lines</option>
          <option value="size" data-i18n="basis.size">Size on disk</option>
        </select>
      </label>
    </div>
    <p class="hint" id="lang-sub"></p>
    <div id="lang-chart"></div>
  </section>
  <div id="act-body2">
    <section class="card">
      <h2 data-i18n="act.monthday">Commits per day of month</h2>
      <div id="act-monthday" class="cat-chart"></div>
    </section>
    <div class="grid-2">
      <section class="card">
        <h2 data-i18n="act.weekday">Commits per weekday</h2>
        <div id="act-weekday" class="cat-chart"></div>
      </section>
      <section class="card">
        <h2 data-i18n="act.hour">Commits per hour (UTC)</h2>
        <div id="act-hour" class="cat-chart"></div>
      </section>
    </div>
  </div>

  <h2 class="section-title" id="sec-contrib" data-i18n="contrib.title">Contributors</h2>
  <p class="section-sub" id="contrib-sub"></p>
  <section class="card contrib-card" id="contrib-card">
    <div id="contrib-list"></div>
  </section>

  <h2 class="section-title" id="sec-branches" data-i18n="branches.title">Branches</h2>
  <p class="section-sub" id="branch-sub"></p>
  <section class="card" id="branch-card">
    <div class="table-wrap">
      <table id="branch-table">
        <thead><tr>
          <th scope="col" data-sort="name"><button type="button" data-i18n="bt.branch">Branch</button></th>
          <th scope="col" data-sort="when"><button type="button" data-i18n="bt.appears">Appears</button></th>
          <th scope="col" data-sort="how"><button type="button" data-i18n="bt.source">Source</button></th>
          <th scope="col" data-sort="own"><button type="button" data-i18n="bt.own">Own commits</button></th>
          <th scope="col" data-sort="total"><button type="button" data-i18n="bt.commits">Commits</button></th>
          <th scope="col" data-sort="last"><button type="button" data-i18n="bt.last">Last commit</button></th>
          <th scope="col" data-sort="rel"><button type="button" data-i18n="bt.rel">Relative to the analyzed branch</button></th>
        </tr></thead>
        <tbody></tbody>
      </table>
    </div>
  </section>

  <details class="card config">
    <summary data-i18n="cfg.summary">What was measured</summary>
    <dl id="config"></dl>
  </details>
</div>
<div id="tooltip" class="tooltip" role="tooltip" hidden></div>
EOF
}

html_app() {
cat <<'EOF'
<script>
(function () {
  'use strict';
  const $ = (id) => document.getElementById(id);
  window.CLOC_HISTORY_STARTED = true;   // the gistpreview check in the <head> looks at this
  // gistpreview never closes the document it writes: close it, so the page stops "loading".
  if (window.CLOC_HISTORY_GIST) setTimeout(() => { if (document.readyState === 'loading') document.close(); }, 0);

  // ---------------------------------------------------------------- language (i18n)
  // English and Spanish texts. The language is the one chosen earlier in this browser; otherwise
  // the browser's; and if it is neither of the two, English. Adding another one only takes an
  // entry in UI_LANGS (with its flagcdn flag) and its dictionary in I18N.
  const UI_LANGS = [
    { code: 'en', name: 'English', flag: 'gb' },
    { code: 'es', name: 'Español', flag: 'es' }
  ];
  const I18N = {
    en: {
      'ui.language': 'Language', 'theme.dark': 'Dark mode', 'theme.toDark': 'Switch to dark mode', 'theme.toLight': 'Switch to light mode',
      'title.default': 'Code history · {0}',
      'sub.commits.one': '{0} commit', 'sub.commits.other': '{0} commits', 'sub.of': ' (of {0})',
      'nav.aria': 'Report sections', 'nav.code': 'Code history', 'nav.activity': 'Repository activity',
      'nav.langs': 'Languages', 'nav.contrib': 'Contributors', 'nav.branches': 'Branches',
      'nav.marks': 'Mark when each branch appears on the charts',
      'load.title': 'Data file missing', 'load.text1': 'Could not read ',
      'load.text2': ' next to this HTML (was it moved without it, or does the browser block local reads?). Pick it or drop it here:',
      'load.bad': 'Not a cloc-history data file: {0}', 'load.loading': 'Loading {0}…', 'load.nodata': 'no data',
      'load.cantload': 'could not load {0}',
      'load.nod3': 'Could not load D3 (offline?). Regenerate the report with --d3 path/to/d3.min.js to embed it.',
      'load.nocommits': 'No commits.',
      'code.title': 'Code history',
      'code.sub': 'Every commit of the main line measured with cloc, using the cloc.sh criteria. The filters below only affect this section.',
      'code.filters': 'Code history filters', 'f.metric': 'Metric', 'f.view': 'View', 'f.lang': 'Language', 'f.branch': 'Branch',
      'metric.code': 'Code', 'metric.comment': 'Comments', 'metric.blank': 'Blank lines', 'metric.lines': 'Total lines',
      'metric.files': 'Files', 'metric.size': 'Size on disk',
      'noun.code': 'lines of code', 'noun.comment': 'comment lines', 'noun.blank': 'blank lines', 'noun.lines': 'total lines',
      'noun.files': 'files', 'noun.size': 'size on disk',
      'short.code': 'code', 'short.comment': 'comm.', 'short.blank': 'blank', 'short.lines': 'lines', 'short.files': 'files', 'short.size': 'size',
      'view.total': 'Total at each commit', 'view.delta': 'Change from the previous commit', 'lang.all': 'All (stacked)', 'lang.allBox': 'All languages', 'lang.some': '{0} of {1} languages',
      'lang.only': 'only', 'lang.onlyAria': 'Show only {0}', 'lang.last': 'At least one language has to stay selected',
      'btn.resetZoom': 'Show the whole history', 'btn.csv': 'Download CSV', 'kpis.aria': 'Totals of the selected commit',
      'chart.hint': 'One bar per commit, in order (the X axis is not to time scale). Hover to see each commit and click to pin it; drag over the chart or in the strip below to zoom (double click: whole history). With the chart focused: ← → (Shift = 10), Home, End, Space.',
      'legend.aria': 'Languages (click to hide or show)', 'legend.toggle': 'Click to hide or show', 'chart.roledesc': 'stacked bar chart',
      'overview.aria': 'Whole history: drag to zoom', 'overview.label': 'History',
      'chart.aria': '{0}. {1} commits. Use the arrow keys to browse them.',
      'player.aria': 'Playback', 'player.first': 'First commit', 'player.prev': 'Previous commit', 'player.playTitle': 'Play the history',
      'player.next': 'Next commit', 'player.last': 'Last commit', 'player.play': '▶ Play', 'player.playAria': 'Play',
      'player.pause': '❚❚ Pause', 'player.pauseAria': 'Pause', 'player.speed': 'Speed',
      'th.lang': 'Language', 'th.files': 'Files', 'th.blank': 'Blank', 'th.comment': 'Comments', 'th.code': 'Code', 'th.lines': 'Lines', 'th.linesTitle': 'Blank + comments + code', 'th.size': 'Size',
      'th.sizeTitle': 'Bytes of the measured files (versioned content, no CRLF conversion)', 'th.pct': '% lines',
      'th.pctTitle': "Language lines (blank + comments + code) over the commit's total lines",
      'th.deltaTitle': 'Change in {0} from the previous commit', 'others': 'Others ({0})', 'total': 'Total',
      'tip.commitOf': 'commit {0} of {1}', 'tip.pctNote': "% = each language's share of the commit's total lines",
      'tip.changed.one': '{0} measured file changed', 'tip.changed.other': '{0} measured files changed',
      'detail.title': 'Commit {0} of {1}', 'detail.empty': 'Nothing measured in this commit',
      'kpi.first': 'first commit', 'kpi.nochange': 'no change', 'kpi.vsPrev': '{0} vs. previous', 'kpi.ofLines': '{0} of the lines',
      'kpi.langs': 'Languages', 'kpi.weight': 'Share of total lines',
      'title.delta': 'Change in {0} from the previous commit', 'title.total': '{0} at each commit', 'title.byLang': ', by language',
      'cfg.summary': 'What was measured', 'cfg.repo': 'Repository', 'cfg.walk': 'Walk', 'cfg.walk.fp': 'main line (--first-parent)',
      'cfg.walk.all': 'all commits (--topo-order)', 'cfg.step': ' · 1 every {0}', 'cfg.last': ' · last {0}', 'cfg.since': ' · since {0}',
      'cfg.until': ' · until {0}', 'cfg.commits': 'Commits', 'cfg.commitsV': '{0} analyzed of {1}', 'cfg.files': 'Files',
      'cfg.filesV': 'versioned in each commit (git ls-tree), like git ls-files in cloc.sh', 'cfg.excludes': 'Exclusions (ERE)',
      'cfg.includes': 'Inclusions (ERE)', 'cfg.paths': 'Paths', 'cfg.filter': 'Filter', 'cfg.onlyLangs': 'Only languages',
      'cfg.emails': 'Emails', 'cfg.emailsV': 'not included (--no-emails): contributors show their name only',
      'cfg.noLangs': 'Without languages', 'cfg.dups': 'Duplicates', 'cfg.dups.all': 'all counted (--skip-uniqueness)',
      'cfg.dups.once': 'identical content counts once (like cloc)', 'cfg.size': 'Size on disk',
      'cfg.sizeV': 'bytes of each measured file as versioned (git blob, no CRLF conversion); repeated content counts once',
      'cfg.na': 'not available in this data (regenerate the report)', 'cfg.branches': 'Branches',
      'cfg.branchesV': '{0} (--branches {1}); each one is dated with the reflog if it still keeps it or, otherwise, with its first own commit',
      'cfg.activity': 'Activity and contributors',
      'cfg.activityV': 'every non-merge commit reachable from each branch (not only the main line), with the commit date in UTC and authors grouped by email (honoring .mailmap); --since, --until and paths also limit it',
      'cfg.always': 'Always excluded',
      'cfg.alwaysV': '.git .svn .hg .bzr .cvs .snapshot .config .venv (folders cloc skips) and the files cloc-history itself generates (reports, data and cache), even if versioned or renamed',
      'cfg.generated': 'Generated', 'cfg.generatedV': '{0} with cloc {1} in {2} s',
      'act.title': 'Repository activity', 'act.filter': 'Activity filter', 'act.kpis': 'Commit statistics of the visible range',
      'act.time': 'Commits over time',
      'act.timeHint': 'Grouped by day, week, month or year depending on the visible range. Drag the minimap handles to narrow the range (double click: whole history).',
      'act.sub': 'Like GitLab «Repository analytics» but without limits: every commit reachable from {0}, without merges, with the commit date in UTC.',
      'act.empty': 'Branch {0} has no commits in the analyzed period.',
      'act.none': 'This report has no activity data: regenerate it with cloc-history.sh.',
      'act.monthday': 'Commits per day of month', 'act.weekday': 'Commits per weekday', 'act.hour': 'Commits per hour (UTC)',
      'langs.title': 'Languages', 'langs.basis': 'Computed over', 'basis.code': 'Lines of code', 'basis.lines': 'Total lines',
      'basis.size': 'Size on disk', 'langs.sub': 'According to cloc, at the last analyzed commit ({0}, {1}).',
      'langs.abs.code': '{0} lines of code', 'langs.abs.lines': '{0} lines',
      'contrib.title': 'Contributors',
      'contrib.sub.one': 'Like GitLab «Contributor analytics», on {0}: {1} author grouped by email (honoring .mailmap). Open one to see their commits over time.',
      'contrib.sub.other': 'Like GitLab «Contributor analytics», on {0}: {1} authors grouped by email (honoring .mailmap). Open one to see their commits over time.',
      'branches.title': 'Branches', 'bt.branch': 'Branch', 'bt.appears': 'Appears', 'bt.source': 'Source', 'bt.own': 'Own commits',
      'bt.commits': 'Commits', 'bt.last': 'Last commit', 'bt.rel': 'Relative to the analyzed branch',
      'dt.see': 'Show data', 'commits.one': '{0} commit', 'commits.other': '{0} commits', 'days.one': '{0} day', 'days.other': '{0} days',
      'week.of': 'Week of {0}',
      'win.full': 'Whole history. Narrow the range with the handles of the «Commits over time» minimap and these figures are recalculated.',
      'win.range': 'Range from {0} to {1} ({2}). ',
      'akpi.commits': 'Commits', 'akpi.commits.full': 'non-merge, whole history', 'akpi.commits.part': 'non-merge · {0} of the total',
      'akpi.authors': 'Authors', 'akpi.authors.full': 'grouped by email', 'akpi.authors.part': 'with commits in the range, of {0}',
      'akpi.avg': 'Average commits per day', 'akpi.avg.d': 'over {0}', 'akpi.active': 'Days with commits', 'akpi.active.d': '{0} of the days',
      'akpi.period': 'Period', 'akpi.period.d': 'until {0}',
      'cat.day': 'Day {0}', 'cat.col.monthday': 'Day of month', 'cat.col.weekday': 'Day', 'cat.col.hour': 'Hour (UTC)',
      'br.optAnalyzed': '{0} (analyzed)', 'br.analyzed': 'analyzed', 'br.view': "See this branch's activity",
      'br.sub.one': '{0} branch, in the order it appears. The date comes from the reflog if it still keeps it; otherwise from the first own commit (the first one not reachable from any branch that does not contain it) or, if it has none, from the commit it points to. Click it to see its activity.',
      'br.sub.other': '{0} branches, in the order they appear unless you sort by another column (click its header). The date comes from the reflog if it still keeps it; otherwise from the first own commit (the first one not reachable from any branch that does not contain it) or, if it has none, from the commit it points to. Click a branch to see its activity.',
      'br.how.reflog': 'reflog', 'br.how.own': 'first own commit', 'br.how.tip': 'commit it points to',
      'br.when.reflog': 'Created on {0} (reflog{1})', 'br.when.from': ', from {0}', 'br.when.own': 'Appears ≈ {0} (its first own commit)',
      'br.when.tip': 'No own commits: points to a commit from {0}',
      'br.rel.current': 'Is the analyzed branch (--rev)', 'br.rel.in': 'Included in {0}', 'br.rel.notIn': 'Not included in {0}',
      'br.rel.behind': ' · {0} behind', 'br.rel.mergesOnly': 'Its commits are in {0} (only merges missing) · {1} behind',
      'br.rel.aheadBehind': '{0} ahead and {1} behind {2}', 'br.selected': ' · selected',
      'br.own.one': '{0} own commit', 'br.own.other': '{0} own commits', 'br.total': ' · {0} in total', 'br.points': 'Points to {0} ({1})'
    },
    es: {
      'ui.language': 'Idioma', 'theme.dark': 'Modo oscuro', 'theme.toDark': 'Cambiar a modo oscuro', 'theme.toLight': 'Cambiar a modo claro',
      'title.default': 'Evolución del código · {0}',
      'sub.commits.one': '{0} commit', 'sub.commits.other': '{0} commits', 'sub.of': ' (de {0})',
      'nav.aria': 'Secciones del informe', 'nav.code': 'Evolución del código', 'nav.activity': 'Actividad del repositorio',
      'nav.langs': 'Lenguajes', 'nav.contrib': 'Contribuidores', 'nav.branches': 'Ramas',
      'nav.marks': 'Marcar en las gráficas cuándo aparece cada rama',
      'load.title': 'Falta el fichero de datos', 'load.text1': 'No se pudo leer ',
      'load.text2': ' junto a este HTML (¿se movió sin él, o el navegador bloquea la lectura local?). Elígelo o arrástralo aquí:',
      'load.bad': 'No es un fichero de datos de cloc-history: {0}', 'load.loading': 'Cargando {0}…', 'load.nodata': 'sin datos',
      'load.cantload': 'no se pudo cargar {0}',
      'load.nod3': 'No se pudo cargar D3 (¿sin conexión?). Regenera el informe con --d3 ruta/a/d3.min.js para incrustarlo.',
      'load.nocommits': 'No hay commits.',
      'code.title': 'Evolución del código',
      'code.sub': 'Cada commit de la línea principal medido con cloc, con los criterios de cloc.sh. Los filtros de aquí abajo solo afectan a esta sección.',
      'code.filters': 'Filtros de la evolución del código', 'f.metric': 'Métrica', 'f.view': 'Vista', 'f.lang': 'Lenguaje', 'f.branch': 'Rama',
      'metric.code': 'Código', 'metric.comment': 'Comentarios', 'metric.blank': 'Líneas en blanco', 'metric.lines': 'Líneas totales',
      'metric.files': 'Archivos', 'metric.size': 'Tamaño en disco',
      'noun.code': 'líneas de código', 'noun.comment': 'líneas de comentario', 'noun.blank': 'líneas en blanco', 'noun.lines': 'líneas totales',
      'noun.files': 'archivos', 'noun.size': 'tamaño en disco',
      'short.code': 'código', 'short.comment': 'coment.', 'short.blank': 'blanco', 'short.lines': 'líneas', 'short.files': 'archivos', 'short.size': 'tamaño',
      'view.total': 'Total en cada commit', 'view.delta': 'Cambio respecto al commit anterior', 'lang.all': 'Todos (apilados)', 'lang.allBox': 'Todos los lenguajes', 'lang.some': '{0} de {1} lenguajes',
      'lang.only': 'solo', 'lang.onlyAria': 'Mostrar solo {0}', 'lang.last': 'Tiene que quedar al menos un lenguaje',
      'btn.resetZoom': 'Ver toda la historia', 'btn.csv': 'Descargar CSV', 'kpis.aria': 'Totales del commit seleccionado',
      'chart.hint': 'Una barra por commit, en orden (el eje X no está a escala de tiempo). Pasa el ratón para ver cada commit y haz clic para fijarlo; arrastra sobre la gráfica o en la franja inferior para hacer zoom (doble clic: toda la historia). Con el foco en la gráfica: ← → (Mayús = 10), Inicio, Fin, Espacio.',
      'legend.aria': 'Lenguajes (clic para ocultar o mostrar)', 'legend.toggle': 'Clic para ocultar o mostrar', 'chart.roledesc': 'gráfica de barras apiladas',
      'overview.aria': 'Historia completa: arrastra para hacer zoom', 'overview.label': 'Historia',
      'chart.aria': '{0}. {1} commits. Usa las flechas para recorrerlos.',
      'player.aria': 'Reproducción', 'player.first': 'Primer commit', 'player.prev': 'Commit anterior', 'player.playTitle': 'Reproducir la historia',
      'player.next': 'Commit siguiente', 'player.last': 'Último commit', 'player.play': '▶ Reproducir', 'player.playAria': 'Reproducir',
      'player.pause': '❚❚ Pausa', 'player.pauseAria': 'Pausa', 'player.speed': 'Velocidad',
      'th.lang': 'Lenguaje', 'th.files': 'Archivos', 'th.blank': 'En blanco', 'th.comment': 'Comentarios', 'th.code': 'Código', 'th.lines': 'Líneas', 'th.linesTitle': 'En blanco + comentarios + código', 'th.size': 'Tamaño',
      'th.sizeTitle': 'Bytes de los ficheros medidos (contenido versionado, sin conversión CRLF)', 'th.pct': '% líneas',
      'th.pctTitle': 'Líneas del lenguaje (en blanco + comentarios + código) sobre el total de líneas del commit',
      'th.deltaTitle': 'Cambio en {0} respecto al commit anterior', 'others': 'Otros ({0})', 'total': 'Total',
      'tip.commitOf': 'commit {0} de {1}', 'tip.pctNote': '% = peso de cada lenguaje en el total de líneas del commit',
      'tip.changed.one': '{0} fichero medido cambiado', 'tip.changed.other': '{0} ficheros medidos cambiados',
      'detail.title': 'Commit {0} de {1}', 'detail.empty': 'Nada medido en este commit',
      'kpi.first': 'primer commit', 'kpi.nochange': 'sin cambios', 'kpi.vsPrev': '{0} vs. anterior', 'kpi.ofLines': '{0} de las líneas',
      'kpi.langs': 'Lenguajes', 'kpi.weight': 'Peso en el total de líneas',
      'title.delta': 'Cambio en {0} respecto al commit anterior', 'title.total': '{0} en cada commit', 'title.byLang': ', por lenguaje',
      'cfg.summary': 'Qué se ha medido', 'cfg.repo': 'Repositorio', 'cfg.walk': 'Recorrido', 'cfg.walk.fp': 'línea principal (--first-parent)',
      'cfg.walk.all': 'todos los commits (--topo-order)', 'cfg.step': ' · 1 de cada {0}', 'cfg.last': ' · últimos {0}', 'cfg.since': ' · desde {0}',
      'cfg.until': ' · hasta {0}', 'cfg.commits': 'Commits', 'cfg.commitsV': '{0} analizados de {1}', 'cfg.files': 'Ficheros',
      'cfg.filesV': 'versionados en cada commit (git ls-tree), como git ls-files en cloc.sh', 'cfg.excludes': 'Exclusiones (ERE)',
      'cfg.includes': 'Inclusiones (ERE)', 'cfg.paths': 'Rutas', 'cfg.filter': 'Filtro', 'cfg.onlyLangs': 'Solo lenguajes',
      'cfg.emails': 'Correos', 'cfg.emailsV': 'no incluidos (--no-emails): los contribuidores solo muestran su nombre',
      'cfg.noLangs': 'Sin lenguajes', 'cfg.dups': 'Duplicados', 'cfg.dups.all': 'se cuentan todos (--skip-uniqueness)',
      'cfg.dups.once': 'el mismo contenido cuenta una vez (como cloc)', 'cfg.size': 'Tamaño en disco',
      'cfg.sizeV': 'bytes de cada fichero medido tal como está versionado (blob de git, sin conversión CRLF); el contenido repetido cuenta una vez',
      'cfg.na': 'no disponible en estos datos (vuelve a generar el informe)', 'cfg.branches': 'Ramas',
      'cfg.branchesV': '{0} (--branches {1}); cada una se fecha con el reflog si aún lo guarda o, si no, con su primer commit propio',
      'cfg.activity': 'Actividad y contribuidores',
      'cfg.activityV': 'todos los commits sin merges alcanzables desde cada rama (no solo la línea principal), con la fecha de commit en UTC y los autores agrupados por email (respetando .mailmap); --since, --until y las rutas también la acotan',
      'cfg.always': 'Siempre fuera',
      'cfg.alwaysV': '.git .svn .hg .bzr .cvs .snapshot .config .venv (carpetas que cloc descarta) y los ficheros que genera el propio cloc-history (informes, datos y caché), aunque estén versionados o tengan otro nombre',
      'cfg.generated': 'Generado', 'cfg.generatedV': '{0} con cloc {1} en {2} s',
      'act.title': 'Actividad del repositorio', 'act.filter': 'Filtro de la actividad', 'act.kpis': 'Estadísticas de commits del tramo visible',
      'act.time': 'Commits a lo largo de la historia',
      'act.timeHint': 'Agrupados por día, semana, mes o año según el tramo visible. Arrastra las asas de la miniatura para acotar el tramo (doble clic: toda la historia).',
      'act.sub': 'Como «Repository analytics» de GitLab pero sin límite: todos los commits alcanzables desde {0}, sin merges, con la fecha de commit en UTC.',
      'act.empty': 'La rama {0} no tiene commits en el periodo analizado.',
      'act.none': 'Este informe no trae datos de actividad: vuelve a generarlo con cloc-history.sh.',
      'act.monthday': 'Commits por día del mes', 'act.weekday': 'Commits por día de la semana', 'act.hour': 'Commits por hora (UTC)',
      'langs.title': 'Lenguajes', 'langs.basis': 'Calcular sobre', 'basis.code': 'Líneas de código', 'basis.lines': 'Líneas totales',
      'basis.size': 'Tamaño en disco', 'langs.sub': 'Según cloc, en el último commit analizado ({0}, {1}).',
      'langs.abs.code': '{0} líneas de código', 'langs.abs.lines': '{0} líneas',
      'contrib.title': 'Contribuidores',
      'contrib.sub.one': 'Como «Contributor analytics» de GitLab, en {0}: {1} autor agrupado por email (respetando .mailmap). Ábrelo para ver sus commits en el tiempo.',
      'contrib.sub.other': 'Como «Contributor analytics» de GitLab, en {0}: {1} autores agrupados por email (respetando .mailmap). Abre uno para ver sus commits en el tiempo.',
      'branches.title': 'Ramas', 'bt.branch': 'Rama', 'bt.appears': 'Aparece', 'bt.source': 'Según', 'bt.own': 'Commits propios',
      'bt.commits': 'Commits', 'bt.last': 'Último commit', 'bt.rel': 'Respecto a la rama analizada',
      'dt.see': 'Ver datos', 'commits.one': '{0} commit', 'commits.other': '{0} commits', 'days.one': '{0} día', 'days.other': '{0} días',
      'week.of': 'Semana del {0}',
      'win.full': 'Toda la historia. Acota el tramo con las asas de la miniatura de «Commits a lo largo de la historia» y estas cifras se recalculan.',
      'win.range': 'Tramo del {0} al {1} ({2}). ',
      'akpi.commits': 'Commits', 'akpi.commits.full': 'sin merges, en toda la historia', 'akpi.commits.part': 'sin merges · {0} del total',
      'akpi.authors': 'Autores', 'akpi.authors.full': 'agrupados por email', 'akpi.authors.part': 'con commits en el tramo, de {0}',
      'akpi.avg': 'Media de commits por día', 'akpi.avg.d': 'en {0}', 'akpi.active': 'Días con commits', 'akpi.active.d': '{0} de los días',
      'akpi.period': 'Periodo', 'akpi.period.d': 'hasta el {0}',
      'cat.day': 'Día {0}', 'cat.col.monthday': 'Día del mes', 'cat.col.weekday': 'Día', 'cat.col.hour': 'Hora (UTC)',
      'br.optAnalyzed': '{0} (analizada)', 'br.analyzed': 'analizada', 'br.view': 'Ver la actividad de esta rama',
      'br.sub.one': '{0} rama, en el orden en que aparece. La fecha viene del reflog si aún la guarda; si no, del primer commit propio (el primero que no alcanza ninguna rama que no la contenga) o, si no tiene, del commit al que apunta. Púlsala para ver su actividad.',
      'br.sub.other': '{0} ramas, en el orden en que aparecen salvo que ordenes por otra columna (pulsa su cabecera). La fecha viene del reflog si aún la guarda; si no, del primer commit propio (el primero que no alcanza ninguna rama que no la contenga) o, si no tiene, del commit al que apunta. Pulsa una rama para ver su actividad.',
      'br.how.reflog': 'reflog', 'br.how.own': 'primer commit propio', 'br.how.tip': 'commit al que apunta',
      'br.when.reflog': 'Creada el {0} (reflog{1})', 'br.when.from': ', desde {0}', 'br.when.own': 'Aparece ≈ {0} (su primer commit propio)',
      'br.when.tip': 'Sin commits propios: apunta a un commit del {0}',
      'br.rel.current': 'Es la rama analizada (--rev)', 'br.rel.in': 'Incluida en {0}', 'br.rel.notIn': 'No incluida en {0}',
      'br.rel.behind': ' · {0} por detrás', 'br.rel.mergesOnly': 'Sus commits están en {0} (solo le faltan merges) · {1} por detrás',
      'br.rel.aheadBehind': '{0} por delante y {1} por detrás de {2}', 'br.selected': ' · seleccionada',
      'br.own.one': '{0} commit propio', 'br.own.other': '{0} commits propios', 'br.total': ' · {0} en total', 'br.points': 'Apunta a {0} ({1})'
    }
  };
  // What is not text: weekdays (starting on Monday), date formats and D3 number/time locales.
  const I18N_FMT = {
    en: {
      wdShort: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
      wdLong: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
      number: { decimal: '.', thousands: ',', grouping: [3], currency: ['$', ''], minus: '−' },
      time: {
        dateTime: '%x, %X', date: '%-m/%-d/%Y', time: '%-I:%M:%S %p', periods: ['AM', 'PM'],
        days: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
        shortDays: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
        months: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
        shortMonths: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
      },
      date: '%b %-d, %Y', dateTime: '%b %-d, %Y, %H:%M', tickTime: '%b %-d, %H:%M', tickDay: '%b %-d', month: '%b %Y',
      pct: '{0}%', pctSmall: '<0.1'
    },
    es: {
      wdShort: ['lun', 'mar', 'mié', 'jue', 'vie', 'sáb', 'dom'],
      wdLong: ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'],
      number: { decimal: ',', thousands: '.', grouping: [3], currency: ['', ' €'], minus: '−' },
      time: {
        dateTime: '%A, %e de %B de %Y, %X', date: '%d/%m/%Y', time: '%H:%M:%S', periods: ['AM', 'PM'],
        days: ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'],
        shortDays: ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'],
        months: ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'],
        shortMonths: ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic']
      },
      date: '%d %b %Y', dateTime: '%d %b %Y, %H:%M', tickTime: '%d %b, %H:%M', tickDay: '%d %b', month: '%b %Y',
      pct: '{0} %', pctSmall: '<0,1'
    }
  };
  const LANG_KEY = 'cloc-history.lang';
  function pickLang() {
    let saved = null;
    try { saved = localStorage.getItem(LANG_KEY); } catch (e) { /* no storage: the browser's language is used */ }
    if (saved && I18N[saved]) return saved;
    for (const l of navigator.languages && navigator.languages.length ? navigator.languages : [navigator.language || '']) {
      const code = String(l).toLowerCase().split('-')[0];
      if (I18N[code]) return code;
    }
    return 'en';
  }
  let UI = pickLang();
  let onLangChange = null;   // set by start() to redraw the report in the new language
  const fill = (s, args) => s.replace(/\{(\d+)\}/g, (m, i) => (args[i] !== undefined ? args[i] : m));
  function msg(key, ...args) {
    const s = (I18N[UI] && I18N[UI][key]) || I18N.en[key] || key;
    return fill(s, args);
  }
  function msgN(key, n, ...args) { return msg(key + (n === 1 ? '.one' : '.other'), ...args); }
  const fmtOfLang = () => I18N_FMT[UI] || I18N_FMT.en;

  // Fixed HTML texts: data-i18n (content) and data-i18n-<attribute> (title, aria-label…).
  function applyStatic() {
    document.documentElement.lang = UI;
    document.querySelectorAll('[data-i18n]').forEach((e) => { e.textContent = msg(e.dataset.i18n); });
    document.querySelectorAll('[data-i18n-title]').forEach((e) => { e.title = msg(e.dataset.i18nTitle); });
    document.querySelectorAll('[data-i18n-aria-label]').forEach((e) => { e.setAttribute('aria-label', msg(e.dataset.i18nAriaLabel)); });
    document.querySelectorAll('[data-i18n-aria-roledescription]').forEach((e) => {
      e.setAttribute('aria-roledescription', msg(e.dataset.i18nAriaRoledescription));
    });
  }

  // Language selector: a custom button and list (a <select> cannot show flags in its options).
  (function () {
    const btn = $('lang-btn'), list = $('lang-list');
    let active = 0;
    const flag = (code) => {
      const img = document.createElement('img');
      img.className = 'flag'; img.alt = ''; img.width = 20; img.height = 15;
      img.src = 'https://flagcdn.com/w20/' + code + '.png';
      img.srcset = 'https://flagcdn.com/w40/' + code + '.png 2x';
      return img;
    };
    function paintButton() {
      const L = UI_LANGS.find((l) => l.code === UI) || UI_LANGS[0];
      const chev = document.createElement('span');
      chev.className = 'chev'; chev.setAttribute('aria-hidden', 'true'); chev.textContent = '▾';
      btn.replaceChildren(flag(L.flag), document.createTextNode(L.name), chev);
    }
    function paintList() {
      list.replaceChildren();
      UI_LANGS.forEach((L, i) => {
        const li = document.createElement('li');
        li.id = 'lang-opt-' + L.code;
        li.setAttribute('role', 'option');
        li.setAttribute('aria-selected', String(L.code === UI));
        li.lang = L.code;
        const tick = document.createElement('span');
        tick.className = 'tick'; tick.setAttribute('aria-hidden', 'true'); tick.textContent = L.code === UI ? '✓' : '';
        li.append(flag(L.flag), document.createTextNode(L.name), tick);
        li.addEventListener('click', () => choose(L.code));
        li.addEventListener('mousemove', () => setActive(i));
        list.appendChild(li);
      });
    }
    function setActive(i) {
      active = (i + UI_LANGS.length) % UI_LANGS.length;
      [...list.children].forEach((li, k) => li.classList.toggle('active', k === active));
      list.setAttribute('aria-activedescendant', list.children[active].id);
    }
    function open() {
      paintList();
      list.hidden = false;
      btn.setAttribute('aria-expanded', 'true');
      setActive(Math.max(0, UI_LANGS.findIndex((l) => l.code === UI)));
      list.focus();
    }
    function close(focusButton) {
      list.hidden = true;
      btn.setAttribute('aria-expanded', 'false');
      if (focusButton) btn.focus();
    }
    function choose(code) {
      close(true);
      if (code === UI) return;
      UI = code;
      try { localStorage.setItem(LANG_KEY, code); } catch (e) { /* remembered for this visit only */ }
      applyStatic();
      paintButton();
      paintThemeButton();
      if (onLangChange) onLangChange();
    }
    btn.addEventListener('click', () => (list.hidden ? open() : close(false)));
    btn.addEventListener('keydown', (e) => {
      if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(e.key)) { e.preventDefault(); open(); }
    });
    list.addEventListener('keydown', (e) => {
      if (e.key === 'ArrowDown') { e.preventDefault(); setActive(active + 1); }
      else if (e.key === 'ArrowUp') { e.preventDefault(); setActive(active - 1); }
      else if (e.key === 'Home') { e.preventDefault(); setActive(0); }
      else if (e.key === 'End') { e.preventDefault(); setActive(UI_LANGS.length - 1); }
      else if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); choose(UI_LANGS[active].code); }
      else if (e.key === 'Escape') { e.preventDefault(); close(true); }
      else if (e.key === 'Tab') close(false);
    });
    document.addEventListener('click', (e) => { if (!list.hidden && !e.target.closest('.lang-switch')) close(false); });
    paintButton();
  })();

  // ---------------------------------------------------------------- light / dark theme
  // Follows the system until the button is pressed; from then on the choice is remembered. The
  // chart colors are CSS variables with a light and a dark version: nothing needs redrawing.
  const THEME_KEY = 'cloc-history.theme';
  const darkQuery = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
  function isDark() {
    const t = document.documentElement.dataset.theme;
    return t ? t === 'dark' : !!(darkQuery && darkQuery.matches);
  }
  function themeIcon(dark) {
    const ns = 'http://www.w3.org/2000/svg', svg = document.createElementNS(ns, 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('aria-hidden', 'true');
    const shapes = dark
      ? [['circle', { cx: 12, cy: 12, r: 4.5 }],
        ['path', { d: 'M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41' }]]
      : [['path', { d: 'M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z' }]];
    for (const [tag, attrs] of shapes) {
      const e = document.createElementNS(ns, tag);
      for (const k in attrs) e.setAttribute(k, attrs[k]);
      svg.appendChild(e);
    }
    return svg;
  }
  function paintThemeButton() {
    const btn = $('theme-btn'), dark = isDark();
    btn.setAttribute('aria-pressed', String(dark));
    btn.setAttribute('aria-label', msg('theme.dark'));
    btn.title = msg(dark ? 'theme.toLight' : 'theme.toDark');
    btn.replaceChildren(themeIcon(dark));   // dark mode offers the sun; light mode, the moon
  }
  $('theme-btn').addEventListener('click', () => {
    const next = isDark() ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    try { localStorage.setItem(THEME_KEY, next); } catch (e) { /* remembered for this visit only */ }
    paintThemeButton();
  });
  if (darkQuery && darkQuery.addEventListener) darkQuery.addEventListener('change', paintThemeButton);
  paintThemeButton();

  applyStatic();
  $('title').textContent = document.title;

  // ---------------------------------------------------------------- data loading
  // The data lives apart, in <report>.json. Opened from disk (file://) the browser blocks
  // fetch(), but it does load a <script> from the same folder: that is why its twin
  // <report>.json.js (window.CLOC_HISTORY_DATA = {...}) is loaded first. Served over http,
  // the .json is read if the .js is missing. And if nothing works, the user is asked for the file.
  const FILES = window.CLOC_HISTORY_FILES || { json: 'commits-reports.json', js: 'commits-reports.json.js', stamp: 0 };
  const bust = /^https?:$/.test(location.protocol) && FILES.stamp ? '?v=' + FILES.stamp : '';

  function loadScript() {
    return new Promise((resolve, reject) => {
      const s = document.createElement('script');
      s.src = FILES.js + bust;
      s.onload = () => (window.CLOC_HISTORY_DATA ? resolve(window.CLOC_HISTORY_DATA) : reject(new Error(msg('load.nodata'))));
      s.onerror = () => reject(new Error(msg('load.cantload', FILES.js)));
      document.head.appendChild(s);
    });
  }
  function loadJson() {
    return fetch(FILES.json + bust).then((r) => {
      if (!r.ok) throw new Error('HTTP ' + r.status);
      return r.json();
    });
  }
  // Accepts both the .json and its .json.js twin.
  function parseData(text) { return JSON.parse(text.slice(text.indexOf('{'), text.lastIndexOf('}') + 1)); }
  // Published through gistpreview (see the <head>): relative paths point at gistpreview itself, so
  // the data is looked up in the same gist (its raw URL, which the browser can fetch; a <script>
  // would be refused because gists serve raw files as text/plain).
  function loadFromGist() {
    const gist = window.CLOC_HISTORY_GIST;
    if (!gist) return Promise.reject(new Error('not a gist'));
    return gist.info().then((info) => {
      const files = info.files || {};
      const f = files[FILES.json] || files[FILES.js] || Object.values(files).find((x) => /\.json(\.js)?$/i.test(x.filename));
      if (!f) throw new Error(msg('load.cantload', FILES.json));
      return fetch(f.raw_url).then((r) => {
        if (!r.ok) throw new Error('HTTP ' + r.status);
        return r.text();
      });
    }).then(parseData);
  }
  function askForFile() {
    return new Promise((resolve) => {
      const panel = $('load-panel');
      $('load-name').textContent = FILES.json;
      $('chart').textContent = '';
      panel.hidden = false;
      document.querySelector('.page').classList.add('waiting');
      const read = (file) => {
        if (!file) return;
        file.text().then((t) => {
          const data = parseData(t);
          panel.hidden = true;
          document.querySelector('.page').classList.remove('waiting');
          resolve(data);
        })
          .catch((e) => { $('load-error').textContent = msg('load.bad', e.message); });
      };
      $('load-input').addEventListener('change', (e) => read(e.target.files[0]));
      panel.addEventListener('dragover', (e) => { e.preventDefault(); panel.classList.add('over'); });
      panel.addEventListener('dragleave', () => panel.classList.remove('over'));
      panel.addEventListener('drop', (e) => { e.preventDefault(); panel.classList.remove('over'); read(e.dataTransfer.files[0]); });
    });
  }

  // A self-contained copy (--embed) carries the data inside the page: there is nothing to load.
  const embedded = $('report-data');
  if (embedded) {
    Promise.resolve().then(() => JSON.parse(embedded.textContent))
      .then(start, (e) => { $('chart').textContent = msg('load.bad', e.message); });
  } else {
    $('chart').textContent = msg('load.loading', FILES.json);
    (window.CLOC_HISTORY_GIST ? loadFromGist() : Promise.reject(new Error('not a gist')))
      .catch(loadScript).catch(loadJson).catch(askForFile).then(start);
  }

  function start(RAW) {
    $('chart').textContent = '';
    const META = RAW.meta, LANGS = RAW.langs, ROWS = RAW.commits;
    const NC = ROWS.length, NL = LANGS.length;
    if (typeof d3 === 'undefined') {
      $('chart').textContent = msg('load.nod3');
      return;
    }

    // ---------------------------------------------------------------- formats (per language)
    // Rebuilt when the language changes (setLocale); the rest of the code uses them by name.
    let loc, tloc;
    const F = {};
    function setLocale() {
      const L = fmtOfLang();
      loc = d3.formatLocale(L.number);
      tloc = d3.timeFormatLocale(L.time);
      F.int = loc.format(',d'); F.dec = loc.format(',.1f');
      F.date = tloc.format(L.date); F.dateTime = tloc.format(L.dateTime); F.tickTime = tloc.format(L.tickTime);
      F.dayU = tloc.utcFormat(L.date); F.monthU = tloc.utcFormat(L.month); F.yearU = tloc.utcFormat('%Y');
      F.tickDayU = tloc.utcFormat(L.tickDay); F.monthShortU = tloc.utcFormat('%b');
    }
    setLocale();
    const fmtInt = (v) => F.int(v);
    const fmtSigned = (v) => (v > 0 ? '+' : '') + fmtInt(v);
    const fmtDate = (v) => F.date(v);
    const fmtDateTime = (v) => F.dateTime(v);
    const fmtTickTime = (v) => F.tickTime(v);
    const fmtCompact = (v) => {
      const a = Math.abs(v);
      if (a >= 1e6) return loc.format(',.1~f')(v / 1e6) + ' M';
      if (a >= 1e4) return loc.format(',.0f')(v / 1e3) + ' k';
      return fmtInt(v);
    };
    const r1 = (v) => Math.round(v * 10) / 10;
    const fmtPct = (v) => fill(fmtOfLang().pct, [v > 0 && v < 0.1 ? fmtOfLang().pctSmall : F.dec(v)]);

    // ---------------------------------------------------------------- data
    // V[m][c * NL + l] with m: 0 files, 1 blank, 2 comments, 3 code, 4 bytes on disk.
    // Each language comes as [li, files, blank, comments, code, bytes] (format 2);
    // old data (format 1) has no bytes and the size metric is hidden.
    // Static percentages that come in the JSON (computed by the script):
    //   PL[c * NL + l] = % of the language's lines over the commit total
    //   PT[c * 3 + k]  = % blank, % comments and % code of the commit
    const G = (META.format || 1) >= 2 ? 6 : 5, HAS_SIZE = G === 6;
    const V = [0, 1, 2, 3, 4].map(() => new Float64Array(NC * NL));
    const PL = new Float64Array(NC * NL), PT = new Float64Array(NC * 3);
    const TS = new Float64Array(NC);
    ROWS.forEach((row, c) => {
      TS[c] = row[1] * 1000;
      const a = row[5], p = row[6] || [], t = row[7] || [];
      for (let j = 0, g = 0; j + G - 1 < a.length; j += G, g++) {
        const o = c * NL + a[j];
        V[0][o] = a[j + 1]; V[1][o] = a[j + 2]; V[2][o] = a[j + 3]; V[3][o] = a[j + 4];
        if (HAS_SIZE) V[4][o] = a[j + 5];
        PL[o] = p[g] || 0;
      }
      for (let k = 0; k < 3; k++) PT[c * 3 + k] = t[k] || 0;
    });
    // Name, noun and abbreviation of each metric, in the current language.
    const METRICS = Object.fromEntries(['code', 'comment', 'blank', 'lines', 'files', 'size'].map((k) => [k, {
      get label() { return msg('metric.' + k); }, get noun() { return msg('noun.' + k); }, get short() { return msg('short.' + k); }
    }]));
    const MIDX = { files: 0, blank: 1, comment: 2, code: 3, size: 4 };
    if (!HAS_SIZE) $('f-metric').querySelector('option[value="size"]').remove();

    // Bytes in units of 1024 (like Windows Explorer): 512 B, 3.4 KB, 1.2 MB…
    const fmtBytes = (v) => {
      const a = Math.abs(v), sign = v < 0 ? '−' : '';
      if (a < 1024) return sign + fmtInt(a) + ' B';
      const units = ['KB', 'MB', 'GB', 'TB'];
      let x = a / 1024, i = 0;
      while (x >= 1024 && i < units.length - 1) { x /= 1024; i++; }
      return sign + loc.format(x >= 100 ? ',.0f' : ',.1f')(x) + ' ' + units[i];
    };
    const fmtBytesSigned = (v) => (v > 0 ? '+' : '') + fmtBytes(v);
    const fmtOf = (metric) => (metric === 'size' ? fmtBytes : fmtInt);
    const fmtDeltaOf = (metric) => (metric === 'size' ? fmtBytesSigned : fmtSigned);
    const deltaCls = (v) => (v > 0 ? 'delta-up' : v < 0 ? 'delta-down' : null);   // no class for 0 or NaN
    function val(metric, c, l) {
      const o = c * NL + l;
      if (metric === 'lines') return V[1][o] + V[2][o] + V[3][o];
      return V[MIDX[metric]][o];
    }

    // Fixed colors per language: the 8 with the most code accumulated over the whole history
    // (a one-off peak is not enough); the rest are grouped in "Others" (a 9th color could not be told apart).
    const weight = new Float64Array(NL), weightFiles = new Float64Array(NL);
    for (let c = 0; c < NC; c++) for (let l = 0; l < NL; l++) {
      const o = c * NL + l;
      weight[l] += V[3][o];
      weightFiles[l] += V[0][o];
    }
    const ORDER = d3.range(NL).sort((a, b) => weight[b] - weight[a] || weightFiles[b] - weightFiles[a] || d3.ascending(LANGS[a], LANGS[b]));
    const SLOTS = 8;
    const colored = NL <= SLOTS ? ORDER : ORDER.slice(0, SLOTS);
    const rest = NL <= SLOTS ? [] : ORDER.slice(SLOTS);
    const COLOR = new Array(NL).fill('var(--other)');
    colored.forEach((l, k) => { COLOR[l] = 'var(--s' + (k + 1) + ')'; });
    const ALL_SERIES = colored.map((l) => ({ id: 'l' + l, name: LANGS[l], langs: [l], color: COLOR[l] }));
    if (rest.length) {
      ALL_SERIES.push({
        id: 'other', get name() { return rest.length === 1 ? LANGS[rest[0]] : msg('others', rest.length); },
        langs: rest, color: 'var(--other)', members: rest.map((l) => LANGS[l])
      });
    }

    // state.langs: the languages shown (never empty). The language picker and the legend both edit it.
    const state = {
      metric: 'code', mode: 'total', langs: new Set(d3.range(NL)),
      i0: 0, i1: NC - 1, sel: NC - 1, playing: false, speed: 10,
      sort: null, sortDir: -1
    };
    const allLangs = () => state.langs.size === NL;
    const onlyLang = () => (state.langs.size === 1 ? state.langs.values().next().value : -1);

    // One series per shown language with its own colour; the shown ones of the "Others" group stay
    // together (a 9th colour could not be told apart). A single language keeps its colour.
    function activeSeries() {
      const one = onlyLang();
      if (one >= 0) return [{ id: 'l' + one, name: LANGS[one], langs: [one], color: COLOR[one] === 'var(--other)' ? 'var(--ink-2)' : COLOR[one] }];
      const out = [];
      for (const s of ALL_SERIES) {
        const langs = s.langs.filter((l) => state.langs.has(l));
        if (langs.length === s.langs.length) out.push(s);
        else if (langs.length) {
          out.push({ id: s.id, name: langs.length === 1 ? LANGS[langs[0]] : msg('others', langs.length), langs, color: s.color, members: langs.map((l) => LANGS[l]) });
        }
      }
      return out;
    }
    function seriesTotal(s, metric, c) { let t = 0; for (const l of s.langs) t += val(metric, c, l); return t; }

    let cacheKey = '', cache = null;
    function matrix(series) {
      const key = [state.metric, state.mode, [...state.langs].sort((a, b) => a - b).join(',')].join('|');
      if (key === cacheKey) return cache;
      cache = series.map((s) => {
        const a = new Float64Array(NC);
        for (let c = 0; c < NC; c++) a[c] = seriesTotal(s, state.metric, c);
        if (state.mode === 'delta') {
          for (let c = NC - 1; c > 0; c--) a[c] -= a[c - 1];
          if (!META.firstIsRoot) a[0] = NaN;
        }
        return a;
      });
      cacheKey = key;
      return cache;
    }

    // ---------------------------------------------------------------- header, filters, configuration
    // Title (the --title one or one in the current language) and subtitle.
    function setHeader() {
      // Data without customTitle (older versions) only had a --title or the old default Spanish title.
      const custom = META.customTitle != null ? META.customTitle : !!META.title && !/^Evolución del código · /.test(META.title);
      const title = custom ? META.title : msg('title.default', META.repo);
      document.title = title;
      $('title').textContent = title;
      const span = NC ? fmtDate(TS[0]) + ' – ' + fmtDate(TS[NC - 1]) : '';
      $('subtitle').textContent = [
        META.repo, META.rev,
        msgN('sub.commits', NC, fmtInt(NC)) + (NC < META.totalCommits ? msg('sub.of', fmtInt(META.totalCommits)) : ''),
        span, 'cloc ' + META.cloc
      ].filter(Boolean).join(' · ');
    }
    setHeader();

    // ---------------------------------------------------------------- language picker (several at once)
    // A button with a panel: "All languages", then one checkbox per language (in colour order) with an
    // "only" shortcut. The last checked language cannot be unchecked, so the chart is never empty.
    const langBtn = $('f-lang'), langPanel = $('f-lang-panel'), langAll = $('f-lang-all'), langItems = $('f-lang-items');
    const langPick = langBtn.parentElement, langBoxes = new Map();   // language -> its checkbox
    function langSummary() {
      if (allLangs()) return msg('lang.all');
      const names = ORDER.filter((l) => state.langs.has(l)).map((l) => LANGS[l]);
      return names.length <= 3 ? names.join(', ') : msg('lang.some', fmtInt(names.length), fmtInt(NL));
    }
    function syncLangPicker() {
      const one = onlyLang();
      for (const [l, box] of langBoxes) {
        box.checked = state.langs.has(l);
        box.disabled = l === one;
        box.parentElement.title = l === one ? msg('lang.last') : '';
      }
      langAll.checked = allLangs();
      langAll.indeterminate = !allLangs();
      langAll.disabled = allLangs();
      langBtn.querySelector('.txt').textContent = langSummary();
    }
    function setLangs(langs) {
      if (!langs.size) return;
      state.langs = langs;
      syncLangPicker();
      refresh();
    }
    function buildLangPicker() {
      langBoxes.clear();
      langItems.replaceChildren();
      for (const l of ORDER) {
        const row = el('div', 'pick-row'), lab = el('label'), box = el('input');
        box.type = 'checkbox';
        box.addEventListener('change', () => {
          const next = new Set(state.langs);
          if (box.checked) next.add(l); else next.delete(l);
          if (next.size) setLangs(next); else box.checked = true;
        });
        const sw = el('span', 'sw'); sw.style.background = COLOR[l];
        lab.append(box, sw, el('span', 'nm', LANGS[l]));
        const only = el('button', 'only', msg('lang.only'));
        only.type = 'button';
        only.setAttribute('aria-label', msg('lang.onlyAria', LANGS[l]));
        only.addEventListener('click', () => setLangs(new Set([l])));
        row.append(lab, only);
        langItems.appendChild(row);
        langBoxes.set(l, box);
      }
      syncLangPicker();
    }
    langAll.addEventListener('change', () => { if (langAll.checked) setLangs(new Set(d3.range(NL))); });
    function openLangPanel(open) {
      langPanel.hidden = !open;
      langBtn.setAttribute('aria-expanded', String(open));
    }
    langBtn.addEventListener('click', () => openLangPanel(langPanel.hidden));
    document.addEventListener('pointerdown', (e) => { if (!langPanel.hidden && !langPick.contains(e.target)) openLangPanel(false); });
    langPick.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && !langPanel.hidden) { e.preventDefault(); openLangPanel(false); langBtn.focus(); }
    });
    langPick.addEventListener('focusout', (e) => { if (e.relatedTarget && !langPick.contains(e.relatedTarget)) openLangPanel(false); });
    buildLangPicker();

    // Data without author emails (--no-emails, or authors that never had one).
    const NO_EMAILS = !!RAW.activity && RAW.activity.people.every((p) => !p[1]) &&
      !(RAW.log && RAW.log.authors.some((a) => a[1]));
    function renderConfig() {
      const dl = $('config');
      dl.replaceChildren();
      const add = (k, v) => {
        const dt = document.createElement('dt'); dt.textContent = k;
        const dd = document.createElement('dd'); dd.textContent = v;
        dl.append(dt, dd);
      };
      add(msg('cfg.repo'), META.repo + ' @ ' + META.rev + ' (' + META.tip.slice(0, 12) + ')');
      add(msg('cfg.walk'), msg(META.firstParent ? 'cfg.walk.fp' : 'cfg.walk.all') +
        (META.step > 1 ? msg('cfg.step', META.step) : '') + (META.maxCommits ? msg('cfg.last', META.maxCommits) : '') +
        (META.since ? msg('cfg.since', META.since) : '') + (META.until ? msg('cfg.until', META.until) : ''));
      add(msg('cfg.commits'), msg('cfg.commitsV', fmtInt(NC), fmtInt(META.totalCommits)));
      add(msg('cfg.files'), msg('cfg.filesV'));
      add(msg('cfg.excludes'), META.excludes.length ? META.excludes.join('   ') : '—');
      if (META.includes.length) add(msg('cfg.includes'), META.includes.join('   '));
      if (META.pathspec.length) add(msg('cfg.paths'), META.pathspec.join(' '));
      if (META.filter) add(msg('cfg.filter'), META.filter);
      if (META.includeLang) add(msg('cfg.onlyLangs'), META.includeLang);
      if (META.excludeLang) add(msg('cfg.noLangs'), META.excludeLang);
      add(msg('cfg.dups'), msg(META.countDuplicates ? 'cfg.dups.all' : 'cfg.dups.once'));
      add(msg('cfg.size'), msg(HAS_SIZE ? 'cfg.sizeV' : 'cfg.na'));
      if (RAW.branches) add(msg('cfg.branches'), msg('cfg.branchesV', RAW.branches.length, META.branches || 'local'));
      add(msg('cfg.activity'), msg(RAW.activity ? 'cfg.activityV' : 'cfg.na'));
      if (NO_EMAILS) add(msg('cfg.emails'), msg('cfg.emailsV'));
      add(msg('cfg.always'), msg('cfg.alwaysV'));
      add(msg('cfg.generated'), msg('cfg.generatedV', META.generated, META.cloc, META.elapsed));
    }
    renderConfig();

    // ---------------------------------------------------------------- main chart
    const chartEl = $('chart');
    const svg = d3.select(chartEl).append('svg');
    const gRoot = svg.append('g');
    const gGrid = gRoot.append('g').attr('class', 'grid');
    const hoverCol = gRoot.append('rect').attr('class', 'hover-col').attr('visibility', 'hidden');
    const gBars = gRoot.append('g').attr('class', 'bars');
    const baseLine = gRoot.append('line').attr('class', 'baseline');
    const gX = gRoot.append('g').attr('class', 'xaxis');
    const selLine = gRoot.append('line').attr('class', 'sel-line');
    const zoomSel = gRoot.append('rect').attr('class', 'zoom-sel').attr('visibility', 'hidden');
    const overlay = gRoot.append('rect').attr('class', 'overlay');
    const gMarks = gRoot.append('g').attr('class', 'branch-marks');
    const GEO = { m: { t: 12, r: 12, b: 30, l: 64 }, iw: 0, ih: 0, step: 1, W: 0, H: 340 };

    // Rectangle with rounded corners only on top (rt) or at the bottom (rb).
    function seg(x, yt, yb, w, rt, rb) {
      const h = yb - yt;
      if (!(h > 0.2) || !(w > 0)) return '';
      rt = Math.min(rt, w / 2, h); rb = Math.min(rb, w / 2, h - rt);
      const xr = x + w;
      let d = 'M' + r1(x) + ',' + r1(yb - rb) + 'V' + r1(yt + rt);
      d += rt ? 'A' + r1(rt) + ',' + r1(rt) + ' 0 0 1 ' + r1(x + rt) + ',' + r1(yt) + 'H' + r1(xr - rt) +
                'A' + r1(rt) + ',' + r1(rt) + ' 0 0 1 ' + r1(xr) + ',' + r1(yt + rt)
              : 'H' + r1(xr);
      d += 'V' + r1(yb - rb);
      d += rb ? 'A' + r1(rb) + ',' + r1(rb) + ' 0 0 1 ' + r1(xr - rb) + ',' + r1(yb) + 'H' + r1(x + rb) +
                'A' + r1(rb) + ',' + r1(rb) + ' 0 0 1 ' + r1(x) + ',' + r1(yb - rb)
              : 'H' + r1(x);
      return d + 'Z';
    }

    function renderMain() {
      const series = activeSeries(), M = matrix(series);
      const m = GEO.m, W = Math.max(280, chartEl.clientWidth), H = GEO.H;
      const iw = W - m.l - m.r, ih = H - m.t - m.b;
      const n = state.i1 - state.i0 + 1, step = iw / n;
      Object.assign(GEO, { W, iw, ih, step });
      svg.attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
      gRoot.attr('transform', 'translate(' + m.l + ',' + m.t + ')');

      let lo = 0, hi = 0;
      for (let c = state.i0; c <= state.i1; c++) {
        let p = 0, q = 0;
        for (let k = 0; k < M.length; k++) { const v = M[k][c]; if (v > 0) p += v; else if (v < 0) q += v; }
        if (p > hi) hi = p; if (q < lo) lo = q;
      }
      if (hi === 0 && lo === 0) hi = 1;
      let y, axis;
      if (state.metric === 'size') {
        // Round ticks in the matching unit (0 / 0.5 / 1 MB…), not in raw bytes.
        const top = Math.max(Math.abs(lo), Math.abs(hi));
        const [unit, name] = top >= 1024 ** 3 ? [1024 ** 3, 'GB'] : top >= 1024 ** 2 ? [1024 ** 2, 'MB'] : top >= 1024 ? [1024, 'KB'] : [1, 'B'];
        const ys = d3.scaleLinear().domain([lo / unit, hi / unit]).nice(5);
        y = d3.scaleLinear().domain(ys.domain().map((d) => d * unit)).range([ih, 0]);
        axis = d3.axisLeft(y).tickValues(ys.ticks(5).map((t) => t * unit))
          .tickFormat((v) => (v < 0 ? '−' : '') + loc.format(',~f')(Math.abs(v) / unit) + ' ' + name);
      } else {
        y = d3.scaleLinear().domain([lo, hi]).nice(5).range([ih, 0]);
        const big = Math.max(Math.abs(y.domain()[0]), Math.abs(y.domain()[1])) >= 1e6;
        axis = d3.axisLeft(y).ticks(5).tickFormat(big ? fmtCompact : fmtInt);
      }
      gGrid.call(axis.tickSize(-iw).tickPadding(8))
        .call((g) => g.selectAll('.tick line').attr('stroke-opacity', (d) => (d === 0 ? 0 : 1)));
      baseLine.attr('x1', 0).attr('x2', iw).attr('y1', r1(y(0)) + 0.5).attr('y2', r1(y(0)) + 0.5);

      // Bars: a single <path> per series with all its segments.
      const gapX = step >= 6 ? 2 : 0;
      const bw = Math.min(24, step - gapX), off = (step - bw) / 2;
      const gapY = bw >= 4 ? 2 : 1, rad = bw >= 8 ? 4 : 0;
      const d = series.map(() => []);
      for (let c = state.i0; c <= state.i1; c++) {
        const x = (c - state.i0) * step + off;
        let top = -1, bot = -1;
        for (let k = 0; k < M.length; k++) { const v = M[k][c]; if (v > 0) top = k; else if (v < 0) bot = k; }
        let p = 0, q = 0;
        for (let k = 0; k < M.length; k++) {
          const v = M[k][c];
          if (!v) continue;
          if (v > 0) {
            const yb = y(p) - (p > 0 ? gapY : 0), yt = y(p + v); p += v;
            d[k].push(seg(x, yt, yb, bw, k === top ? rad : 0, 0));
          } else {
            const yt = y(q) + (q < 0 ? gapY : 0), yb = y(q + v); q += v;
            d[k].push(seg(x, yt, yb, bw, 0, k === bot ? rad : 0));
          }
        }
      }
      gBars.selectAll('path').data(series, (s) => s.id).join('path')
        .style('fill', (s) => s.color)
        .attr('d', (s, k) => d[k].join(''));

      // X axis: dates at evenly spread commits.
      const maxTicks = Math.max(2, Math.floor(iw / 110));
      let ticks = n <= maxTicks ? d3.range(state.i0, state.i1 + 1) : d3.ticks(state.i0, state.i1, maxTicks).filter((t) => Number.isInteger(t) && t >= state.i0 && t <= state.i1);
      if (!ticks.length) ticks = [state.i0];
      const xc = (c) => (c - state.i0 + 0.5) * step;
      gX.attr('transform', 'translate(0,' + ih + ')');
      const tk = gX.selectAll('g.tick').data(ticks, (t) => t).join((e) => {
        const g = e.append('g').attr('class', 'tick');
        g.append('line').attr('y2', 4);
        g.append('text').attr('y', 16).attr('dy', '0.32em');
        return g;
      });
      const spanMs = TS[state.i1] - TS[state.i0];
      const tickFmt = spanMs < 3 * 864e5 ? fmtTickTime : fmtDate;
      tk.attr('transform', (t) => 'translate(' + r1(xc(t)) + ',0)');
      tk.select('text').text((t) => tickFmt(TS[t])).attr('text-anchor', 'middle');
      // No overlaps: labels at the edges are shifted and those that would overlap the previous one are hidden.
      let lastRight = -Infinity, lastLabel = null;
      tk.each(function (t) {
        const text = this.querySelector('text'), w = text.getComputedTextLength(), cx = xc(t);
        const dx = cx - w / 2 < 0 ? w / 2 - cx : cx + w / 2 > iw ? iw - w / 2 - cx : 0;
        const ok = cx + dx - w / 2 >= lastRight + 12 && text.textContent !== lastLabel;
        text.setAttribute('x', r1(dx));
        this.style.display = ok ? '' : 'none';
        if (ok) { lastRight = cx + dx + w / 2; lastLabel = text.textContent; }
      });

      overlay.attr('width', iw).attr('height', ih);
      hoverCol.attr('y', 0).attr('height', ih);
      selLine.attr('y1', 0).attr('y2', ih);
      zoomSel.attr('y', 0).attr('height', ih);
      drawBranchMarks(gMarks, BRANCH_BY_COMMIT, (gr) => (gr.idx - state.i0 + 0.5) * step, ih, iw);
      chartEl.setAttribute('aria-label', msg('chart.aria', $('chart-title').textContent, fmtInt(n)));
      placeSelection();
    }

    function colAt(mx) { return Math.max(state.i0, Math.min(state.i1, state.i0 + Math.floor(mx / GEO.step))); }
    function placeSelection() {
      const c = state.sel;
      if (c < state.i0 || c > state.i1) { selLine.attr('visibility', 'hidden'); }
      else {
        const x = Math.round((c - state.i0 + 0.5) * GEO.step) + 0.5;
        selLine.attr('visibility', 'visible').attr('x1', x).attr('x2', x);
      }
      ovSel.attr('x1', ovX(c + 0.5)).attr('x2', ovX(c + 0.5));
    }
    function showHover(c) {
      hoverCol.attr('visibility', 'visible').attr('x', (c - state.i0) * GEO.step).attr('width', Math.max(1, GEO.step));
    }
    function hideHover() { hoverCol.attr('visibility', 'hidden'); $('tooltip').hidden = true; }

    function el(tag, cls, text) {
      const e = document.createElement(tag);
      if (cls) e.className = cls;
      if (text != null) e.textContent = text;
      return e;
    }
    function showTooltip(c, cx, cy) {
      const tip = $('tooltip'), row = ROWS[c];
      const series = activeSeries(), M = matrix(series);
      const fmt = state.mode === 'delta' ? fmtDeltaOf(state.metric) : fmtOf(state.metric);
      tip.replaceChildren();
      tip.append(
        el('div', 't-head', row[0].slice(0, 10) + ' · ' + fmtDateTime(TS[c])),
        el('div', 't-meta', row[2] + ' · ' + msg('tip.commitOf', fmtInt(c + 1), fmtInt(NC))),
        el('div', 't-subj', row[3])
      );
      let total = 0, any = false;
      for (let k = series.length - 1; k >= 0; k--) {
        const v = M[k][c];
        const r = el('div', 't-row');
        const key = el('span', 't-key'); key.style.background = series[k].color;
        let name = series[k].name;
        if (state.mode === 'total') { let p = 0; for (const l of series[k].langs) p += PL[c * NL + l]; name += ' · ' + fmtPct(p); }
        const vc = state.mode === 'delta' ? deltaCls(v) : null;
        r.append(key, el('span', vc ? 't-val ' + vc : 't-val', Number.isNaN(v) ? '—' : fmt(v)), el('span', 't-name', name));
        tip.append(r);
        if (!Number.isNaN(v)) { total += v; any = true; }
      }
      if (series.length > 1) {
        const r = el('div', 't-row t-total');
        const tc = state.mode === 'delta' && any ? deltaCls(total) : null;
        r.append(el('span'), el('span', tc ? 't-val ' + tc : 't-val', any ? fmt(total) : '—'), el('span', 't-name', msg('total')));
        tip.append(r);
      }
      if (state.mode === 'total') tip.append(el('div', 't-foot', msg('tip.pctNote')));
      tip.append(el('div', 't-foot', msgN('tip.changed', row[4], fmtInt(row[4]))));
      tip.hidden = false;
      const pad = 14, w = tip.offsetWidth, h = tip.offsetHeight;
      let x = cx + pad, yy = cy + pad;
      if (x + w > window.innerWidth - 8) x = Math.max(8, cx - pad - w);
      if (yy + h > window.innerHeight - 8) yy = Math.max(8, cy - pad - h);
      tip.style.left = x + 'px'; tip.style.top = yy + 'px';
    }

    // Mouse: hover = tooltip, click = pin a commit, drag = zoom to that range, double click = whole history.
    let drag = null;
    overlay
      .on('pointerdown', (ev) => {
        if (ev.button !== 0) return;
        const [mx] = d3.pointer(ev, gRoot.node());
        drag = { x0: mx, c0: colAt(mx), moved: false };
        overlay.node().setPointerCapture(ev.pointerId);
      })
      .on('pointermove', (ev) => {
        const [mx] = d3.pointer(ev, gRoot.node());
        const c = colAt(mx);
        if (drag && (drag.moved || Math.abs(mx - drag.x0) > 4)) {
          drag.moved = true;
          const a = Math.min(drag.c0, c), b = Math.max(drag.c0, c);
          zoomSel.attr('visibility', 'visible').attr('x', (a - state.i0) * GEO.step).attr('width', (b - a + 1) * GEO.step);
          hideHover();
          return;
        }
        showHover(c);
        showTooltip(c, ev.clientX, ev.clientY);
      })
      .on('pointerup', (ev) => {
        if (!drag) return;
        const [mx] = d3.pointer(ev, gRoot.node());
        const c = colAt(mx), d = drag;
        drag = null;
        zoomSel.attr('visibility', 'hidden');
        stop();
        if (!d.moved) { select(c); return; }
        const a = Math.min(d.c0, c), b = Math.max(d.c0, c);
        if (b > a) { hideHover(); setRange(a, b); }
      })
      .on('pointercancel', () => { drag = null; zoomSel.attr('visibility', 'hidden'); })
      .on('pointerleave', () => { if (!drag) hideHover(); })
      .on('dblclick', (ev) => { ev.preventDefault(); if (state.i0 !== 0 || state.i1 !== NC - 1) setRange(0, NC - 1); });

    chartEl.addEventListener('keydown', (ev) => {
      let c = state.sel;
      const jump = ev.shiftKey ? 10 : 1;
      if (ev.key === 'ArrowRight') c += jump;
      else if (ev.key === 'ArrowLeft') c -= jump;
      else if (ev.key === 'Home') c = state.i0;
      else if (ev.key === 'End') c = state.i1;
      else if (ev.key === ' ') { ev.preventDefault(); togglePlay(); return; }
      else return;
      ev.preventDefault();
      stop();
      select(Math.max(state.i0, Math.min(state.i1, c)));
      tooltipAtSelection();
    });
    chartEl.addEventListener('blur', hideHover);
    function tooltipAtSelection() {
      const c = state.sel;
      if (c < state.i0 || c > state.i1) return;
      const rect = chartEl.getBoundingClientRect();
      showHover(c);
      showTooltip(c, rect.left + GEO.m.l + (c - state.i0 + 0.5) * GEO.step, rect.top + GEO.m.t + 16);
    }

    // ---------------------------------------------------------------- overview + zoom
    const ovEl = $('overview');
    const ovSvg = d3.select(ovEl).append('svg');
    const ovG = ovSvg.append('g');
    const ovArea = ovG.append('path').attr('class', 'ov-area');
    const ovLine = ovG.append('path').attr('class', 'ov-line');
    const ovSel = ovG.append('line').attr('class', 'sel-line');
    const ovLabel = ovG.append('text').attr('class', 'ov-label').attr('x', -12).attr('text-anchor', 'end');
    // Whatever is outside the window is dimmed; the handles go on top of the brush (drawing only: the
    // drag is caught by d3's .handle elements, widened so they are easy to grab).
    const ovShade = ovG.selectAll('rect.ov-shade').data([0, 1]).join('rect').attr('class', 'ov-shade');
    const gBrush = ovG.append('g').attr('class', 'brush');
    const ovHandles = ovG.append('g').attr('class', 'ov-handles').selectAll('g').data([0, 1]).join((e) => {
      const g = e.append('g').attr('class', 'ov-handle');
      g.append('rect').attr('x', -5).attr('width', 10).attr('rx', 3);
      g.append('line').attr('x1', -1.5).attr('x2', -1.5);
      g.append('line').attr('x1', 1.5).attr('x2', 1.5);
      return g;
    });
    let ovX = d3.scaleLinear(), ovW = 0;
    let brush = null, brushSilent = false;

    function placeHandles(sel) {
      const [x0, x1] = sel || [0, ovW];
      ovHandles.attr('transform', (d) => 'translate(' + r1(d ? x1 : x0) + ',0)');
      ovShade.attr('x', (d) => (d ? x1 : 0)).attr('width', (d) => Math.max(0, d ? ovW - x1 : x0));
    }

    function renderOverview() {
      const series = activeSeries();
      const m = GEO.m, W = Math.max(280, ovEl.clientWidth), H = 60, ih = H - 8;
      const iw = W - m.l - m.r;
      ovW = iw;
      ovShade.attr('y', 0).attr('height', ih);
      const gh = Math.min(34, ih - 8), gy = (ih - gh) / 2;
      ovHandles.select('rect').attr('y', gy).attr('height', gh);
      ovHandles.selectAll('line').attr('y1', ih / 2 - 6).attr('y2', ih / 2 + 6);
      ovSvg.attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
      ovG.attr('transform', 'translate(' + m.l + ',4)');
      const tot = new Float64Array(NC + 1);
      for (let c = 0; c < NC; c++) { let t = 0; for (const s of series) t += seriesTotal(s, state.metric, c); tot[c] = t; }
      tot[NC] = tot[NC - 1];
      ovX = d3.scaleLinear().domain([0, NC]).range([0, iw]);
      const y = d3.scaleLinear().domain([0, d3.max(tot) || 1]).range([ih, 0]);
      const area = d3.area().x((d, i) => ovX(i)).y0(ih).y1((d) => y(d)).curve(d3.curveStepAfter);
      const pts = Array.from(tot);
      ovArea.attr('d', area(pts));
      ovLine.attr('d', area.lineY1()(pts));
      ovSel.attr('y1', 0).attr('y2', ih);
      ovLabel.attr('y', ih / 2).attr('dy', '0.32em').text(msg('overview.label'));
      brush = d3.brushX().extent([[0, 0], [iw, ih]]).handleSize(14).on('start brush end', brushed);
      gBrush.call(brush).on('dblclick.reset', () => setRange(0, NC - 1));
      syncBrush();
      placeSelection();
    }
    // The window is always selected (also with the whole history) so the handles are visible.
    function syncBrush() {
      brushSilent = true;
      const full = state.i0 === 0 && state.i1 === NC - 1;
      gBrush.call(brush.move, full ? [0, ovW] : [ovX(state.i0), ovX(state.i1 + 1)]);
      brushSilent = false;
      $('reset-zoom').hidden = full;
    }
    function brushed(ev) {
      placeHandles(ev.selection);
      if (brushSilent || !ev.sourceEvent || ev.type === 'start') return;
      if (!ev.selection) { setRange(0, NC - 1); return; }
      const a = Math.max(0, Math.floor(ovX.invert(ev.selection[0])));
      const b = Math.min(NC - 1, Math.max(a, Math.ceil(ovX.invert(ev.selection[1])) - 1));
      setRange(a, b, ev.type === 'end');
    }
    function setRange(a, b, final) {
      state.i0 = a; state.i1 = b;
      const outside = state.sel < a || state.sel > b;
      if (outside) state.sel = Math.max(a, Math.min(b, state.sel));
      $('reset-zoom').hidden = a === 0 && b === NC - 1;
      renderMain();
      if (outside) { renderDetail(false); renderKpis(); }
      if (final === undefined) syncBrush();
    }
    $('reset-zoom').addEventListener('click', () => setRange(0, NC - 1));

    // ---------------------------------------------------------------- legend
    function renderLegend() {
      const box = $('legend');
      box.replaceChildren();
      for (const s of ALL_SERIES) {
        const b = el('button');
        b.type = 'button';
        const shown = s.langs.filter((l) => state.langs.has(l)).length;   // "Others" can be partly shown
        b.setAttribute('aria-pressed', shown === s.langs.length ? 'true' : shown ? 'mixed' : 'false');
        const sw = el('span', 'sw'); sw.style.background = s.color;
        b.append(sw, document.createTextNode(s.name));
        b.title = (s.members ? s.members.join(', ') + '. ' : '') + msg('legend.toggle');
        b.addEventListener('click', () => {
          const next = new Set(state.langs);
          for (const l of s.langs) { if (shown === s.langs.length) next.delete(l); else next.add(l); }
          setLangs(next);   // hiding the last shown group does nothing
        });
        box.appendChild(b);
      }
    }

    // ---------------------------------------------------------------- commit detail (playback)
    const barsEl = $('bars');
    const bSvg = d3.select(barsEl).append('svg');
    function hbar(x0, y, w, h) {
      const r = Math.min(4, w / 2, h / 2);
      return 'M' + r1(x0) + ',' + r1(y) + 'H' + r1(x0 + w - r) +
        'A' + r1(r) + ',' + r1(r) + ' 0 0 1 ' + r1(x0 + w) + ',' + r1(y + r) + 'V' + r1(y + h - r) +
        'A' + r1(r) + ',' + r1(r) + ' 0 0 1 ' + r1(x0 + w - r) + ',' + r1(y + h) + 'H' + r1(x0) + 'Z';
    }
    function duration() { return state.playing ? Math.min(250, 900 / state.speed) : 250; }

    // Subject and commit data on a single line each, with "…" when they do not fit (so the panel
    // does not jump during playback). When cut, the full text shows in a tooltip on hover or on
    // keyboard focus; screen readers already read the whole text.
    function cutTip(e) {
      let open = false;
      const isCut = () => e.scrollWidth > e.clientWidth + 1;
      const show = (cx, cy) => {
        if (!isCut()) return;
        const tip = $('tooltip');
        tip.replaceChildren(el('div', 't-full', e.textContent));
        placeTip(tip, cx, cy);
        open = true;
      };
      const hide = () => { if (open) { hideTip(); open = false; } };
      e.addEventListener('pointermove', (ev) => show(ev.clientX, ev.clientY));
      e.addEventListener('pointerleave', hide);
      e.addEventListener('focus', () => { const r = e.getBoundingClientRect(); show(r.left, r.bottom); });
      e.addEventListener('blur', hide);
      // After the text changes (another commit, another language or another width).
      return function refresh() {
        const cut = isCut();
        e.classList.toggle('is-cut', cut);
        if (cut) e.tabIndex = 0; else e.removeAttribute('tabindex');
        if (!open) return;
        const box = $('tooltip').firstChild;
        if (cut && box && box.classList.contains('t-full')) box.textContent = e.textContent;
        else hide();
      };
    }
    const refreshMetaCut = cutTip($('d-meta')), refreshSubjectCut = cutTip($('d-subject'));

    function renderDetail(animate) {
      const c = state.sel, row = ROWS[c], metric = state.metric;
      $('d-title').textContent = msg('detail.title', fmtInt(c + 1), fmtInt(NC));
      $('d-meta').textContent = row[0].slice(0, 10) + ' · ' + fmtDateTime(TS[c]) + ' · ' + row[2];
      $('d-subject').textContent = row[3];
      refreshMetaCut();
      refreshSubjectCut();

      const items = [];
      for (let l = 0; l < NL; l++) { const v = val(metric, c, l); if (v > 0) items.push({ l, v }); }
      items.sort((a, b) => b.v - a.v || d3.ascending(LANGS[a.l], LANGS[b.l]));
      const W = Math.max(240, barsEl.clientWidth), rowH = 26;
      const labelW = Math.min(170, Math.round(W * 0.36)), valW = 76;
      const H = Math.max(rowH, items.length * rowH);
      bSvg.attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
      const x = d3.scaleLinear().domain([0, d3.max(items, (d) => d.v) || 1]).range([0, Math.max(10, W - labelW - valW)]);
      const t = bSvg.transition().duration(animate ? duration() : 0).ease(d3.easeLinear);
      const rows = bSvg.selectAll('g.row').data(items, (d) => d.l);
      const enter = rows.enter().append('g').attr('class', 'row')
        .attr('transform', (d, i) => 'translate(0,' + i * rowH + ')').style('opacity', 0);
      enter.append('text').attr('class', 'lab').attr('x', labelW - 8).attr('y', rowH / 2).attr('dy', '0.35em').attr('text-anchor', 'end');
      enter.append('path').attr('class', 'bar').attr('d', hbar(labelW, rowH / 2 - 7, 0, 14));
      enter.append('text').attr('class', 'val').attr('y', rowH / 2).attr('dy', '0.35em').attr('x', labelW + 6);
      const all = enter.merge(rows);
      all.select('text.lab').text((d) => {
        const s = LANGS[d.l];
        return s.length > 22 ? s.slice(0, 21) + '…' : s;
      });
      all.select('path.bar').style('fill', (d) => COLOR[d.l]);
      all.transition(t)
        .attr('transform', (d, i) => 'translate(0,' + i * rowH + ')')
        .style('opacity', (d) => (!allLangs() && !state.langs.has(d.l) ? 0.35 : 1));
      all.select('path.bar').transition(t).attr('d', (d) => hbar(labelW, rowH / 2 - 7, x(d.v), 14));
      all.select('text.val').transition(t)
        .attr('x', (d) => labelW + x(d.v) + 6)
        .textTween(function (d) {
          const i = d3.interpolateRound(this.__v || 0, d.v);
          this.__v = d.v;
          return (tt) => fmtOf(metric)(i(tt));
        });
      rows.exit().transition(t).style('opacity', 0).remove();
      if (!items.length) {
        bSvg.selectAll('text.empty-msg').data([0]).join('text').attr('class', 'lab empty-msg').attr('x', 0).attr('y', rowH / 2).attr('dy', '0.35em')
          .text(msg('detail.empty'));
      } else bSvg.selectAll('text.empty-msg').remove();
      renderTable();
    }

    // Table order: the clicked column (state.sort) or, if none was clicked, the chosen metric's
    // column from highest to lowest ("total lines" sorts the same as "% lines").
    const METRIC_COL = { files: 'f', blank: 'b', comment: 'cm', code: 'cd', size: 'z', lines: 'ln' };
    const SORT_VALUE = {
      name: (r) => LANGS[r.l], f: (r) => r.f, b: (r) => r.b, cm: (r) => r.cm, cd: (r) => r.cd, ln: (r) => r.ln,
      z: (r) => r.z, p: (r) => r.p, delta: (r) => r.delta
    };
    const sortHeaders = document.querySelectorAll('#lang-table thead th[data-sort]');
    sortHeaders.forEach((th) => {
      th.querySelector('button').addEventListener('click', () => {
        const key = th.dataset.sort, current = state.sort || METRIC_COL[state.metric];
        if (key === current) state.sortDir = state.sort ? -state.sortDir : 1;
        else state.sortDir = key === 'name' ? 1 : -1;
        state.sort = key;
        renderTable();
      });
    });
    const byName = (a, b) => LANGS[a.l].localeCompare(LANGS[b.l], UI, { sensitivity: 'base' });

    function renderTable() {
      const c = state.sel, metric = state.metric;
      $('th-delta').querySelector('button').textContent = 'Δ ' + METRICS[metric].short;
      $('th-delta').title = msg('th.deltaTitle', METRICS[metric].noun);
      const rows = [];
      for (let l = 0; l < NL; l++) {
        const o = c * NL + l;
        const f = V[0][o], b = V[1][o], cm = V[2][o], cd = V[3][o], z = V[4][o];
        const prev = c > 0 ? val(metric, c - 1, l) : (META.firstIsRoot ? 0 : NaN);
        const delta = val(metric, c, l) - prev;
        if (f > 0 || (delta && !Number.isNaN(delta))) rows.push({ l, f, b, cm, cd, ln: b + cm + cd, z, p: f > 0 ? PL[o] : 0, delta });
      }
      const key = state.sort || METRIC_COL[metric], dir = state.sort ? state.sortDir : -1, get = SORT_VALUE[key];
      rows.sort((a, b) => {
        if (key === 'name') return dir * byName(a, b);
        const x = get(a), y = get(b), nx = Number.isNaN(x), ny = Number.isNaN(y);
        if (nx || ny) return nx === ny ? byName(a, b) : nx ? 1 : -1;   // no value (Δ of the first commit) goes last
        return dir * (x - y) || byName(a, b);
      });
      sortHeaders.forEach((th) => th.setAttribute('aria-sort',
        th.dataset.sort === key ? (dir > 0 ? 'ascending' : 'descending') : 'none'));
      const tb = document.querySelector('#lang-table tbody'), tf = document.querySelector('#lang-table tfoot');
      tb.replaceChildren(); tf.replaceChildren();
      const tot = { f: 0, b: 0, cm: 0, cd: 0, ln: 0, z: 0, p: 0, delta: 0 };
      for (const r of rows) {
        const tr = el('tr');
        if (!allLangs() && state.langs.has(r.l)) tr.className = 'focus';
        const name = el('td'); const sw = el('span', 'sw'); sw.style.background = COLOR[r.l];
        name.append(sw, document.createTextNode(LANGS[r.l])); name.title = LANGS[r.l];
        tr.append(name, el('td', null, fmtInt(r.f)), el('td', null, fmtInt(r.b)), el('td', null, fmtInt(r.cm)), el('td', null, fmtInt(r.cd)),
          el('td', null, fmtInt(r.ln)), el('td', null, HAS_SIZE ? fmtBytes(r.z) : '—'), el('td', null, r.f > 0 ? fmtPct(r.p) : '—'),
          el('td', deltaCls(r.delta), Number.isNaN(r.delta) ? '—' : r.delta ? fmtDeltaOf(metric)(r.delta) : '0'));
        tb.appendChild(tr);
        tot.f += r.f; tot.b += r.b; tot.cm += r.cm; tot.cd += r.cd; tot.ln += r.ln; tot.z += r.z; tot.p += r.p; if (!Number.isNaN(r.delta)) tot.delta += r.delta;
      }
      const tr = el('tr');
      tr.append(el('td', null, msg('total')), el('td', null, fmtInt(tot.f)), el('td', null, fmtInt(tot.b)), el('td', null, fmtInt(tot.cm)),
        el('td', null, fmtInt(tot.cd)), el('td', null, fmtInt(tot.ln)), el('td', null, HAS_SIZE ? fmtBytes(tot.z) : '—'),
        el('td', null, rows.length ? fmtPct(tot.p) : '—'), el('td', deltaCls(tot.delta), tot.delta ? fmtDeltaOf(metric)(tot.delta) : '0'));
      tf.appendChild(tr);
    }

    function renderKpis() {
      const c = state.sel;
      const langs = [...state.langs];
      const tot = (metric, cc) => { let t = 0; for (const l of langs) t += val(metric, cc, l); return t; };
      const box = $('kpis');
      box.replaceChildren();
      // [text, class]: the class colours a change up or down.
      const change = (delta, fmt) => (delta == null ? [msg('kpi.first')]
        : delta === 0 ? [msg('kpi.nochange')] : [(delta > 0 ? '▲ ' : '▼ ') + msg('kpi.vsPrev', fmt(delta)), deltaCls(delta)]);
      const tile = (label, value, delta, share) => {
        const t = el('div', 'tile');
        t.append(el('div', 'k', label), el('div', 'v', value));
        if (share != null) t.append(el('div', 'd', share));
        t.append(el('div', delta[1] ? 'd ' + delta[1] : 'd', delta[0]));
        box.appendChild(t);
      };
      // % of each line type over the commit total (static JSON data; only with every language shown).
      const SHARE = { blank: 0, comment: 1, code: 2 };
      for (const [k, label] of [['files', msg('metric.files')], ['code', msg('metric.code')], ['comment', msg('metric.comment')], ['blank', msg('metric.blank')]]) {
        const share = allLangs() && k in SHARE ? msg('kpi.ofLines', fmtPct(PT[c * 3 + SHARE[k]])) : null;
        tile(label, fmtInt(tot(k, c)), change(c > 0 ? tot(k, c) - tot(k, c - 1) : null, fmtSigned), share);
      }
      if (HAS_SIZE) tile(msg('metric.size'), fmtBytes(tot('size', c)), change(c > 0 ? tot('size', c) - tot('size', c - 1) : null, fmtBytesSigned));
      if (allLangs()) {
        const count = (cc) => { let n = 0; for (let l = 0; l < NL; l++) if (V[0][cc * NL + l] > 0) n++; return n; };
        tile(msg('kpi.langs'), fmtInt(count(c)), change(c > 0 ? count(c) - count(c - 1) : null, fmtSigned));
      } else {
        // Share of the commit's lines of the shown languages: the static per-language % add up.
        const weightAt = (cc) => { let p = 0; for (const l of langs) p += PL[cc * NL + l]; return p; };
        const p = weightAt(c);
        const pts = (v) => (v > 0 ? '+' : '') + loc.format(',.1f')(v) + ' pp';
        tile(msg('kpi.weight'), fmtPct(p), change(c > 0 ? Math.round((p - weightAt(c - 1)) * 10) / 10 : null, pts));
      }
    }

    function select(c) {
      if (c === state.sel) return;
      state.sel = c;
      placeSelection();
      renderDetail(true);
      renderKpis();
    }

    // ---------------------------------------------------------------- playback (gource style)
    let timer = null;
    function paintPlay() {
      $('p-play').textContent = msg(state.playing ? 'player.pause' : 'player.play');
      $('p-play').setAttribute('aria-label', msg(state.playing ? 'player.pauseAria' : 'player.playAria'));
    }
    function play() {
      if (state.sel >= state.i1) state.sel = state.i0 - 1;
      state.playing = true;
      paintPlay();
      const tick = () => {
        if (state.sel >= state.i1) { stop(); return; }
        select(state.sel + 1);
        timer = setTimeout(tick, 1000 / state.speed);
      };
      tick();
    }
    function stop() {
      if (timer) clearTimeout(timer);
      timer = null;
      if (!state.playing) return;
      state.playing = false;
      paintPlay();
    }
    function togglePlay() { if (state.playing) stop(); else play(); }
    paintPlay();   // the button text depends on the state and the language: it is not a fixed HTML text
    $('p-play').addEventListener('click', togglePlay);
    $('p-first').addEventListener('click', () => { stop(); select(state.i0); });
    $('p-prev').addEventListener('click', () => { stop(); select(Math.max(state.i0, state.sel - 1)); });
    $('p-next').addEventListener('click', () => { stop(); select(Math.min(state.i1, state.sel + 1)); });
    $('p-last').addEventListener('click', () => { stop(); select(state.i1); });
    $('p-speed').addEventListener('change', (e) => { state.speed = +e.target.value; });

    // ---------------------------------------------------------------- filters
    function titles() {
      const mt = METRICS[state.metric];
      let t = state.mode === 'delta' ? msg('title.delta', mt.noun) : msg('title.total', mt.label);
      t += allLangs() ? msg('title.byLang') : ' · ' + langSummary();
      $('chart-title').textContent = t;
    }
    function refresh() {
      titles();
      renderLegend();
      renderMain();
      renderOverview();
      renderDetail(false);
      renderKpis();
    }
    $('f-metric').addEventListener('change', (e) => { state.metric = e.target.value; refresh(); });
    $('f-mode').addEventListener('change', (e) => { state.mode = e.target.value; refresh(); });

    // ---------------------------------------------------------------- CSV
    $('csv').addEventListener('click', () => {
      const q = (s) => (/[",;\n\r]/.test(String(s)) ? '"' + String(s).replace(/"/g, '""') + '"' : String(s));
      const out = ['commit,date,author,subject,language,files,blank,comment,code,bytes'];
      for (let c = 0; c < NC; c++) {
        const r = ROWS[c], date = new Date(TS[c]).toISOString();
        for (const l of ORDER) {
          const o = c * NL + l;
          if (V[0][o] > 0) out.push([r[0], date, q(r[2]), q(r[3]), q(LANGS[l]), V[0][o], V[1][o], V[2][o], V[3][o], HAS_SIZE ? V[4][o] : ''].join(','));
        }
      }
      const blob = new Blob(['\ufeff' + out.join('\n') + '\n'], { type: 'text/csv;charset=utf-8' });
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = (META.repo || 'repo') + '-cloc-history.csv';
      document.body.appendChild(a); a.click(); a.remove();
      setTimeout(() => URL.revokeObjectURL(a.href), 2000);
    });

    // ================================================================ activity (like GitLab)
    // GitLab's "Repository analytics" and "Contributor analytics" without their commit limit. The
    // data is computed by the script ("activity" and "languages" blocks of the JSON); with old data
    // that lacks them, the activity is hidden and the languages come from the last commit.
    // Branches (format 4): [name, commit, commit date, commits, own, start, created (reflog),
    // from, included in --rev, current, kind]. "when" is when it appeared: from the reflog while it
    // still keeps it; otherwise its first own commit; if it has none, the commit it points to.
    const BRANCHES = (RAW.branches || []).map((b, i) => ({
      i, name: b[0], tip: b[1], tipTs: b[2], total: b[3], own: b[4], start: b[5], created: b[6], from: b[7],
      inRev: !!b[8], current: !!b[9], kind: b[10],
      when: b[6] != null ? b[6] : b[5] != null ? b[5] : b[2],
      how: b[6] != null ? 'reflog' : b[5] != null ? 'own' : 'tip'
    }));
    // Log of the commits without merges of every branch: [date, author, branch mask (30 bits per word)...].
    const LOG = RAW.log && BRANCHES.length ? RAW.log : null;
    const REV_BRANCH = BRANCHES.find((b) => b.current) || null;
    let curBranch = REV_BRANCH ? REV_BRANCH.i : -1;
    let showBranches = true;
    const inBranch = (j, bi) => (LOG.commits[j + 2 + Math.floor(bi / 30)] >> (bi % 30)) & 1;
    // Ahead of / behind the analyzed branch, in commits without merges.
    if (LOG && REV_BRANCH) {
      const step = 2 + LOG.words;
      for (const b of BRANCHES) { b.ahead = 0; b.behind = 0; }
      for (let j = 0; j < LOG.commits.length; j += step) {
        const inRevC = inBranch(j, REV_BRANCH.i);
        for (const b of BRANCHES) {
          const inB = inBranch(j, b.i);
          if (inB && !inRevC) b.ahead++;
          else if (!inB && inRevC) b.behind++;
        }
      }
    }

    // Same as the JSON "activity" block but for any branch, computed from the log.
    function aggregateBranch(bi) {
      const step = 2 + LOG.words, c = LOG.commits;
      const weekday = new Array(7).fill(0), hour = new Array(24).fill(0), monthDay = new Array(31).fill(0);
      const days = new Map(), people = new Map();
      let n = 0, d0 = Infinity, d1 = -Infinity;
      for (let j = 0; j < c.length; j += step) {
        if (!inBranch(j, bi)) continue;
        const ts = c[j], a = c[j + 1], day = Math.floor(ts / 86400), dt = new Date(ts * 1000);
        n++;
        weekday[(day + 3) % 7]++;   // 1970-01-01 was a Thursday; 0 = Monday
        hour[dt.getUTCHours()]++;
        monthDay[dt.getUTCDate() - 1]++;
        days.set(day, (days.get(day) || 0) + 1);
        let p = people.get(a);
        if (!p) { p = { n: 0, first: day, last: day, days: new Map() }; people.set(a, p); }
        p.n++; p.first = Math.min(p.first, day); p.last = Math.max(p.last, day);
        p.days.set(day, (p.days.get(day) || 0) + 1);
        d0 = Math.min(d0, day); d1 = Math.max(d1, day);
      }
      if (!n) return null;
      const flat = (m) => [...m.entries()].sort((x, y) => x[0] - y[0]).flat();
      const plist = [...people.entries()]
        .map(([a, q]) => [LOG.authors[a][0], LOG.authors[a][1], q.n, q.first, q.last, flat(q.days)])
        .sort((x, y) => y[2] - x[2] || x[0].localeCompare(y[0], UI, { sensitivity: 'base' }));
      return { tz: 'UTC', commits: n, authors: plist.length, firstDay: d0, lastDay: d1, avgPerDay: n / (d1 - d0 + 1),
        weekday, hour, monthDay, days: flat(days), people: plist };
    }
    let ACT = LOG && curBranch >= 0 ? aggregateBranch(curBranch)
      : RAW.activity && RAW.activity.commits ? RAW.activity : null;
    const DAY_MS = 864e5;
    const dayDate = (d) => new Date(d * DAY_MS);
    const fmtDayU = (d) => F.dayU(d), fmtMonthU = (d) => F.monthU(d), fmtYearU = (d) => F.yearU(d);
    const fmtTickU = (d) => (d3.utcMonth(d) < d ? F.tickDayU : d3.utcYear(d) < d ? F.monthShortU : F.yearU)(d);
    const fmtDec = (v) => F.dec(v);
    const activityCharts = [];
    let actTime = null;   // overall histogram: its window drives the activity KPIs

    function placeTip(tip, cx, cy) {
      tip.hidden = false;
      const pad = 14, w = tip.offsetWidth, h = tip.offsetHeight;
      let x = cx + pad, yy = cy + pad;
      if (x + w > window.innerWidth - 8) x = Math.max(8, cx - pad - w);
      if (yy + h > window.innerHeight - 8) yy = Math.max(8, cy - pad - h);
      tip.style.left = x + 'px'; tip.style.top = yy + 'px';
    }
    function simpleTip(lines, cx, cy) {
      const tip = $('tooltip');
      tip.replaceChildren(el('div', 't-head', lines[0]));
      for (const t of lines.slice(1)) tip.append(el('div', 't-meta', t));
      placeTip(tip, cx, cy);
    }

    // ---------------------------------------------------------------- branch marks
    const revName = REV_BRANCH ? REV_BRANCH.name : META.rev;
    function whenText(b) {
      if (b.how === 'reflog') return msg('br.when.reflog', fmtDateTime(new Date(b.when * 1000)), b.from ? msg('br.when.from', b.from) : '');
      if (b.how === 'own') return msg('br.when.own', fmtDayU(new Date(b.when * 1000)));
      return msg('br.when.tip', fmtDayU(new Date(b.when * 1000)));
    }
    function relText(b) {
      if (b.current) return msg('br.rel.current');
      if (b.ahead == null) return msg(b.inRev ? 'br.rel.in' : 'br.rel.notIn', revName);
      const commits = (n) => msgN('commits', n, fmtInt(n));
      if (b.inRev) return msg('br.rel.in', revName) + (b.behind ? msg('br.rel.behind', commits(b.behind)) : '');
      if (!b.ahead) return msg('br.rel.mergesOnly', revName, commits(b.behind));
      return msg('br.rel.aheadBehind', commits(b.ahead), fmtInt(b.behind), revName);
    }
    function branchTip(gr, cx, cy) {
      const tip = $('tooltip');
      tip.replaceChildren();
      gr.branches.forEach((b, k) => {
        const box = el('div', k ? 't-branch t-total' : 't-branch');
        box.append(el('div', 't-head', b.name + (b.i === curBranch ? msg('br.selected') : '')),
          el('div', 't-meta', whenText(b)),
          el('div', 't-meta', msgN('br.own', b.own, fmtInt(b.own)) + msg('br.total', fmtInt(b.total))),
          el('div', 't-meta', msg('br.points', b.tip.slice(0, 10), fmtDayU(new Date(b.tipTs * 1000)))),
          el('div', 't-meta', relText(b)));
        tip.append(box);
      });
      placeTip(tip, cx, cy);
    }
    // Branches grouped by the day they appear (UTC) and by the commit in the code chart.
    const BRANCH_BY_DAY = [...d3.group(BRANCHES, (b) => Math.floor(b.when / 86400))]
      .map(([day, list]) => ({ key: 'd' + day, ts: d3.min(list, (b) => b.when), branches: list.sort((a, b) => a.when - b.when) }))
      .sort((a, b) => a.ts - b.ts);
    const BRANCH_BY_COMMIT = [...d3.group(BRANCHES, (b) => Math.min(NC - 1, d3.bisectLeft(TS, b.when * 1000)))]
      .map(([idx, list]) => ({ key: 'c' + idx, idx, branches: list.sort((a, b) => a.when - b.when) }));
    // One line per group with a mark on top; the hit area is wider than the line.
    function drawBranchMarks(gm, groups, xOf, ih, iw) {
      const vis = showBranches ? groups.filter((gr) => { const x = xOf(gr); return x >= 0 && x <= iw; }) : [];
      const sel = gm.selectAll('g.bmark').data(vis, (gr) => gr.key).join((e) => {
        const k = e.append('g').attr('class', 'bmark');
        k.append('line');
        k.append('path').attr('class', 'flag').attr('d', 'M0,-5L4,-1L0,3L-4,-1Z');
        k.append('rect').attr('class', 'bmark-hit').attr('x', -6).attr('width', 12).attr('y', -7);
        return k;
      });
      sel.attr('transform', (gr) => 'translate(' + (Math.round(xOf(gr)) + 0.5) + ',0)')
        .classed('is-current', (gr) => gr.branches.some((b) => b.i === curBranch));
      sel.select('line').attr('y1', 0).attr('y2', ih);
      sel.select('rect.bmark-hit').attr('height', ih + 7)
        .on('pointermove', (ev, gr) => branchTip(gr, ev.clientX, ev.clientY))
        .on('pointerleave', hideTip);
    }
    const hideTip = () => { $('tooltip').hidden = true; };

    // Handles of a minimap window (drawing only; the drag is caught by d3's .handle elements).
    function windowHandles(g) {
      return g.append('g').attr('class', 'ov-handles').selectAll('g').data([0, 1]).join((e) => {
        const h = e.append('g').attr('class', 'ov-handle');
        h.append('rect').attr('x', -5).attr('width', 10).attr('rx', 3);
        h.append('line').attr('x1', -1.5).attr('x2', -1.5);
        h.append('line').attr('x1', 1.5).attr('x2', 1.5);
        return h;
      });
    }

    // Table with a chart's data (the accessible alternative to the tooltip).
    function dataTable(host, head, rows) {
      const det = host.appendChild(el('details', 'data-toggle'));
      det.appendChild(el('summary', null, msg('dt.see')));
      const wrap = det.appendChild(el('div', 'table-wrap'));
      const table = wrap.appendChild(el('table'));
      const tr = table.appendChild(el('thead')).appendChild(el('tr'));
      head.forEach((h) => { const th = el('th', null, h); th.scope = 'col'; tr.appendChild(th); });
      const tb = table.appendChild(el('tbody'));
      rows.forEach((r) => { const row = tb.appendChild(el('tr')); r.forEach((v) => row.appendChild(el('td', null, v))); });
    }

    // Commits-over-time histogram + minimap with a draggable window (like "Contributor
    // analytics"). days = [day, n, day, n, ...] (days since 1970, UTC). The domain is the whole
    // repository's, so each author's histogram can be compared with the overall one.
    function timeHistogram(host, days, opts) {
      const D0 = ACT.firstDay, D1 = ACT.lastDay, N = D1 - D0 + 1;
      const pref = new Float64Array(N + 1);   // prefix sums: commits in [a, b) in O(1)
      for (let i = 0; i + 1 < days.length; i += 2) pref[days[i] - D0 + 1] += days[i + 1];
      for (let i = 1; i <= N; i++) pref[i] += pref[i - 1];
      const sum = (a, b) => pref[Math.max(0, Math.min(N, b - D0))] - pref[Math.max(0, Math.min(N, a - D0))];
      const H = opts.height, m = { t: 10, r: 12, b: 26, l: 52 }, OH = 44, oh = OH - 8;
      const svg = d3.select(host).append('svg');
      const g = svg.append('g');
      const gGrid = g.append('g').attr('class', 'grid');
      const hover = g.append('rect').attr('class', 'hover-col').attr('visibility', 'hidden');
      const bars = g.append('path').style('fill', 'var(--s1)');
      const gX = g.append('g').attr('class', 'xaxis');
      const overlay = g.append('rect').attr('class', 'overlay');
      const gMarks = g.append('g').attr('class', 'branch-marks');
      const ov = d3.select(host).append('svg').attr('class', 'th-ov');
      const og = ov.append('g');
      const ovArea = og.append('path').attr('class', 'ov-area');
      const ovLine = og.append('path').attr('class', 'ov-line');
      const shade = og.selectAll('rect.ov-shade').data([0, 1]).join('rect').attr('class', 'ov-shade');
      const gBrush = og.append('g').attr('class', 'brush');
      const handles = windowHandles(og);
      let w0 = D0, w1 = D1 + 1, ovW = 0, ovx = null, brush = null, silent = false, view = null;

      // Bars of at least ~6 px: per day, week, month or year depending on the visible range.
      function binning(span, iw) {
        const perBar = span / Math.max(1, iw / 6);
        if (perBar <= 1) return { iv: d3.utcDay, label: fmtDayU };
        if (perBar <= 7) return { iv: d3.utcMonday, label: (d) => msg('week.of', fmtDayU(d)) };
        if (perBar <= 31) return { iv: d3.utcMonth, label: fmtMonthU };
        return { iv: d3.utcYear, label: fmtYearU };
      }
      function drawMain() {
        const W = Math.max(280, host.clientWidth), iw = W - m.l - m.r, ih = H - m.t - m.b;
        svg.attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
        g.attr('transform', 'translate(' + m.l + ',' + m.t + ')');
        const t0 = dayDate(w0), t1 = dayDate(w1);
        const x = d3.scaleUtc().domain([t0, t1]).range([0, iw]);
        const B = binning(w1 - w0, iw);
        const bins = B.iv.range(B.iv.floor(t0), t1).map((b) => {
          const e = B.iv.offset(b, 1);
          const a = Math.max(w0, Math.round(+b / DAY_MS)), z = Math.min(w1, Math.round(+e / DAY_MS));
          return { b, xa: x(new Date(Math.max(+b, +t0))), xb: x(new Date(Math.min(+e, +t1))), n: sum(a, z) };
        });
        const y = d3.scaleLinear().domain([0, d3.max(bins, (d) => d.n) || 1]).nice(4).range([ih, 0]);
        gGrid.call(d3.axisLeft(y).ticks(4).tickSize(-iw).tickPadding(8).tickFormat((v) => (Number.isInteger(v) ? fmtInt(v) : '')));
        let d = '';
        for (const bn of bins) {
          if (!bn.n) continue;
          const w = bn.xb - bn.xa, bw = Math.min(24, Math.max(0.5, w - (w >= 6 ? 2 : 0)));
          d += seg(bn.xa + (w - bw) / 2, y(bn.n), ih, bw, bw >= 8 ? 4 : 0, 0);
        }
        bars.attr('d', d);
        gX.attr('transform', 'translate(0,' + ih + ')')
          .call(d3.axisBottom(x).ticks(Math.max(2, Math.floor(iw / 100))).tickFormat(fmtTickU).tickSizeOuter(0));
        overlay.attr('width', iw).attr('height', ih);
        hover.attr('visibility', 'hidden').attr('y', 0).attr('height', ih);
        drawBranchMarks(gMarks, BRANCH_BY_DAY, (gr) => x(new Date(gr.ts * 1000)), ih, iw);
        view = { x, bins, B };
      }
      overlay
        .on('pointermove', (ev) => {
          if (!view || !view.bins.length) return;
          const [mx] = d3.pointer(ev, g.node());
          const i = Math.max(0, d3.bisectRight(view.bins.map((b) => b.xa), mx) - 1), bn = view.bins[i];
          hover.attr('visibility', 'visible').attr('x', bn.xa).attr('width', Math.max(1, bn.xb - bn.xa));
          simpleTip([view.B.label(bn.b), msgN('commits', bn.n, fmtInt(bn.n))], ev.clientX, ev.clientY);
        })
        .on('pointerleave', () => { hover.attr('visibility', 'hidden'); hideTip(); });

      function drawOverview() {
        const W = Math.max(280, host.clientWidth), iw = W - m.l - m.r;
        ov.attr('width', W).attr('height', OH).attr('viewBox', '0 0 ' + W + ' ' + OH);
        og.attr('transform', 'translate(' + m.l + ',4)');
        ovW = iw;
        ovx = d3.scaleLinear().domain([D0, D1 + 1]).range([0, iw]);
        const step = Math.max(1, Math.ceil(N / Math.max(1, iw / 2)));
        const pts = [];
        for (let a = D0; a <= D1; a += step) pts.push([a, sum(a, a + step)]);
        pts.push([D1 + 1, pts.length ? pts[pts.length - 1][1] : 0]);
        const oy = d3.scaleLinear().domain([0, d3.max(pts, (p) => p[1]) || 1]).range([oh, 0]);
        const area = d3.area().x((p) => ovx(p[0])).y0(oh).y1((p) => oy(p[1])).curve(d3.curveStepAfter);
        ovArea.attr('d', area(pts));
        ovLine.attr('d', area.lineY1()(pts));
        shade.attr('y', 0).attr('height', oh);
        const gh = Math.min(28, oh - 6);
        handles.select('rect').attr('y', (oh - gh) / 2).attr('height', gh);
        handles.selectAll('line').attr('y1', oh / 2 - 5).attr('y2', oh / 2 + 5);
        brush = d3.brushX().extent([[0, 0], [iw, oh]]).handleSize(14).on('start brush end', brushed);
        gBrush.call(brush).on('dblclick.reset', () => setWindow(D0, D1 + 1));
        syncBrush();
      }
      function place(sel) {
        const [a, b] = sel || [0, ovW];
        handles.attr('transform', (k) => 'translate(' + r1(k ? b : a) + ',0)');
        shade.attr('x', (k) => (k ? b : 0)).attr('width', (k) => Math.max(0, k ? ovW - b : a));
      }
      function syncBrush() { silent = true; gBrush.call(brush.move, [ovx(w0), ovx(w1)]); silent = false; }
      function brushed(ev) {
        place(ev.selection);
        if (silent || !ev.sourceEvent || ev.type === 'start') return;
        if (!ev.selection) { setWindow(D0, D1 + 1); return; }
        const a = Math.max(D0, Math.floor(ovx.invert(ev.selection[0])));
        w0 = a; w1 = Math.min(D1 + 1, Math.max(a + 1, Math.ceil(ovx.invert(ev.selection[1]))));
        drawMain();
        if (opts.onWindow) opts.onWindow(w0, w1);
      }
      function setWindow(a, b) {
        w0 = a; w1 = b; drawMain(); syncBrush();
        if (opts.onWindow) opts.onWindow(w0, w1);
      }
      function render() { drawMain(); drawOverview(); }
      render();
      return { render, setWindow };
    }

    // Columns per category (day of the month, weekday, hour) with a tooltip and a data table.
    function categoryBars(host, labels, values, opts) {
      const total = d3.sum(values) || 1;
      const svg = d3.select(host).append('svg');
      const g = svg.append('g');
      const gGrid = g.append('g').attr('class', 'grid');
      const hover = g.append('rect').attr('class', 'hover-col').attr('visibility', 'hidden');
      const bars = g.append('g'), vals = g.append('g'), gX = g.append('g').attr('class', 'xaxis'), hits = g.append('g');
      const data = values.map((v, i) => ({ v, i }));
      function render() {
        const H = opts.height || 210, m = { t: 18, r: 8, b: 26, l: 48 };
        const W = Math.max(220, host.clientWidth), iw = W - m.l - m.r, ih = H - m.t - m.b;
        svg.attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
        g.attr('transform', 'translate(' + m.l + ',' + m.t + ')');
        const n = values.length, step = iw / n, bw = Math.min(24, step - 2), xo = (i) => i * step + (step - bw) / 2;
        const y = d3.scaleLinear().domain([0, d3.max(values) || 1]).nice(4).range([ih, 0]);
        gGrid.call(d3.axisLeft(y).ticks(4).tickSize(-iw).tickPadding(8).tickFormat((v) => (Number.isInteger(v) ? fmtInt(v) : '')));
        bars.selectAll('path').data(data).join('path').style('fill', 'var(--s1)')
          .attr('d', (d) => (d.v ? seg(xo(d.i), y(d.v), ih, bw, bw >= 8 ? 4 : 0, 0) : ''));
        const every = Math.max(1, Math.ceil(26 / step));   // axis labels: all of them if they fit
        gX.attr('transform', 'translate(0,' + ih + ')').selectAll('text').data(labels).join('text')
          .attr('x', (d, i) => r1(i * step + step / 2)).attr('y', 16).attr('text-anchor', 'middle')
          .text((d, i) => (i % every === 0 ? d : ''));
        vals.selectAll('text').data(opts.valueLabels ? data : []).join('text').attr('class', 'val')
          .attr('x', (d) => r1(d.i * step + step / 2)).attr('y', (d) => y(d.v) - 5).attr('text-anchor', 'middle')
          .text((d) => fmtInt(d.v));
        hover.attr('visibility', 'hidden').attr('y', 0).attr('height', ih).attr('width', step);
        hits.selectAll('rect').data(data).join('rect').attr('class', 'overlay')
          .attr('x', (d) => d.i * step).attr('width', step).attr('y', 0).attr('height', ih)
          .on('pointermove', (ev, d) => {
            hover.attr('visibility', 'visible').attr('x', d.i * step);
            simpleTip([opts.title(d.i), msgN('commits', d.v, fmtInt(d.v)) + ' · ' + fmtPct(d.v * 100 / total)], ev.clientX, ev.clientY);
          })
          .on('pointerleave', () => { hover.attr('visibility', 'hidden'); hideTip(); });
      }
      render();
      dataTable(host, [opts.column, 'Commits', '%'], data.map((d) => [opts.title(d.i), fmtInt(d.v), fmtPct(d.v * 100 / total)]));
      return { render };
    }

    // Languages of the last commit (static % from the JSON; with old data, computed from the last commit row).
    const LROWS = (() => {
      if (RAW.languages) {
        return RAW.languages.map((r) => ({ l: r[0], f: r[1], b: r[2], cm: r[3], cd: r[4], z: r[5], p: { lines: r[6], code: r[7], size: r[8] } }));
      }
      const c = NC - 1, rows = [];
      let tl = 0, tk = 0, tz = 0;
      for (let l = 0; l < NL; l++) {
        const o = c * NL + l;
        if (!(V[0][o] > 0)) continue;
        rows.push({ l, f: V[0][o], b: V[1][o], cm: V[2][o], cd: V[3][o], z: V[4][o] });
        tl += V[1][o] + V[2][o] + V[3][o]; tk += V[3][o]; tz += V[4][o];
      }
      const pc = (x, t) => (t > 0 ? x * 100 / t : 0);
      rows.forEach((r) => { r.p = { lines: pc(r.b + r.cm + r.cd, tl), code: pc(r.cd, tk), size: pc(r.z, tz) }; });
      return rows;
    })();
    const LANG_ABS = {
      code: (d) => msg('langs.abs.code', fmtInt(d.cd)),
      lines: (d) => msg('langs.abs.lines', fmtInt(d.b + d.cm + d.cd)),
      size: (d) => fmtBytes(d.z)
    };
    function renderLanguages() {
      const basis = $('lang-basis').value, host = $('lang-chart');
      const items = LROWS.filter((r) => r.p[basis] > 0).sort((a, b) => b.p[basis] - a.p[basis] || byName(a, b));
      const W = Math.max(260, host.clientWidth), rowH = 26, labelW = Math.min(170, Math.round(W * 0.3)), valW = 64;
      const H = Math.max(rowH, items.length * rowH);
      const svg = d3.select(host).selectAll('svg').data([0]).join('svg')
        .attr('width', W).attr('height', H).attr('viewBox', '0 0 ' + W + ' ' + H);
      const x = d3.scaleLinear().domain([0, d3.max(items, (d) => d.p[basis]) || 1]).range([0, Math.max(10, W - labelW - valW)]);
      const rows = svg.selectAll('g.row').data(items, (d) => d.l).join((e) => {
        const r = e.append('g').attr('class', 'row');
        r.append('rect').attr('class', 'row-hit');
        r.append('text').attr('class', 'lab').attr('text-anchor', 'end').attr('dy', '0.35em');
        r.append('path').attr('class', 'bar');
        r.append('text').attr('class', 'val').attr('dy', '0.35em');
        return r;
      });
      rows.attr('transform', (d, i) => 'translate(0,' + i * rowH + ')')
        .on('pointermove', (ev, d) => simpleTip([LANGS[d.l], fmtPct(d.p[basis]) + ' · ' + LANG_ABS[basis](d)], ev.clientX, ev.clientY))
        .on('pointerleave', hideTip);
      rows.select('rect.row-hit').attr('width', W).attr('height', rowH);
      rows.select('text.lab').attr('x', labelW - 8).attr('y', rowH / 2).text((d) => LANGS[d.l]);
      rows.select('path.bar').style('fill', (d) => COLOR[d.l]).attr('d', (d) => hbar(labelW, rowH / 2 - 7, x(d.p[basis]), 14));
      rows.select('text.val').attr('x', (d) => labelW + x(d.p[basis]) + 6).attr('y', rowH / 2).text((d) => fmtPct(d.p[basis]));
    }

    // Stats for any range of days [a, b): days with commits, sorted, with their running total,
    // and each author's days, sorted (binary search), so they are recomputed while dragging.
    let ACT_DAYS = [], ACT_CUM = [0], PEOPLE_DAYS = [];
    function prepareWindowStats() {
      ACT_DAYS = []; ACT_CUM = [0];
      for (let i = 0; i + 1 < (ACT ? ACT.days.length : 0); i += 2) {
        ACT_DAYS.push(ACT.days[i]);
        ACT_CUM.push(ACT_CUM[ACT_CUM.length - 1] + ACT.days[i + 1]);
      }
      PEOPLE_DAYS = ACT ? ACT.people.map((p) => Float64Array.from(p[5].filter((v, i) => i % 2 === 0)).sort()) : [];
    }
    function windowStats(a, b) {
      const i0 = d3.bisectLeft(ACT_DAYS, a), i1 = d3.bisectLeft(ACT_DAYS, b);
      let authors = 0;
      for (const ds of PEOPLE_DAYS) { const k = d3.bisectLeft(ds, a); if (k < ds.length && ds[k] < b) authors++; }
      return { commits: ACT_CUM[i1] - ACT_CUM[i0], active: i1 - i0, authors, span: b - a };
    }

    function renderActivityKpis(a, b) {
      const full = a <= ACT.firstDay && b >= ACT.lastDay + 1;
      const st = windowStats(a, b), days = (n) => msgN('days', n, fmtInt(n));
      const hint = $('act-window');
      hint.replaceChildren();
      if (full) {
        hint.textContent = msg('win.full');
      } else {
        const reset = el('button', 'link-btn', msg('btn.resetZoom'));
        reset.type = 'button';
        reset.addEventListener('click', () => actTime.setWindow(ACT.firstDay, ACT.lastDay + 1));
        hint.append(msg('win.range', fmtDayU(dayDate(a)), fmtDayU(dayDate(b - 1)), days(st.span)), reset);
      }
      const box = $('act-kpis');
      box.replaceChildren();
      const tile = (k, v, d) => {
        const t = el('div', 'tile');
        t.append(el('div', 'k', k), el('div', 'v', v));
        if (d) t.append(el('div', 'd', d));
        box.appendChild(t);
      };
      tile(msg('akpi.commits'), fmtInt(st.commits), full ? msg('akpi.commits.full') : msg('akpi.commits.part', fmtPct(st.commits * 100 / ACT.commits)));
      tile(msg('akpi.authors'), fmtInt(st.authors), full ? msg('akpi.authors.full') : msg('akpi.authors.part', fmtInt(ACT.authors)));
      tile(msg('akpi.avg'), fmtDec(st.commits / st.span), msg('akpi.avg.d', days(st.span)));
      tile(msg('akpi.active'), fmtInt(st.active), msg('akpi.active.d', fmtPct(st.active * 100 / st.span)));
      tile(msg('akpi.period'), fmtDayU(dayDate(a)), msg('akpi.period.d', fmtDayU(dayDate(b - 1))));
    }

    // Accordion with every author; each one's histogram is drawn when it is opened.
    function renderContributors() {
      const list = $('contrib-list');
      list.replaceChildren();
      list.classList.toggle('no-mail', NO_EMAILS);   // --no-emails: no email column
      ACT.people.forEach((p, i) => {
        const [name, email, n, first, last, days] = p;
        const det = el('details', 'contrib');
        const sum = el('summary');
        sum.append(el('span', 'c-rank', String(i + 1)), el('span', 'c-name', name || email || '—'));
        if (!NO_EMAILS) sum.append(el('span', 'c-mail', email));
        sum.append(el('span', 'c-n', msgN('commits', n, fmtInt(n)) + ' · ' + fmtPct(n * 100 / ACT.commits)),
          el('span', 'c-meta', fmtDayU(dayDate(first)) + ' – ' + fmtDayU(dayDate(last))));
        const body = el('div', 'c-body');
        det.append(sum, body);
        let chart = null;
        det.addEventListener('toggle', () => {
          if (!det.open) return;
          if (!chart) { chart = timeHistogram(body, days, { height: 170 }); activityCharts.push(chart); }
          else chart.render();
        });
        list.appendChild(det);
      });
    }

    // Branch table, in order of appearance until a header is clicked (clicking it again reverses the
    // order); clicking a branch filters the activity by it. Names and the source sort A→Z first;
    // numbers and dates, highest / latest first.
    const BR_ASC_FIRST = { name: true, how: true, rel: true };
    const brSort = { key: 'when', dir: 1 };
    const howText = (b) => msg({ reflog: 'br.how.reflog', own: 'br.how.own', tip: 'br.how.tip' }[b.how]);
    // Relative to the analyzed branch: the branch itself, then those it contains (fewest commits
    // behind first), then those with work of their own (fewest ahead first).
    const relKey = (b) => (b.current ? [-1, 0] : b.ahead == null ? [b.inRev ? 0 : Infinity, 0] : [b.ahead, b.behind]);
    const BR_SORT = {
      name: (x, y) => x.name.localeCompare(y.name, UI, { sensitivity: 'base' }),
      when: (x, y) => x.when - y.when,
      how: (x, y) => howText(x).localeCompare(howText(y), UI),
      own: (x, y) => x.own - y.own,
      total: (x, y) => x.total - y.total,
      last: (x, y) => x.tipTs - y.tipTs,
      rel: (x, y) => { const a = relKey(x), b = relKey(y); return (a[0] === b[0] ? 0 : a[0] - b[0]) || a[1] - b[1]; }
    };
    const brHeaders = document.querySelectorAll('#branch-table thead th[data-sort]');
    brHeaders.forEach((th) => {
      th.querySelector('button').addEventListener('click', () => {
        const key = th.dataset.sort;
        brSort.dir = key === brSort.key ? -brSort.dir : BR_ASC_FIRST[key] ? 1 : -1;
        brSort.key = key;
        renderBranchTable();
      });
    });

    function renderBranchTable() {
      const tb = document.querySelector('#branch-table tbody');
      tb.replaceChildren();
      const cmp = BR_SORT[brSort.key], dir = brSort.dir;
      brHeaders.forEach((th) => th.setAttribute('aria-sort',
        th.dataset.sort === brSort.key ? (dir > 0 ? 'ascending' : 'descending') : 'none'));
      for (const b of BRANCHES.slice().sort((x, y) => dir * cmp(x, y) || x.when - y.when || x.name.localeCompare(y.name))) {
        const tr = el('tr');
        if (b.i === curBranch) tr.className = 'focus';
        const name = el('td');
        const btn = el('button', 'link-btn', b.name);
        btn.type = 'button';
        btn.title = msg('br.view');
        btn.addEventListener('click', () => { setBranch(b.i); $('sec-activity').scrollIntoView({ behavior: 'smooth' }); });
        name.append(btn);
        if (b.current) name.append(el('span', 'tag', msg('br.analyzed')));
        const when = el('td', null, b.how === 'reflog' ? fmtDayU(new Date(b.when * 1000)) : '≈ ' + fmtDayU(new Date(b.when * 1000)));
        when.title = whenText(b);
        tr.append(name, when, el('td', 'muted', howText(b)),
          el('td', null, fmtInt(b.own)), el('td', null, fmtInt(b.total)), el('td', null, fmtDayU(new Date(b.tipTs * 1000))),
          el('td', 'rel', relText(b)));
        tb.appendChild(tr);
      }
    }

    function setBranch(bi) {
      if (!LOG || bi === curBranch) return;
      curBranch = bi;
      $('f-branch').value = String(bi);
      ACT = aggregateBranch(bi);
      renderActivityBody();
      renderBranchTable();
      renderMain();   // the selected branch's mark is highlighted in the code evolution chart too
    }

    // Everything that depends on the branch: KPIs, histograms, columns and contributors.
    function renderActivityBody() {
      activityCharts.length = 0;
      for (const id of ['act-time', 'act-monthday', 'act-weekday', 'act-hour', 'contrib-list']) $(id).replaceChildren();
      const bname = curBranch >= 0 ? BRANCHES[curBranch].name : META.rev;
      if (!ACT) {
        $('act-sub').textContent = LOG ? msg('act.empty', bname) : msg('act.none');
        for (const id of ['act-body', 'act-body2', 'contrib-sub', 'contrib-card']) $(id).hidden = true;
        return;
      }
      for (const id of ['act-body', 'act-body2', 'contrib-sub', 'contrib-card']) $(id).hidden = false;
      prepareWindowStats();
      $('act-sub').textContent = msg('act.sub', bname);
      $('contrib-sub').textContent = msgN('contrib.sub', ACT.authors, bname, fmtInt(ACT.authors));
      renderActivityKpis(ACT.firstDay, ACT.lastDay + 1);
      actTime = timeHistogram($('act-time'), ACT.days, { height: 240, onWindow: renderActivityKpis });
      activityCharts.push(actTime);
      const WD_SHORT = fmtOfLang().wdShort, WD_LONG = fmtOfLang().wdLong;
      const hh = (h) => String(h).padStart(2, '0');
      activityCharts.push(categoryBars($('act-monthday'), d3.range(1, 32).map(String), ACT.monthDay,
        { title: (i) => msg('cat.day', i + 1), column: msg('cat.col.monthday') }));
      activityCharts.push(categoryBars($('act-weekday'), WD_SHORT, ACT.weekday,
        { title: (i) => WD_LONG[i], column: msg('cat.col.weekday'), valueLabels: true }));
      activityCharts.push(categoryBars($('act-hour'), d3.range(24).map(hh), ACT.hour,
        { title: (i) => hh(i) + ':00–' + hh(i) + ':59 UTC', column: msg('cat.col.hour') }));
      renderContributors();
    }

    // Activity texts that are not rebuilt on redraw: the languages subtitle, the branch selector
    // options and the branch table explanation.
    function paintActivityTexts() {
      $('lang-sub').textContent = msg('langs.sub', ROWS[NC - 1][0].slice(0, 10), fmtDayU(new Date(TS[NC - 1])));
      if (!LOG) return;
      for (const o of $('f-branch').options) {
        const b = BRANCHES[+o.value];
        o.textContent = b.current ? msg('br.optAnalyzed', b.name) : b.name;
      }
      $('branch-sub').textContent = msgN('br.sub', BRANCHES.length, fmtInt(BRANCHES.length));
    }

    function initActivity() {
      const basis = $('lang-basis');
      if (!HAS_SIZE) basis.querySelector('option[value="size"]').remove();
      basis.addEventListener('change', renderLanguages);
      paintActivityTexts();
      renderLanguages();
      // Redraws when the page width changes (also when the scrollbar appears).
      let lastW = 0, raf = 0;
      new ResizeObserver(() => {
        const w = document.querySelector('.page').clientWidth;
        if (w === lastW) return;
        lastW = w;
        cancelAnimationFrame(raf);
        raf = requestAnimationFrame(() => { activityCharts.forEach((ch) => ch.render()); renderLanguages(); });
      }).observe(document.querySelector('.page'));
      // Branches: selector (like GitLab's), table and marks on the charts.
      if (LOG) {
        const sel = $('f-branch');
        const order = BRANCHES.slice().sort((x, y) => (y.current - x.current) || (y.tipTs - x.tipTs));
        for (const b of order) {
          const o = el('option', null, b.name);
          o.value = String(b.i);
          sel.appendChild(o);
        }
        sel.value = String(curBranch);
        sel.addEventListener('change', (e) => setBranch(+e.target.value));
        paintActivityTexts();
        renderBranchTable();
      } else {
        for (const id of ['branch-filter', 'sec-branches', 'branch-sub', 'branch-card', 'toc-branches', 'toc-marks']) $(id).hidden = true;
      }
      const marks = $('show-branches');
      marks.addEventListener('change', () => {
        showBranches = marks.checked;
        renderMain();
        activityCharts.forEach((ch) => ch.render());
      });
      renderActivityBody();
    }
    // ---------------------------------------------------------------- startup
    if (!NC) { $('chart').textContent = msg('load.nocommits'); return; }
    refresh();
    initActivity();
    // Language change: formats, texts and every chart, without reloading or losing the state.
    onLangChange = () => {
      setLocale();
      setHeader();
      buildLangPicker();
      renderConfig();
      paintPlay();
      refresh();
      paintActivityTexts();
      renderLanguages();
      renderActivityBody();
      if (LOG) renderBranchTable();
    };
    // Compared with the width it was drawn at (GEO.W): the scrollbar can appear right after the
    // first draw, and then a redraw is needed even if nothing else changed.
    let raf = 0;
    new ResizeObserver(() => {
      if (Math.max(280, chartEl.clientWidth) === GEO.W) return;
      cancelAnimationFrame(raf);
      raf = requestAnimationFrame(() => { renderMain(); renderOverview(); renderDetail(false); });
    }).observe(chartEl);
  }
})();
</script>
</body>
</html>
EOF
}

# A script inlined in the page: "</script" inside it would close the tag ("<\/" means the same in JS).
inline_js() { sed 's#</script#<\\/script#g; s#</SCRIPT#<\\/SCRIPT#g' "$1"; }

if [ "$SPLIT" = 1 ]; then
# Data apart, next to the HTML: <report>.json (plain JSON) and <report>.json.js (the same JSON as a
# script, the only thing the browser lets a page opened from disk read).
cp "$WORK/data.json" "$DATA_JSON.tmp"
mv -f "$DATA_JSON.tmp" "$DATA_JSON"
{
  printf '/* Data for %s (cloc-history.sh): the same content as %s, wrapped in a\n' "$(basename "$OUT")" "$DATA_JSON_NAME"
  printf '   script so the HTML can read it when opened from disk (file://). */\n'
  printf 'window.CLOC_HISTORY_DATA =\n'
  cat "$WORK/data.json"
  printf ';\n'
} > "$DATA_JS.tmp"
mv -f "$DATA_JS.tmp" "$DATA_JS"

{
  html_head
  printf '<script>window.CLOC_HISTORY_FILES = {"json":%s,"js":%s,"stamp":%s};</script>\n' \
    "$(json_str "$DATA_JSON_NAME")" "$(json_str "$DATA_JS_NAME")" "$(date +%s)"
  if [ -f "$D3_SRC" ]; then
    printf '<script>\n'; inline_js "$D3_SRC"; printf '\n</script>\n'
  else
    printf '<script src="%s"></script>\n' "$(html_esc "$D3_SRC")"
  fi
  html_app
} > "$OUT.tmp"
mv -f "$OUT.tmp" "$OUT"
fi

# Self-contained copy (--embed): the same page with the data inside, in a JSON <script> block that
# the page reads before trying any file (and that --rebuild can read back), and D3 inline: from
# --d3 FILE, or downloaded once from the --d3 URL (if that fails, the copy loads it from the URL).
# In JSON "<" can only appear inside strings, where < means the same: no "</script" in the block.
if [ -n "$EMBED_OUT" ]; then
  D3_FILE=''
  if [ -f "$D3_SRC" ]; then
    D3_FILE=$D3_SRC
  else
    dlog "curl $D3_SRC"
    if command -v curl >/dev/null 2>&1; then curl -fsSL --max-time 60 -o "$WORK/d3.js" "$D3_SRC" 2>/dev/null || rm -f "$WORK/d3.js"
    elif command -v wget >/dev/null 2>&1; then wget -q -T 60 -O "$WORK/d3.js" "$D3_SRC" 2>/dev/null || rm -f "$WORK/d3.js"; fi
    if [ -s "$WORK/d3.js" ] && [ "$(head -c 1 "$WORK/d3.js")" != '<' ]; then D3_FILE="$WORK/d3.js"; else warn "$(tx embed_d3 "$D3_SRC")"; fi
  fi
  {
    html_head
    printf '%s: page, data and D3 in a single file -->\n' "$EMBED_MARK"
    printf '<script id="report-data" type="application/json">'
    sed 's/</\\u003c/g' "$WORK/data.json"
    printf '</script>\n'
    if [ -n "$D3_FILE" ]; then
      printf '<script>\n'; inline_js "$D3_FILE"; printf '\n</script>\n'
    else
      printf '<script src="%s"></script>\n' "$(html_esc "$D3_SRC")"
    fi
    html_app
  } > "$EMBED_OUT.tmp"
  mv -f "$EMBED_OUT.tmp" "$EMBED_OUT"
fi

# Incremental cache: this run's commits + the earlier ones that were not analyzed again,
# and every measured version (those of the previous cache + the new ones).
if [ -z "$REBUILD" ] && [ "$USE_CACHE" = 1 ]; then
  {
    printf '%s\n' "$CACHE_HEADER"
    printf '# config\t%s\n' "$CACHE_SIG"
    printf '# repo\t%s · %s · %s · do not edit by hand (delete it to start from scratch)\n' "$REPO_NAME" "$REV" "$(date '+%Y-%m-%d %H:%M')"
    printf '# C\tcommit\tdate\tfiles\tblank\tcomment\tcode\tbytes\tsubject   (commits already analyzed)\n'
    printf '# V\tblob/name\tlanguage\tblank\tcomment\tcode\tbytes   (cloc of each version; - = cloc ignores it)\n'
    printf '# A\tcommit\tunix_time\tauthor\temail   (activity: commits without merges of every branch)\n'
    awk '{ print "A\t" $0 }' "$WORK/meta"
    cat "$WORK/clines"
    # Commits of earlier runs that this one did not analyze again (the C lines of a v1 cache
    # had no bytes: only the ones analyzed again are kept).
    if [ "$CACHE_V1" = 0 ]; then
      awk -v cur="$WORK/clines" 'BEGIN { while ((getline ln < cur) > 0) { split(ln, a, "\t"); N[a[2]] = 1 } }
        { split($0, a, "\t"); if (!(a[2] in N)) print }' "$WORK/ccache"
    fi
    # Versions of the previous cache, filling in the size of those that came from a v1.
    awk -F'\t' -v szf="$WORK/sizes" '
      BEGIN { while ((getline ln < szf) > 0) { split(ln, a, "\t"); SZ[a[1]] = a[2] } }
      { sha = $1; sub(/\/.*/, "", sha); print "V\t" $0 ((NF < 6 && (sha in SZ)) ? "\t" SZ[sha] : "") }' "$WORK/vcache"
    cat "$WORK/vnew"
  } > "$CACHE.tmp"
  mv -f "$CACHE.tmp" "$CACHE"
  log "$(tx cache_saved "$CACHE" "$(grep -c '^C' "$CACHE")" "$(grep -c '^V' "$CACHE")" "$(grep -c '^A' "$CACHE")")"
fi

kb() { printf '%s KB' $(( ( $(wc -c < "$1") + 1023 ) / 1024 )); }
if [ "$SPLIT" = 1 ]; then
  if [ -z "$EMBED_OUT" ]; then :
  elif [ -n "$D3_FILE" ]; then log "$(tx embed_saved "$EMBED_OUT" "$(kb "$EMBED_OUT")")"
  else log "$(tx embed_saved_url "$EMBED_OUT" "$(kb "$EMBED_OUT")" "$D3_SRC")"; fi
  log "$(tx done "$(fmt_dur $(( $(date +%s) - START_TS )))" "$OUT" "$(kb "$OUT")" "$DATA_JSON" "$(kb "$DATA_JSON")" "$DATA_JS_NAME" "$NCOMMITS")"
else
  log "$(tx done_embed "$(fmt_dur $(( $(date +%s) - START_TS )))" "$EMBED_OUT" "$(kb "$EMBED_OUT")" "$NCOMMITS")"
fi
