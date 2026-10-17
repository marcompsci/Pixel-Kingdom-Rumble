#!/usr/bin/env python3
"""
Replaces the App Store screenshots of Pixel Kingdom Rumble's version in "Prepare for Submission" (en-US) through
the App Store Connect API, using the team key in ../.appstoreconnect/AuthKey_<id>.p8 (outside the repo).

  python3 Tools/asc_screenshots.py Docs/AppStore/screenshots_manifest.json

The manifest maps a display type to the files, in order, e.g.
  {"APP_IPHONE_67": ["Docs/AppStore/v1/01_rail_yard.png", ...], "APP_IPHONE_61": [...]}
Each listed set is emptied and refilled in that order. Only Python's standard library and the system openssl are used
(the watcher runs it on the Mac: Logs/asc_request). Results: Logs/asc/SUMMARY.txt. Never submits anything.
"""
import base64, glob, hashlib, json, os, subprocess, sys, time, urllib.request, urllib.error

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP_ID = "6821419588"
ISSUER = os.environ.get("ASC_ISSUER", "e68f3170-f3b3-42c1-a66a-c33b1455907e")
API = "https://api.appstoreconnect.apple.com/v1"
OUT = os.path.join(ROOT, "Logs", "asc")
os.makedirs(OUT, exist_ok=True)
SUMMARY = open(os.path.join(OUT, "SUMMARY.txt"), "w")


def say(*a):
    s = " ".join(str(x) for x in a)
    print(s)
    SUMMARY.write(s + "\n")
    SUMMARY.flush()


def b64u(b):
    return base64.urlsafe_b64encode(b).rstrip(b"=").decode()


def der_to_raw(der):
    # ECDSA DER: 30 len 02 lr r 02 ls s  ->  r||s (32 bytes each)
    i = 2
    if der[1] & 0x80:
        i = 2 + (der[1] & 0x7F)
    assert der[i] == 2
    lr = der[i + 1]; r = der[i + 2:i + 2 + lr]; i = i + 2 + lr
    assert der[i] == 2
    ls = der[i + 1]; s = der[i + 2:i + 2 + ls]
    r = r.lstrip(b"\0").rjust(32, b"\0"); s = s.lstrip(b"\0").rjust(32, b"\0")
    return r + s


def token():
    keys = glob.glob(os.path.join(ROOT, "..", ".appstoreconnect", "AuthKey_*.p8"))
    if not keys:
        raise SystemExit("No App Store Connect key in ../.appstoreconnect/")
    key = keys[0]
    kid = os.path.basename(key)[len("AuthKey_"):-3]
    now = int(time.time())
    header = b64u(json.dumps({"alg": "ES256", "kid": kid, "typ": "JWT"}).encode())
    payload = b64u(json.dumps({"iss": ISSUER, "iat": now, "exp": now + 1100, "aud": "appstoreconnect-v1"}).encode())
    msg = (header + "." + payload).encode()
    der = subprocess.run(["openssl", "dgst", "-sha256", "-sign", key], input=msg, capture_output=True, check=True).stdout
    return header + "." + payload + "." + b64u(der_to_raw(der))


TOKEN = token()


def call(method, url, body=None, raw=None, headers=None):
    h = {}
    if not url.startswith("http"):
        url = API + url
    if url.startswith(API):
        h["Authorization"] = "Bearer " + TOKEN   # the upload URLs are pre-signed and refuse other auth
    data = None
    if body is not None:
        data = json.dumps(body).encode(); h["Content-Type"] = "application/json"
    if raw is not None:
        data = raw
    if headers:
        h.update(headers)
    req = urllib.request.Request(url, data=data, method=method, headers=h)
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            txt = r.read()
            return r.status, (json.loads(txt) if txt and r.headers.get("Content-Type", "").startswith("application/json") else None)
    except urllib.error.HTTPError as e:
        txt = e.read().decode(errors="replace")
        return e.code, {"error": txt[:800]}


