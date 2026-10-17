#!/usr/bin/env python3
"""
Tiny App Store Connect API client (standard library + the system openssl), shared by the Tools/asc_*.py scripts.
The team key lives OUTSIDE the repo in ../.appstoreconnect/AuthKey_<id>.p8 and never enters it.
"""
import base64, glob, json, os, subprocess, time, urllib.error, urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP_ID = "6821419588"
ISSUER = os.environ.get("ASC_ISSUER", "e68f3170-f3b3-42c1-a66a-c33b1455907e")
API = "https://api.appstoreconnect.apple.com/v1"


def _b64u(b):
    return base64.urlsafe_b64encode(b).rstrip(b"=").decode()


def _der_to_raw(der):
    i = 2 + (der[1] & 0x7F if der[1] & 0x80 else 0)
    assert der[i] == 2
    lr = der[i + 1]; r = der[i + 2:i + 2 + lr]; i += 2 + lr
    assert der[i] == 2
    ls = der[i + 1]; s = der[i + 2:i + 2 + ls]
    return r.lstrip(b"\0").rjust(32, b"\0") + s.lstrip(b"\0").rjust(32, b"\0")


def key_path():
    keys = glob.glob(os.path.join(ROOT, "..", ".appstoreconnect", "AuthKey_*.p8"))
    return keys[0] if keys else None


def token():
    key = key_path()
    if not key:
        raise SystemExit("No App Store Connect key in ../.appstoreconnect/")
    kid = os.path.basename(key)[len("AuthKey_"):-3]
    now = int(time.time())
    header = _b64u(json.dumps({"alg": "ES256", "kid": kid, "typ": "JWT"}).encode())
    payload = _b64u(json.dumps({"iss": ISSUER, "iat": now, "exp": now + 1100, "aud": "appstoreconnect-v1"}).encode())
    msg = (header + "." + payload).encode()
    der = subprocess.run(["openssl", "dgst", "-sha256", "-sign", key], input=msg, capture_output=True, check=True).stdout
    return header + "." + payload + "." + _b64u(_der_to_raw(der))


_TOKEN = None


def call(method, url, body=None, raw=None, headers=None):
    """(status, json-or-None). API paths get the bearer token; pre-signed upload URLs must not."""
    global _TOKEN
    h = {}
    if not url.startswith("http"):
        url = API + url
    if url.startswith(API):
        if _TOKEN is None:
            _TOKEN = token()
        h["Authorization"] = "Bearer " + _TOKEN
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
            ctype = r.headers.get("Content-Type", "")
            return r.status, (json.loads(txt) if txt and "json" in ctype else None)
    except urllib.error.HTTPError as e:
        return e.code, {"error": e.read().decode(errors="replace")[:800]}
    except Exception as e:  # network down etc.
        return 0, {"error": str(e)}


def get(url):
    st, j = call("GET", url)
    return j if st == 200 and j else {"data": None, "status": st, "error": (j or {}).get("error")}
