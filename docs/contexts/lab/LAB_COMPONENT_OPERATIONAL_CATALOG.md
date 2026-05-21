# LAB_COMPONENT_OPERATIONAL_CATALOG.md

> **Status:** Human curation candidate — not loaded into database until seed generation pass.  
> **Scope:** Clinical Pathology (PK) operational components for Indonesian RS LIS seeding.  
> **Target:** `LabComponentMaster` — see [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md).

**ResultType:** `1` = Numeric, `2` = Text, `3` = Option, `4` = Narrative

---

## 1. Component Summary

| Category | SEED_EXISTS | NEW | Total |
|----------|-------------|-----|-------|
| Hematology | 4 | 26 | 30 |
| Chemistry | 5 | 38 | 43 |
| Lipid | 1 | 7 | 8 |
| Electrolyte | 0 | 11 | 11 |
| Coagulation | 0 | 10 | 10 |
| Serology | 0 | 37 | 37 |
| Urinalysis | 1 | 16 | 17 |
| Microbiology | 0 | 12 | 12 |
| **Total** | **12** | **156** | **168** |

| Confidence | Count |
|------------|-------|
| HIGH | 97 |
| MEDIUM | 60 |
| LOW | 11 |

| Row status | Count |
|------------|-------|
| SEED_EXISTS | 12 |
| NEW | 156 |

**ProposedComponentId range (NEW):** `MLC000D` … `MLC00A9` (sequential hex)

---

## 2. Canonical Component Tables

### Hematology

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC0001 | HB | Hemoglobin | Hemoglobin | 1 | g/dL | 718-7 | SEED_EXISTS |
| HIGH | MLC0002 | WBC | Leukocyte | Leukosit | 1 | 10^3/uL | 6690-2 | SEED_EXISTS |
| HIGH | MLC0003 | RBC | Erythrocyte | Eritrosit | 1 | 10^6/uL | 789-8 | SEED_EXISTS |
| HIGH | MLC0004 | PLT | Platelet | Trombosit | 1 | 10^3/uL | 777-3 | SEED_EXISTS |
| HIGH | MLC000D | HCT | Hematocrit | Hematokrit | 1 | % | 4544-3 | |
| HIGH | MLC000E | MCV | Mean Corpuscular Volume | Volume Eritrosit Tengah | 1 | fL | 787-2 | |
| HIGH | MLC000F | MCH | Mean Corpuscular Hemoglobin | Hemoglobin Eritrosit Tengah | 1 | pg | 785-6 | |
| HIGH | MLC0010 | MCHC | Mean Corpuscular Hemoglobin Concentration | Konsentrasi Hemoglobin Eritrosit Tengah | 1 | g/dL | 786-4 | |
| HIGH | MLC0011 | RDW | Red Cell Distribution Width | Lebar Distribusi Eritrosit | 1 | % | 788-0 | |
| HIGH | MLC0012 | NEUT_PCT | Neutrophil Percent | Neutrofil % | 1 | % | 770-8 | |
| HIGH | MLC0013 | LYMPH_PCT | Lymphocyte Percent | Limfosit % | 1 | % | 736-9 | |
| HIGH | MLC0014 | MONO_PCT | Monocyte Percent | Monosit % | 1 | % | 5905-5 | |
| HIGH | MLC0015 | EO_PCT | Eosinophil Percent | Eosinofil % | 1 | % | 713-8 | |
| HIGH | MLC0016 | BASO_PCT | Basophil Percent | Basofil % | 1 | % | 706-2 | |
| HIGH | MLC0017 | NEUT_ABS | Neutrophil Absolute | Neutrofil Absolut | 1 | 10^3/uL | 751-8 | |
| HIGH | MLC0018 | LYMPH_ABS | Lymphocyte Absolute | Limfosit Absolut | 1 | 10^3/uL | 731-0 | |
| HIGH | MLC0019 | MONO_ABS | Monocyte Absolute | Monosit Absolut | 1 | 10^3/uL | 742-7 | |
| HIGH | MLC001A | EO_ABS | Eosinophil Absolute | Eosinofil Absolut | 1 | 10^3/uL | 711-2 | |
| HIGH | MLC001B | BASO_ABS | Basophil Absolute | Basofil Absolut | 1 | 10^3/uL | 704-7 | |
| HIGH | MLC001C | ESR | Erythrocyte Sedimentation Rate | Laju Endap Darah | 1 | mm/hr | 4537-7 | |
| MEDIUM | MLC001D | RETIC | Reticulocyte Count | Retikulosit | 1 | % | 4679-7 | Unit may be % or 10^9/L |
| HIGH | MLC001E | MPV | Mean Platelet Volume | Volume Trombosit Tengah | 1 | fL | 32623-1 | |
| MEDIUM | MLC001F | PDW | Platelet Distribution Width | Lebar Distribusi Trombosit | 1 | fL | 32207-3 | |
| MEDIUM | MLC0020 | PCT | Plateletcrit | Plateletcrit | 1 | % | 51637-7 | |
| LOW | MLC0021 | NRBC | Nucleated Red Blood Cell | Eritrosit Bermi Inti | 1 | /100 WBC | 773-2 | Hospital-specific reporting |
| HIGH | MLC0022 | ABO | ABO Blood Group | Golongan Darah ABO | 3 | | 882-1 | OPTION: A, B, AB, O |
| HIGH | MLC0023 | RH | Rh Blood Group | Rh | 3 | | 10331-7 | OPTION: Positif, Negatif |
| MEDIUM | MLC0024 | IG_PCT | Immature Granulocyte Percent | Granulosit Immatur % | 1 | % | 38518-7 | Not all analyzers report |
| MEDIUM | MLC0025 | BAND_PCT | Band Neutrophil Percent | Stab % | 1 | % | | Manual diff / legacy naming |
| MEDIUM | MLC0026 | HCT_CORR | Corrected Hematocrit | Hematokrit Terkoreksi | 1 | % | | Calculated; LOW usage at some RS |

