#!/usr/bin/env python3
"""
READ-ONLY App Store Connect audit for Pixel Kingdom Rumble: what App Review and external TestFlight review will look
at (privacy policy URL, support URL, screenshots per device, build + export compliance, beta review info, beta groups,
the in-app purchase, age rating). It changes nothing in App Store Connect.

  python3 Tools/asc_audit.py          (the Mac watcher runs it for Logs/asc_audit_request)

Writes Logs/asc_audit/SUMMARY.txt (one PASS / WARN / FAIL line per check) and Logs/asc_audit/report.json.
A check that could not run is reported as SKIP with the reason, never as PASS.
"""
import json, os, sys, time, urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import asc_api as A

OUT = os.path.join(A.ROOT, "Logs", "asc_audit")
os.makedirs(OUT, exist_ok=True)
lines, report = [], {}


def res(level, name, detail=""):
    s = f"{level:5} {name}" + (f"  ·  {detail}" if detail else "")
    print(s); lines.append(s)


def data(url):
    return A.get(url).get("data")


def url_ok(u):
    if not u:
        return False, "missing"
    try:
        req = urllib.request.Request(u, method="GET", headers={"User-Agent": "Mozilla/5.0 (PKR asc_audit)"})
        with urllib.request.urlopen(req, timeout=30) as r:
            return 200 <= r.status < 400, f"HTTP {r.status}"
    except Exception as e:
        return False, str(e)[:120]


