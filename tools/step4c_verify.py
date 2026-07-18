#!/usr/bin/env python3
"""Step 4C verification helper: JWT, HTTP, and SQL evidence queries."""

from __future__ import annotations

import base64
import hashlib
import hmac
import json
import sys
import time
import uuid
from pathlib import Path
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

API_BASE = __import__("os").environ.get(
    "STEP4C_API_BASE", "http://dev.smart-ics.com:8089/BilregApi/api"
)
JWT_KEY = b"ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0="
OUT_DIR = Path(__file__).resolve().parent / "step4c_artifacts"
JWT_PATH = OUT_DIR / "jwt.txt"


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def make_jwt() -> str:
    header = b64url(json.dumps({"alg": "HS256", "typ": "JWT"}, separators=(",", ":")).encode())
    now = int(time.time())
    payload_obj = {
        "sub": "BilregApiAccessToken",
        "jti": str(uuid.uuid4()),
        "iat": now,
        "exp": now + 43200,
        "iss": "BilregApiServer",
        "aud": "BilregApiClient",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "step4c@verify.local",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "Step4CVerifier",
    }
    payload = b64url(json.dumps(payload_obj, separators=(",", ":")).encode())
    msg = f"{header}.{payload}".encode()
    sig = b64url(hmac.new(JWT_KEY, msg, hashlib.sha256).digest())
    return f"{header}.{payload}.{sig}"


def api(method: str, path: str, token: str, body: dict | None = None) -> tuple[int, Any]:
    url = f"{API_BASE}/{path.lstrip('/')}"
    data = None if body is None else json.dumps(body).encode()
    req = Request(url, data=data, method=method)
    req.add_header("Authorization", f"Bearer {token}")
    req.add_header("Accept", "application/json")
    if body is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urlopen(req, timeout=60) as resp:
            raw = resp.read().decode()
            return resp.status, json.loads(raw) if raw else None
    except HTTPError as e:
        raw = e.read().decode() if e.fp else ""
        try:
            parsed = json.loads(raw) if raw else {"raw": raw}
        except json.JSONDecodeError:
            parsed = {"raw": raw}
        return e.code, parsed
    except URLError as e:
        return 0, {"error": str(e)}


def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    cmd = sys.argv[1] if len(sys.argv) > 1 else "rollout"

    if cmd == "jwt":
        token = make_jwt()
        JWT_PATH.write_text(token, encoding="utf-8")
        print(f"JWT_WRITTEN {len(token)}")
        return 0

    token = JWT_PATH.read_text(encoding="utf-8").strip() if JWT_PATH.exists() else make_jwt()
    if not JWT_PATH.exists():
        JWT_PATH.write_text(token, encoding="utf-8")

    if cmd == "rollout":
        code, body = api("GET", "admisi-ranap/rollout/status", token)
        print(json.dumps({"status": code, "body": body}, indent=2))
        return 0 if code == 200 else 1

    if cmd == "get":
        path = sys.argv[2]
        code, body = api("GET", path, token)
        print(json.dumps({"status": code, "body": body}, indent=2))
        return 0 if 200 <= code < 300 else 1

    if cmd == "post":
        path = sys.argv[2]
        if len(sys.argv) > 3 and sys.argv[3].startswith("@"):
            body = json.loads(Path(sys.argv[3][1:]).read_text(encoding="utf-8"))
        else:
            body = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        code, resp = api("POST", path, token, body)
        out = {"status": code, "request": body, "response": resp}
        stamp = int(time.time())
        (OUT_DIR / f"post_{stamp}.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
        print(json.dumps(out, indent=2))
        return 0 if 200 <= code < 300 else 1

    print(f"Unknown command: {cmd}")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