### Chemistry

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC0005 | GLU | Glucose | Glukosa Darah | 1 | mg/dL | 2345-7 | SEED_EXISTS |
| HIGH | MLC0006 | UREA | Urea | Urea | 1 | mg/dL | 3094-0 | SEED_EXISTS |
| HIGH | MLC0007 | CREA | Creatinine | Kreatinin | 1 | mg/dL | 2160-0 | SEED_EXISTS |
| HIGH | MLC0008 | ALT | Alanine Aminotransferase | Alanin Aminotransferase | 1 | U/L | 1742-6 | SEED_EXISTS; alias SGPT |
| HIGH | MLC0009 | AST | Aspartate Aminotransferase | Aspartat Aminotransferase | 1 | U/L | 1920-8 | SEED_EXISTS; alias SGOT |
| HIGH | MLC0027 | BILI_T | Bilirubin Total | Bilirubin Total | 1 | mg/dL | 1975-2 | |
| HIGH | MLC0028 | BILI_D | Bilirubin Direct | Bilirubin Direk | 1 | mg/dL | 1968-7 | |
| MEDIUM | MLC0029 | BILI_I | Bilirubin Indirect | Bilirubin Indirek | 1 | mg/dL | | Often calculated |
| HIGH | MLC002A | ALP | Alkaline Phosphatase | Alkaline Fosfatase | 1 | U/L | 6768-6 | |
| HIGH | MLC002B | GGT | Gamma Glutamyl Transferase | Gamma GT | 1 | U/L | 2324-2 | |
| HIGH | MLC002C | LDH | Lactate Dehydrogenase | Laktat Dehidrogenase | 1 | U/L | 2532-7 | |
| HIGH | MLC002D | ALB | Albumin | Albumin | 1 | g/dL | 1751-7 | |
| HIGH | MLC002E | TP | Total Protein | Protein Total | 1 | g/dL | 2885-2 | |
| MEDIUM | MLC002F | GLOB | Globulin | Globulin | 1 | g/dL | | Often calculated |
| MEDIUM | MLC0030 | AG_RATIO | Albumin Globulin Ratio | Rasio Albumin Globulin | 1 | ratio | | Calculated |
| HIGH | MLC0031 | AMYL | Amylase | Amilase | 1 | U/L | 1798-8 | |
| HIGH | MLC0032 | LIPASE | Lipase | Lipase | 1 | U/L | 3040-7 | |
| HIGH | MLC0033 | UA | Uric Acid | Asam Urat | 1 | mg/dL | 3084-1 | |
| HIGH | MLC0034 | CA | Calcium | Kalsium | 1 | mg/dL | 17861-6 | |
| HIGH | MLC0035 | PHOS | Phosphorus | Fosfor Anorganik | 1 | mg/dL | 2777-1 | |
| HIGH | MLC0036 | MG | Magnesium | Magnesium | 1 | mg/dL | 19123-9 | |
| HIGH | MLC0037 | HBA1C | Hemoglobin A1c | HbA1c | 1 | % | 4548-4 | NGSP % default |
| HIGH | MLC0038 | CRP | C-Reactive Protein | Protein C Reaktif | 1 | mg/L | 1988-5 | Quantitative |
| MEDIUM | MLC0039 | HSCRP | High Sensitivity CRP | hs-CRP | 1 | mg/L | 30522-7 | |
| HIGH | MLC003A | FERR | Ferritin | Ferritin | 1 | ng/mL | 2276-4 | |
| HIGH | MLC003B | IRON | Iron | Besi Serum | 1 | ug/dL | 2498-4 | |
| MEDIUM | MLC003C | TIBC | Total Iron Binding Capacity | TIBC | 1 | ug/dL | 2500-7 | |
| MEDIUM | MLC003D | TRANSFERRIN | Transferrin | Transferin | 1 | mg/dL | 3034-0 | |
| HIGH | MLC003E | TSH | Thyroid Stimulating Hormone | TSH | 1 | mIU/L | 3016-3 | |
| HIGH | MLC003F | FT4 | Free Thyroxine | FT4 | 1 | ng/dL | 3024-7 | |
| HIGH | MLC0040 | FT3 | Free Triiodothyronine | FT3 | 1 | pg/mL | 3051-0 | |
| MEDIUM | MLC0041 | BNP | B-Type Natriuretic Peptide | BNP | 1 | pg/mL | 30934-4 | |
| MEDIUM | MLC0042 | NT_PROBNP | NT-proBNP | NT-proBNP | 1 | pg/mL | 33762-6 | Separate order at many RS |
| HIGH | MLC0043 | CK | Creatine Kinase | Kreatin Kinase | 1 | U/L | 2157-6 | |
| HIGH | MLC0044 | CKMB | Creatine Kinase MB | CK-MB | 1 | ng/mL | 13969-1 | Unit varies U/L vs ng/mL |
| HIGH | MLC0045 | TROP_I | Troponin I | Troponin I | 1 | ng/mL | 10839-9 | |
| MEDIUM | MLC0046 | TROP_T | Troponin T | Troponin T | 1 | ng/mL | 6598-7 | |
| HIGH | MLC0047 | EGFR | Estimated Glomerular Filtration Rate | eGFR | 1 | mL/min/1.73m2 | 62238-1 | Calculated from CREA |
| MEDIUM | MLC0048 | CREAT_CLR | Creatinine Clearance | Klearance Kreatinin | 1 | mL/min | | 24h urine or calculated |
| MEDIUM | MLC0049 | PROCALC | Procalcitonin | Prokalsitonin | 1 | ng/mL | 33959-8 | |
| MEDIUM | MLC004A | BHCG | Beta Human Chorionic Gonadotropin | Beta HCG | 1 | mIU/mL | 21198-7 | |
| LOW | MLC004B | NH3 | Ammonia | Amonia | 1 | umol/L | 22767-0 | Specialty / referral RS |
| MEDIUM | MLC004C | INSULIN | Insulin | Insulin | 1 | uU/mL | 20448-7 | Fasting context hospital-specific |

