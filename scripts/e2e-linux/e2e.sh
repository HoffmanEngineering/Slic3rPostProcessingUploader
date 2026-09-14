#!/bin/bash
# End-to-end check of the setup wizard against a real OrcaSlicer on Linux, driven headlessly.
#
#   1. Extract the AppImage and complete OrcaSlicer's first-run wizard (Klipper + Afinia printers).
#   2. Run `Slic3rPostProcessingUploader install` (the real binary) against the config it wrote.
#   3. Swap the binary for a stub that logs its arguments, relaunch OrcaSlicer, add a cube, slice,
#      export G-code, and assert that OrcaSlicer invoked the stub with the installed flags.
#
# Inputs (copied in): /uploader   linux-x64 build of Slic3rPostProcessingUploader
#                    /appimage   OrcaSlicer_Linux_AppImage_*.AppImage (or set APPIMAGE_URL)
# Outputs:           /out        screenshots, logs, the exported G-code and the captured config tree
#
# The click coordinates assume a 1600x1000 screen and the OrcaSlicer 2.4.x wizard layout; when the
# layout changes, take a screenshot (see shot()) and adjust.
set -euo pipefail

OUT=/out; mkdir -p "$OUT"
export HOME=/home/tester; mkdir -p "$HOME/.config"
CONFIG="$HOME/.config/OrcaSlicer"
STEP=0

log()  { echo "[e2e] $*"; }
shot() { STEP=$((STEP + 1)); import -display :99 -window root "$OUT/$(printf '%02d' $STEP)-$1.png"; }
fail() { log "FAIL: $*"; shot "fail"; exit 1; }
click() { xdotool mousemove "$1" "$2" click "${3:-1}"; sleep "${4:-2}"; }
wclick() { # wclick <window title regex> <x> <y> [sleep] — coordinates relative to that window's client area
    local w; w=$(xdotool search --name "$1" 2>/dev/null | head -1 || true); [ -n "$w" ] || fail "no window '$1' to click in"
    xdotool mousemove --window "$w" "$2" "$3" click 1; sleep "${4:-2}"
}
main_window() { # the live "… - OrcaSlicer" window: the modified ("*…") one when a model is loaded, else the largest
    local best="" best_area=0 w area name
    for w in $(xdotool search --name " - OrcaSlicer$" 2>/dev/null || true); do
        name=$(xdotool getwindowname "$w" 2>/dev/null || true)
        case "$name" in \**) echo "$w"; return;; esac
        eval "$(xdotool getwindowgeometry --shell "$w" 2>/dev/null)"; area=$(( ${WIDTH:-0} * ${HEIGHT:-0} ))
        [ "$area" -gt "$best_area" ] && { best=$w; best_area=$area; }
    done
    echo "$best"
}
place_main_window() { # place_main_window <x> <y> <w> <h>
    local m; m=$(main_window); [ -n "$m" ] || fail "no main window"
    xdotool windowmove --sync "$m" "$1" "$2"; xdotool windowsize --sync "$m" "$3" "$4"; sleep 2
}
wait_window() { # wait_window <title regex> [timeout s]
    local deadline=$(( $(date +%s) + ${2:-60} ))
    until xdotool search --name "$1" >/dev/null 2>&1; do
        [ "$(date +%s)" -lt "$deadline" ] || fail "window '$1' did not appear"
        sleep 1
    done
}
wait_gone() { # wait_gone <title regex> [timeout s]
    local deadline=$(( $(date +%s) + ${2:-60} ))
    while xdotool search --name "$1" >/dev/null 2>&1; do
        [ "$(date +%s)" -lt "$deadline" ] || fail "window '$1' did not close"
        sleep 1
    done
}
launch_orca() { # launch_orca <log name> [model file to open]
    ( cd /work/squashfs-root && ./AppRun ${2:-} >"$OUT/orca-$1.log" 2>&1 ) &
    ORCA_PID=$!
    # SSL certificate question ("use system SSL certificate … continue?"): a modal titled just "OrcaSlicer".
    # It can show up before or alongside the main window, so look for it by title rather than by order.
    local deadline=$(( $(date +%s) + 90 )) d
    while :; do
        d=$(xdotool search --name "^OrcaSlicer$" 2>/dev/null | head -1 || true)
        if [ -n "$d" ]; then sleep 2; shot "ssl-question"; xdotool mousemove --window "$d" 635 111 click 1; sleep 2; break; fi
        [ "$(date +%s)" -lt "$deadline" ] || break
        sleep 1
    done
    wait_window " - OrcaSlicer$" 120   # "Untitled - OrcaSlicer" or "<model> - OrcaSlicer"
    sleep 6
    place_main_window 200 100 1200 800   # where the absolute coordinates below expect it
}
write_cube_stl() {
    local s=20
    echo "solid cube"
    tri() { echo " facet normal 0 0 0"; echo "  outer loop"; for v in "$@"; do echo "   vertex $v"; done; echo "  endloop"; echo " endfacet"; }
    tri "0 0 0" "$s 0 0" "$s $s 0";   tri "0 0 0" "$s $s 0" "0 $s 0"      # bottom
    tri "0 0 $s" "$s $s $s" "$s 0 $s"; tri "0 0 $s" "0 $s $s" "$s $s $s"   # top
    tri "0 0 0" "0 $s 0" "0 $s $s";   tri "0 0 0" "0 $s $s" "0 0 $s"      # left
    tri "$s 0 0" "$s $s $s" "$s $s 0"; tri "$s 0 0" "$s 0 $s" "$s $s $s"   # right
    tri "0 0 0" "0 0 $s" "$s 0 $s";   tri "0 0 0" "$s 0 $s" "$s 0 0"      # front
    tri "0 $s 0" "$s $s $s" "0 $s $s"; tri "0 $s 0" "$s $s 0" "$s $s $s"   # back
    echo "endsolid cube"
}
stop_orca() {
    pkill -x orca-slicer 2>/dev/null || true
    pkill -x AppRun 2>/dev/null || true
    sleep 3
}

