# Python Map Renderer

Лёгкий рендерер карт SS14/StarHorizon на Python, который читает:

- map-файлы из `Resources/Maps/...`
- prototype-файлы с `mapPath` или `shuttlePath`, например `Resources/Prototypes/_NF/Shipyard/akupara.yml`

Что умеет сейчас:

- берёт tile definitions из `Resources/Prototypes`
- берёт PNG тайлов из `Resources/Textures`
- декодирует `MapGrid` чанки из map YAML
- подтягивает entity prototypes с наследованием `parent`
- рендерит базовые `Sprite` и `Sprite.layers` из `.rsi`
- учитывает `Transform`, `rot`, `parent`, `state`, `sprite`, `offset`, `color`
- рендерит каждый grid в отдельный PNG

Что пока не рендерит:

- декали
- освещение и параллакс
- сложные runtime-визуализаторы и appearance-driven состояния один в один с клиентом

## Зависимости

Нужны `PyYAML` и `Pillow`.

## Примеры

Рендер по map-файлу:

```powershell
python Tools/map_renderer_py/render_map.py Resources/Maps/_NF/Shuttles/akupara.yml
```

По умолчанию PNG уйдут во временную папку пользователя, чтобы рендер работал даже если репозиторий лежит в защищённой директории вроде `Program Files`.

Рендер по shipyard/game-map prototype:

```powershell
python Tools/map_renderer_py/render_map.py Resources/Prototypes/_NF/Shipyard/akupara.yml
```

Явная папка вывода:

```powershell
python Tools/map_renderer_py/render_map.py Resources/Prototypes/_NF/Shipyard/akupara.yml -o out/akupara
```

Непрозрачный фон:

```powershell
python Tools/map_renderer_py/render_map.py Resources/Prototypes/_NF/Shipyard/akupara.yml --background "#111111"
```
