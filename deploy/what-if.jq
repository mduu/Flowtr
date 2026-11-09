# Makes a Markdown table from the JSON of "az deployment group what-if --no-pretty-print" (workflow "Infrastructure").
def change_label: {"Create": "➕ Neu", "Delete": "🗑️ Löschen", "Modify": "✏️ Ändern", "Deploy": "🔁 Deploy",
    "Unsupported": "⚠️ Nicht prüfbar"}[.] // .;
def cell: (if type == "string" then . elif . == null then "–" else tojson end)
    | if length > 60 then .[0:57] + "…" else . end | gsub("[|\n]"; " ");
def leaves($prefix): .[] | ($prefix + .path) as $path
    | if .children then .children | leaves($path + ".") else {$path, before, after} end;
def resource_type: (rindex("providers")) as $i | .[$i + 1:] as $parts
    | $parts[0] + "/" + ([$parts[1:] | to_entries[] | select(.key % 2 == 0) | .value] | join("/"));

(.changes // []) as $changes
| if ($changes | length) == 0 then "Keine Änderungen."
  else
    "| Änderung | Ressource | Typ | Eigenschaften |",
    "|---|---|---|---|",
    ($changes | sort_by(.changeType)[]
        | (.resourceId | split("/")) as $id
        | ([(.delta // []) | leaves("") | "`\(.path)`: \(.before | cell) → \(.after | cell)"] | join("<br>")) as $delta
        | "| \(.changeType | change_label) | `\($id[-1])` | \($id | resource_type) | \(if $delta == "" then "–" else $delta end) |")
  end
