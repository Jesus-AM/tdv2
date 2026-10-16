"""Pruebas del lector Excel con archivos sintéticos dentro de .artifacts."""
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("extractor", ROOT / "scripts/Extract-ApplicationCatalogs.py")
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)


class ExtractionTests(unittest.TestCase):
    def setUp(self):
        (ROOT / ".artifacts").mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / ".artifacts", prefix="excel-test-")
        self.addCleanup(self.temp.cleanup)

    def workbook(self, rows):
        path = Path(self.temp.name) / "fixture.xlsx"
        main = extractor.NS["s"]
        def text(ref, value):
            return f'<c r="{ref}" t="inlineStr"><is><t xml:space="preserve">{escape(value)}</t></is></c>'
        body = '<row r="1">' + text("A1", "Aplicación") + text("B1", "Ignorada") + '</row>'
        for n, cells in enumerate(rows, 2):
            body += f'<row r="{n}">' + ''.join(text(f'{column}{n}', value) if isinstance(value, str)
                else f'<c r="{column}{n}"><v>{value}</v></c>' for column, value in cells.items()) + '</row>'
        with zipfile.ZipFile(path, 'w') as package:
            package.writestr('xl/workbook.xml', f'<workbook xmlns="{main}" xmlns:r="{extractor.NS["r"]}"><sheets><sheet name="Sheet1" r:id="rId1"/></sheets></workbook>')
            package.writestr('xl/_rels/workbook.xml.rels', '<Relationships><Relationship Id="rId1" Target="worksheets/list.xml"/></Relationships>')
            package.writestr('xl/worksheets/list.xml', f'<worksheet xmlns="{main}"><sheetData>{body}</sheetData></worksheet>')
        return path

    def test_trim_accents_blank_and_stable_ids(self):
        path = self.workbook([{'A': '  Aplicación áéñ  '}, {}, {'A': 'Otro nombre'}])
        first, report = extractor.extract(path, 'web', 2, {})
        self.assertEqual(report['incidencias'], [])
        self.assertEqual(report['filasVaciasOmitidas'], 1)
        self.assertEqual(first['aplicaciones'][0]['nombre'], 'Aplicación áéñ')
        old = {('web', x['nombre']): x['id'] for x in first['aplicaciones']}
        second, _ = extractor.extract(self.workbook([{'A': 'Otro nombre'}, {'A': 'Aplicación áéñ'}]), 'web', 2, old)
        self.assertEqual(first['aplicaciones'], second['aplicaciones'])

    def test_invalid_rows_and_duplicate_reported(self):
        _, report = extractor.extract(self.workbook([{'A': 'Uno'}, {'A': ' Uno '}, {'B': 'Sin nombre'}, {'A': 17}]), 'web', 4, {})
        self.assertEqual([i.get('fila') for i in report['incidencias'][:3]], ['3', '4', '5'])
        self.assertEqual(report['validas'], 1)

    def test_same_name_in_other_category_has_own_identity(self):
        path = self.workbook([{'A': 'Mismo nombre'}])
        old = {('web', 'Mismo nombre'): 'local:11111111-1111-4111-8111-111111111111'}
        web, _ = extractor.extract(path, 'web', 1, old)
        mobile, _ = extractor.extract(path, 'moviles', 1, old)
        self.assertNotEqual(web['aplicaciones'][0]['id'], mobile['aplicaciones'][0]['id'])

    def test_rich_text_preserves_only_visible_content(self):
        node = extractor.ET.fromstring(f'<si xmlns="{extractor.NS["s"]}">\n<t>á </t><r><t>ñ</t></r><rPh><t>Oculto</t></rPh>\n</si>')
        self.assertEqual(extractor.string_content(node), 'á ñ')


if __name__ == '__main__':
    unittest.main()
