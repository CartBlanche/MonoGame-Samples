#!/usr/bin/env python3
"""Tiny OBS remote control (obs-websocket v5), standard library only. Adapted from the Vector Rumble and CatapultWars ones.

  OBS_PASSWORD=... itch/video/obs.py [--host H] [--port P] <command> [args]

  version            OBS and websocket versions
  status             is it recording, and for how long
  scenes             list scenes (current one marked)
  scene <name>       switch to a scene
  start              start recording
  stop               stop recording, prints the file it wrote
  folder [path]      show or set the recording folder
  windows            list the windows OBS can capture (to check the name the game's window has)
  bind-all [names]   make two scenes, "Racing Game - Old" and "Racing Game - New", each showing one of the two game windows
                     (the lowest window id is the first named: the games are started in that order, Old first)
  cut <name> [swipe [ms]]  switch to that scene (Old or New); with "swipe" use the Swipe transition (default 700 ms)
  call <type> [json] any raw request, for everything else

Host, port and password come from --host/--port and the OBS_PASSWORD environment variable
(OBS_HOST / OBS_PORT also work). The password is never stored in the repo.
In OBS: Tools > WebSocket Server Settings > enable it, and set a password.
OBS_WINDOW_MATCH (default: a regular expression, ^\[dotnet\] Racing ?Game) must match a window's "[process] title"; the games run under dotnet, and an editor window with the game's name in its title is not taken.
Sound: the window capture carries the game's own sound. Keep the mic (Mic/Aux) muted (record-video.sh does).
Source names are global to the whole OBS: only the "Racing Game ..." ones here are made, changed or removed.
"""
import base64, hashlib, json, os, re, socket, struct, sys, time, uuid


class Obs:
    def __init__(self, host, port, password, timeout=10):
        self.sock = socket.create_connection((host, port), timeout=timeout)
        self._handshake(host, port)
        hello = self._recv_json()
        if hello.get("op") != 0:
            raise RuntimeError("unexpected greeting: %r" % hello)
        ident = {"rpcVersion": 1, "eventSubscriptions": 0}
        auth = hello["d"].get("authentication")
        if auth:
            if not password:
                raise RuntimeError("OBS wants a password: set OBS_PASSWORD")
            secret = base64.b64encode(hashlib.sha256((password + auth["salt"]).encode()).digest()).decode()
            ident["authentication"] = base64.b64encode(
                hashlib.sha256((secret + auth["challenge"]).encode()).digest()).decode()
        self._send_json({"op": 1, "d": ident})
        reply = self._recv_json()
        if reply.get("op") != 2:
            raise RuntimeError("login refused: %r" % reply)

    def request(self, kind, data=None):
        rid = uuid.uuid4().hex
        msg = {"requestType": kind, "requestId": rid}
        if data:
            msg["requestData"] = data
        self._send_json({"op": 6, "d": msg})
        while True:
            reply = self._recv_json()
            if reply.get("op") == 7 and reply["d"].get("requestId") == rid:
                d = reply["d"]
                if not d["requestStatus"]["result"]:
                    raise RuntimeError("%s failed: %s" % (kind, d["requestStatus"].get("comment", d["requestStatus"])))
                return d.get("responseData") or {}

    def close(self):
        try:
            self.sock.close()
        except OSError:
            pass

    # -- minimal websocket framing (client side, text frames) --
    def _handshake(self, host, port):
        key = base64.b64encode(os.urandom(16)).decode()
        self.sock.sendall(("GET / HTTP/1.1\r\nHost: %s:%d\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
                           "Sec-WebSocket-Key: %s\r\nSec-WebSocket-Version: 13\r\n"
                           "Sec-WebSocket-Protocol: obswebsocket.json\r\n\r\n" % (host, port, key)).encode())
        buf = b""
        while b"\r\n\r\n" not in buf:
            chunk = self.sock.recv(1024)
            if not chunk:
                raise RuntimeError("connection closed during handshake")
            buf += chunk
        if b" 101 " not in buf.split(b"\r\n")[0]:
            raise RuntimeError("websocket upgrade refused: " + buf.split(b"\r\n")[0].decode())
        self._pending = buf.split(b"\r\n\r\n", 1)[1]

    def _read(self, n):
        while len(self._pending) < n:
            chunk = self.sock.recv(65536)
            if not chunk:
                raise RuntimeError("connection closed")
            self._pending += chunk
        out, self._pending = self._pending[:n], self._pending[n:]
        return out

    def _send_json(self, obj):
        payload = json.dumps(obj).encode()
        mask = os.urandom(4)
        n = len(payload)
        head = b"\x81" + (bytes([0x80 | n]) if n < 126 else b"\xfe" + struct.pack(">H", n) if n < 65536
                          else b"\xff" + struct.pack(">Q", n))
        self.sock.sendall(head + mask + bytes(b ^ mask[i % 4] for i, b in enumerate(payload)))

    def _recv_json(self):
        message = b""
        while True:
            b1, b2 = self._read(2)
            n = b2 & 0x7F
            if n == 126:
                n = struct.unpack(">H", self._read(2))[0]
            elif n == 127:
                n = struct.unpack(">Q", self._read(8))[0]
            data = self._read(n)
            opcode = b1 & 0x0F
            if opcode == 8:
                raise RuntimeError("OBS closed the connection: %r" % data[2:])
            if opcode in (9, 10):
                continue
            message += data
            if b1 & 0x80:
                return json.loads(message.decode())


