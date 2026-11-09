# Makes a Markdown table from the JSON of "az deployment group what-if --no-pretty-print" (workflow "Infrastructure").
# What-if also shows properties that do not change: expressions it can resolve only at deployment ([reference(...)]),
# properties without effect, and the same value in another format. These go into a closed section.
def change_label: {"Create": "➕ Neu", "Delete": "🗑️ Löschen", "Modify": "✏️ Ändern", "Deploy": "🔁 Deploy",
    "Unsupported": "⚠️ Nicht prüfbar"}[.] // .;
def cell: (if type == "string" then . elif . == null then "–" else tojson end)
    | if length > 60 then .[0:57] + "…" else . end | gsub("[|\n]"; " ");
def leaves($prefix): .[] | ($prefix + .path) as $path
    | if .children then .children | leaves($path + ".")
      else {$path, type: .propertyChangeType, before, after} end;
def resource_type: (rindex("providers")) as $i | .[$i + 1:] as $parts
    | $parts[0] + "/" + ([$parts[1:] | to_entries[] | select(.key % 2 == 0) | .value] | join("/"));
def expression: type == "string" and startswith("[") and endswith("]");
def same_value: .before as $before | .after as $after
    | ($before | type) == "string" and ($after | type) == "string" and $after != ""
    and (($before | startswith($after)) or ($after | startswith($before)));
def noise: .type == "NoEffect" or (.after | expression) or same_value;
def property: "`\(.path)`: \(.before | cell) → \(.after | cell)";
def row($properties): (.resourceId | split("/")) as $id
    | "| \(.changeType | change_label) | `\($id[-1])` | \($id | resource_type) | \($properties) |";

[(.changes // [])[] | . + {properties: [(.delta // []) | leaves("")]}] as $changes
| [$changes[] | select(.changeType != "Modify" or any(.properties[]; noise | not))] as $real
| [$changes[] | select(.changeType == "Modify" and all(.properties[]; noise))] as $quiet
| if ($real | length) == 0 then "**Keine wirksamen Änderungen.**" else
    "| Änderung | Ressource | Typ | Eigenschaften |",
    "|---|---|---|---|",
    ($real | sort_by(.changeType)[]
        | row([.properties[] | select(noise | not) | property] | join("<br>") | if . == "" then "–" else . end))
  end,
  if ($quiet | length) > 0 or any($real[].properties[]; noise) then
    "",
    "<details><summary>Ohne Wirkung oder erst beim Deploy bekannt</summary>",
    "",
    "| Änderung | Ressource | Typ | Eigenschaften |",
    "|---|---|---|---|",
    (($real + $quiet)[] | select(any(.properties[]; noise))
        | row([.properties[] | select(noise) | property] | join("<br>"))),
    "",
    "</details>"
  else empty end
