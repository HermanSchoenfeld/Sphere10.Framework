#!/usr/bin/env python3
"""Regenerates src/Sphere10.Framework/Resources/MimeTypes.tsv.gz, the extension table MimeHelper embeds, from the mime-db database.

mime-db (https://github.com/jshttp/mime-db, MIT licence) combines the IANA media type registry with the Apache and nginx type maps.
Where several types claim an extension, the most specific registered type wins: any type over application/octet-stream, then an IANA
type over an Apache one over an nginx one, then a standard subtype over a vnd. one over an x- or prs. one, then a specific top-level
type (video, audio, image, text, font, model) over application/, then the shorter name. So .mp4 is video/mp4 rather than
application/mp4. Extensions the table already maps and mime-db lacks keep their mapping, and a specific earlier mapping is never
replaced by application/octet-stream, so nothing is lost. OVERRIDES settles the few extensions whose registered claim is not what
files with that extension are in practice.

The table is gzip-compressed tab-separated text, one "extension<TAB>type" line per extension in ordinal order, written with a fixed
timestamp so the same input always produces the same bytes. MimeHelper reads it on its first lookup.

Usage: python scripts/generate-mime-types.py <path to mime-db db.json> [--version <mime-db version>] [--seed <MimeHelper.cs>]
Download db.json from https://cdn.jsdelivr.net/npm/mime-db@<version>/db.json. --seed reads the earlier mappings from a MimeHelper.cs
that still held them as a switch expression, instead of from the existing table. The script prints each added and changed extension.
"""
import gzip
import io
import json
import re
import sys
from pathlib import Path

SOURCE_RANK = {'iana': 3, 'apache': 2, None: 1, 'nginx': 0}
# Extensions whose highest-ranked claim misdescribes the files that use it.
OVERRIDES = {
    '.aif': 'audio/aiff',  # The unprefixed name is the one browsers and media servers use.
    '.aifc': 'audio/aiff',
    '.aiff': 'audio/aiff',
    '.jfif': 'image/jpeg',  # JFIF files are ordinary JPEG images, not progressive-only image/pjpeg.
    '.mpp': 'application/vnd.ms-project',  # Microsoft Project files, not the rarely seen DASH patch documents.
    '.wav': 'audio/wav',  # The unprefixed name is the one browsers and media servers use.
}
TABLE = Path(__file__).resolve().parent.parent / 'src' / 'Sphere10.Framework' / 'Resources' / 'MimeTypes.tsv.gz'
SWITCH_ENTRY = re.compile(r'^\s*"(\.[^"]+)" => "([^"]+)",', re.M)


def rank(media_type, database):
    """Orders the types that claim one extension; the highest rank wins."""
    top, subtype = media_type.split('/', 1)
    facet = 0 if subtype.startswith(('x-', 'x.', 'prs.')) else 1 if subtype.startswith('vnd.') else 2
    return (media_type != 'application/octet-stream', SOURCE_RANK.get(database[media_type].get('source'), 1), facet,
            top != 'application', -len(media_type), media_type)


def mime_db_types(database):
    """Maps each extension to its highest-ranked type."""
    claims = {}
    for media_type, details in database.items():
        for extension in details.get('extensions', []):
            claims.setdefault(extension, []).append(media_type)
    return {'.' + extension: max(types, key=lambda media_type: rank(media_type, database)) for extension, types in claims.items()}


def read_table():
    if not TABLE.exists():
        return {}
    lines = gzip.decompress(TABLE.read_bytes()).decode('ascii').splitlines()
    return dict(line.split('\t', 1) for line in lines if '\t' in line)


def write_table(mappings):
    text = ''.join(f'{extension}\t{mappings[extension]}\n' for extension in sorted(mappings))
    buffer = io.BytesIO()
    with gzip.GzipFile(filename='', mode='wb', fileobj=buffer, compresslevel=9, mtime=0) as archive:
        archive.write(text.encode('ascii'))
    TABLE.write_bytes(buffer.getvalue())
    return len(text), len(buffer.getvalue())


def main():
    arguments = sys.argv[1:]
    version = arguments[arguments.index('--version') + 1] if '--version' in arguments else 'unknown'
    database = json.loads(Path(arguments[0]).read_text(encoding='utf-8'))
    if '--seed' in arguments:
        current = dict(SWITCH_ENTRY.findall(Path(arguments[arguments.index('--seed') + 1]).read_text(encoding='utf-8')))
    else:
        current = read_table()
    generated = mime_db_types(database)
    merged = {**current, **generated}
    for extension, media_type in generated.items():
        if media_type == 'application/octet-stream' and current.get(extension, media_type) != media_type:
            merged[extension] = current[extension]
    merged.update(OVERRIDES)
    for extension in sorted(merged):
        if extension not in current:
            print(f'added {extension} {merged[extension]}')
        elif current[extension] != merged[extension]:
            print(f'changed {extension} {current[extension]} -> {merged[extension]}')
    plain, compressed = write_table(merged)
    print(f'{len(merged)} extensions: {len(generated)} from mime-db {version}, {len(merged) - len(generated)} kept from the earlier table; '
          f'{plain} bytes compressed to {compressed}')


if __name__ == '__main__':
    main()