SCENE = "Racing Game"
# OBS source names are global to the whole OBS, so these are specific to this game
SOURCE = "Racing Game window"
LISTER = "Racing Game window-list"
WINDOW_MATCH = os.environ.get("OBS_WINDOW_MATCH", r"^\[dotnet\] Racing ?Game")
TITLE_BAR = 56          # capture pixels of title bar (28 points at 2x Retina), cropped off the top


def list_windows(obs):
    """[(id, "[process] title")] of the windows OBS can capture. A dedicated, disabled window-capture source lists them;
    its settings are forced to window mode each time, because a source left in another mode has no "window" property."""
    existing = {i["inputName"] for i in obs.request("GetInputList")["inputs"]}
    if SCENE not in {s["sceneName"] for s in obs.request("GetSceneList")["scenes"]}:
        obs.request("CreateScene", {"sceneName": SCENE})
    if LISTER not in existing:
        obs.request("CreateInput", {"sceneName": SCENE, "inputName": LISTER, "inputKind": "screen_capture",
                                    "inputSettings": {"type": 1}, "sceneItemEnabled": False})
    obs.request("SetInputSettings", {"inputName": LISTER, "inputSettings": {"type": 1}})
    for _ in range(10):
        try:
            items = obs.request("GetInputPropertiesListPropertyItems",
                                {"inputName": LISTER, "propertyName": "window"})["propertyItems"]
            return [(int(i["itemValue"]), i["itemName"]) for i in items]
        except RuntimeError:
            time.sleep(0.5)
    raise RuntimeError("could not list the windows")


