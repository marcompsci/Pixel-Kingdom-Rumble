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


def _show(url):
    st, j = A.call("GET", url)
    if st != 200:
        return f"HTTP {st} {(j or {}).get('error', '')[:160]}"
    d = (j or {}).get("data")
    if isinstance(d, list):
        return [x.get("attributes") for x in d]
    return d.get("attributes") if d else None


def iap_info(_):
    for p in iaps():
        say("IAP", p["id"], json.dumps(p["attributes"])[:400])
        for rel in ("inAppPurchaseLocalizations", "iapPriceSchedule", "inAppPurchaseAvailability", "appStoreReviewScreenshot", "content"):
            say("   ", rel, str(_show(f"{V2}/inAppPurchases/{p['id']}/{rel}"))[:500])
    return
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


def _iap(product):
    return next((x for x in iaps() if x["attributes"].get("productId") == product), None)


def iap_delete(a):
    """Deletes an UNSUBMITTED in-app purchase draft (Apple refuses for submitted ones). Needs the exact product id."""
    p = _iap(a["product"])
    if not p:
        say("SKIP no IAP", a["product"]); return
    if p["attributes"].get("state") not in ("MISSING_METADATA", "READY_TO_SUBMIT"):
        say("SKIP not a draft", a["product"], p["attributes"].get("state")); return
    st, r = A.call("DELETE", f"{V2}/inAppPurchases/{p['id']}")
    say("Deleted IAP draft", a["product"], st if st < 300 else f"FAIL {st} {r}")


def iap_price(a):
    """Sets the base price (US) from a customer price like '3.99'; App Store Connect fills other territories."""
    p = _iap(a["product"])
    if not p:
        say("FAIL no IAP", a["product"]); return
    want = a["price"]
    url = f"{V2}/inAppPurchases/{p['id']}/pricePoints?filter[territory]=USA&limit=200"
    pp = None
    while url and not pp:
        st, j = A.call("GET", url)
        if st != 200:
            say("FAIL price points", st, j); return
        for x in j.get("data", []):
            if x["attributes"].get("customerPrice") == want:
                pp = x; break
        url = (j.get("links") or {}).get("next")
    if not pp:
        say("FAIL no USA price point", want); return
    body = {"data": {"type": "inAppPurchasePriceSchedules", "relationships": {
                "inAppPurchase": {"data": {"type": "inAppPurchases", "id": p["id"]}},
                "baseTerritory": {"data": {"type": "territories", "id": "USA"}},
                "manualPrices": {"data": [{"type": "inAppPurchasePrices", "id": "${price0}"}]}}},
            "included": [{"type": "inAppPurchasePrices", "id": "${price0}", "attributes": {"startDate": None},
                          "relationships": {"inAppPurchasePricePoint": {"data": {"type": "inAppPurchasePricePoints", "id": pp["id"]}},
                                            "inAppPurchaseV2": {"data": {"type": "inAppPurchases", "id": p["id"]}}}}]}
    st, r = A.call("POST", "/inAppPurchasePriceSchedules", body)
    say("IAP price", a["product"], want, "USD (base USA)", "saved" if st < 300 else f"FAIL {st} {r}")


def iap_availability(a):
    """Available in every App Store territory, and in new ones as Apple adds them."""
    p = _iap(a["product"])
    if not p:
        say("FAIL no IAP", a["product"]); return
    terr, url = [], "/territories?limit=200"
    while url:
        st, j = A.call("GET", url)
        if st != 200:
            say("FAIL territories", st, j); return
        terr += [t["id"] for t in j.get("data", [])]
        url = (j.get("links") or {}).get("next")
    body = {"data": {"type": "inAppPurchaseAvailabilities", "attributes": {"availableInNewTerritories": True},
                     "relationships": {"inAppPurchase": {"data": {"type": "inAppPurchases", "id": p["id"]}},
                                       "availableTerritories": {"data": [{"type": "territories", "id": t} for t in terr]}}}}
    st, r = A.call("POST", "/inAppPurchaseAvailabilities", body)
    say("IAP availability", a["product"], f"{len(terr)} territories + new ones", "saved" if st < 300 else f"FAIL {st} {r}")


