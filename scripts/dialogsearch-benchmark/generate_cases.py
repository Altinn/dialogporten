#!/usr/bin/env python3
import argparse
import json
import math
import os
import random
import re
import sys
from itertools import product


DEFAULT_SEED = 20260205
MAX_DEFAULT_CASES = 50


def warn(message: str) -> None:
    print(f"warning: {message}", file=sys.stderr)


def read_lines(path: str) -> list[str]:
    try:
        with open(path, "r", encoding="utf-8") as handle:
            lines = [line.strip() for line in handle if line.strip()]
    except FileNotFoundError as exc:
        raise SystemExit(f"File not found: {path}") from exc
    if not lines:
        raise SystemExit(f"File is empty: {path}")
    return lines


def clamp_value(value: int, max_value: int, label: str) -> int:
    if value > max_value:
        warn(f"{label} {value} exceeds available {max_value}; clamped to {max_value}")
        return max_value
    return value


def clamp_list(values: list[int], max_value: int, label: str) -> list[int]:
    clamped = [min(value, max_value) for value in values]
    if any(value > max_value for value in values):
        warn(f"Some {label} values exceed available {max_value}; clamped")
    unique = []
    seen = set()
    for value in clamped:
        if value not in seen:
            unique.append(value)
            seen.add(value)
    return unique


def next_counter(seed: int, out_dir: str, include_seed: bool) -> int:
    if include_seed:
        pattern = re.compile(r"^(?P<counter>\d{3})-(?P<seed>\d+)-.*\.json$")
    else:
        pattern = re.compile(r"^(?P<counter>\d{3})-.*\.json$")
    max_counter = 0
    for name in os.listdir(out_dir):
        match = pattern.match(name)
        if not match:
            continue
        if include_seed:
            if int(match.group("seed")) != seed:
                continue
        max_counter = max(max_counter, int(match.group("counter")))
    return max_counter + 1


def distribute_parties(rng: random.Random, parties: list[str], group_count: int) -> list[list[str]]:
    parties = list(parties)
    rng.shuffle(parties)
    groups = [[] for _ in range(group_count)]
    for index, party in enumerate(parties):
        groups[index % group_count].append(party)
    return groups


def build_group_services(
    rng: random.Random,
    services_pool: list[str],
    total_services: int,
    group_count: int,
) -> list[list[str]]:
    selected = rng.sample(services_pool, total_services)
    min_size = max(1, math.ceil(total_services * 0.5))
    groups = []
    for _ in range(group_count):
        size = rng.randint(min_size, total_services)
        group_services = rng.sample(selected, size)
        rng.shuffle(group_services)
        groups.append(group_services)
    return groups


def generate_case(
    rng: random.Random,
    party_pool: list[str],
    service_pool: list[str],
    total_parties: int,
    total_services: int,
    group_count: int,
) -> list[dict]:
    if total_parties < 1 or total_services < 1:
        raise SystemExit("Total party and service counts must be >= 1.")
    if group_count < 1:
        raise SystemExit("Group count must be >= 1.")
    if total_parties < group_count:
        raise SystemExit("Group count cannot exceed total parties (no empty groups).")

    parties = rng.sample(party_pool, total_parties)
    party_groups = distribute_parties(rng, parties, group_count)
    service_groups = build_group_services(rng, service_pool, total_services, group_count)

    return [
        {"Parties": party_groups[index], "Services": service_groups[index]}
        for index in range(group_count)
    ]


def pick_evenly_spaced_indices(count: int, target: int) -> list[int]:
    if target <= 0:
        raise ValueError("target must be >= 1")
    if target == 1:
        return [count // 2] if count > 0 else []
    if count <= target:
        return list(range(count))
    step = (count - 1) / (target - 1)
    indices = [int(round(i * step)) for i in range(target)]
    unique = sorted(set(indices))
    if len(unique) < target:
        for idx in range(count):
            if idx not in unique:
                unique.append(idx)
                if len(unique) == target:
                    break
        unique.sort()
    return unique


def build_default_combinations(
    party_pool: list[str],
    service_pool: list[str],
) -> list[tuple[int, int, int]]:
    party_candidates = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000]
    service_candidates = [1, 2, 5, 10, 100, 1000, 3000]
    group_candidates = [1, 2, 5, 10, 20, 50]

    party_values = clamp_list(party_candidates, len(party_pool), "party count")
    service_values = clamp_list(service_candidates, len(service_pool), "service count")
    group_values = group_candidates[:]

    combos = list(product(party_values, service_values, group_values))
    if len(combos) > MAX_DEFAULT_CASES:
        indices = pick_evenly_spaced_indices(len(combos), MAX_DEFAULT_CASES)
        combos = [combos[i] for i in indices]

    unique = []
    seen = set()
    for combo in combos:
        if combo not in seen:
            unique.append(combo)
            seen.add(combo)
    return unique


