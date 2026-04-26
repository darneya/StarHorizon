#!/usr/bin/env python3
from __future__ import annotations

import argparse
import base64
import copy
import io
import json
import math
import os
import struct
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml
from PIL import Image, ImageChops, ImageColor, ImageOps


TILE_SIZE = 32
CHUNK_DEFAULT_SIZE = 16


class SS14SafeLoader(yaml.SafeLoader):
    pass


def _construct_unknown_tag(loader: SS14SafeLoader, tag_suffix: str, node: yaml.Node):
    if isinstance(node, yaml.ScalarNode):
        return loader.construct_scalar(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)
    raise TypeError(f"Unsupported YAML node: {node!r}")


SS14SafeLoader.add_multi_constructor("!type:", _construct_unknown_tag)
SS14SafeLoader.add_multi_constructor("!", _construct_unknown_tag)


@dataclass(frozen=True)
class TileDef:
    id: str
    sprite: str | None
    variants: int


@dataclass(frozen=True)
class DecodedTile:
    proto_id: str
    flags: int
    variant: int
    rotation_mirroring: int


@dataclass(frozen=True)
class GridData:
    uid: int
    chunks: dict[str, Any]


@dataclass(frozen=True)
class MapEntity:
    uid: int
    proto_id: str
    components: dict[str, dict[str, Any]]


@dataclass(frozen=True)
class TransformData:
    parent: int | None
    x: float
    y: float
    rotation: float