def bind(obs):
    windows = [w for w in list_windows(obs) if re.search(WINDOW_MATCH, w[1], re.I)]
    if not windows:
        raise RuntimeError("no window matching %r: start the game first (and see the 'windows' command)" % WINDOW_MATCH)
    window = min(windows)[0]       # the host started first, so it has the lowest id
    video = obs.request("GetVideoSettings")
    width, height = video["baseWidth"], video["baseHeight"]
    # The source is made fresh every run: an existing one pointed at a new window can sit at 0x0 and never deliver frames.
    existing = {i["inputName"] for i in obs.request("GetInputList")["inputs"]}
    if SOURCE in existing:
        obs.request("RemoveInput", {"inputName": SOURCE})
    for _ in range(20):          # a removed source goes away a moment later, and its name is busy till then
        try:
            item = obs.request("CreateInput", {"sceneName": SCENE, "inputName": SOURCE, "inputKind": "screen_capture",
                "inputSettings": {"type": 1, "window": window, "show_cursor": False},
                "sceneItemEnabled": True})["sceneItemId"]
            break
        except RuntimeError as e:
            if "already exists" not in str(e):
                raise
            time.sleep(0.5)
    else:
        raise RuntimeError("could not create %s" % SOURCE)
    for _ in range(20):
        time.sleep(0.5)
        got = obs.request("GetSceneItemTransform", {"sceneName": SCENE, "sceneItemId": item})["sceneItemTransform"]
        if got["sourceWidth"] > 0:
            break
    else:
        raise RuntimeError("%s never started delivering frames (window %d)" % (SOURCE, window))
    # fill the canvas with the window minus its title bar
    src_w, src_h = got["sourceWidth"], got["sourceHeight"] - TITLE_BAR
    scale = min(width / src_w, height / src_h)
    obs.request("SetSceneItemTransform", {"sceneName": SCENE, "sceneItemId": item, "sceneItemTransform": {
        "positionX": (width - src_w * scale) / 2, "positionY": (height - src_h * scale) / 2,
        "scaleX": scale, "scaleY": scale, "cropTop": TITLE_BAR, "cropLeft": 0, "cropRight": 0, "cropBottom": 0,
        "alignment": 5, "boundsType": "OBS_BOUNDS_NONE"}})
    print("capturing window %d of %d matching %r" % (window, len(windows), WINDOW_MATCH))


PLAYERS = ["Old", "New"]


def scene_of(player):
    return "Racing Game - " + player


def bind_all(obs, order=None):
    """One scene per player, each showing that player's game window, filling the canvas (title bar cropped)."""
    windows = sorted(w for w in list_windows(obs) if re.search(WINDOW_MATCH, w[1], re.I))
    if len(windows) < len(PLAYERS):
        raise RuntimeError("expected %d game windows matching %r, found %d: start the games first" % (len(PLAYERS), WINDOW_MATCH, len(windows)))
    video = obs.request("GetVideoSettings")
    width, height = video["baseWidth"], video["baseHeight"]
    scenes = {s["sceneName"] for s in obs.request("GetSceneList")["scenes"]}
    # the sources of earlier runs go (only ours: they start with this prefix); a name that was just removed stays busy for a
    # while, so each run's sources get a name of their own
    for i in obs.request("GetInputList")["inputs"]:
        if i["inputName"].startswith("Racing Game window - "):
            try:
                obs.request("RemoveInput", {"inputName": i["inputName"]})
            except RuntimeError:
                pass
    run = str(int(time.time()))
    # the windows are in the order the games were started, and the players are named in that order
    for (window, _), player in zip(windows, order or PLAYERS):
        scene = scene_of(player)
        source = "Racing Game window - %s %s" % (player, run)
        if scene not in scenes:
            obs.request("CreateScene", {"sceneName": scene})
        item = obs.request("CreateInput", {"sceneName": scene, "inputName": source, "inputKind": "screen_capture",
            "inputSettings": {"type": 1, "window": window, "show_cursor": False}, "sceneItemEnabled": True})["sceneItemId"]
        for _ in range(20):
            time.sleep(0.5)
            got = obs.request("GetSceneItemTransform", {"sceneName": scene, "sceneItemId": item})["sceneItemTransform"]
            if got["sourceWidth"] > 0:
                break
        else:
            raise RuntimeError("%s never started delivering frames (window %d)" % (source, window))
        src_w, src_h = got["sourceWidth"], got["sourceHeight"] - TITLE_BAR
        scale = min(width / src_w, height / src_h)
        obs.request("SetSceneItemTransform", {"sceneName": scene, "sceneItemId": item, "sceneItemTransform": {
            "positionX": (width - src_w * scale) / 2, "positionY": (height - src_h * scale) / 2,
            "scaleX": scale, "scaleY": scale, "cropTop": TITLE_BAR, "cropLeft": 0, "cropRight": 0, "cropBottom": 0,
            "alignment": 5, "boundsType": "OBS_BOUNDS_NONE"}})
        print("%s: window %d, %dx%d at %.2fx" % (player, window, src_w, src_h, scale))


