import sys
import json

sys.path.insert(0, r"D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api\tools")
from step6_browser_support import sql_query, cleanup

rows = sql_query(
    "SELECT OpnameRequestId, ClinicalNotes, PasienId FROM BILRG_AdmOpnameRequest WHERE ClinicalNotes LIKE @n",
    {"n": "%STEP6%"},
)
print("opn", json.dumps(rows, default=str))
for r in rows:
    cleanup(None, r["OpnameRequestId"], None)
    print("cleaned", r["OpnameRequestId"])

rows2 = sql_query(
    "SELECT ReservationId, PasienId, ReservationStatus FROM BILRG_AdmReservation WHERE PasienId IN (@a,@b) AND ReservationStatus IN (0,1)",
    {"a": "347137300000037", "b": "347137300000014"},
)
print("rsv", json.dumps(rows2, default=str))
for r in rows2:
    cleanup(None, None, r["ReservationId"])
    print("cleaned", r["ReservationId"])

for p in ["347137300000057", "347137300000037", "347137300000014", "347137300000070"]:
    aktif = sql_query("SELECT RegId FROM BILRG_RegAktif WHERE PasienId=@p", {"p": p})
    print(p, aktif)
