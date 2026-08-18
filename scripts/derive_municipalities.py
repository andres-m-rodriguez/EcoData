"""Derive each species' municipality assignments from its occurrence locations.

Every species record carries `locations` (occurrence points) and
`municipalityGeoJsonIds` (the municipios it is recorded in). The two were out of
step: locations run up to 66 points per species, while municipalityGeoJsonIds
never exceeded 5 on any of the 79 records — the signature of a truncated export
rather than of real distributions. Species with 28 occurrence points were
carried with none at all.

This recomputes the assignments by point-in-polygon against pr-municipios.geojson
and unions the result with whatever the record already had. It is a union rather
than a replacement on purpose: 94 of the existing ids are not reproducible from
the point set, because they carry curated range knowledge that the occurrence
points do not cover. Dropping them to "recompute cleanly" would trade one kind
of data loss for another.

Points that fall outside every municipio contribute nothing, so marine and
offshore-island records are left as they are rather than being forced ashore.

Usage:
    python scripts/derive_municipalities.py [--check]

--check reports what would change and exits non-zero if anything would, so CI
can assert the committed seed data is in sync with the geometry. Re-runnable:
same inputs in, same file out.
"""

import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "src" / "Host" / "EcoData.Seeder" / "Data"


def read_json(name):
    return json.loads((DATA_DIR / name).read_text(encoding="utf-8"))


def write_json(name, payload):
    (DATA_DIR / name).write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


def load_municipios():
    """geoJsonId -> exterior ring, keyed the same way the seeder keys them."""
    geo = read_json("pr-municipios.geojson")
    municipios = []
    for feature in geo["features"]:
        props = feature["properties"]
        geometry = feature["geometry"]
        if geometry["type"] != "Polygon":
            raise SystemExit(f"Unsupported geometry {geometry['type']!r}; expected Polygon.")
        geo_json_id = f"{props['STATE']}{props['COUNTY']}"
        municipios.append((geo_json_id, geometry["coordinates"][0]))
    return municipios


def contains(ring, longitude, latitude):
    """Ray casting against a closed exterior ring."""
    inside = False
    count = len(ring)
    for index in range(count):
        x1, y1 = ring[index][0], ring[index][1]
        x2, y2 = ring[(index + 1) % count][0], ring[(index + 1) % count][1]
        if (y1 > latitude) != (y2 > latitude):
            crossing = (x2 - x1) * (latitude - y1) / (y2 - y1) + x1
            if longitude < crossing:
                inside = not inside
    return inside


def derive(municipios, locations):
    found = set()
    for location in locations or []:
        longitude, latitude = location["longitude"], location["latitude"]
        for geo_json_id, ring in municipios:
            if contains(ring, longitude, latitude):
                found.add(geo_json_id)
                break
    return found


def main():
    check_only = "--check" in sys.argv[1:]

    municipios = load_municipios()
    species = read_json("species.json")

    changed = []
    before = after = 0
    for record in species:
        existing = set(record.get("municipalityGeoJsonIds") or [])
        merged = existing | derive(municipios, record.get("locations"))
        before += len(existing)
        after += len(merged)
        if merged != existing:
            changed.append((record["scientificName"], len(existing), len(merged)))
        record["municipalityGeoJsonIds"] = sorted(merged)

    if check_only:
        for name, was, now in changed:
            print(f"  {name:42s} {was:>2} -> {now:>2}")
        print(f"{len(changed)} record(s) out of sync; links {before} -> {after}")
        raise SystemExit(1 if changed else 0)

    write_json("species.json", species)

    widest = max(len(r["municipalityGeoJsonIds"]) for r in species)
    print(f"species.json      {len(species)} records")
    print(f"  municipality links  {before} -> {after}")
    print(f"  widest distribution {widest} municipios (was capped at 5)")
    print(f"  records updated     {len(changed)}")
    for name, was, now in sorted(changed, key=lambda c: c[2] - c[1], reverse=True)[:10]:
        print(f"    {name:40s} {was:>2} -> {now:>2}")


if __name__ == "__main__":
    main()
