#!/usr/bin/env python3
"""Step 6 browser E2E support: seed sources, SQL evidence, cleanup."""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parent
ART = ROOT / "step6_artifacts"
ART.mkdir(parents=True, exist_ok=True)

API_BASE = "http://dev.smart-ics.com:8089/BilregApi/api"
CONN_STR = (
    "Server=dev.smart-ics.com;Database=HOSPITAL_HPL;"
    "User Id=bilregLogin;Password=bilreg123!;"
    "TrustServerCertificate=True;Connection Timeout=30"
)

sys.path.insert(0, str(ROOT))
from step4c_verify import make_jwt  # noqa: E402


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


def _ps_sql(script: str) -> Any:
    with tempfile.NamedTemporaryFile("w", suffix=".ps1", delete=False, encoding="utf-8") as f:
        f.write(script)
        path = f.name
    try:
        completed = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                path,
            ],
            capture_output=True,
            text=True,
            timeout=120,
            check=False,
        )
        out = (completed.stdout or "").strip()
        err = (completed.stderr or "").strip()
        if completed.returncode != 0:
            raise RuntimeError(f"SQL powershell failed: {err or out}")
        if not out:
            return None
        return json.loads(out)
    finally:
        Path(path).unlink(missing_ok=True)


def sql_query(sql: str, params: dict[str, Any] | None = None) -> list[dict[str, Any]]:
    params = params or {}
    param_lines = []
    for k, v in params.items():
        lit = "NULL" if v is None else ("'" + str(v).replace("'", "''") + "'")
        param_lines.append(f'$cmd.Parameters.AddWithValue("@{k}", {lit}) | Out-Null')
    param_block = "\n".join(param_lines)
    # Convert ? placeholders to named @p0 @p1 when dict uses p0.. style, else keep named.
    script = f"""
$ErrorActionPreference = 'Stop'
$conn = New-Object System.Data.SqlClient.SqlConnection '{CONN_STR}'
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @'
{sql}
'@
{param_block}
$reader = $cmd.ExecuteReader()
$rows = @()
while ($reader.Read()) {{
  $obj = [ordered]@{{}}
  for ($i = 0; $i -lt $reader.FieldCount; $i++) {{
    $name = $reader.GetName($i)
    $val = if ($reader.IsDBNull($i)) {{ $null }} else {{ $reader.GetValue($i) }}
    $obj[$name] = $val
  }}
  $rows += [pscustomobject]$obj
}}
$reader.Close()
$conn.Close()
$rows | ConvertTo-Json -Depth 6 -Compress
"""
    result = _ps_sql(script)
    if result is None:
        return []
    if isinstance(result, dict):
        return [result]
    if isinstance(result, list):
        return result
    return []


def sql_exec_many(statements: list[tuple[str, dict[str, Any]]]) -> None:
    parts = []
    for idx, (sql, params) in enumerate(statements):
        binds = []
        for k, v in params.items():
            lit = "NULL" if v is None else ("'" + str(v).replace("'", "''") + "'")
            binds.append(f'$cmd{idx}.Parameters.AddWithValue("@{k}", {lit}) | Out-Null')
        parts.append(
            f"""
$cmd{idx} = $conn.CreateCommand()
$cmd{idx}.Transaction = $tx
$cmd{idx}.CommandText = @'
{sql}
'@
{chr(10).join(binds)}
$cmd{idx}.ExecuteNonQuery() | Out-Null
"""
        )
    script = f"""
$ErrorActionPreference = 'Stop'
$conn = New-Object System.Data.SqlClient.SqlConnection '{CONN_STR}'
$conn.Open()
$tx = $conn.BeginTransaction()
try {{
{"".join(parts)}
  $tx.Commit()
  Write-Output '{{"ok":true}}'
}} catch {{
  $tx.Rollback()
  throw
}} finally {{
  $conn.Close()
}}
"""
    _ps_sql(script)


def seed_opname(token: str, pasien_id: str, note: str) -> str:
    code, body = api(
        "POST",
        "admisi-ranap/opname-request",
        token,
        {
            "pasienId": pasien_id,
            "dokterId": "DR00000015",
            "plannedDate": "2026-07-20",
            "clinicalNotes": note,
            "userId": "step6e2e",
        },
    )
    if code >= 300:
        raise RuntimeError(f"create opname failed: {code} {body}")
    return body["data"]["opnameRequestId"]


