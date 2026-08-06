"""Regenerate the Wildlife seed data from the Macheo matching workbook.

The workbook is authoritative for the species/practice/action matrix and for each
species' taxonomic classification. Everything else in the existing seed files
(images, occurrence locations, municipality assignments, Spanish justifications)
is curated elsewhere and is carried forward untouched.

Usage:
    python scripts/import_macheo.py "path/to/Macheo-Final.xlsx"

Writes fws_links.json, fws_actions.json and species.json in place under
src/Host/EcoData.Seeder/Data. Re-runnable: same workbook in, same files out.
"""

import json
import re
import sys
from pathlib import Path

import openpyxl

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "src" / "Host" / "EcoData.Seeder" / "Data"

# Workbook scientific names that refer to a species already seeded under a
# different name. Confirmed against each existing record's commonNameEn, which
# carries the workbook's spelling (e.g. Cyclura cornuta -> "Cyclura stejnegeri").
SYNONYMS = {
    "Varronia bellonis (Cordia bellonis)": "Cordia bellonis",
    "Varronia rupicola": "Cordia rupicola",
    "Tectaria estremerana": "Tectaria estremeriana",
    "Cyclura stejnegeri": "Cyclura cornuta",
}

# Workbook Classification -> (species category code, isFauna)
CLASSIFICATION = {
    "Flowering Plants": ("plant", False),
    "Ferns and Allies": ("plant", False),
    "Birds": ("bird", True),
    "Reptile": ("reptile", True),
    "Amphibian": ("amphib", True),
    "Insects": ("invert", True),
    "Mammal": ("mammal", True),
}

# FWS action codes referenced by the workbook but absent from fws_actions.json.
NEW_ACTIONS = {
    "2.3": ("Ex-Situ Conservation", "Conservacion Ex-Situ"),
}


def read_json(name):
    return json.loads((DATA_DIR / name).read_text(encoding="utf-8"))


def write_json(name, payload):
    (DATA_DIR / name).write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


def locale(values):
    return [{"code": code, "value": value} for code, value in values.items() if value]


def load_matrix(workbook_path):
    """Return (rows, species_index) from the workbook's Matching Matrix sheet."""
    book = openpyxl.load_workbook(workbook_path, data_only=True)
    rows = []
    species_index = {}

    for raw in book["Matching Matrix"].iter_rows(min_row=2, values_only=True):
        if not any(raw):
            continue

        practice_code = re.match(r"^\s*(\d+)", str(raw[0])).group(1)
        action_code = str(raw[1]).strip()
        workbook_name = str(raw[4]).strip()
        classification = str(raw[5]).strip()
        justification = str(raw[6]).strip()

        scientific_name = SYNONYMS.get(workbook_name, workbook_name)
        rows.append((practice_code, action_code, scientific_name, justification))
        species_index[scientific_name] = (str(raw[3]).strip(), classification)

    return rows, species_index


def build_links(rows, existing_links):
    """Workbook rows -> fws_links records, keeping Spanish text already written."""
    spanish = {}
    for link in existing_links:
        key = (
            link["nrcsPracticeCode"],
            link["fwsActionCode"],
            link["speciesScientificName"],
        )
        for value in link.get("justification", []):
            if value["code"] == "es":
                spanish[key] = value["value"]

    links = {}
    for practice_code, action_code, scientific_name, justification in rows:
        key = (practice_code, action_code, scientific_name)
        # The workbook contains one exact duplicate row; first occurrence wins.
        if key in links:
            continue
        links[key] = {
            "speciesScientificName": scientific_name,
            "nrcsPracticeCode": practice_code,
            "fwsActionCode": action_code,
            "justification": locale({"en": justification, "es": spanish.get(key)}),
        }

    return [links[key] for key in sorted(links)]


def build_actions(rows, existing_actions):
    codes = {action["code"] for action in existing_actions}
    actions = list(existing_actions)

    for code in sorted({action_code for _, action_code, _, _ in rows}):
        if code in codes:
            continue
        if code not in NEW_ACTIONS:
            raise SystemExit(f"Workbook action code {code!r} has no name mapping.")
        name_en, name_es = NEW_ACTIONS[code]
        actions.append({"code": code, "name": locale({"en": name_en, "es": name_es})})

    return sorted(actions, key=lambda action: action["code"])


def build_species(species_index, existing_species):
    """Carry forward curated species data; correct taxonomy from the workbook."""
    by_name = {}
    for record in existing_species:
        # species.json carries a duplicate Cordia rupicola record; the richer of
        # the two wins so no occurrence data is lost when collapsing them.
        name = record["scientificName"]
        current = by_name.get(name)
        if current is None or len(record.get("locations") or []) > len(
            current.get("locations") or []
        ):
            by_name[name] = record

    added = []
    for scientific_name, (common_name, classification) in sorted(species_index.items()):
        if classification not in CLASSIFICATION:
            raise SystemExit(f"Unmapped classification {classification!r}.")
        category_code, is_fauna = CLASSIFICATION[classification]

        record = by_name.get(scientific_name)
        if record is None:
            record = {
                "scientificName": scientific_name,
                "commonNameEn": common_name,
                "commonNameEs": common_name,
                "elCode": "",
                "gRank": "",
                "sRank": "",
                "locations": [],
                "municipalityGeoJsonIds": [],
            }
            by_name[scientific_name] = record
            added.append(scientific_name)

        # The workbook is authoritative for taxonomy. Everything else on an
        # existing record (image, ranks, locations, municipalities) is untouched.
        record["isFauna"] = is_fauna
        record["categoryCodes"] = [category_code]

    return [by_name[name] for name in sorted(by_name)], added


def main():
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)

    rows, species_index = load_matrix(sys.argv[1])

    existing_links = read_json("fws_links.json")
    existing_actions = read_json("fws_actions.json")
    existing_species = read_json("species.json")

    links = build_links(rows, existing_links)
    actions = build_actions(rows, existing_actions)
    species, added = build_species(species_index, existing_species)

    unknown = {link["speciesScientificName"] for link in links} - {
        record["scientificName"] for record in species
    }
    if unknown:
        raise SystemExit(f"Links reference unknown species: {sorted(unknown)}")

    write_json("fws_links.json", links)
    write_json("fws_actions.json", actions)
    write_json("species.json", species)

    with_spanish = sum(
        1 for link in links if any(v["code"] == "es" for v in link["justification"])
    )
    print(f"fws_links.json    {len(existing_links):>5} -> {len(links):>5}")
    print(f"  keeping Spanish justification on {with_spanish} of them")
    print(f"fws_actions.json  {len(existing_actions):>5} -> {len(actions):>5}")
    print(f"species.json      {len(existing_species):>5} -> {len(species):>5}")
    print(f"  added: {', '.join(added) if added else 'none'}")


if __name__ == "__main__":
    main()
