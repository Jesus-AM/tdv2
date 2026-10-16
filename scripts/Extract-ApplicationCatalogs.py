"""Extrae exclusivamente nombres autorizados; no requiere Excel ni paquetes externos."""
import argparse
import hashlib
import json
from pathlib import Path
import posixpath
import uuid
import zipfile
import xml.etree.ElementTree as ET

NS = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
      "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships"}


def string_content(node):
    # Sólo texto real o runs: no incorpora sangría XML ni anotaciones fonéticas.
    return "".join((child.text or "") if child.tag == "{" + NS["s"] + "}t"
                   else child.findtext("s:t", default="", namespaces=NS)
                   for child in node if child.tag in ("{" + NS["s"] + "}t", "{" + NS["s"] + "}r"))


def extract(path, category, expected, old):
    errors, entries, blanks, seen = [], [], 0, set()
    with zipfile.ZipFile(path) as package:
        strings = []
        if "xl/sharedStrings.xml" in package.namelist():
            strings = [string_content(n) for n in ET.fromstring(package.read("xl/sharedStrings.xml")).findall("s:si", NS)]
        book = ET.fromstring(package.read("xl/workbook.xml"))
        sheet = next(s for s in book.findall("s:sheets/s:sheet", NS) if s.attrib["name"] == "Sheet1")
        rels = ET.fromstring(package.read("xl/_rels/workbook.xml.rels"))
        target = next(r.attrib["Target"] for r in rels if r.attrib["Id"] == sheet.attrib["{" + NS["r"] + "}id"])
        target = target.lstrip("/") if target.startswith("/") else posixpath.normpath("xl/" + target)
        rows = ET.fromstring(package.read(target)).findall("s:sheetData/s:row", NS)
        def value(cell):
            kind = cell.get("t")
            if kind == "inlineStr": return string_content(cell.find("s:is", NS))
            raw = cell.findtext("s:v", default="", namespaces=NS)
            return strings[int(raw)] if kind == "s" and raw else raw
        header = None
        for row in rows:
            cells = {"".join(c for c in cell.attrib["r"] if c.isalpha()): cell for cell in row.findall("s:c", NS)}
            values = {k: value(c) for k, c in cells.items()}
            if not any(v.strip() for v in values.values()): blanks += 1; continue
            if header is None:
                matches = [k for k, v in values.items() if v.strip() == "Aplicación"]
                if len(matches) != 1: raise ValueError(f"{category}: encabezado Aplicación ausente o ambiguo")
                header = matches[0]; continue
            cell = cells.get(header)
            name = values.get(header, "").strip()
            if not name or cell is None or cell.get("t") not in ("s", "inlineStr", "str") or cell.find("s:f", NS) is not None:
                errors.append({"fila": row.get("r"), "motivo": "Aplicación vacía, no textual o fórmula"}); continue
            if name in seen:
                errors.append({"fila": row.get("r"), "motivo": "Aplicación duplicada", "nombre": name}); continue
            if len(name) > 4000 or "\x00" in name:
                errors.append({"fila": row.get("r"), "motivo": "Nombre fuera del contrato"}); continue
            seen.add(name)
            # Un UUID se asigna una vez y queda versionado; nunca procede del número de fila o de la descripción.
            entries.append({"id": old.get((category, name), "local:" + str(uuid.uuid4())), "nombre": name})
    if header is None: errors.append({"motivo": "Hoja sin encabezado"})
    if len(entries) != expected: errors.append({"motivo": f"Se esperaban {expected} nombres y se obtuvieron {len(entries)}"})
    report = {"categoria": category, "archivo": path.name, "filasVaciasOmitidas": blanks, "validas": len(entries), "incidencias": errors}
    return {"categoria": category, "archivo": path.name, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
            "esperadas": expected, "aplicaciones": sorted(entries, key=lambda e: e["nombre"])}, report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--web", type=Path, required=True)
    parser.add_argument("--moviles", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[1] / "tdv2/Contracts/aplicaciones-2026-10-14.json")
    args = parser.parse_args()
    old = {}
    if args.output.exists():
        previous = json.loads(args.output.read_text(encoding="utf-8-sig"))
        old = {(c["categoria"], e["nombre"]): e["id"] for c in previous["catalogos"] for e in c["aplicaciones"]}
    catalogs, reports = [], []
    for path, category, count in ((args.web, "web", 207), (args.moviles, "moviles", 26)):
        catalog, report = extract(path, category, count, old); catalogs.append(catalog); reports.append(report)
    print(json.dumps(reports, ensure_ascii=False, indent=2))
    if any(r["incidencias"] for r in reports): raise SystemExit(1)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps({"version": 1, "edicion": "2026-10-14", "catalogos": catalogs, "extraccion": reports}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__": main()