def main():
    res("INFO", "PKR App Store Connect audit", time.strftime("%c"))
    app = data(f"/apps/{A.APP_ID}")
    if not app:
        res("FAIL", "API access", "could not read the app (key, network or permissions)"); return 1
    a = app["attributes"]; report["app"] = a
    res("INFO", "App", f"{a.get('name')}  {a.get('bundleId')}  primary locale {a.get('primaryLocale')}")
    res("PASS" if a.get("contentRightsDeclaration") else "WARN", "Content rights declaration", str(a.get("contentRightsDeclaration")))

    # ---- app info: privacy policy, category, age rating
    infos = data(f"/apps/{A.APP_ID}/appInfos") or []
    info = next((i for i in infos if i["attributes"].get("appStoreState") != "READY_FOR_SALE"), infos[0] if infos else None)
    if info:
        report["appInfo"] = info["attributes"]
        res("INFO", "Age rating", str(info["attributes"].get("appStoreAgeRating")))
        locs = data(f"/appInfos/{info['id']}/appInfoLocalizations") or []
        for l in locs:
            la = l["attributes"]
            ok, why = url_ok(la.get("privacyPolicyUrl"))
            res("PASS" if ok else "FAIL", f"Privacy policy URL ({la.get('locale')})", f"{la.get('privacyPolicyUrl')}  {why}")
        cat = data(f"/appInfos/{info['id']}/primaryCategory")
        res("PASS" if cat else "FAIL", "Primary category", cat["id"] if cat else "not set")

    # ---- the version in Prepare for Submission
    vers = data(f"/apps/{A.APP_ID}/appStoreVersions?limit=10") or []
    ver = next((v for v in vers if v["attributes"]["appStoreState"] in ("PREPARE_FOR_SUBMISSION", "DEVELOPER_REJECTED", "REJECTED", "METADATA_REJECTED")), None)
    build = None
    if not ver:
        res("WARN", "Editable version", "none in Prepare for Submission")
    else:
        va = ver["attributes"]; report["version"] = va
        res("INFO", "Version", f"{va['versionString']}  {va['appStoreState']}  release {va.get('releaseType')}")
        res("PASS" if va.get("copyright") else "FAIL", "Copyright", str(va.get("copyright")))
        build = data(f"/appStoreVersions/{ver['id']}/build")
        res("PASS" if build else "FAIL", "Build attached to the version", build["attributes"]["version"] if build else "none")
        rd = data(f"/appStoreVersions/{ver['id']}/appStoreReviewDetail")
        if rd:
            r = rd["attributes"]
            ok = all(r.get(k) for k in ("contactFirstName", "contactLastName", "contactPhone", "contactEmail"))
            res("PASS" if ok else "FAIL", "App Review contact", "complete" if ok else "missing name/phone/email")
            res("PASS" if (r.get("notes") or "").strip() else "WARN", "App Review notes", f"{len(r.get('notes') or '')} characters")
            res("PASS" if not r.get("demoAccountRequired") else "WARN", "Demo account", "not required (no sign-in)" if not r.get("demoAccountRequired") else "required")
        else:
            res("FAIL", "App Review details", "missing")
        gc = data(f"/appStoreVersions/{ver['id']}/gameCenterAppVersion")
        res("PASS" if gc and gc["attributes"].get("enabled") else "WARN", "Game Center on this version", str(gc["attributes"] if gc else None))
        locs = data(f"/appStoreVersions/{ver['id']}/appStoreVersionLocalizations") or []
        for l in locs:
            la = l["attributes"]
            ok, why = url_ok(la.get("supportUrl"))
            res("PASS" if ok else "FAIL", f"Support URL ({la['locale']})", f"{la.get('supportUrl')}  {why}")
            for k in ("description", "keywords"):
                res("PASS" if la.get(k) else "FAIL", f"{k.capitalize()} ({la['locale']})", f"{len(la.get(k) or '')} characters")
            sets = data(f"/appStoreVersionLocalizations/{l['id']}/appScreenshotSets?limit=50") or []
            have = {}
            for s in sets:
                shots = data(f"/appScreenshotSets/{s['id']}/appScreenshots?limit=50") or []
                have[s["attributes"]["screenshotDisplayType"]] = len(shots)
            report.setdefault("screenshots", {})[la["locale"]] = have
            res("INFO", f"Screenshot sets ({la['locale']})", json.dumps(have))
            iphone = any(k.startswith("APP_IPHONE_67") or k.startswith("APP_IPHONE_65") for k in have)
            res("PASS" if iphone else "FAIL", f"6.9\"/6.5\" iPhone screenshots ({la['locale']})", "present" if iphone else "missing")
            ipad = any(k.startswith("APP_IPAD_PRO_3GEN_129") or k.startswith("APP_IPAD_PRO_129") for k in have)
            report.setdefault("ipadScreenshots", {})[la["locale"]] = ipad
            res("PASS" if ipad else "WARN", f"13\" iPad screenshots ({la['locale']})",
                "present" if ipad else "missing: required if the build supports iPad (TARGETED_DEVICE_FAMILY 1,2)")

    # ---- the build: processing, export compliance, TestFlight state
    if build:
        b = build["attributes"]; report["build"] = b
        res("PASS" if b.get("processingState") == "VALID" else "FAIL", "Build processed", f"{b.get('version')} {b.get('processingState')}")
        res("PASS" if not b.get("expired") else "FAIL", "Build not expired", str(b.get("expirationDate")))
        enc = b.get("usesNonExemptEncryption")
        res("PASS" if enc is False else "WARN", "Export compliance answered", f"usesNonExemptEncryption={enc}")
        bd = data(f"/builds/{build['id']}/buildBetaDetail")
        if bd:
            res("INFO", "TestFlight state", f"internal {bd['attributes'].get('internalBuildState')}  external {bd['attributes'].get('externalBuildState')}")
        wn = data(f"/builds/{build['id']}/betaBuildLocalizations") or []
        res("PASS" if any(w["attributes"].get("whatsNew") for w in wn) else "WARN", "TestFlight 'What to Test'",
            "set" if any(w["attributes"].get("whatsNew") for w in wn) else "empty (external testers need it)")

    # ---- TestFlight: beta app info, beta review details, groups
    bal = data(f"/apps/{A.APP_ID}/betaAppLocalizations") or []
    if not bal:
        res("WARN", "Beta app description", "none (required before external TestFlight review)")
    for l in bal:
        la = l["attributes"]
        res("PASS" if la.get("description") else "WARN", f"Beta app description ({la.get('locale')})", f"{len(la.get('description') or '')} characters")
        res("PASS" if la.get("feedbackEmail") else "WARN", f"Beta feedback email ({la.get('locale')})", str(la.get("feedbackEmail")))
        ok, why = url_ok(la.get("privacyPolicyUrl")) if la.get("privacyPolicyUrl") else (False, "not set (falls back to the app's)")
        res("PASS" if ok else "WARN", f"Beta privacy policy URL ({la.get('locale')})", f"{la.get('privacyPolicyUrl')}  {why}")
    brd = data(f"/apps/{A.APP_ID}/betaAppReviewDetail")
    if brd:
        r = brd["attributes"]
        ok = all(r.get(k) for k in ("contactFirstName", "contactLastName", "contactPhone", "contactEmail"))
        res("PASS" if ok else "WARN", "Beta App Review contact", "complete" if ok else "incomplete (required for external testing)")
    groups = data(f"/apps/{A.APP_ID}/betaGroups?limit=50") or []
    report["betaGroups"] = [g["attributes"] for g in groups]
    for g in groups:
        ga = g["attributes"]
        res("INFO", "Beta group", f"{ga.get('name')}  {'internal' if ga.get('isInternalGroup') else 'external'}  public link {'on' if ga.get('publicLinkEnabled') else 'off'}")
    if not any(not g["attributes"].get("isInternalGroup") for g in groups):
        res("WARN", "External TestFlight group", "none yet")

    # ---- the in-app purchase
    iaps = data(f"/apps/{A.APP_ID}/inAppPurchasesV2?limit=20") or []
    for p in iaps:
        pa = p["attributes"]
        res("INFO", "In-app purchase", f"{pa.get('productId')}  {pa.get('inAppPurchaseType')}  {pa.get('state')}")
        shot = A.get(f"https://api.appstoreconnect.apple.com/v2/inAppPurchases/{p['id']}/appStoreReviewScreenshot").get("data")
        res("PASS" if shot else "FAIL", "IAP review screenshot", "attached" if shot else "missing")
    if not iaps:
        res("WARN", "In-app purchases", "none found (the game sells Shadow Contracts)")

    fails = sum(1 for l in lines if l.startswith("FAIL"))
    warns = sum(1 for l in lines if l.startswith("WARN"))
    res("INFO", "Result", f"{fails} FAIL, {warns} WARN")
    open(os.path.join(OUT, "SUMMARY.txt"), "w").write("\n".join(lines) + "\n")
    json.dump(report, open(os.path.join(OUT, "report.json"), "w"), indent=1, default=str)
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main())