# ---- 0. Inputs -------------------------------------------------------------------------------------
[ -f /uploader ] || fail "/uploader is missing (mount the linux-x64 build)"
# Work on a copy: the mount is read-only and step 3 replaces the file in place.
UPLOADER=/opt/3DPrintLog/Slic3rPostProcessingUploader; mkdir -p "$(dirname "$UPLOADER")"; cp /uploader "$UPLOADER"; chmod +x "$UPLOADER"
mkdir -p /work && cd /work
if [ -f /appimage ]; then cp /appimage ./slicer.AppImage
elif [ -n "${APPIMAGE_URL:-}" ]; then curl -sL -o slicer.AppImage "$APPIMAGE_URL"
else fail "mount an AppImage at /appimage or set APPIMAGE_URL"; fi
chmod +x slicer.AppImage
./slicer.AppImage --appimage-extract >/dev/null 2>&1 || fail "AppImage extraction failed"
log "extracted $(ls squashfs-root/bin 2>/dev/null | head -1)"

Xvfb :99 -screen 0 1600x1000x24 >"$OUT/xvfb.log" 2>&1 &
sleep 2
openbox >"$OUT/openbox.log" 2>&1 &
sleep 1

# ---- 1. First-run wizard ----------------------------------------------------------------------------
launch_orca "first-run"
W="Setup Wizard"
wait_window "$W" 30
shot "welcome"
wclick "$W" 417 422                         # Get Started
wclick "$W" 414 414 1; wclick "$W" 754 618  # Region: North America → Next
shot "printer-selection"
wclick "$W" 114 170 1                       # Custom Printer → Klipper
wclick "$W" 782 483 1                       # Afinia (whole vendor)
wclick "$W" 754 618 8                       # Next (filaments)
wclick "$W" 754 618 6                       # Next (stealth mode)
wclick "$W" 129 309 1; wclick "$W" 754 601  # Enable stealth mode → Next
shot "plugins"
wclick "$W" 754 601 15                      # Finish
[ -d "$CONFIG/system/Custom" ] && [ -d "$CONFIG/system/Afinia" ] || fail "wizard did not write system profiles"
[ -d "$CONFIG/user/default/process" ] || fail "wizard did not create user/default/process"
shot "wizard-done"
stop_orca
log "wizard complete: $(ls "$CONFIG/system" | tr '\n' ' ')"
cp -r "$CONFIG" "$OUT/config-after-wizard"