def write_case(
    out_dir: str,
    counter: int,
    seed: int,
    case: list[dict],
    totals: tuple[int, int, int],
    include_seed: bool,
) -> str:
    total_parties, total_services, group_count = totals
    if include_seed:
        filename = f"{counter:03d}-{seed}-{total_parties}p-{total_services}s-{group_count}g.json"
    else:
        filename = f"{counter:03d}-{total_parties}p-{total_services}s-{group_count}g.json"
    path = os.path.join(out_dir, filename)
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(case, handle)
    return filename


def parse_generate_set(value: str) -> list[tuple[int, int, int]]:
    if not value.strip():
        raise SystemExit("generate_set cannot be empty.")

    combos = []
    for raw in value.split(";"):
        entry = raw.strip()
        if not entry:
            continue
        parts = [p.strip() for p in entry.split(",")]
        if len(parts) != 3:
            raise SystemExit(f"Invalid generate_set entry: '{entry}'. Expected 'parties,services,groups'.")
        try:
            parties = int(parts[0])
            services = int(parts[1])
            groups = int(parts[2])
        except ValueError as exc:
            raise SystemExit(f"Invalid generate_set entry: '{entry}'. Must be integers.") from exc
        combos.append((parties, services, groups))

    if not combos:
        raise SystemExit("generate_set did not contain any valid entries.")
    return combos


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate party/service test cases.")
    parser.add_argument("--parties-path", default="parties.txt", help="Path to parties.txt (default: cwd)")
    parser.add_argument("--services-path", default="services.txt", help="Path to services.txt (default: cwd)")
    parser.add_argument("--out-dir", default=".", help="Output directory (default: cwd)")
    parser.add_argument("--seed", type=int, default=DEFAULT_SEED, help="Random seed (default: 20260205)")
    parser.add_argument(
        "--omit-seed-in-filename",
        action="store_true",
        help="Omit seed from output filenames",
    )

    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--generate-default-set", action="store_true", help="Generate a default set of cases")
    mode.add_argument(
        "--generate-set",
        help="Semicolon-separated list of parties,services,groups (e.g. '1,1,1;5,3000,4')",
    )
    mode.add_argument("--parties", type=int, help="Total party count")
    parser.add_argument("--services", type=int, help="Total service count")
    parser.add_argument("--groups", type=int, default=1, help="Group count (default: 1)")

    args = parser.parse_args()

    party_pool = read_lines(args.parties_path)
    service_pool = read_lines(args.services_path)

    os.makedirs(args.out_dir, exist_ok=True)
    include_seed = not args.omit_seed_in_filename
    counter = next_counter(args.seed, args.out_dir, include_seed)
    rng = random.Random(args.seed)

    created = []

    def emit_case(total_parties: int, total_services: int, group_count: int) -> None:
        nonlocal counter
        if total_parties < group_count:
            warn(f"Skipping {total_parties} parties with {group_count} groups (no empty groups allowed).")
            return
        case = generate_case(
            rng,
            party_pool,
            service_pool,
            total_parties,
            total_services,
            group_count,
        )
        filename = write_case(
            args.out_dir,
            counter,
            args.seed,
            case,
            (total_parties, total_services, group_count),
            include_seed,
        )
        created.append(filename)
        counter += 1

    def process_combinations(combos: list[tuple[int, int, int]], clamp_counts: bool) -> None:
        for total_parties, total_services, group_count in combos:
            if clamp_counts:
                total_parties = clamp_value(total_parties, len(party_pool), "party count")
                total_services = clamp_value(total_services, len(service_pool), "service count")
            emit_case(total_parties, total_services, group_count)

    if args.generate_default_set:
        process_combinations(build_default_combinations(party_pool, service_pool), clamp_counts=False)
    elif args.generate_set:
        process_combinations(parse_generate_set(args.generate_set), clamp_counts=True)
    else:
        if args.parties is None or args.services is None:
            raise SystemExit("Both --parties and --services are required unless --generate-default-set is used.")

        total_parties = clamp_value(args.parties, len(party_pool), "party count")
        total_services = clamp_value(args.services, len(service_pool), "service count")
        group_count = args.groups

        emit_case(total_parties, total_services, group_count)

    for name in created:
        print(name)


if __name__ == "__main__":
    main()