### Lipid

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC000A | CHOL | Total Cholesterol | Kolesterol Total | 1 | mg/dL | 2093-3 | SEED_EXISTS |
| HIGH | MLC004D | HDL | HDL Cholesterol | Kolesterol HDL | 1 | mg/dL | 2085-9 | |
| HIGH | MLC004E | LDL | LDL Cholesterol | Kolesterol LDL | 1 | mg/dL | 13457-7 | Direct or calculated |
| HIGH | MLC004F | TRIG | Triglyceride | Trigliserida | 1 | mg/dL | 2571-8 | |
| MEDIUM | MLC0050 | NHDL | Non-HDL Cholesterol | Kolesterol Non-HDL | 1 | mg/dL | 43396-1 | Calculated |
| MEDIUM | MLC0051 | APO_A1 | Apolipoprotein A1 | Apolipoprotein A1 | 1 | mg/dL | 1869-7 | Not all RS routine |
| MEDIUM | MLC0052 | APO_B | Apolipoprotein B | Apolipoprotein B | 1 | mg/dL | 1884-6 | |
| MEDIUM | MLC0053 | LDL_HDL_R | LDL HDL Ratio | Rasio LDL/HDL | 1 | ratio | | Calculated |

### Electrolyte

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC0054 | NA | Sodium | Natrium | 1 | mmol/L | 2951-2 | |
| HIGH | MLC0055 | K | Potassium | Kalium | 1 | mmol/L | 2823-3 | |
| HIGH | MLC0056 | CL | Chloride | Klorida | 1 | mmol/L | 2075-0 | |
| HIGH | MLC0057 | HCO3 | Bicarbonate | Bikarbonat | 1 | mmol/L | 1963-8 | CO2 total at some RS |
| MEDIUM | MLC0058 | ANGAP | Anion Gap | Anion Gap | 1 | mmol/L | 33037-3 | Calculated |
| LOW | MLC0059 | OSM | Osmolality Serum | Osmolalitas Serum | 1 | mOsm/kg | 2692-2 | Less common routine |
| MEDIUM | MLC005A | LAC | Lactate | Laktat | 1 | mmol/L | 2524-4 | Critical care / ER |
| MEDIUM | MLC005B | CA_ION | Ionized Calcium | Kalsium Terionisasi | 1 | mmol/L | 1994-3 | Blood gas / specialty |
| MEDIUM | MLC005C | ANION_GAP_K | Anion Gap with Potassium | Anion Gap K | 1 | mmol/L | | Variant calculation |
| LOW | MLC005D | PH_BLD | Blood pH | pH Darah | 1 | | 2744-1 | ABG context; overlaps blood gas |
| HIGH | MLC005E | CO2 | Carbon Dioxide Total | CO2 Total | 1 | mmol/L | 2028-9 | Alias for HCO3 at some RS |