@dataclass(frozen=True)
class RenderEntity:
    uid: int
    proto_id: str
    x: float
    y: float
    rotation: float
    sprite: dict[str, Any]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Render SS14/StarHorizon map grids to PNG using repository resources."
    )
    parser.add_argument(
        "input",
        help="Path to a map file in Resources/Maps or a prototype file containing mapPath/shuttlePath.",
    )
    parser.add_argument(
        "-o",
        "--output",
        default=None,
        help="Output directory. Defaults to Tools/map_renderer_py/out/<map-name>/",
    )
    parser.add_argument(
        "--repo-root",
        default=None,
        help="Repository root. Defaults to auto-detection from this script location.",
    )
    parser.add_argument(
        "--background",
        default="#00000000",
        help="Canvas background color, e.g. '#00000000' or '#101010'. Default is transparent.",
    )
    parser.add_argument(
        "--grid",
        type=int,
        action="append",
        help="Render only the specified 0-based grid index. Can be passed multiple times.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = resolve_repo_root(args.repo_root)
    input_path = Path(args.input)
    if not input_path.is_absolute():
        input_path = (Path.cwd() / input_path).resolve()

    if not input_path.exists():
        raise SystemExit(f"Input file does not exist: {input_path}")

    map_path = resolve_map_path(input_path, repo_root)
    tile_defs = load_tile_definitions(repo_root)
    entity_prototypes = load_entity_prototypes(repo_root)
    sprite_cache: dict[tuple[str, int], Image.Image] = {}
    rsi_meta_cache: dict[str, dict[str, Any]] = {}
    bg_color = ImageColor.getcolor(args.background, "RGBA")

    map_data = load_yaml(map_path)
    grid_data = extract_grids(map_data)
    map_entities = extract_map_entities(map_data)
    if not grid_data:
        raise SystemExit(f"No MapGrid chunks were found in {map_path}")

    selected_grids = set(args.grid or [])
    output_dir = Path(args.output) if args.output else Path(tempfile.gettempdir()) / "starhorizon-map-renderer" / map_path.stem
    output_dir.mkdir(parents=True, exist_ok=True)

    rendered = 0
    for grid_index, grid in enumerate(grid_data):
        if selected_grids and grid_index not in selected_grids:
            continue

        image = render_grid(
            grid,
            map_data["tilemap"],
            map_entities,
            entity_prototypes,
            tile_defs,
            sprite_cache,
            rsi_meta_cache,
            repo_root,
            bg_color,
        )
        out_path = output_dir / f"{map_path.stem}-grid-{grid_index}.png"
        image.save(out_path)
        rendered += 1
        print(f"Rendered grid {grid_index} -> {out_path}")

    if rendered == 0:
        raise SystemExit("No grids matched the requested filters.")

    return 0


def resolve_repo_root(repo_root_arg: str | None) -> Path:
    if repo_root_arg:
        return Path(repo_root_arg).resolve()
    return Path(__file__).resolve().parents[2]


def resolve_map_path(input_path: Path, repo_root: Path) -> Path:
    if "Maps" in input_path.parts:
        return input_path

    data = load_yaml(input_path)
    candidates = []
    for item in iter_yaml_items(data):
        if not isinstance(item, dict):
            continue
        for key in ("mapPath", "shuttlePath"):
            value = item.get(key)
            if isinstance(value, str) and value.startswith("/"):
                candidates.append(value)

    if not candidates:
        raise SystemExit(
            f"Could not find mapPath or shuttlePath in prototype file: {input_path}"
        )

    rel = candidates[0].lstrip("/").replace("/", os.sep)
    resolved = (repo_root / "Resources" / rel).resolve()
    if not resolved.exists():
        raise SystemExit(f"Resolved map path does not exist: {resolved}")
    return resolved


def load_yaml(path: Path) -> Any:
    with path.open("r", encoding="utf-8") as handle:
        return yaml.load(handle, Loader=SS14SafeLoader)


def iter_yaml_items(node: Any):
    if isinstance(node, list):
        for item in node:
            yield item
            yield from iter_yaml_items(item)
    elif isinstance(node, dict):
        for value in node.values():
            yield from iter_yaml_items(value)


def load_tile_definitions(repo_root: Path) -> dict[str, TileDef]:
    prototypes_dir = repo_root / "Resources" / "Prototypes"
    tile_defs: dict[str, TileDef] = {}

    for path in prototypes_dir.rglob("*.yml"):
        try:
            data = load_yaml(path)
        except Exception:
            continue

        if not isinstance(data, list):
            continue

        for item in data:
            if not isinstance(item, dict):
                continue
            if item.get("type") != "tile":
                continue

            tile_id = item.get("id")
            if not isinstance(tile_id, str):
                continue

            sprite = item.get("sprite")
            variants = item.get("variants", 1)
            if not isinstance(sprite, str):
                sprite = None
            if not isinstance(variants, int) or variants < 1:
                variants = 1

            tile_defs[tile_id] = TileDef(id=tile_id, sprite=sprite, variants=variants)

    if not tile_defs:
        raise SystemExit("No tile definitions were found under Resources/Prototypes")

    return tile_defs


def extract_grid_chunks(map_data: dict[str, Any]) -> list[dict[str, Any]]:
    return [grid.chunks for grid in extract_grids(map_data)]


def extract_grids(map_data: dict[str, Any]) -> list[GridData]:
    entities_root = map_data.get("entities")
    if not isinstance(entities_root, list):
        return []

    grids: list[GridData] = []
    for bucket in entities_root:
        if not isinstance(bucket, dict):
            continue
        entities = bucket.get("entities")
        if not isinstance(entities, list):
            continue

        for entity in entities:
            if not isinstance(entity, dict):
                continue
            components = entity.get("components")
            if not isinstance(components, list):
                continue

            for component in components:
                if not isinstance(component, dict):
                    continue
                if component.get("type") == "MapGrid" and isinstance(component.get("chunks"), dict):
                    uid = entity.get("uid")
                    if isinstance(uid, int):
                        grids.append(GridData(uid=uid, chunks=component["chunks"]))
                    break

    return grids


def extract_map_entities(map_data: dict[str, Any]) -> list[MapEntity]:
    entities_root = map_data.get("entities")
    if not isinstance(entities_root, list):
        return []

    result: list[MapEntity] = []
    for bucket in entities_root:
        if not isinstance(bucket, dict):
            continue

        proto_id = bucket.get("proto")
        if not isinstance(proto_id, str):
            proto_id = ""

        entities = bucket.get("entities")
        if not isinstance(entities, list):
            continue

        for entity in entities:
            if not isinstance(entity, dict):
                continue
            uid = entity.get("uid")
            components = entity.get("components")
            if not isinstance(uid, int) or not isinstance(components, list):
                continue

            comp_map: dict[str, dict[str, Any]] = {}
            for component in components:
                if not isinstance(component, dict):
                    continue
                comp_type = component.get("type")
                if isinstance(comp_type, str):
                    comp_map[comp_type] = component

            result.append(MapEntity(uid=uid, proto_id=proto_id, components=comp_map))

    return result


def load_entity_prototypes(repo_root: Path) -> dict[str, dict[str, Any]]:
    prototypes_dir = repo_root / "Resources" / "Prototypes"
    raw: dict[str, dict[str, Any]] = {}

    for path in prototypes_dir.rglob("*.yml"):
        try:
            data = load_yaml(path)
        except Exception:
            continue

        if not isinstance(data, list):
            continue

        for item in data:
            if not isinstance(item, dict):
                continue
            if item.get("type") != "entity":
                continue
            proto_id = item.get("id")
            if isinstance(proto_id, str):
                raw[proto_id] = item

    resolved: dict[str, dict[str, Any]] = {}
    resolving: set[str] = set()

    def resolve(proto_id: str) -> dict[str, Any]:
        if proto_id in resolved:
            return resolved[proto_id]
        if proto_id in resolving:
            raise ValueError(f"Cyclic entity prototype inheritance detected for {proto_id}")

        source = raw.get(proto_id)
        if source is None:
            raise KeyError(proto_id)

        resolving.add(proto_id)
        merged: dict[str, Any] = {}
        parents = source.get("parent")
        parent_ids = []
        if isinstance(parents, str):
            parent_ids = [parents]
        elif isinstance(parents, list):
            parent_ids = [parent for parent in parents if isinstance(parent, str)]

        for parent_id in parent_ids:
            if parent_id in raw:
                merged = deep_merge(merged, resolve(parent_id))

        merged = deep_merge(merged, source)
        merged["components_by_type"] = components_to_map(merged.get("components"))
        resolving.remove(proto_id)
        resolved[proto_id] = merged
        return merged

    for proto_id in list(raw):
        resolve(proto_id)

    return resolved


def deep_merge(base: Any, override: Any) -> Any:
    if isinstance(base, dict) and isinstance(override, dict):
        result = {key: copy.deepcopy(value) for key, value in base.items()}
        for key, value in override.items():
            if key == "components":
                result[key] = merge_components(result.get(key), value)
            elif key in result:
                result[key] = deep_merge(result[key], value)
            else:
                result[key] = copy.deepcopy(value)
        return result

    if isinstance(override, list):
        return copy.deepcopy(override)

    return copy.deepcopy(override)


def merge_components(base: Any, override: Any) -> list[Any]:
    result = components_to_map(base)
    incoming = components_to_map(override)
    for comp_type, value in incoming.items():
        if comp_type in result:
            result[comp_type] = deep_merge(result[comp_type], value)
        else:
            result[comp_type] = copy.deepcopy(value)
    return list(result.values())


def components_to_map(components: Any) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    if not isinstance(components, list):
        return result
    for component in components:
        if not isinstance(component, dict):
            continue
        comp_type = component.get("type")
        if isinstance(comp_type, str):
            result[comp_type] = copy.deepcopy(component)
    return result


def render_grid(
    grid: GridData,
    tilemap: dict[Any, Any],
    map_entities: list[MapEntity],
    entity_prototypes: dict[str, dict[str, Any]],
    tile_defs: dict[str, TileDef],
    sprite_cache: dict[tuple[str, int], Image.Image],
    rsi_meta_cache: dict[str, dict[str, Any]],
    repo_root: Path,
    background: tuple[int, int, int, int],
) -> Image.Image:
    normalized_tilemap = {int(key): str(value) for key, value in tilemap.items()}
    decoded_tiles: dict[tuple[int, int], DecodedTile] = {}

    for chunk_key, chunk_data in grid.chunks.items():
        if not isinstance(chunk_data, dict):
            continue

        ind_value = chunk_data.get("ind", chunk_key)
        chunk_x, chunk_y = parse_vec2i(ind_value)
        chunk_size = int(chunk_data.get("size", CHUNK_DEFAULT_SIZE))
        chunk_blob = chunk_data.get("tiles")
        version = int(chunk_data.get("version", 1))
        if not isinstance(chunk_blob, str):
            continue

        for local_x, local_y, tile in decode_chunk_tiles(chunk_blob, chunk_size, version, normalized_tilemap):
            world_x = chunk_x * chunk_size + local_x
            world_y = chunk_y * chunk_size + local_y
            if tile.proto_id == "Space":
                continue
            decoded_tiles[(world_x, world_y)] = tile

    if not decoded_tiles:
        return Image.new("RGBA", (1, 1), background)

    xs = [pos[0] for pos in decoded_tiles]
    ys = [pos[1] for pos in decoded_tiles]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)

    width = (max_x - min_x + 1) * TILE_SIZE
    height = (max_y - min_y + 1) * TILE_SIZE
    canvas = Image.new("RGBA", (width, height), background)

    for (tile_x, tile_y), tile in sorted(decoded_tiles.items(), key=lambda item: (item[0][1], item[0][0])):
        tile_def = tile_defs.get(tile.proto_id)
        if tile_def is None or tile_def.sprite is None:
            paste_missing_tile(canvas, tile_x, tile_y, min_x, max_y)
            continue

        tile_image = get_tile_variant(tile_def, tile.variant, sprite_cache, repo_root)
        tile_image = transform_tile(tile_image, tile.rotation_mirroring)
        dest_x = (tile_x - min_x) * TILE_SIZE
        dest_y = (max_y - tile_y) * TILE_SIZE
        canvas.alpha_composite(tile_image, (dest_x, dest_y))

    render_entities = build_render_entities(grid.uid, map_entities, entity_prototypes)
    paint_entities(canvas, render_entities, min_x, max_y, sprite_cache, rsi_meta_cache, repo_root)

    return canvas


