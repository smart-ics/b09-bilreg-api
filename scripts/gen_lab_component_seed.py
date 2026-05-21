"""Generate BILRG_LabComponentMaster.dataseed.sql from LAB_COMPONENT_OPERATIONAL_CATALOG.md."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "docs/contexts/lab/LAB_COMPONENT_OPERATIONAL_CATALOG.md"
OUTPUT = ROOT / "Bilreg.SqlDb/LabContext/LabComponentMasterFeature/Data/BILRG_LabComponentMaster.dataseed.sql"

text = CATALOG.read_text(encoding="utf-8")
section2_end = text.find("## 3. Alias")
section2 = text[:section2_end] if section2_end != -1 else text

rows: list[tuple] = []
for line in section2.splitlines():
    line = line.strip()
    if not line.startswith("| HIGH |") and not line.startswith("| MEDIUM |") and not line.startswith("| LOW |"):
        continue
    parts = [p.strip() for p in line.split("|")]
    # parts[0]='', parts[1]=confidence, ... parts[9]=notes
    if len(parts) < 10 or parts[2][:3] != "MLC":
        continue
    confidence = parts[1]
    component_id = parts[2]
    code = parts[3]
    name = parts[4]
    name_id = parts[5]
    result_type = parts[6]
    unit = parts[7]
    loinc = parts[8]
    rows.append((confidence, component_id, code, name, name_id, result_type, unit, loinc))

# MLC000C REMARK is documented in catalog §1 but omitted from §2 tables; preserve from prior seed.
REMARK_ROW = (
    "HIGH",
    "MLC000C",
    "REMARK",
    "Remark",
    "Catatan",
    "4",
    "",
    "",
)
if not any(r[1] == "MLC000C" for r in rows):
    rows.append(REMARK_ROW)

# Catalog §2 has 168 rows; MLC000C REMARK is §1/§5 only → 169 seed rows total.
if len(rows) != 169:
    raise SystemExit(f"Expected 169 rows, parsed {len(rows)}")


def sql_str(value: str) -> str:
    if not value:
        return "NULL"
    return "'" + value.replace("'", "''") + "'"


def sort_key(line: str) -> int:
    match = re.search(r"'(MLC[0-9A-F]{4})'", line)
    return int(match.group(1)[3:], 16)


lines: list[str] = []
for _conf, component_id, code, name, name_id, result_type, unit, loinc in rows:
    loinc_sql = sql_str(loinc) if loinc else "NULL"
    unit_sql = sql_str(unit) if unit else "''"
    is_active = "0" if component_id == "MLC000C" else "1"
    lines.append(
        f"    ({sql_str(component_id)}, {loinc_sql}, {sql_str(code)}, {sql_str(name)}, "
        f"{sql_str(name_id)}, {result_type}, {unit_sql}, 1, {is_active}, "
        f"'SEED', '3000-01-01', 'SEED', '3000-01-01', '', '3000-01-01')"
    )

lines.sort(key=sort_key)

header = """DELETE FROM BILRG_LabComponentMaster;
GO

INSERT INTO BILRG_LabComponentMaster (
    ComponentId, LoincCode, ComponentCode, ComponentName, ComponentNameIndonesia, ResultType, DefaultUnit,
    IsSystem, IsActive, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
VALUES
"""
footer = ";\nGO\n"

OUTPUT.write_text(header + ",\n".join(lines) + footer, encoding="utf-8")
print(f"Wrote {len(lines)} rows to {OUTPUT}")
