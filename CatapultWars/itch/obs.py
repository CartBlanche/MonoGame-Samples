#!/usr/bin/env python3
"""Tiny OBS remote control (obs-websocket v5), standard library only.

  OBS_PASSWORD=... itch/obs.py [--host H] [--port P] <command> [args]

  version            OBS and websocket versions
  status             is it recording, and for how long
  scenes             list scenes (current one marked)
  scene <name>       switch to a scene
  start              start recording
  stop               stop recording, prints the file it wrote
  folder [path]      show or set the recording folder
  bind               point the "CatapultWars LAN" scene's two window sources at the running game
                     windows (lower window id = the host) and lay them out side by side
  call <type> [json] any raw request, for everything else

Host, port and password come from --host/--port and the OBS_PASSWORD environment variable
(OBS_HOST / OBS_PORT also work). The password is never stored in the repo.
In OBS: Tools > WebSocket Server Settings > enable it, and set a password.
"""
import base64, hashlib, json, os, socket, struct, sys, time, uuid


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


SCENE = "CatapultWars LAN"
# OBS source names are global to the whole OBS, not per scene, so these are deliberately specific:
# another scene (a racing game's LAN recording, say) may well have "Dominique window" and "window-list".
HOST_SOURCE, GUEST_SOURCE = "CatapultWars host window", "CatapultWars guest window"
LISTER = "CatapultWars window-list"
WINDOW_TITLE = "[CatapultNetWars] Catapult Wars"   # [process] window title
TITLE_BAR = 56          # capture pixels of title bar (28 points at 2x Retina), cropped off the top
SCALE = 0.6             # 1600 px wide capture -> 960, so two fit the 1920 canvas


def bind(obs):
    # A dedicated, disabled window-capture source lists the windows. Its settings are forced to window
    # mode each time, because a source left in another mode has no "window" property to list.
    existing = {i["inputName"] for i in obs.request("GetInputList")["inputs"]}
    if LISTER not in existing:
        obs.request("CreateInput", {"sceneName": SCENE, "inputName": LISTER, "inputKind": "screen_capture",
                                    "inputSettings": {"type": 1}, "sceneItemEnabled": False})
    obs.request("SetInputSettings", {"inputName": LISTER, "inputSettings": {"type": 1}})
    items = None
    for _ in range(10):
        try:
            items = obs.request("GetInputPropertiesListPropertyItems",
                                {"inputName": LISTER, "propertyName": "window"})["propertyItems"]
            break
        except RuntimeError:
            time.sleep(0.5)
    if items is None:
        raise RuntimeError("could not list the windows")
    ids = sorted(int(i["itemValue"]) for i in items if i["itemName"].startswith(WINDOW_TITLE))
    if len(ids) != 2:
        raise RuntimeError("expected 2 game windows, found %d: start the game first" % len(ids))
    video = obs.request("GetVideoSettings")
    width, height = video["baseWidth"], video["baseHeight"]
    # Capture size is 2x the 800x480 game plus the title bar; each half is scaled to width / 2.
    # The sources are created fresh every run: an existing source pointed at a new window can sit at
    # 0x0 and never deliver frames, while a new one starts straight away.
    existing = {i["inputName"] for i in obs.request("GetInputList")["inputs"]}
    for n, (source, window) in enumerate(((HOST_SOURCE, ids[0]), (GUEST_SOURCE, ids[1]))):
        if source in existing:
            obs.request("RemoveInput", {"inputName": source})
        for _ in range(20):          # a removed source goes away a moment later, and its name is busy till then
            try:
                item = obs.request("CreateInput", {"sceneName": SCENE, "inputName": source, "inputKind": "screen_capture",
                    "inputSettings": {"type": 1, "window": window, "show_cursor": False},
                    "sceneItemEnabled": True})["sceneItemId"]
                break
            except RuntimeError as e:
                if "already exists" not in str(e):
                    raise
                time.sleep(0.5)
        else:
            raise RuntimeError("could not create %s" % source)
        for _ in range(20):
            time.sleep(0.5)
            got = obs.request("GetSceneItemTransform", {"sceneName": SCENE, "sceneItemId": item})["sceneItemTransform"]
            if got["sourceWidth"] > 0:
                break
        else:
            raise RuntimeError("%s never started delivering frames (window %d)" % (source, window))
        scale = (width / 2) / 1600.0
        content_h = 960 * scale
        obs.request("SetSceneItemTransform", {"sceneName": SCENE, "sceneItemId": item, "sceneItemTransform": {
            "positionX": n * width / 2, "positionY": (height - content_h) / 2, "scaleX": scale, "scaleY": scale,
            "cropTop": TITLE_BAR, "cropLeft": 0, "cropRight": 0, "cropBottom": 0,
            "alignment": 5, "boundsType": "OBS_BOUNDS_NONE"}})
    print("host window %d, guest window %d" % (ids[0], ids[1]))


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
            obs.request("SetCurrentProgramScene", {"sceneName": rest[0]})
        elif cmd == "start":
            obs.request("StartRecord")
        elif cmd == "stop":
            print(obs.request("StopRecord").get("outputPath", ""))
        elif cmd == "folder":
            if rest:
                obs.request("SetRecordDirectory", {"recordDirectory": rest[0]})
            print(obs.request("GetRecordDirectory")["recordDirectory"])
        elif cmd == "bind":
            bind(obs)
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