### Coagulation

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC005F | PT | Prothrombin Time | Waktu Protrombin | 1 | sec | 5902-2 | |
| HIGH | MLC0060 | INR | International Normalized Ratio | INR | 1 | ratio | 6301-6 | |
| HIGH | MLC0061 | APTT | Activated Partial Thromboplastin Time | APTT | 1 | sec | 14979-9 | |
| HIGH | MLC0062 | FIB | Fibrinogen | Fibrinogen | 1 | mg/dL | 3255-7 | |
| HIGH | MLC0063 | DDIMER | D-Dimer | D-Dimer | 1 | ng/mL | 48065-7 | Unit FEU vs DDU varies |
| MEDIUM | MLC0064 | TT | Thrombin Time | Waktu Trombin | 1 | sec | 3243-3 | |
| MEDIUM | MLC0065 | FDP | Fibrin Degradation Products | FDP | 1 | ug/mL | | |
| MEDIUM | MLC0066 | PT_PC | Prothrombin Activity Percent | Aktivitas Protrombin % | 1 | % | | Common on Indonesian coag reports |
| LOW | MLC0067 | APCR | Activated Protein C Resistance | Resistensi Protein C Aktif | 3 | | | Specialty coag |
| MEDIUM | MLC0068 | FIB_CLAU | Fibrinogen Clauss | Fibrinogen Clauss | 1 | mg/dL | | Method-specific duplicate risk vs FIB |