def beta_submit(a):
    """Adds a processed build to an external TestFlight group and submits it for TestFlight (beta) App Review.
    Beta review only: this never submits the app to the App Store."""
    b = A.get(f"/builds?filter[app]={A.APP_ID}&filter[version]={a['build']}&limit=1").get("data") or []
    if not b:
        say("FAIL no build", a["build"]); return
    b = b[0]
    if b["attributes"].get("processingState") != "VALID":
        say("FAIL build not processed yet", b["attributes"].get("processingState")); return
    groups = A.get(f"/apps/{A.APP_ID}/betaGroups?limit=50").get("data") or []
    g = next((x for x in groups if x["attributes"].get("name") == a["group"]), None)
    if not g or g["attributes"].get("isInternalGroup"):
        say("FAIL no external group named", a["group"]); return
    st, r = A.call("POST", f"/betaGroups/{g['id']}/relationships/builds", {"data": [{"type": "builds", "id": b["id"]}]})
    say("Build", a["build"], "added to", a["group"], st if st < 300 else f"FAIL {st} {r}")
    st, r = A.call("POST", "/betaAppReviewSubmissions", {"data": {"type": "betaAppReviewSubmissions",
                   "relationships": {"build": {"data": {"type": "builds", "id": b["id"]}}}}})
    if st < 300:
        say("Submitted for TestFlight App Review:", r["data"]["attributes"].get("betaReviewState"))
    else:
        say("Beta review submission", f"FAIL {st} {r}")
    bd = A.get(f"/builds/{b['id']}/buildBetaDetail").get("data")
    if bd:
        say("  external state:", bd["attributes"].get("externalBuildState"))


def version_build(a):
    """Attaches a processed build to the version in Prepare for Submission (does not submit it)."""
    b = A.get(f"/builds?filter[app]={A.APP_ID}&filter[version]={a['build']}&limit=1").get("data") or []
    if not b or b[0]["attributes"].get("processingState") != "VALID":
        say("FAIL build not found or not processed", a["build"]); return
    vers = A.get(f"/apps/{A.APP_ID}/appStoreVersions?limit=10").get("data") or []
    v = next((x for x in vers if x["attributes"]["appStoreState"] == "PREPARE_FOR_SUBMISSION"), None)
    if not v:
        say("FAIL no version in Prepare for Submission"); return
    st, r = A.call("PATCH", f"/appStoreVersions/{v['id']}/relationships/build", {"data": {"type": "builds", "id": b[0]["id"]}})
    say("Version", v["attributes"]["versionString"], "now uses build", a["build"], st if st < 300 else f"FAIL {st} {r}")


def beta_info(_):
    for rel in ("betaLicenseAgreement", "betaAppReviewDetail", "betaAppLocalizations"):
        say(rel, str(_show(f"/apps/{A.APP_ID}/{rel}"))[:600])


def beta_text(a):
    """TestFlight Test Information: beta app description (en-US) and the notes for Beta App Review."""
    desc = open(os.path.join(A.ROOT, a["description"]), encoding="utf-8").read().strip()
    notes = open(os.path.join(A.ROOT, a["notes"]), encoding="utf-8").read().strip()
    locs = A.get(f"/apps/{A.APP_ID}/betaAppLocalizations").get("data") or []
    en = next((l for l in locs if l["attributes"].get("locale") == "en-US"), None)
    if en:
        st, r = A.call("PATCH", f"/betaAppLocalizations/{en['id']}", {"data": {"type": "betaAppLocalizations", "id": en["id"], "attributes": {"description": desc}}})
        say("Beta app description", "saved" if st < 300 else f"FAIL {st} {r}", f"({len(desc)} characters)")
    rd = A.get(f"/apps/{A.APP_ID}/betaAppReviewDetail").get("data")
    if rd:
        st, r = A.call("PATCH", f"/betaAppReviewDetails/{rd['id']}", {"data": {"type": "betaAppReviewDetails", "id": rd["id"], "attributes": {"notes": notes}}})
        say("Beta App Review notes", "saved" if st < 300 else f"FAIL {st} {r}", f"({len(notes)} characters)")


# ---- Game Center: leaderboards and achievements (created only if missing; nothing is released or submitted) ----------

def _gc_detail():
    d = A.get(f"/apps/{A.APP_ID}/gameCenterDetail").get("data")
    if not d:
        raise RuntimeError("Game Center isn't set up for the app")
    return d["id"]


def _all(url):
    out = []
    while url:
        j = A.get(url)
        out += j.get("data") or []
        url = (j.get("links") or {}).get("next")
    return out