def seed_reservation(token: str, pasien_id: str) -> str:
    code, body = api(
        "POST",
        "admisi-ranap/reservation",
        token,
        {
            "pasienId": pasien_id,
            "plannedDate": "2026-08-01",
            "kelasId": "K04",
            "bangsalId": "R1",
            "userId": "step6e2e",
        },
    )
    if code >= 300:
        raise RuntimeError(f"create reservation failed: {code} {body}")
    reservation_id = body["data"]["reservationId"]
    code2, body2 = api(
        "PUT",
        f"admisi-ranap/reservation/{reservation_id}",
        token,
        {
            "plannedDate": "2026-08-01",
            "kelasId": "K04",
            "bangsalId": "R1",
            "userId": "step6e2e",
        },
    )
    if code2 >= 300:
        raise RuntimeError(f"maintain reservation failed: {code2} {body2}")
    return reservation_id


def evidence(reg_id: str) -> dict[str, Any]:
    out: dict[str, Any] = {"regId": reg_id}
    queries = {
        "admission": (
            "SELECT RegId, AdmissionStatus, AdmissionSource, PasienId FROM BILRG_AdmAdmission WHERE RegId=@regId",
            {"regId": reg_id},
        ),
        "jaminan": (
            "SELECT fs_kd_reg, fs_kd_polis FROM ta_reg_jaminan WHERE fs_kd_reg=@regId",
            {"regId": reg_id},
        ),
        "registrasi": (
            "SELECT fs_kd_reg, fs_kd_jenis_reg, fs_kd_medis, fs_kd_layanan, fs_kd_tipe_jaminan FROM ta_registrasi WHERE fs_kd_reg=@regId",
            {"regId": reg_id},
        ),
        "reg_inap": (
            "SELECT fs_kd_reg, fs_kd_caramasuk_inap, fs_kd_trs_booking_bed, fs_kd_medis_sekunder FROM ta_reg_inap WHERE fs_kd_reg=@regId",
            {"regId": reg_id},
        ),
        "history_dokter": (
            "SELECT fs_kd_reg, fs_kd_dokter, fb_primer FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId",
            {"regId": reg_id},
        ),
        "reg_aktif": (
            "SELECT RegId, PasienId FROM BILRG_RegAktif WHERE RegId=@regId",
            {"regId": reg_id},
        ),
        "audit_admission": (
            "SELECT EntityId, ActionType, EntityName FROM BILRG_AuditLog WHERE EntityId=@regId AND EntityName='AdmissionModel'",
            {"regId": reg_id},
        ),
        "waiting_list": (
            "SELECT WaitingListId, RegId, WaitingListStatus FROM BILRG_BedWaitingList WHERE RegId=@regId",
            {"regId": reg_id},
        ),
    }
    for key, (sql, params) in queries.items():
        out[key] = sql_query(sql, params)
    return out


def source_evidence(opname_id: str | None = None, reservation_id: str | None = None) -> dict[str, Any]:
    out: dict[str, Any] = {}
    if opname_id:
        out["opname"] = sql_query(
            "SELECT OpnameRequestId, OpnameRequestStatus, FulfilledRegId, PasienId FROM BILRG_AdmOpnameRequest WHERE OpnameRequestId=@id",
            {"id": opname_id},
        )
        out["opname_audit"] = sql_query(
            "SELECT EntityId, ActionType, EntityName FROM BILRG_AuditLog WHERE EntityId=@id",
            {"id": opname_id},
        )
    if reservation_id:
        out["reservation"] = sql_query(
            "SELECT ReservationId, ReservationStatus, RealizedRegId, PasienId FROM BILRG_AdmReservation WHERE ReservationId=@id",
            {"id": reservation_id},
        )
        out["reservation_audit"] = sql_query(
            "SELECT EntityId, ActionType, EntityName FROM BILRG_AuditLog WHERE EntityId=@id",
            {"id": reservation_id},
        )
    return out