### Serology

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC0069 | HBSAG | Hepatitis B Surface Antigen | HBsAg | 3 | | 5196-1 | Reaktif / Non Reaktif |
| HIGH | MLC006A | ANTI_HBS | Hepatitis B Surface Antibody | Anti HBs | 3 | | 16935-9 | |
| HIGH | MLC006B | ANTI_HBC | Hepatitis B Core Antibody | Anti HBc | 3 | | 16933-4 | Total vs IgM split below |
| MEDIUM | MLC006C | ANTI_HBC_IGM | Hepatitis B Core Antibody IgM | Anti HBc IgM | 3 | | 24113-3 | |
| MEDIUM | MLC006D | HBEAG | Hepatitis B e Antigen | HBeAg | 3 | | 5195-3 | |
| MEDIUM | MLC006E | ANTI_HBE | Hepatitis B e Antibody | Anti HBe | 3 | | 16934-2 | |
| HIGH | MLC006F | ANTI_HCV | Hepatitis C Antibody | Anti HCV | 3 | | 16128-1 | |
| HIGH | MLC0070 | ANTI_HIV | HIV Antibody | Anti HIV | 3 | | 68961-2 | Screening |
| MEDIUM | MLC0071 | HIV_COMBO | HIV Antigen Antibody | HIV Ag/Ab | 3 | | 56888-1 | 4th gen |
| HIGH | MLC0072 | RPR | Rapid Plasma Reagin | RPR Sifilis | 3 | | 5292-8 | |
| HIGH | MLC0073 | VDRL | VDRL | VDRL | 3 | | 5291-0 | |
| MEDIUM | MLC0074 | TPHA | Treponema Pallidum Hemagglutination | TPHA | 3 | | 5393-4 | |
| HIGH | MLC0075 | DENGUE_NS1 | Dengue NS1 Antigen | Dengue NS1 | 3 | | 91064-6 | |
| HIGH | MLC0076 | DENGUE_IGM | Dengue IgM | Dengue IgM | 3 | | 23958-2 | |
| HIGH | MLC0077 | DENGUE_IGG | Dengue IgG | Dengue IgG | 3 | | 23958-2 | |
| MEDIUM | MLC0078 | TYPHIDOT_IGM | Typhoid IgM Rapid | Typhidot IgM | 3 | | | Brand-specific |
| MEDIUM | MLC0079 | TYPHIDOT_IGG | Typhoid IgG Rapid | Typhidot IgG | 3 | | | |
| LOW | MLC007A | WIDAL_O | Widal O Agglutinin | Widal O | 3 | | | Legacy; declining use |
| LOW | MLC007B | WIDAL_H | Widal H Agglutinin | Widal H | 3 | | | Legacy |
| HIGH | MLC007C | RF | Rheumatoid Factor | Faktor Rheumatoid | 3 | | 11572-5 | Qualitative common |
| HIGH | MLC007D | ASO | Antistreptolysin O | ASO | 3 | | 5370-2 | |
| MEDIUM | MLC007E | IGE_TOTAL | Total IgE | IgE Total | 1 | IU/mL | 19113-0 | |
| MEDIUM | MLC007F | IGG | Immunoglobulin G | IgG | 1 | mg/dL | 2465-3 | |
| MEDIUM | MLC0080 | IGM | Immunoglobulin M | IgM | 1 | mg/dL | 2472-9 | |
| MEDIUM | MLC0081 | IGA | Immunoglobulin A | IgA | 1 | mg/dL | 2458-8 | |
| LOW | MLC0082 | ANA_SCR | Antinuclear Antibody Screen | ANA Skrining | 3 | | 5047-6 | Referral / immunology |
| MEDIUM | MLC0083 | HPYLORI_IGG | Helicobacter pylori IgG | Anti H. pylori IgG | 3 | | 5176-3 | |
| MEDIUM | MLC0084 | TOXO_IGM | Toxoplasma IgM | Toxoplasma IgM | 3 | | 22496-4 | TORCH panels |
| MEDIUM | MLC0085 | RUBELLA_IGG | Rubella IgG | Rubella IgG | 3 | | 5334-8 | |
| MEDIUM | MLC0086 | CMV_IGM | Cytomegalovirus IgM | CMV IgM | 3 | | 22244-8 | |
| MEDIUM | MLC0087 | MALARIA_RDT | Malaria Rapid Test | Malaria RDT | 3 | | | Species line optional |
| HIGH | MLC0088 | CRP_QUAL | C-Reactive Protein Qualitative | CRP Kualitatif | 3 | | | Separate from quant CRP |
| HIGH | MLC0089 | COVID_AG | SARS-CoV-2 Antigen Rapid | Antigen COVID-19 | 3 | | 94500-6 | Operational RS item |
| MEDIUM | MLC008A | COVID_IGG | SARS-CoV-2 IgG Antibody | IgG COVID-19 | 3 | | | |
| MEDIUM | MLC008B | COVID_IGM | SARS-CoV-2 IgM Antibody | IgM COVID-19 | 3 | | | |
| HIGH | MLC008C | HAV_IGM | Hepatitis A IgM | Anti HAV IgM | 3 | | 32018-4 | |
| MEDIUM | MLC008D | HAV_TOTAL | Hepatitis A Antibody Total | Anti HAV Total | 3 | | | |