def build_render_entities(
    grid_uid: int,
    map_entities: list[MapEntity],
    entity_prototypes: dict[str, dict[str, Any]],
) -> list[RenderEntity]:
    entity_by_uid = {entity.uid: entity for entity in map_entities}
    transforms: dict[int, TransformData] = {}

    for entity in map_entities:
        transform = entity.components.get("Transform", {})
        transforms[entity.uid] = TransformData(
            parent=parse_parent_uid(transform.get("parent")),
            x=parse_float_pair(transform.get("pos"), 0),
            y=parse_float_pair(transform.get("pos"), 1),
            rotation=parse_radians(transform.get("rot")),
        )

    world_cache: dict[int, tuple[float, float, float, int | None]] = {}

    def resolve_world(uid: int) -> tuple[float, float, float, int | None]:
        cached = world_cache.get(uid)
        if cached is not None:
            return cached

        transform = transforms.get(uid, TransformData(parent=None, x=0.0, y=0.0, rotation=0.0))
        parent = transform.parent
        if parent is None or parent not in transforms:
            result = (transform.x, transform.y, transform.rotation, parent)
        else:
            parent_x, parent_y, parent_rot, root_parent = resolve_world(parent)
            cos_r = math.cos(parent_rot)
            sin_r = math.sin(parent_rot)
            world_x = parent_x + transform.x * cos_r - transform.y * sin_r
            world_y = parent_y + transform.x * sin_r + transform.y * cos_r
            result = (world_x, world_y, parent_rot + transform.rotation, root_parent if root_parent is not None else parent)

        world_cache[uid] = result
        return result

    renderables: list[RenderEntity] = []
    for entity in map_entities:
        if not entity.proto_id:
            continue
        prototype = entity_prototypes.get(entity.proto_id)
        if prototype is None:
            continue

        sprite = prototype.get("components_by_type", {}).get("Sprite")
        if not isinstance(sprite, dict):
            continue

        _, _, _, root_parent = resolve_world(entity.uid)
        if root_parent != grid_uid:
            continue

        x, y, rotation, _ = resolve_world(entity.uid)
        renderables.append(
            RenderEntity(
                uid=entity.uid,
                proto_id=entity.proto_id,
                x=x,
                y=y,
                rotation=rotation,
                sprite=sprite,
            )
        )

    renderables.sort(key=lambda entity: (sprite_depth(entity.sprite), entity.y, entity.x, entity.uid))
    return renderables


