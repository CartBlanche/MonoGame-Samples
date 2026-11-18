#!/bin/zsh
# usage: run_shot.sh out.png [extra game args]  (env vars TPS_* pass through). Run from ThirdPersonShooter/.
out=$1; shift
caffeinate -u -t 2
dotnet Desktop/bin/Release/net10.0/ThirdPersonShooter.dll --shot "$out" "$@" > /tmp/tps_last.log 2>&1 &
pid=$!; for i in {1..40}; do sleep 1; kill -0 $pid 2>/dev/null || break; done; kill $pid 2>/dev/null; true