### Urinalysis

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC000B | URINE_PH | Urine pH | pH Urin | 1 | | | SEED_EXISTS |
| HIGH | MLC008E | URINE_PROT | Urine Protein | Protein Urin | 3 | | 2888-6 | Dipstick semiquant |
| HIGH | MLC008F | URINE_GLU | Urine Glucose | Glukosa Urin | 3 | | 25428-4 | |
| HIGH | MLC0090 | URINE_KET | Urine Ketone | Keton Urin | 3 | | 2514-3 | |
| HIGH | MLC0091 | URINE_BLD | Urine Blood | Darah Urin | 3 | | 5794-3 | |
| HIGH | MLC0092 | URINE_NIT | Urine Nitrite | Nitrit Urin | 3 | | 5802-4 | |
| HIGH | MLC0093 | URINE_LEU | Urine Leukocyte Esterase | Esterase Leukosit Urin | 3 | | 5799-2 | |
| HIGH | MLC0094 | URINE_SG | Urine Specific Gravity | Berat Jenis Urin | 1 | | 2965-2 | |
| MEDIUM | MLC0095 | URINE_UBG | Urine Urobilinogen | Urobilinogen Urin | 3 | | 32727-9 | |
| MEDIUM | MLC0096 | URINE_BILI | Urine Bilirubin | Bilirubin Urin | 3 | | 1977-8 | |
| HIGH | MLC0097 | URINE_CAST | Urine Casts | Silinder Urin | 2 | | 87827-2 | Microscopy text |
| MEDIUM | MLC0098 | URINE_CRYS | Urine Crystals | Kristal Urin | 2 | | | Microscopy |
| HIGH | MLC0099 | URINE_RBC | Urine Red Blood Cell | Eritrosit Urin | 2 | | 13945-1 | /HPF common |
| HIGH | MLC009A | URINE_WBC | Urine White Blood Cell | Leukosit Urin | 2 | | 13946-9 | /HPF |
| MEDIUM | MLC009B | URINE_EPI | Urine Epithelial Cell | Sel Epitel Urin | 2 | | | |
| MEDIUM | MLC009C | URINE_BACT | Urine Bacteria | Bakteri Urin | 2 | | | |
| MEDIUM | MLC009D | URINE_COLOR | Urine Color | Warna Urin | 2 | | | |

### Microbiology

| Confidence | ProposedComponentId | ComponentCode | ComponentName | ComponentNameIndonesia | SuggestedResultType | SuggestedDefaultUnit | OptionalLoincCode | Notes |
|------------|---------------------|---------------|---------------|------------------------|---------------------|----------------------|-------------------|-------|
| HIGH | MLC009E | BLOOD_CULT_GR | Blood Culture Growth | Hasil Kultur Darah | 2 | | | Growth summary TEXT |
| HIGH | MLC009F | URINE_CULT_GR | Urine Culture Growth | Hasil Kultur Urin | 2 | | | |
| HIGH | MLC00A0 | STOOL_CULT_GR | Stool Culture Growth | Hasil Kultur Feses | 2 | | | |
| MEDIUM | MLC00A1 | SPUTUM_CULT_GR | Sputum Culture Growth | Hasil Kultur Sputum | 2 | | | |
| HIGH | MLC00A2 | GRAM_STN | Gram Stain Result | Pewarnaan Gram | 2 | | | |
| MEDIUM | MLC00A3 | AFB_STN | Acid Fast Bacilli Stain | BTA | 2 | | | |
| LOW | MLC00A4 | KOH_PREP | KOH Preparation | Preparat KOH | 2 | | | Fungal |
| LOW | MLC00A5 | INK_STN | India Ink Stain | Tinta India | 2 | | | Cryptococcus |
| LOW | MLC00A6 | STOOL_PARA | Stool Parasite Exam | Parasit Feses | 2 | | | |
| MEDIUM | MLC00A7 | CANDIDA_GRAM | Candida on Gram Stain | Candida Gram | 2 | | | |
| HIGH | MLC00A8 | CULT_SENS | Culture Sensitivity Summary | Kepekaan Antibiotik | 2 | | | Narrative summary not full MIC grid |
| MEDIUM | MLC00A9 | URINE_CULT_COL | Urine Culture Colony Count | Hitung Koloni Urin | 2 | | | CFU/mL in text |

---

## 3. Alias / Synonym Candidates

