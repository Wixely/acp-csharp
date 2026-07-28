"""Compare the fork's C# schema records against the official ACP v1 schema.

Usage: py tools/spec-check.py <schema.json> [schema.unstable.json]
Parses JsonPropertyName attributes per record and diffs property sets and
required-ness against $defs entries of the same name.
"""
import json, re, sys, pathlib

root = pathlib.Path(__file__).resolve().parent.parent / "src" / "AgentClientProtocol"
stable = json.load(open(sys.argv[1], encoding="utf-8"))["$defs"]
unstable = json.load(open(sys.argv[2], encoding="utf-8"))["$defs"] if len(sys.argv) > 2 else {}

# --- parse C# records -> {typename: {prop: required?}} ---
cs_types = {}
record_re = re.compile(r"public (?:abstract )?record (\w+)")
prop_re = re.compile(
    r'\[JsonPropertyName\("([^"]+)"\)\][^\[]*?public\s+(required\s+)?[\w\?\[\]<>, ]+\s+\w+\s*(?:{|=>)',
    re.S,
)
for f in list(root.rglob("*.cs")):
    text = f.read_text(encoding="utf-8")
    # split file into record chunks
    starts = [(m.start(), m.group(1)) for m in record_re.finditer(text)]
    for i, (pos, name) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else len(text)
        chunk = text[pos:end]
        props = {}
        for pm in prop_re.finditer(chunk):
            props[pm.group(1)] = bool(pm.group(2))
        cs_types[name] = props

# --- compare where names match ---
IGNORE_PROPS = set()  # nothing ignored; _meta is in spec everywhere
matched, mismatches = 0, []
for name, props in sorted(cs_types.items()):
    d = stable.get(name)
    origin = "stable"
    if d is None:
        d = unstable.get(name)
        origin = "unstable"
    if d is None or "properties" not in d:
        continue
    matched += 1
    spec_props = set(d["properties"].keys())
    spec_req = set(d.get("required", []))
    ours = set(props.keys())
    issues = []
    missing = spec_props - ours - IGNORE_PROPS
    extra = ours - spec_props - IGNORE_PROPS
    req_missing = {p for p in spec_req & ours if not props[p]}
    req_extra = {p for p, r in props.items() if r and p in spec_props and p not in spec_req}
    if missing:
        issues.append(f"missing props: {sorted(missing)}")
    if extra:
        issues.append(f"extra props (not in spec): {sorted(extra)}")
    if req_missing:
        issues.append(f"spec-required but optional in C#: {sorted(req_missing)}")
    if req_extra:
        issues.append(f"required in C# but optional in spec: {sorted(req_extra)}")
    if issues:
        mismatches.append((name, origin, issues))

print(f"C# record types parsed: {len(cs_types)}; matched to spec defs: {matched}\n")
print("=== TYPE MISMATCHES ===")
for name, origin, issues in mismatches:
    print(f"{name} [{origin}]")
    for i in issues:
        print(f"   - {i}")
if not mismatches:
    print("(none)")

# --- C# types with no spec counterpart ---
print("\n=== C# TYPES NOT IN SPEC (stable or unstable) ===")
for name in sorted(cs_types):
    if name not in stable and name not in unstable and cs_types[name]:
        print(f"  {name}")

# --- spec defs (stable, request/response/notification-ish) with no C# type ---
print("\n=== STABLE SPEC DEFS WITH NO C# TYPE (Request/Response/Notification/Update/Capabilities) ===")
for name in sorted(stable):
    if name in cs_types:
        continue
    if re.search(r"(Request|Response|Notification|Update|Capabilit)", name):
        print(f"  {name}")
