# Tarmac Toolbox outline library

Each `*.json` file here is one circuit outline that appears in the editor's **Trace** tab. `index.json` lists the files to load, in order.

## File format

```json
{
  "format": "tarmac-toolbox-outline",
  "version": 1,
  "id": "mon",
  "name": "Mon-Zzzz-a Park",
  "note": "One line shown under the name.",
  "source": "Where the shape came from.",
  "license": "Who may use it, and any credit the data needs.",
  "points": [[10, 50], [14, 30], [30, 18]]
}
```

- `points` is a closed loop of `[x, y]` pairs in any units (y up). The editor centres it, scales it to the track length you choose and smooths it, so only the shape matters. Three or more points; about 15 to 120 is plenty.
- Give every outline a funny, generic name. Please do not use official circuit names or branding.
- If the shape was traced from map data, say so in `source` and keep the data's credit in `license` (OpenStreetMap data needs "(c) OpenStreetMap contributors", ODbL).

## Adding one

- **From a file:** in the Trace tab choose **Open file** and pick a GPX, GeoJSON, CSV ("lat, lon" per line) or one of these JSON files. Then **Save outline** writes it out as a JSON file you can drop in this folder.
- **By hand:** copy one of the files, change `id`, `name`, `note` and `points`, and add the file name to `index.json`.

The four outlines here (`booty`, `burger`, `rumble`, `dose`) were traced from screenshots of circuit maps: only the shape of the dark track line was extracted, with no labels or artwork, and pit roads were left out. Each file's `license` field says to confirm the original map's credit before publishing.