def cleanup(reg_id: str | None = None, opname_id: str | None = None, reservation_id: str | None = None) -> None:
    if reg_id == "RGA4LISM7N":
        raise RuntimeError("Refusing to modify retained smoke record RGA4LISM7N")
    stmts: list[tuple[str, dict[str, Any]]] = []
    if reg_id:
        for sql in [
            "DELETE FROM ta_registrasi2 WHERE fs_kd_reg=@regId",
            "DELETE FROM ta_reg_history_dokter WHERE fs_kd_reg=@regId",
            "DELETE FROM ta_reg_inap WHERE fs_kd_reg=@regId",
            "DELETE FROM ta_reg_jaminan WHERE fs_kd_reg=@regId",
            "DELETE FROM BILRG_RegAktif WHERE RegId=@regId",
            "DELETE FROM BILRG_BedWaitingList WHERE RegId=@regId",
            "DELETE FROM BILRG_AdmAdmission WHERE RegId=@regId",
            "DELETE FROM ta_registrasi WHERE fs_kd_reg=@regId",
            "DELETE FROM BILRG_AuditLog WHERE EntityId=@regId",
        ]:
            stmts.append((sql, {"regId": reg_id}))
    if opname_id:
        stmts.append(("DELETE FROM BILRG_AuditLog WHERE EntityId=@id", {"id": opname_id}))
        stmts.append(
            ("DELETE FROM BILRG_AdmOpnameRequest WHERE OpnameRequestId=@id", {"id": opname_id})
        )
    if reservation_id:
        stmts.append(("DELETE FROM BILRG_AuditLog WHERE EntityId=@id", {"id": reservation_id}))
        stmts.append(
            ("DELETE FROM BILRG_AdmReservation WHERE ReservationId=@id", {"id": reservation_id})
        )
    if stmts:
        sql_exec_many(stmts)


def main() -> int:
    cmd = sys.argv[1] if len(sys.argv) > 1 else "help"
    token = make_jwt()
    (ART / "jwt.txt").write_text(token, encoding="utf-8")

    if cmd == "seed-opname":
        pasien = sys.argv[2]
        note = sys.argv[3] if len(sys.argv) > 3 else f"STEP6-E2E {int(time.time())}"
        oid = seed_opname(token, pasien, note)
        out = {"opnameRequestId": oid, "pasienId": pasien, "note": note}
        (ART / "seed_opname.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
        print(json.dumps(out))
        return 0

    if cmd == "seed-reservation":
        pasien = sys.argv[2]
        rid = seed_reservation(token, pasien)
        out = {"reservationId": rid, "pasienId": pasien}
        (ART / "seed_reservation.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
        print(json.dumps(out))
        return 0

    if cmd == "evidence":
        reg_id = sys.argv[2]
        opname_id = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] != "-" else None
        reservation_id = sys.argv[4] if len(sys.argv) > 4 and sys.argv[4] != "-" else None
        out = evidence(reg_id)
        out["source"] = source_evidence(opname_id, reservation_id)
        (ART / f"evidence_{reg_id}.json").write_text(
            json.dumps(out, indent=2, default=str), encoding="utf-8"
        )
        print(json.dumps(out, default=str))
        return 0

    if cmd == "cleanup":
        reg_id = sys.argv[2] if len(sys.argv) > 2 and sys.argv[2] != "-" else None
        opname_id = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] != "-" else None
        reservation_id = sys.argv[4] if len(sys.argv) > 4 and sys.argv[4] != "-" else None
        cleanup(reg_id, opname_id, reservation_id)
        print(
            json.dumps(
                {"cleaned": {"regId": reg_id, "opnameId": opname_id, "reservationId": reservation_id}}
            )
        )
        return 0

    if cmd == "residue-check":
        pasien = sys.argv[2]
        out = {
            "reg_aktif": sql_query(
                "SELECT RegId, PasienId FROM BILRG_RegAktif WHERE PasienId=@pasienId",
                {"pasienId": pasien},
            ),
            "open_opname": sql_query(
                "SELECT OpnameRequestId, OpnameRequestStatus, ClinicalNotes FROM BILRG_AdmOpnameRequest WHERE PasienId=@pasienId AND OpnameRequestStatus=0",
                {"pasienId": pasien},
            ),
        }
        print(json.dumps(out, default=str))
        return 0

    print("Commands: seed-opname|seed-reservation|evidence|cleanup|residue-check")
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