| ComponentCode | Alias | SourceHint | Confidence |
|---------------|-------|------------|------------|
| HB | HGB | Analyzer / RS label | HIGH |
| HB | Hemoglobin | Report header | HIGH |
| HB | Haemoglobin | Legacy spelling | MEDIUM |
| WBC | LEU | Hematology counter | MEDIUM |
| WBC | Leukosit | Indonesian label | HIGH |
| WBC | White Blood Cell | English report | HIGH |
| RBC | Eritrosit | Indonesian label | HIGH |
| RBC | Red Blood Cell | English report | HIGH |
| PLT | Trombosit | Indonesian label | HIGH |
| PLT | Platelet Count | English report | HIGH |
| GLU | GDS | Indonesian RS | HIGH |
| GLU | Gula Darah | Indonesian label | HIGH |
| GLU | Blood Sugar | English legacy | MEDIUM |
| GLU | GDP | Puasa | MEDIUM |
| GLU | GDPP | Post prandial | MEDIUM |
| UREA | UREUM | Indonesian spelling | HIGH |
| UREA | BUN | US nomenclature | HIGH |
| UREA | Ureum Darah | RS label | HIGH |
| CREA | Kreatinin | Indonesian label | HIGH |
| CREA | Creatinin | Typo variant | MEDIUM |
| ALT | SGPT | Indonesian RS standard | HIGH |
| ALT | GPT | Legacy | MEDIUM |
| AST | SGOT | Indonesian RS standard | HIGH |
| AST | GOT | Legacy | MEDIUM |
| CHOL | Kolesterol | Indonesian | HIGH |
| CHOL | Kolesterol Total | Panel label | HIGH |
| HDL | Kolesterol HDL | Indonesian | HIGH |
| LDL | Kolesterol LDL | Indonesian | HIGH |
| TRIG | Trigliserida | Indonesian | HIGH |
| HCT | Hematokrit | Indonesian | HIGH |
| HCT | Ht | Short form | MEDIUM |
| ESR | LED | Indonesian | HIGH |
| ESR | ESR 1h / 2h | Method variants | MEDIUM |
| NA | Natrium | Indonesian | HIGH |
| K | Kalium | Indonesian | HIGH |
| CL | Cl | Short form | MEDIUM |
| HCO3 | CO2 | Combined reporting | MEDIUM |
| BILI_T | BT | Indonesian short | HIGH |
| BILI_D | BD | Indonesian short | HIGH |
| UA | Asam Urat | Indonesian | HIGH |
| HBA1C | HbA1c | Case variants | HIGH |
| PT | Prothrombin Time | English | HIGH |
| APTT | PTT | US naming | MEDIUM |
| APTT | APPT | Typo | LOW |
| HBSAG | HBs Ag | Spacing variant | HIGH |
| ANTI_HCV | Anti HCV | Spacing | HIGH |
| ANTI_HIV | Anti HIV | Spacing | HIGH |
| DENGUE_NS1 | NS1 Ag Dengue | RS label | HIGH |
| URINE_PROT | PROT | Dipstick strip | MEDIUM |
| URINE_GLU | GLU Urine | Strip label | MEDIUM |
| URINE_BLD | BLOOD | Strip label | MEDIUM |
| URINE_LEU | LEU | Strip label | MEDIUM |
| URINE_SG | SG | Short | MEDIUM |
| GRAM_STN | Gram | Short | HIGH |
| AFB_STN | BTA | Indonesian | HIGH |
| EGFR | LFG | French/Indonesian calc label | MEDIUM |
| CRP | CRP Kuantitatif | vs qualitative split | MEDIUM |
| REMARK | Catatan | Indonesian | HIGH |
| REMARK | Comment | English | MEDIUM |
| REMARK | Notes | LIS field | MEDIUM |

---

## 4. Uncertain Components