def gc_info(_):
    gid = _gc_detail()
    for lb in _all(f"/gameCenterDetails/{gid}/gameCenterLeaderboards?limit=200"):
        say("LEADERBOARD", lb["attributes"].get("vendorIdentifier"), lb["attributes"].get("referenceName"), lb["attributes"].get("archived"))
    for ac in _all(f"/gameCenterDetails/{gid}/gameCenterAchievements?limit=200"):
        a = ac["attributes"]
        say("ACHIEVEMENT", a.get("vendorIdentifier"), a.get("referenceName"), a.get("points"), a.get("archived"))
    say("Game Center versions:", str(_show(f"/gameCenterDetails/{gid}/gameCenterAppVersions"))[:600])


def gc_leaderboard(a):
    """{"do":"gc_leaderboard","vendor":"pkr.lb.gravemoor","ref":"Last Stand: Gravemoor","name":"...","suffix":" rounds","suffix_one":" round"}"""
    gid = _gc_detail()
    have = {x["attributes"].get("vendorIdentifier"): x for x in _all(f"/gameCenterDetails/{gid}/gameCenterLeaderboards?limit=200")}
    lb = have.get(a["vendor"])
    if lb is None:
        st, r = A.call("POST", "/gameCenterLeaderboards", {"data": {"type": "gameCenterLeaderboards", "attributes": {
            "referenceName": a["ref"], "vendorIdentifier": a["vendor"], "defaultFormatter": "INTEGER",
            "submissionType": "BEST_SCORE", "scoreSortType": "DESC"},
            "relationships": {"gameCenterDetail": {"data": {"type": "gameCenterDetails", "id": gid}}}}})
        if st >= 300:
            say("Leaderboard", a["vendor"], f"FAIL {st} {r}"); return
        lb = r["data"]
        say("Leaderboard", a["vendor"], "created", lb["id"])
    else:
        say("Leaderboard", a["vendor"], "already exists", lb["id"])
    locs = _all(f"/gameCenterLeaderboards/{lb['id']}/localizations?limit=50")
    if not any(l["attributes"].get("locale") == "en-US" for l in locs):
        st, r = A.call("POST", "/gameCenterLeaderboardLocalizations", {"data": {"type": "gameCenterLeaderboardLocalizations", "attributes": {
            "locale": "en-US", "name": a["name"], "formatterSuffix": a.get("suffix", ""), "formatterSuffixSingular": a.get("suffix_one", "")},
            "relationships": {"gameCenterLeaderboard": {"data": {"type": "gameCenterLeaderboards", "id": lb["id"]}}}}})
        say("   en-US", "saved" if st < 300 else f"FAIL {st} {r}")
    else:
        say("   en-US already there")


def _upload(kind, rel_name, rel_type, rel_id, path):
    data = open(os.path.join(A.ROOT, path), "rb").read()
    st, r = A.call("POST", "/" + kind, {"data": {"type": kind, "attributes": {"fileName": os.path.basename(path), "fileSize": len(data)},
                                                "relationships": {rel_name: {"data": {"type": rel_type, "id": rel_id}}}}})
    if st >= 300:
        return f"reserve FAIL {st} {r}"
    obj = r["data"]
    for op in obj["attributes"]["uploadOperations"]:
        hdr = {x["name"]: x["value"] for x in op.get("requestHeaders", [])}
        pst, perr = A.call(op["method"], op["url"], raw=data[op["offset"]:op["offset"] + op["length"]], headers=hdr)
        if pst >= 300:
            return f"upload FAIL {pst} {perr}"
    st, r = A.call("PATCH", f"/{kind}/{obj['id']}", {"data": {"type": kind, "id": obj["id"], "attributes": {"uploaded": True}}})
    return "uploaded" if st < 300 else f"commit FAIL {st} {r}"