# ---- 2. Install with the real uploader ----------------------------------------------------------------
# Answers: install=Y, note template 2 (full), opt out of telemetry=y, no extra flags.
printf "y\n2\ny\n\n" | "$UPLOADER" install | tee "$OUT/install.log"
CREATED=$(ls "$CONFIG/user/default/process" | grep -c -- ' - 3DPrintLog.json' || true)
[ "$CREATED" -gt 0 ] || fail "install created no overrides"
grep -q "\"$UPLOADER --full --opt-out-telemetry\"" "$CONFIG/user/default/process/0.20mm Standard @MyKlipper - 3DPrintLog.json" \
    || fail "override does not carry the expected post_process entry"
log "install created $CREATED overrides"
printf "y\n2\ny\n\n" | "$UPLOADER" install | tee "$OUT/install-again.log" | grep -q "$CREATED skipped" \
    || fail "second install was not a no-op"
cp -r "$CONFIG" "$OUT/config-after-install"

# ---- 3. Prove OrcaSlicer runs it --------------------------------------------------------------------
# Same path, different program: a stub that records how the slicer called it (nothing is uploaded).
mv "$UPLOADER" "$UPLOADER.real"
cat >"$UPLOADER" <<'STUB'
#!/bin/bash
{ echo "args: $*"; echo "SLIC3R_PP_OUTPUT_NAME=$SLIC3R_PP_OUTPUT_NAME"; head -2 "${@: -1}"; } >> /tmp/pp.log
STUB
chmod +x "$UPLOADER"

# A 20 mm cube as ASCII STL, opened straight from the command line (more reliable than the context menu).
write_cube_stl >/tmp/cube.stl
launch_orca "with-overrides" /tmp/cube.stl
click 330 155 1 4                   # Prepare tab
click 246 458 1 3                   # process preset dropdown
shot "preset-dropdown"
click 410 633 1 4                   # "0.20mm Standard @MyKlipper - 3DPrintLog"
shot "cube-ready"

# Documentation shot: the preset's Others tab with the post-processing script filled in. That field only
# exists in Advanced mode, and sits near the bottom of a long page, so grow the window for the capture.
place_main_window 200 0 1200 980
click 572 314 1 3                   # Simple/Advanced toggle → Advanced
click 632 397 1 3                   # Others tab
xdotool mousemove 430 700; for _ in $(seq 1 40); do xdotool click 5; done; sleep 1
xdotool click 4 click 4 click 4; sleep 2
shot "others-post-processing"
click 240 397 1 2                   # back to Quality tab (keeps later coordinates valid)
place_main_window 200 100 1200 800
click 1102 155 1 25                 # Slice plate
shot "sliced"
click 1275 155 1 6                  # Export G-code file
wait_window "Save G-code file as" 30
xdotool key ctrl+a; xdotool type "/tmp/cube-test.gcode"; xdotool key Return
wait_gone "Save G-code file as" 60
sleep 10
shot "exported"
stop_orca

[ -f /tmp/cube-test.gcode ] || fail "no G-code exported"
cp /tmp/cube-test.gcode "$OUT/"
[ -f /tmp/pp.log ] || fail "OrcaSlicer never invoked the post-process script"
cp /tmp/pp.log "$OUT/"
grep -q '^args: --full --opt-out-telemetry ' /tmp/pp.log || fail "script was called with unexpected arguments: $(head -1 /tmp/pp.log)"
grep -q '^SLIC3R_PP_OUTPUT_NAME=/tmp/cube-test.gcode$' /tmp/pp.log || fail "SLIC3R_PP_OUTPUT_NAME not passed"
grep -q 'generated by OrcaSlicer' /tmp/pp.log || fail "script did not receive the G-code path"

# ---- 4. Uninstall round-trips ----------------------------------------------------------------------
mv "$UPLOADER.real" "$UPLOADER"
printf "y\n" | "$UPLOADER" uninstall | tee "$OUT/uninstall.log"
LEFT=$(ls "$CONFIG/user/default/process" | grep -c -- ' - 3DPrintLog.json' || true)
[ "$LEFT" -eq 0 ] || fail "uninstall left $LEFT overrides behind"

log "PASS: OrcaSlicer ran the installed uploader:"
cat /tmp/pp.log
