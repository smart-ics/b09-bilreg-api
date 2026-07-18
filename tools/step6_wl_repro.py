import json
import sys
import time

sys.path.insert(0, r"D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api\tools")
from step4c_verify import make_jwt, api
from step6_browser_support import cleanup, seed_opname

token = make_jwt()
pasien = "347137300000057"
oid = seed_opname(token, pasien, f"WL-INV2 {int(time.time())}")
body = {
    "opnameRequestId": oid,
    "kelasDkId": "3",
    "bangsalId": "R1",
    "userId": "SPR001",
    "registration": {
        "tipeJaminanId": "00000",
        "caraMasukDkId": "8",
        "prosedurMasukInapId": "IGD",
        "rujukanId": "RJK00291",
        "dokterId": "DR00000015",
        "layananId": "RI001",
        "karcisId": "02",
        "pesertaJaminanId": "",
    },
}
code, resp = api("POST", "admisi-ranap/admission/from-opname-request", token, body)
print("process", code, resp)
reg_id = resp["data"]["regId"]

c, a = api("GET", f"admisi-ranap/admission/{reg_id}", token)
print("GET admission", c, json.dumps(a, ensure_ascii=False)[:600])

c, k = api("GET", "Kelas/K04", token)
print("GET Kelas K04", c, json.dumps(k, ensure_ascii=False)[:400])

for path in ["Bangsal/R1", "bangsal/R1"]:
    c, b = api("GET", path, token)
    print("GET", path, c, str(b)[:200])

for kelas in ["K04", "K09", "K10"]:
    c, w = api(
        "POST",
        "admisi-ranap/waiting-list",
        token,
        {
            "regId": reg_id,
            "kelasId": kelas,
            "bangsalId": "R1",
            "priority": 3,
            "userId": "SPR001",
        },
    )
    print("WL", kelas, c, json.dumps(w, ensure_ascii=False))

# keep for a moment? cleanup
cleanup(reg_id, oid, None)
print("cleaned", reg_id, oid)
