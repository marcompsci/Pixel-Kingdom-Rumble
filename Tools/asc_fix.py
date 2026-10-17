#!/usr/bin/env python3
"""
Small, explicit App Store Connect metadata fixes for Pixel Kingdom Rumble (never submits anything for review).
The Mac watcher runs it when Logs/asc_fix_request exists; that file is JSON: a list of actions, e.g.
  [{"do": "iap_info"},
   {"do": "iap_review_screenshot", "product": "com.marcompsci.pixelkingdomrumble.shadowcontracts",
    "file": "Docs/AppStore/5_shadow_contracts_board.png"},
   {"do": "whats_new", "build": "20261016.1842", "file": "Docs/whats-to-test.txt"},
   {"do": "content_rights", "value": "DOES_NOT_USE_THIRD_PARTY_CONTENT"}]
Results: Logs/asc_fix/SUMMARY.txt.
"""
import hashlib, json, os, sys, time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import asc_api as A

V2 = "https://api.appstoreconnect.apple.com/v2"
OUT = os.path.join(A.ROOT, "Logs", "asc_fix")
os.makedirs(OUT, exist_ok=True)
LOG = open(os.path.join(OUT, "SUMMARY.txt"), "w")


def say(*a):
    s = " ".join(str(x) for x in a)
    print(s); LOG.write(s + "\n"); LOG.flush()


def iaps():
    return A.get(f"/apps/{A.APP_ID}/inAppPurchasesV2?limit=50").get("data") or []


def iap_info(_):
    for p in iaps():
        pid = p["id"]
        loc = A.get(f"{V2}/inAppPurchases/{pid}/inAppPurchaseLocalizations").get("data") or []
        sched = A.get(f"{V2}/inAppPurchases/{pid}/iapPriceSchedule").get("data")
        avail = A.get(f"{V2}/inAppPurchases/{pid}/inAppPurchaseAvailability").get("data")
        shot = A.get(f"{V2}/inAppPurchases/{pid}/appStoreReviewScreenshot").get("data")
        say("IAP", p["attributes"].get("productId"), p["attributes"].get("name"), p["attributes"].get("state"),
            "| localizations:", [(l["attributes"].get("locale"), l["attributes"].get("name"), l["attributes"].get("state")) for l in loc],
            "| price schedule:", bool(sched), "| availability:", bool(avail),
            "| review screenshot:", (shot or {}).get("attributes", {}).get("assetDeliveryState") if shot else None,
            "| review note:", bool(p["attributes"].get("reviewNote")), "| familySharable:", p["attributes"].get("familySharable"))


def iap_review_screenshot(a):
    p = next((x for x in iaps() if x["attributes"].get("productId") == a["product"]), None)
    if not p:
        say("FAIL no IAP with product id", a["product"]); return
    old = A.get(f"{V2}/inAppPurchases/{p['id']}/appStoreReviewScreenshot").get("data")
    if old:
        st, _ = A.call("DELETE", f"/inAppPurchaseAppStoreReviewScreenshots/{old['id']}")
        say("removed the old review screenshot", st)
    data = open(os.path.join(A.ROOT, a["file"]), "rb").read()
    st, r = A.call("POST", "/inAppPurchaseAppStoreReviewScreenshots", {"data": {"type": "inAppPurchaseAppStoreReviewScreenshots",
                   "attributes": {"fileName": os.path.basename(a["file"]), "fileSize": len(data)},
                   "relationships": {"inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": p["id"]}}}}})
    if st >= 300:
        say("FAIL reserve", st, r); return
    s = r["data"]
    for op in s["attributes"]["uploadOperations"]:
        hdr = {x["name"]: x["value"] for x in op.get("requestHeaders", [])}
        pst, pr = A.call(op["method"], op["url"], raw=data[op["offset"]:op["offset"] + op["length"]], headers=hdr)
        if pst >= 300:
            say("FAIL upload part", pst, pr); return
    st, r = A.call("PATCH", f"/inAppPurchaseAppStoreReviewScreenshots/{s['id']}", {"data": {"type": "inAppPurchaseAppStoreReviewScreenshots",
                   "id": s["id"], "attributes": {"uploaded": True, "sourceFileChecksum": hashlib.md5(data).hexdigest()}}})
    say("IAP review screenshot", a["product"], "committed" if st < 300 else f"FAIL {st} {r}")
    time.sleep(15)
    got = A.get(f"/inAppPurchaseAppStoreReviewScreenshots/{s['id']}").get("data")
    say("  state:", (got or {}).get("attributes", {}).get("assetDeliveryState"))


def whats_new(a):
    b = A.get(f"/builds?filter[app]={A.APP_ID}&filter[version]={a['build']}&limit=1").get("data") or []
    if not b:
        say("FAIL no build", a["build"]); return
    text = open(os.path.join(A.ROOT, a["file"]), encoding="utf-8").read().strip()
    locs = A.get(f"/builds/{b[0]['id']}/betaBuildLocalizations").get("data") or []
    en = next((l for l in locs if l["attributes"].get("locale") == "en-US"), None)
    if en:
        st, r = A.call("PATCH", f"/betaBuildLocalizations/{en['id']}", {"data": {"type": "betaBuildLocalizations", "id": en["id"], "attributes": {"whatsNew": text}}})
    else:
        st, r = A.call("POST", "/betaBuildLocalizations", {"data": {"type": "betaBuildLocalizations", "attributes": {"locale": "en-US", "whatsNew": text},
                       "relationships": {"build": {"data": {"type": "builds", "id": b[0]["id"]}}}}})
    say("What to Test for", a["build"], "saved" if st < 300 else f"FAIL {st} {r}", f"({len(text)} characters)")


def content_rights(a):
    st, r = A.call("PATCH", f"/apps/{A.APP_ID}", {"data": {"type": "apps", "id": A.APP_ID, "attributes": {"contentRightsDeclaration": a["value"]}}})
    say("Content rights declaration", a["value"], "saved" if st < 300 else f"FAIL {st} {r}")


ACTIONS = {"iap_info": iap_info, "iap_review_screenshot": iap_review_screenshot, "whats_new": whats_new, "content_rights": content_rights}


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(A.ROOT, "Logs", "asc_fix.json")
    todo = json.load(open(path))
    say("PKR App Store Connect fixes", time.strftime("%c"))
    for a in todo:
        fn = ACTIONS.get(a.get("do"))
        if not fn:
            say("SKIP unknown action", a); continue
        try:
            fn(a)
        except Exception as e:
            say("FAIL", a.get("do"), repr(e))
    say("DONE.")


if __name__ == "__main__":
    main()