def parse_parent_uid(value: Any) -> int | None:
    if isinstance(value, int):
        return value
    if isinstance(value, str):
        if value == "invalid":
            return None
        try:
            return int(value)
        except ValueError:
            return None
    return None


def parse_float_pair(value: Any, index: int) -> float:
    if isinstance(value, str):
        left, right = value.split(",", 1)
        return float((left, right)[index])
    if isinstance(value, (list, tuple)) and len(value) == 2:
        return float(value[index])
    return 0.0


def parse_radians(value: Any) -> float:
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str):
        return float(value.replace(" rad", ""))
    return 0.0


def sprite_depth(sprite: dict[str, Any]) -> float:
    value = sprite.get("drawdepth", 0)
    if isinstance(value, (int, float)):
        return float(value)
    return 0.0


def paint_entities(
    canvas: Image.Image,
    entities: list[RenderEntity],
    min_x: int,
    max_y: int,
    sprite_cache: dict[tuple[str, int], Image.Image],
    rsi_meta_cache: dict[str, dict[str, Any]],
    repo_root: Path,
) -> None:
    for entity in entities:
        sprite = entity.sprite
        if sprite.get("visible") is False:
            continue

        for layer in iter_sprite_layers(sprite):
            if layer.get("visible") is False:
                continue

            image, size_x, size_y = load_layer_image(
                sprite,
                layer,
                entity.rotation,
                sprite_cache,
                rsi_meta_cache,
                repo_root,
            )
            if image is None:
                continue

            tint = multiply_colors(sprite.get("color"), layer.get("color"))
            if tint is not None:
                image = apply_tint(image, tint)

            offset_x, offset_y = parse_offset(sprite.get("offset"))
            dest_x = int(round((entity.x - min_x) * TILE_SIZE + offset_x * TILE_SIZE - size_x / 2))
            dest_y = int(round((max_y - entity.y) * TILE_SIZE - offset_y * TILE_SIZE - size_y / 2))
            canvas.alpha_composite(image, (dest_x, dest_y))


