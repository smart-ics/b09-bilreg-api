import sys, json
sys.path.insert(0, r'D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api\tools')
from step6_browser_support import sql_query, cleanup

aktif = sql_query(
    "SELECT RegId, PasienId FROM BILRG_RegAktif WHERE PasienId IN (@a,@b,@c)",
    {'a': '347137300000057', 'b': '347137300000037', 'c': '347137300000014'},
)
print('aktif', json.dumps(aktif, default=str))

opn = sql_query(
    "SELECT TOP 20 OpnameRequestId, OpnameRequestStatus, FulfilledRegId, ClinicalNotes, PasienId FROM BILRG_AdmOpnameRequest WHERE ClinicalNotes LIKE @n OR FulfilledRegId IN (SELECT RegId FROM BILRG_RegAktif WHERE PasienId=@p)",
    {'n': '%STEP6%', 'p': '347137300000057'},
)
print('opn', json.dumps(opn, default=str)[:2000])

# recent admissions for test patients
adm = sql_query(
    "SELECT TOP 10 RegId, PasienId, AdmissionStatus FROM BILRG_AdmAdmission WHERE PasienId IN (@a,@b,@c) ORDER BY RegId DESC",
    {'a': '347137300000057', 'b': '347137300000037', 'c': '347137300000014'},
)
print('adm', json.dumps(adm, default=str))

for row in aktif:
    rid = row['RegId']
    if rid == 'RGA4LISM7N':
        continue
    print('cleaning orphan', rid)
    cleanup(rid, None, None)