def main():
    manifest = json.load(open(os.path.join(ROOT, sys.argv[1] if len(sys.argv) > 1 else "Docs/AppStore/screenshots_manifest.json")))
    say("PKR App Store screenshots", time.strftime("%c"))
    st, v = call("GET", f"/apps/{APP_ID}/appStoreVersions?filter[platform]=IOS&limit=5")
    ver = [x for x in v["data"] if x["attributes"]["appStoreState"] == "PREPARE_FOR_SUBMISSION"]
    if not ver:
        say("No version in Prepare for Submission."); return 1
    ver = ver[0]
    say("Version", ver["attributes"]["versionString"], ver["id"])
    st, locs = call("GET", f"/appStoreVersions/{ver['id']}/appStoreVersionLocalizations")
    en = [x for x in locs["data"] if x["attributes"]["locale"] == "en-US"][0]
    st, sets = call("GET", f"/appStoreVersionLocalizations/{en['id']}/appScreenshotSets?limit=50")
    by_type = {s["attributes"]["screenshotDisplayType"]: s for s in sets["data"]}
    ok = True
    for dtype, files in manifest.items():
        say(f"[{dtype}] {len(files)} files")
        s = by_type.get(dtype)
        if s is None:
            st, r = call("POST", "/appScreenshotSets", {"data": {"type": "appScreenshotSets", "attributes": {"screenshotDisplayType": dtype},
                         "relationships": {"appStoreVersionLocalization": {"data": {"type": "appStoreVersionLocalizations", "id": en["id"]}}}}})
            if st >= 300:
                say("  could not create the set:", r); ok = False; continue
            s = r["data"]; say("  created set", s["id"])
        st, old = call("GET", f"/appScreenshotSets/{s['id']}/appScreenshots?limit=50")
        for o in old.get("data", []):
            st, _ = call("DELETE", f"/appScreenshots/{o['id']}")
            say("  removed", o["attributes"]["fileName"], st)
        new_ids = []
        for f in files:
            path = os.path.join(ROOT, f)
            data = open(path, "rb").read()
            name = os.path.basename(f)
            st, r = call("POST", "/appScreenshots", {"data": {"type": "appScreenshots", "attributes": {"fileName": name, "fileSize": len(data)},
                         "relationships": {"appScreenshotSet": {"data": {"type": "appScreenshotSets", "id": s["id"]}}}}})
            if st >= 300:
                say("  reserve failed", name, r); ok = False; continue
            shot = r["data"]
            for op in shot["attributes"]["uploadOperations"]:
                hdr = {x["name"]: x["value"] for x in op.get("requestHeaders", [])}
                part = data[op["offset"]:op["offset"] + op["length"]]
                pst, _ = call(op["method"], op["url"], raw=part, headers=hdr)
                if pst >= 300:
                    say("  upload part failed", name, pst, _); ok = False
            st, r = call("PATCH", f"/appScreenshots/{shot['id']}", {"data": {"type": "appScreenshots", "id": shot["id"],
                         "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(data).hexdigest()}}})
            say("  uploaded", name, st)
            new_ids.append(shot["id"])
        if new_ids:
            st, _ = call("PATCH", f"/appScreenshotSets/{s['id']}/relationships/appScreenshots",
                         {"data": [{"type": "appScreenshots", "id": i} for i in new_ids]})
            say("  order set", st)
    # wait for processing and report
    time.sleep(20)
    for dtype in manifest:
        st, sets = call("GET", f"/appStoreVersionLocalizations/{en['id']}/appScreenshotSets?limit=50")
        for s in sets["data"]:
            if s["attributes"]["screenshotDisplayType"] != dtype:
                continue
            st, shots = call("GET", f"/appScreenshotSets/{s['id']}/appScreenshots?limit=50")
            for x in shots["data"]:
                a = x["attributes"]
                state = (a.get("assetDeliveryState") or {}).get("state")
                say(f"  {dtype}: {a['fileName']}  {state}  {(a.get('assetDeliveryState') or {}).get('errors') or ''}")
                if state not in ("COMPLETE", "UPLOAD_COMPLETE"):
                    ok = ok and state is not None
    say("DONE." if ok else "DONE WITH PROBLEMS.")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