def iter_sprite_layers(sprite: dict[str, Any]) -> list[dict[str, Any]]:
    layers = sprite.get("layers")
    if isinstance(layers, list) and layers:
        return [layer for layer in layers if isinstance(layer, dict)]

    implicit = {}
    if isinstance(sprite.get("sprite"), str):
        implicit["sprite"] = sprite["sprite"]
    if isinstance(sprite.get("state"), str):
        implicit["state"] = sprite["state"]
    return [implicit]


def load_layer_image(
    sprite: dict[str, Any],
    layer: dict[str, Any],
    world_rotation: float,
    sprite_cache: dict[tuple[str, int], Image.Image],
    rsi_meta_cache: dict[str, dict[str, Any]],
    repo_root: Path,
) -> tuple[Image.Image | None, int, int]:
    sprite_path = layer.get("sprite", sprite.get("sprite"))
    if not isinstance(sprite_path, str):
        return None, TILE_SIZE, TILE_SIZE

    state = layer.get("state", sprite.get("state"))
    if not isinstance(state, str):
        state = "base"

    rsi_path = sprite_path
    if not rsi_path.endswith(".rsi"):
        return None, TILE_SIZE, TILE_SIZE

    meta = load_rsi_meta(rsi_path, rsi_meta_cache, repo_root)
    if meta is None:
        return None, TILE_SIZE, TILE_SIZE

    size = meta.get("size", {})
    size_x = int(size.get("x", TILE_SIZE))
    size_y = int(size.get("y", TILE_SIZE))
    state_meta = next((item for item in meta.get("states", []) if isinstance(item, dict) and item.get("name") == state), None)
    if state_meta is None:
        return make_missing_sprite((size_x, size_y)), size_x, size_y

    directions = int(state_meta.get("directions", 1))
    direction_index = rotation_to_direction_index(world_rotation, directions, sprite.get("snapCardinals"))
    frame_count = len(state_meta.get("delays", [[]])[0]) if isinstance(state_meta.get("delays"), list) and state_meta.get("delays") else 1
    cache_key = (f"{rsi_path}/{state}", direction_index)
    cached = sprite_cache.get(cache_key)
    if cached is not None:
        return cached.copy(), size_x, size_y

    image_path = repo_root / "Resources" / "Textures" / rsi_path.replace("/", os.sep) / f"{state}.png"
    if not image_path.exists():
        return make_missing_sprite((size_x, size_y)), size_x, size_y

    sheet = Image.open(image_path).convert("RGBA")
    states_x = max(sheet.width // size_x, 1)
    total_frames = max((sheet.width // size_x) * (sheet.height // size_y), 1)
    frame_count = max(min(frame_count, total_frames // max(directions, 1) or 1), 1)
    target = min(direction_index, max(directions - 1, 0)) * frame_count
    target_y = target // states_x
    target_x = target % states_x
    left = target_x * size_x
    top = target_y * size_y
    cropped = sheet.crop((left, top, left + size_x, top + size_y)).copy()
    sprite_cache[cache_key] = cropped
    return cropped.copy(), size_x, size_y


def load_rsi_meta(rsi_path: str, rsi_meta_cache: dict[str, dict[str, Any]], repo_root: Path) -> dict[str, Any] | None:
    cached = rsi_meta_cache.get(rsi_path)
    if cached is not None:
        return cached

    meta_path = repo_root / "Resources" / "Textures" / rsi_path.replace("/", os.sep) / "meta.json"
    if not meta_path.exists():
        return None

    with meta_path.open("r", encoding="utf-8-sig") as handle:
        meta = json.load(handle)
    rsi_meta_cache[rsi_path] = meta
    return meta


def rotation_to_direction_index(rotation: float, directions: int, snap_cardinals: Any) -> int:
    if directions <= 1:
        return 0
    if directions == 4 or snap_cardinals:
        quarter_turns = int(round(rotation / (math.pi / 2.0))) % 4
        return quarter_turns
    return 0


def parse_offset(value: Any) -> tuple[float, float]:
    if isinstance(value, str):
        left, right = value.split(",", 1)
        return float(left), float(right)
    if isinstance(value, (list, tuple)) and len(value) == 2:
        return float(value[0]), float(value[1])
    if isinstance(value, dict):
        return float(value.get("x", 0.0)), float(value.get("y", 0.0))
    return 0.0, 0.0


def multiply_colors(sprite_color: Any, layer_color: Any) -> tuple[int, int, int, int] | None:
    colors = []
    for value in (sprite_color, layer_color):
        color = parse_color(value)
        if color is not None:
            colors.append(color)

    if not colors:
        return None

    out = [255, 255, 255, 255]
    for color in colors:
        for i in range(4):
            out[i] = (out[i] * color[i]) // 255
    return tuple(out)


def parse_color(value: Any) -> tuple[int, int, int, int] | None:
    if isinstance(value, str):
        try:
            return ImageColor.getcolor(value, "RGBA")
        except ValueError:
            return None
    return None


def apply_tint(image: Image.Image, color: tuple[int, int, int, int]) -> Image.Image:
    overlay = Image.new("RGBA", image.size, color)
    return ImageChops.multiply(image, overlay)


def make_missing_sprite(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", size, (255, 0, 255, 180))


def parse_vec2i(value: Any) -> tuple[int, int]:
    if isinstance(value, str):
        left, right = value.split(",", 1)
        return int(left), int(right)
    if isinstance(value, (list, tuple)) and len(value) == 2:
        return int(value[0]), int(value[1])
    raise ValueError(f"Unsupported Vector2i value: {value!r}")


def decode_chunk_tiles(
    encoded: str,
    chunk_size: int,
    version: int,
    tilemap: dict[int, str],
):
    payload = base64.b64decode(encoded)
    stream = io.BytesIO(payload)

    if version >= 7:
        fmt = "<iBBB"
    elif version >= 6:
        fmt = "<iBB"
    else:
        fmt = "<HBB"

    struct_size = struct.calcsize(fmt)
    expected_size = chunk_size * chunk_size * struct_size
    if len(payload) != expected_size:
        raise ValueError(
            f"Chunk payload size mismatch: got {len(payload)}, expected {expected_size}"
        )

    for local_y in range(chunk_size):
        for local_x in range(chunk_size):
            raw = stream.read(struct_size)
            if version >= 7:
                yaml_id, flags, variant, rotation_mirroring = struct.unpack(fmt, raw)
            else:
                yaml_id, flags, variant = struct.unpack(fmt, raw)
                rotation_mirroring = 0

            proto_id = tilemap.get(yaml_id, f"__unknown_{yaml_id}")
            yield local_x, local_y, DecodedTile(
                proto_id=proto_id,
                flags=flags,
                variant=variant,
                rotation_mirroring=rotation_mirroring,
            )


def get_tile_variant(
    tile_def: TileDef,
    variant: int,
    sprite_cache: dict[tuple[str, int], Image.Image],
    repo_root: Path,
) -> Image.Image:
    sprite_path = repo_root / "Resources" / tile_def.sprite.lstrip("/").replace("/", os.sep)
    if not sprite_path.exists():
        raise FileNotFoundError(f"Sprite file does not exist: {sprite_path}")

    index = variant % max(tile_def.variants, 1)
    cache_key = (str(sprite_path), index)
    cached = sprite_cache.get(cache_key)
    if cached is not None:
        return cached

    sheet = Image.open(sprite_path).convert("RGBA")
    if sheet.height != TILE_SIZE:
        raise ValueError(f"Unexpected tile height in {sprite_path}: {sheet.height}")

    if sheet.width < TILE_SIZE:
        raise ValueError(f"Unexpected tile width in {sprite_path}: {sheet.width}")

    left = index * TILE_SIZE
    if tile_def.variants <= 1 or left + TILE_SIZE > sheet.width:
        left = 0

    tile = sheet.crop((left, 0, left + TILE_SIZE, TILE_SIZE)).copy()
    sprite_cache[cache_key] = tile
    return tile


def transform_tile(image: Image.Image, rotation_mirroring: int) -> Image.Image:
    transformed = image.copy()
    rotation = rotation_mirroring % 4
    mirrored = rotation_mirroring > 3

    if rotation == 1:
        transformed = transformed.rotate(-90, expand=False)
    elif rotation == 2:
        transformed = transformed.rotate(180, expand=False)
    elif rotation == 3:
        transformed = transformed.rotate(-270, expand=False)

    if mirrored:
        transformed = ImageOps.mirror(transformed)

    return transformed


def paste_missing_tile(canvas: Image.Image, tile_x: int, tile_y: int, min_x: int, max_y: int) -> None:
    dest_x = (tile_x - min_x) * TILE_SIZE
    dest_y = (max_y - tile_y) * TILE_SIZE
    missing = Image.new("RGBA", (TILE_SIZE, TILE_SIZE), (255, 0, 255, 160))
    canvas.alpha_composite(missing, (dest_x, dest_y))


if __name__ == "__main__":
    raise SystemExit(main())
