#!/usr/bin/env python3
import argparse
import re
from pathlib import Path
from typing import Dict, List, Tuple


KEEP_PREFIXES = (
    "Index Cond:",
    "Filter:",
    "Join Filter:",
    "Rows Removed by Filter:",
    "Rows Removed by Join Filter:",
    "Recheck Cond:",
    "Sort Key:",
    "Group Key:",
    "Buffers:",
    "Heap Fetches:",
    "Planning Time:",
    "Execution Time:",
)

DROP_PREFIXES = (
    "Sort Method:",
    "Memory:",
    "Batches:",
    "Cache Key:",
    "Cache Mode:",
    "Hits:",
    "Misses:",
    "Evictions:",
    "Overflows:",
    "Memory Usage:",
    "Output:",
    "Worker ",
)


NODE_LINE_RE = re.compile(r"^\s*(-> )?[^\(]+\(.*\)")
ACTUAL_RE = re.compile(r"actual time=[^\)]*")


def is_header(line: str) -> bool:
    return line.startswith("== ") and line.endswith(" ==")


def normalize_space(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def simplify_node_line(line: str) -> str:
    leading = ""
    stripped = line.lstrip()
    if stripped.startswith("->"):
        leading = "-> "
        stripped = stripped[2:].lstrip()

    name_part = stripped.split("(")[0].strip()
    m = ACTUAL_RE.search(line)
    if m:
        actual = m.group(0)
        actual = normalize_space(actual)
        actual = actual.replace("actual time=", "t=")
        actual = actual.replace("rows=", "r=")
        actual = actual.replace("loops=", "l=")
        return f"{leading}{name_part} ({actual})"
    return f"{leading}{name_part}"


def should_keep(line: str) -> bool:
    stripped = line.strip()
    if not stripped:
        return False
    if is_header(stripped):
        return True
    if stripped.startswith(KEEP_PREFIXES):
        return True
    if stripped.startswith(DROP_PREFIXES):
        return False
    if NODE_LINE_RE.match(line):
        return True
    return False


def condense(lines: List[str]) -> List[str]:
    out: List[str] = []
    for line in lines:
        stripped = line.strip()
        if not stripped:
            continue
        if is_header(stripped):
            out.append(stripped)
            continue
        if stripped.startswith(DROP_PREFIXES):
            continue
        if NODE_LINE_RE.match(line):
            out.append(simplify_node_line(line))
            continue
        if stripped.startswith(KEEP_PREFIXES):
            out.append(normalize_space(stripped))
            continue
    return out


COMMON_TERMS = [
    "Index Only Scan",
    "Index Scan",
    "Bitmap Heap Scan",
    "Bitmap Index Scan",
    "Seq Scan",
    "Nested Loop",
    "Merge Join",
    "Hash Join",
    "HashAggregate",
    "Aggregate",
    "Subquery Scan",
    "CTE Scan",
    "Function Scan",
    "Gather Merge",
    "Gather",
    "Memoize",
    "Sort",
    "Limit",
    "Unique",
    "Materialize",
    "Result",
    "Filter:",
    "Index Cond:",
    "Join Filter:",
    "Rows Removed by Filter:",
    "Rows Removed by Join Filter:",
    "Recheck Cond:",
    "Sort Key:",
    "Group Key:",
    "Buffers:",
    "Heap Fetches:",
    "Planning Time:",
    "Execution Time:",
    "Semi Join",
    "Append",
    "ProjectSet",
    "on",
    "using",
    "shared hit",
    "read",
    "written",
]


def build_term_dict() -> Dict[str, str]:
    pool = list("!#$%&*+;:?@^_|~")
    pool += list("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789")
    if len(COMMON_TERMS) > len(pool):
        raise SystemExit("Not enough single-character codes for COMMON_TERMS")
    return {term: pool[idx] for idx, term in enumerate(COMMON_TERMS)}


def extract_index_names(lines: List[str]) -> List[str]:
    names = []
    index_scan_pattern = re.compile(r"Index (?:Only )?Scan using ([^\s]+)")
    table_pattern = re.compile(r"\b(?:on|using)\s+\"([^\"]+)\"")
    table_pattern_bare = re.compile(r"\b(?:on|using)\s+([A-Za-z0-9_\.]+)")
    quoted_pattern = re.compile(r"\"([^\"]+)\"")
    for line in lines:
        match = index_scan_pattern.search(line)
        if match:
            name = match.group(1).strip().strip('"')
            if name not in names:
                names.append(name)
        for tbl_pattern in (table_pattern, table_pattern_bare):
            for m in tbl_pattern.finditer(line):
                name = m.group(1).strip().strip('"')
                if not name or len(name) <= 1:
                    continue
                if not (name.startswith(("IX_", "PK_", "FK_")) or "." in name or name[0].isupper()):
                    continue
                if name not in names:
                    names.append(name)
        for m in quoted_pattern.finditer(line):
            name = m.group(1).strip()
            if not name or len(name) <= 1:
                continue
            if name not in names:
                names.append(name)
    filtered = []
    for name in names:
        if not name or len(name) <= 1:
            continue
        if name.startswith(("IX_", "PK_", "FK_")) or "." in name or name[0].isupper():
            filtered.append(name)
    return filtered


def build_index_dict(names: List[str]) -> Dict[str, str]:
    codes = []
    for first in range(ord("A"), ord("Z") + 1):
        for second in range(ord("A"), ord("Z") + 1):
            codes.append(chr(first) + chr(second))
    if len(names) > len(codes):
        raise SystemExit("Too many index names for two-letter codes")
    return {name: codes[idx] for idx, name in enumerate(names)}


def apply_replacements(
    lines: List[str],
    term_dict: Dict[str, str],
    index_dict: Dict[str, str],
) -> List[str]:
    out: List[str] = []
    term_patterns = []
    for term, code in sorted(term_dict.items(), key=lambda x: -len(x[0])):
        if term.endswith(":") or " " in term:
            pattern = re.compile(re.escape(term))
        else:
            pattern = re.compile(rf"\b{re.escape(term)}\b")
        term_patterns.append((pattern, code))
    for line in lines:
        updated = line
        for name, code in index_dict.items():
            updated = updated.replace(f"\"{name}\"", code)
            updated = re.sub(rf"\b{re.escape(name)}\b", code, updated)
        for pattern, code in term_patterns:
            updated = pattern.sub(code, updated)
        out.append(updated)
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description="Condense EXPLAIN ANALYZE output for LLM processing")
    parser.add_argument("input", help="Path to explains_all.txt")
    parser.add_argument("--out", help="Output path (default: <input>.condensed.txt)")
    args = parser.parse_args()

    in_path = Path(args.input)
    if not in_path.is_file():
        raise SystemExit(f"Input not found: {in_path}")

    out_path = Path(args.out) if args.out else in_path.with_suffix(in_path.suffix + ".condensed.txt")

    lines = in_path.read_text(encoding="utf-8").splitlines()
    condensed = condense(lines)
    term_dict = build_term_dict()
    index_names = extract_index_names(condensed)
    index_dict = build_index_dict(index_names)
    replaced = apply_replacements(condensed, term_dict, index_dict)

    header = (
        "Format: blocks start with '== file =='; lines show plan nodes with actual time/rows/loops, "
        "plus key conditions and buffers; costs/widths/memory/cache stats removed. "
        "Dictionary compression applied: common EXPLAIN terms replaced with single characters; "
        "identifiers (tables/indexes) replaced with two-letter codes. "
        "Node timing labels shortened to t/r/l."
    )

    dict_lines = ["TERMS:"]
    for term, code in term_dict.items():
        dict_lines.append(f"{code}={term}")
    dict_lines.append("IDENTIFIERS:")
    for name, code in index_dict.items():
        dict_lines.append(f"{code}={name}")

    out_path.write_text("\n".join([header] + dict_lines + replaced) + "\n", encoding="utf-8")
    print(str(out_path))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