def cut(obs, player, swipe=False, ms=700):
    """Switch to a player's scene: a hard cut, or the Swipe transition."""
    name = "Cut"
    if swipe:
        # OBS has to have a "Swipe" transition (Scene Transitions panel, the + button, Swipe); the websocket cannot add one.
        # Without it a Fade is used, so a take is never stuck.
        have = {t["transitionName"] for t in obs.request("GetSceneTransitionList")["transitions"]}
        name = "Swipe" if "Swipe" in have else "Fade"
        if name == "Fade":
            print("obs.py: no Swipe transition in OBS, using Fade (add one: Scene Transitions, +, Swipe)", file=sys.stderr)
    obs.request("SetCurrentSceneTransition", {"transitionName": name})
    if swipe:
        obs.request("SetCurrentSceneTransitionDuration", {"transitionDuration": ms})
        if name == "Swipe":
            try:
                obs.request("SetCurrentSceneTransitionSettings", {"transitionSettings": {"direction": "left"}, "overlay": True})
            except RuntimeError:
                pass
    obs.request("SetCurrentProgramScene", {"sceneName": scene_of(player)})


def main(argv):
    host = os.environ.get("OBS_HOST", "localhost")
    port = int(os.environ.get("OBS_PORT", "4455"))
    args = []
    it = iter(argv)
    for a in it:
        if a == "--host":
            host = next(it)
        elif a == "--port":
            port = int(next(it))
        else:
            args.append(a)
    if not args:
        print(__doc__)
        return 1

    obs = Obs(host, port, os.environ.get("OBS_PASSWORD", ""))
    try:
        cmd, rest = args[0], args[1:]
        if cmd == "version":
            v = obs.request("GetVersion")
            print("OBS %s, obs-websocket %s, %s" % (v["obsVersion"], v["obsWebSocketVersion"], v["platform"]))
        elif cmd == "status":
            s = obs.request("GetRecordStatus")
            print("recording" if s["outputActive"] else "not recording", s.get("outputTimecode", ""))
        elif cmd == "scenes":
            s = obs.request("GetSceneList")
            for sc in reversed(s["scenes"]):
                print(("* " if sc["sceneName"] == s["currentProgramSceneName"] else "  ") + sc["sceneName"])
        elif cmd == "scene":
            names = {s["sceneName"] for s in obs.request("GetSceneList")["scenes"]}
            if rest[0] not in names:
                obs.request("CreateScene", {"sceneName": rest[0]})
            obs.request("SetCurrentProgramScene", {"sceneName": rest[0]})
        elif cmd == "start":
            obs.request("StartRecord")
        elif cmd == "stop":
            print(obs.request("StopRecord").get("outputPath", ""))
        elif cmd == "folder":
            if rest:
                obs.request("SetRecordDirectory", {"recordDirectory": rest[0]})
            print(obs.request("GetRecordDirectory")["recordDirectory"])
        elif cmd == "windows":
            for wid, name in sorted(list_windows(obs)):
                print("%d  %s" % (wid, name))
        elif cmd == "bind":
            bind(obs)
        elif cmd == "bind-all":
            bind_all(obs, rest or None)
        elif cmd == "cut":
            cut(obs, rest[0], rest[1:] and rest[1] == "swipe", int(rest[2]) if len(rest) > 2 else 700)
        elif cmd == "call":
            print(json.dumps(obs.request(rest[0], json.loads(rest[1]) if len(rest) > 1 else None), indent=2))
        else:
            print(__doc__)
            return 1
    finally:
        obs.close()
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main(sys.argv[1:]))
    except (RuntimeError, OSError) as e:
        print("obs.py: %s" % e, file=sys.stderr)
        sys.exit(2)