| ProposedComponentId | ComponentCode | Issue | Suggested action |
|---------------------|---------------|-------|-------------------|
| MLC0021 | NRBC | Reporting unit and clinical use vary | Confirm include; set unit per hospital policy |
| MLC0026 | HCT_CORR | Rare vs calculated duplicate of HCT | Defer or merge with HCT |
| MLC0029 | BILI_I | Often calculated not measured | Keep if RS reports; else alias to calculated |
| MLC004B | NH3 | Not routine at all RS | Mark inactive at small RS |
| MLC0048 | CREAT_CLR | 24h urine vs calculated | Human pick default unit/context |
| MLC0059 | OSM | Low routine volume | Optional seed row |
| MLC005D | PH_BLD | Overlaps ABG module | Exclude from PK seed if ABG separate |
| MLC0067 | APCR | Specialty | Defer or inactive default |
| MLC0068 | FIB_CLAU | Duplicate risk with FIB | Pick one fibrinogen method per vendor |
| MLC007A | WIDAL_O | Legacy test | Inactive default recommended |
| MLC007B | WIDAL_H | Legacy test | Inactive default recommended |
| MLC0082 | ANA_SCR | Immunology referral | Inactive at basic PK sites |
| MLC00A4 | KOH_PREP | Low volume fungal | Optional |
| MLC00A5 | INK_STN | Low volume | Optional |
| MLC00A6 | STOOL_PARA | Parasitology workflow split | Confirm PK vs mikro sublab |
| MLC0039 | HSCRP | vs CRP overlap | Document panel rules |
| MLC0042 | NT_PROBNP | vs BNP | Do not merge |
| MLC0046 | TROP_T | vs TROP_I | Hospital uses one; seed both for mapping |
| MLC0044 | CKMB | Unit U/L vs ng/mL | Human confirm default unit |
| MLC0063 | DDIMER | FEU vs DDU units | Human confirm default unit |
| MLC0053 | LDL_HDL_R | Calculated ratio | Optional row |
| MLC0025 | BAND_PCT | Manual diff only | Optional |
| MLC001D | RETIC | % vs absolute | Confirm default unit |
| MLC005E | CO2 | Duplicate semantics with HCO3 | Alias only vs separate row |
| MLC0088 | CRP_QUAL | Overlap with CRP quant | Panel policy |
| MLC00A9 | URINE_CULT_COL | Text vs numeric CFU | Confirm result type |

---

## 5. Seed Readiness Notes

### SQL generation (`BILRG_LabComponentMaster.dataseed.sql`)

- Pattern: `DELETE` full table then `INSERT` all rows (vendor full-replace per [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md) §9.1).
- **Preserve** existing `MLC0001`–`MLC000C` field values unless human review changes them.
- **Allocate** new rows `MLC000D` through `MLC00A9` as proposed; sequential hex only; no gaps required but keep order stable for diff review.
- **Defaults:** `IsSystem = 1`, `IsActive = 1` for all operational components except:
  - `REMARK` (`MLC000C`): `IsActive = 0` (narrative template).
  - Optional: set `IsActive = 0` for §4 uncertain rows marked defer (WIDAL_*, NH3, APCR, etc.) after human sign-off.
- **Audit columns:** `CrtUser/UpdUser = 'SEED'`, dates per existing seed file.
- **ResultType:** map catalog `SuggestedResultType` integer directly to `ResultType` column.
- **Semantic change rule:** never overwrite meaning of existing `MLCxxxx`; add new `MLC` + deactivate old row.

### JSON / CSV export column order

```text
ComponentId,LoincCode,ComponentCode,ComponentName,ComponentNameIndonesia,ResultType,DefaultUnit,IsSystem,IsActive,Category,Confidence
```

- `Category` and `Confidence` are curation-only; omit from SQL INSERT unless staging table used.

### MLCxxxx assignment checklist

| Range | Category |
|-------|----------|
| MLC0001–MLC000C | Existing seed (12) |
| MLC000D–MLC0026 | Hematology new |
| MLC0027–MLC004C | Chemistry new |
| MLC004D–MLC0053 | Lipid new |
| MLC0054–MLC005E | Electrolyte new |
| MLC005F–MLC0068 | Coagulation new |
| MLC0069–MLC008D | Serology new |
| MLC008E–MLC009D | Urinalysis new |
| MLC009E–MLC00A9 | Microbiology new (12 rows) |

### Alias table (future)

- §3 is not persisted in V1 schema.
- Follow-up agent may generate `LabComponentAlias` import or analyzer mapping CSV from §3.
- Do not duplicate `SGOT`/`SGPT` as separate `ComponentCode` rows.

### Canonicalization locked for seed agent

| Topic | Rule |
|-------|------|
| SGOT / SGPT | Alias → `AST` / `ALT` |
| Ureum / BUN | Alias → `UREA` |
| HB / HGB | Single row `HB` |
| CO2 vs HCO3 | Prefer `HCO3`; `CO2` row optional — human pick |
| Qualitative serology | `ResultType = 3` (Option) |
| Culture results | `ResultType = 2` (Text) summary lines |
| PA / histology | Excluded from this catalog |

### Human review sign-off fields

- [ ] Category counts accepted
- [ ] Uncertain rows (§4) resolved: keep / inactive / drop
- [ ] Default units confirmed (CKMB, DDIMER, RETIC)
- [ ] LOINC blanks acceptable for operational seed
- [ ] Approved for seed.sql generation pass