def gc_achievement(a):
    """{"do":"gc_achievement","vendor":"pkr.ach.z_chapter4","ref":"...","points":40,"name":"...","before":"...","after":"...","image":"Docs/..png"}"""
    gid = _gc_detail()
    have = {x["attributes"].get("vendorIdentifier"): x for x in _all(f"/gameCenterDetails/{gid}/gameCenterAchievements?limit=200")}
    ac = have.get(a["vendor"])
    if ac is None:
        st, r = A.call("POST", "/gameCenterAchievements", {"data": {"type": "gameCenterAchievements", "attributes": {
            "referenceName": a["ref"], "vendorIdentifier": a["vendor"], "points": int(a["points"]),
            "showBeforeEarned": True, "repeatable": False},
            "relationships": {"gameCenterDetail": {"data": {"type": "gameCenterDetails", "id": gid}}}}})
        if st >= 300:
            say("Achievement", a["vendor"], f"FAIL {st} {r}"); return
        ac = r["data"]
        say("Achievement", a["vendor"], "created", ac["id"])
    else:
        say("Achievement", a["vendor"], "already exists", ac["id"])
    locs = _all(f"/gameCenterAchievements/{ac['id']}/localizations?limit=50")
    loc = next((l for l in locs if l["attributes"].get("locale") == "en-US"), None)
    if loc is None:
        st, r = A.call("POST", "/gameCenterAchievementLocalizations", {"data": {"type": "gameCenterAchievementLocalizations", "attributes": {
            "locale": "en-US", "name": a["name"], "beforeEarnedDescription": a["before"], "afterEarnedDescription": a["after"]},
            "relationships": {"gameCenterAchievement": {"data": {"type": "gameCenterAchievements", "id": ac["id"]}}}}})
        if st >= 300:
            say("   en-US", f"FAIL {st} {r}"); return
        loc = r["data"]
        say("   en-US saved")
    img = A.get(f"/gameCenterAchievementLocalizations/{loc['id']}/gameCenterAchievementImage").get("data")
    if img:
        say("   image already there", (img["attributes"].get("assetDeliveryState") or {}).get("state"))
    elif a.get("image"):
        say("   image", _upload("gameCenterAchievementImages", "gameCenterAchievementLocalization", "gameCenterAchievementLocalizations", loc["id"], a["image"]))


def gc_release_info(_):
    """Which leaderboards and achievements are on the app's Game Center releases (what the version's Game Center section shows)."""
    gid = _gc_detail()
    for kind, rel in (("achievementReleases", "gameCenterAchievement"), ("leaderboardReleases", "gameCenterLeaderboard")):
        st, j = A.call("GET", f"/gameCenterDetails/{gid}/{kind}?limit=200&include={rel}")
        if st != 200:
            say(kind, f"HTTP {st} {(j or {}).get('error', '')[:200]}"); continue
        inc = {x["id"]: x["attributes"].get("vendorIdentifier") for x in (j.get("included") or [])}
        for r in j.get("data") or []:
            rid = ((r.get("relationships") or {}).get(rel) or {}).get("data") or {}
            say(kind, inc.get(rid.get("id"), rid.get("id")), json.dumps(r.get("attributes"))[:200])


def gc_release(a):
    """{"do":"gc_release","vendor":"pkr.ach.z_chapter4"} adds it to the app's Game Center release (the version's Game Center section)."""
    gid = _gc_detail()
    is_lb = a["vendor"].startswith("pkr.lb.")
    items = _all(f"/gameCenterDetails/{gid}/{'gameCenterLeaderboards' if is_lb else 'gameCenterAchievements'}?limit=200")
    item = next((x for x in items if x["attributes"].get("vendorIdentifier") == a["vendor"]), None)
    if item is None:
        say("Release", a["vendor"], "FAIL not found"); return
    typ = "gameCenterLeaderboardReleases" if is_lb else "gameCenterAchievementReleases"
    rel = "gameCenterLeaderboard" if is_lb else "gameCenterAchievement"
    st, r = A.call("POST", "/" + typ, {"data": {"type": typ, "relationships": {
        "gameCenterDetail": {"data": {"type": "gameCenterDetails", "id": gid}},
        rel: {"data": {"type": rel + "s", "id": item["id"]}}}}})
    say("Release", a["vendor"], "added" if st < 300 else f"HTTP {st} {(r or {}).get('error', '')[:300]}")


ACTIONS = {"gc_release_info": gc_release_info, "gc_release": gc_release, "gc_info": gc_info, "gc_leaderboard": gc_leaderboard, "gc_achievement": gc_achievement, "beta_text": beta_text, "beta_info": beta_info, "version_build": version_build, "beta_submit": beta_submit, "iap_delete": iap_delete, "iap_price": iap_price, "iap_availability": iap_availability, "iap_info": iap_info, "iap_review_screenshot": iap_review_screenshot, "whats_new": whats_new, "content_rights": content_rights}


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
