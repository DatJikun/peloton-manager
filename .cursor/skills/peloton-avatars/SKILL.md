---
name: peloton-avatars
description: "Build, extend or restyle the Peloton Manager rider avatar system: deterministic layered portraits composed from PNG asset packs. Use when adding or editing avatar assets (heads, hair, beards, kits, helmets), changing the art style, adding jersey overrides, touching the trait generator, or when the owner gives feedback on how the portraits look."
---

# Peloton Manager — rider avatars

Deterministic, layered portrait system. A rider's appearance is **computed from his
database row**, and the portrait is **composited in game code from PNG layers**. No AI
image call ever happens at runtime; AI (if used at all) only produces the asset library.

Everything lives in `experiments/avatar_prototype/` (status: EXPERIMENT, Python). Read
`experiments/avatar_prototype/DESIGN_SKETCH.md` for the full design before changing
anything structural; this skill is the operating manual.

## The owner's taste, in one paragraph

The approved look is the **`poster` style profile**: constructivist poster / comic, ink
keylines about 4 px on a 512 px master, two flat tones, no highlights, almost no skin
detail, nose and lips as line work, everyone slightly smiling. It exists because the
owner rejected softer, more painted passes with: *"próbuje być zbyt realistycznym
względem funkcjonalności po prostu jako awatar, zwłaszcza patrząc na ui w grze"*. An
avatar here is a **UI element that must read at 48 px**, not a portrait. He also
rejected a featureless-face variant (`09-avatar-lab.html`, deleted) as "ohydne", so do
not remove facial features either. When in doubt: more graphic, never more photographic.

Locked answers you do not need to ask again:

| Question | Answer |
|---|---|
| view | front-facing only (lets eye/brow/ear assets be mirrored) |
| helmet in the portrait | off by default; helmet and glasses are optional layers |
| peloton | male riders + manager outfits; no women's pack, no historical eras |
| size in the UI | rider card up to ~1/6 of a laptop page; list icons use `head_crop` |
| expression | closed mouths keep a hint of smile (a straight line reads sullen); **open / laugh / high / low / thick / thin are identity variants** the owner asked for, not a second emotion system |
| kits | team kits + Tour (yellow) / Giro (pink) / Vuelta (red) GC leader, world, national |

## Master reference — immutable

Every asset in a pack shares one framing. It is defined once in
`avatarlab/bake/draw.py` and copied into `manifest.canvas`, so the runtime can assert it.

```text
canvas 512x512 RGBA, transparent background, front-facing head and shoulders
centre line x = 256          head half width 109      (head 218 x 280, ratio 0.78)
skull top  y = 72            brow line  y = 186
EYE LINE   y = 204  <- composite pivot, never moves
nose tip   y = 268           mouth line y = 303       chin y = 352
single-eye anchor x = 256 + 47
neck top   y = 330           shoulder line y = 456    torso half width 246
head_crop  (96, 44, 416, 364)  <- square crop for 48-96 px UI icons
light: key from upper-left, ~35 degrees elevation
```

Changing any of these is an art-direction reset that invalidates the whole pack. Do not
do it to fix one asset. Use `hy(f)` (fraction of head height) and `HEAD_HW` in recipes so
geometry survives a future re-scale.

## How a portrait is built

```text
appearance = generate(rider, manifest)      # pure function of the rider row
portrait   = render(appearance, pack)       # PNG layer compositing, ~20 ms in Python
```

Layer order comes from `manifest.layer_order`:

```text
neck, jersey, jersey_overlay, ears, head, nose, mouth, eyes, eyebrows,
skin_details, wrinkles, facial_hair, hair, glasses, helmet
```

An **asset** is a set of **parts**; each part is one PNG plus:

- `blend`: `normal` | `multiply` | `screen`
- `color_slot`: `skin` | `lip` | `hair` | `brow` | `facial_hair` | `iris` |
  `team_primary` | `team_secondary` | `team_accent` — the PNG is greyscale and tinted at
  composite time, so one asset covers every colour
- `opacity_from`: a continuous appearance value that drives alpha (e.g. `wrinkle_strength`)

Part order inside an asset matters: a cast shadow goes before the fill, a keyline after it.

Three mechanisms create variety from ~120 assets:

1. discrete assets (which head / nose / hair PNG)
2. colour slots (continuous skin tone, hair colour, team kit)
3. continuous per-feature affines (`shape` block: `face_width`, `eye_spacing`, ...)

## Rules that must never break

- **Identity is permanent.** `identity` + `shape` are frozen for a rider's whole career.
  `mutable` (hair, gray, wrinkles, beard) is derived from age; `equipment` from team and
  results. A transfer or a birthday may never move a skull, nose, eye or ear.
- **Domain-separated streams.** Every trait draws from `rng.stream("<domain>")`. Adding a
  category later must not shift existing draws. Never use `random` or a shared stream.
- **Salt only moves secondary traits.** Clone resolution re-rolls with `salt += 1`, and
  only `salted=True` streams (hair, hair colour, beard) see it.
- **`seed_version` bump changes every face.** It is an opt-in migration, never a fix.
- **Region is probabilistic only.** `region_weights` may tilt a distribution and bias the
  skin-tone mean. Nothing may be assigned deterministically from nationality, and every
  option stays reachable from every region.
- **Aging is monotonic.** Wrinkles, gray and hairline recession never decrease with age.
- **Adding an asset must not move the riders who keep their old one.** See the weights note
  below; `selftest.py` asserts that 0 riders swap between two old assets.
- **No new asset may dead-end generation.** There must always be at least one legal hair
  option for every age x hairline-state combination (the validator checks this).

## The gate — run all four, in order

```bash
cd experiments/avatar_prototype
python3 scripts/bake_pack.py poster      # or `all` for every style, ~30 s per style
python3 scripts/validate_pack.py         # size / alpha / alignment / manifest rules
python3 scripts/selftest.py poster       # 36 behavioural assertions
python3 scripts/render_demo.py poster    # review sheets into out/demo/
```

Never hand over a pack that fails `validate_pack.py`, and never claim a visual result
without looking at the rendered sheets. Copy the sheets you actually judged into
`demo/` when the change is meant to be reviewed by the owner.

What each sheet answers:

| Sheet | Question |
|---|---|
| `01_contact_sheet` | do 40 riders look like 40 people, or one person in wigs? |
| `02_aging` | is the 19 and the 44 year old still the same man? |
| `03_teams` | do kit and GC leader jerseys change only the kit? |
| `04_equipment` | do helmet and glasses sit correctly? |
| `05_trait_variants` | accept/reject sheet: one base rider, one trait swapped |
| `06_skin_and_hair` | do skin tone and hair colour slots look natural? |
| `07_styles` | style comparison across profiles |
| `08_display_sizes` | does it read at 380 / 180 / 96 / 48 px? |
| `09_managers` | do managers in civilian torsos work? |

Sheets render on the UI palette (paper `#f3ede1`, ink `#0c0c0d`, red `#d11f1f`, bordered
cards) because judging a portrait on a dark background says nothing about a light UI.

## Recipes — how to add things

All recipes are plain tables in `avatarlab/bake/pack.py`. A new asset is a few numbers,
never free-hand drawing.

**A hairstyle** (`HAIR_RECIPES`): `t` = thickness, `hl` = hairline as a fraction of head
height (smaller = higher forehead), `side` = how far down the sides reach, `style` = one of
`straight`, `round`, `swept`, `m_shape`, `quiff`, `fringe`, `undercut`, `mid_part`,
`wob` = `("curl"|"spike", amount)`.

```python
{"id": "hair_26_short_wave", "w": 0.06, "t": 13.0, "hl": 0.22, "side": 0.50,
 "style": "swept", "wob": ("curl", 5.0)}
```

Gate a style on state with `requires`/`excludes`/`min_age`/`max_age`, e.g. a receding cut
uses `"requires": ("hairline_receded",)`. Receded riders still need live hair
(`hair_12_receded`, `hair_32_combover`) after horseshoe retirement. Short crops must
dominate. Radical silhouettes (cartoon spikes, horseshoe, bowl, mullet, pompadour, flat
top, long swept, afro puff) stay at `weight: 0`. Soften tall curls in place; do not
retire every curl. Append mild peloton cuts (`hair_41`…`hair_50`) rather than restoring
retired ids.

**A head** (`HEAD_RECIPES`): multipliers around 1.0 on `cranium_w`, `temple_w`, `cheek_w`,
`jaw_w`, `chin_w`, `crown_w`, plus `crown` and `chin_len` in px. Tag it `jaw_narrow` /
`jaw_medium` / `jaw_wide` so beards and hair can react. Extreme values (chin_w below
~0.75, jaw_w above ~1.16, chin_len beyond ±12) read as caricature; soften in place
(pack version bump), do not add a more extreme head. Heart / diamond / lantern / block /
oblong / squat / needle-chin recipes stay at `weight: 0`. Variety comes from **mild
neighbours** (`head_19`…`head_26`: soft oval/square/round, narrow/wide/short/tall oval,
soft angular), not from those caricatures. Live cluster stays around oval.

**Eyes** (`EYE_RECIPES`): keep a **neutral-open** cluster as the common weight. Live `hw` is **21.6–22.0** (baker cap 22). Aperture sits **just under** the 0.11.0 mid: live **th 11.3–14.0 / bh 9.6–11.7** — a small relax from the remaining shocked look, not a return to the 0.8.0 slits. Do **not** floor every recipe to the tall band — that made the whole peloton look startled. Outer lid ~0.76 `th`. Brows sit on `BROW_Y` (raised brows read as surprise). Default `iris_dy` **2.0** so the lid covers the top of the iris. Affine `scale_x` 0.97–1.02, `scale_y` **0.97–1.02**, pair `dx` ±5. **Iris and pupil must differ:** live `iris_r` **8.6–12.4**, `pupil` fraction **0.28–0.56** (baker clamps 8.4–12.6 / 0.26–0.58). A peloton of 11.0 irises with pupil 0.42 reads as one sticker. Feature keyline scale **0.76**. Retired ids keep `weight: 0`.

**Mouths** (`MOUTH_RECIPES`): corners sit **under the pupils** (`hw` around `EYE_DX` = 47, never above it). Lip thickness sits **a step under** the 0.12 midpoint: baker scale ~1.08 / 1.10, ~16% meat at the corners. Not a thread (1.0) and not a sausage (1.32). Affine `scale_y` **0.98–1.02**. Lip fill must still contrast with skin (`lip_rgb`). Variety is bow, smile, open/laugh, lift, skew, and upper/lower ratio, not more length. Open mouths still wrap the opening in lip meat. Closed-lip keyline scale **0.78**, open envelope / opening **0.76 / 0.55**. Retired ids keep `weight: 0`. Do not ship thread-wide mouths (`hw` above ~47, or closed `upper`/`lower` under ~10/12) or sausage mouths (baker scale above ~1.2 with 40% blunt corners).

**Brows** (`BROW_RECIPES`): recipes must differ in **inner gap, length, thickness taper,
arch and peak**, not just `th`/`arch` on the same polygon. `bake_brow` takes `inner` /
`outer` (fractions of `HEAD_HW`), `inner_th` / `outer_th`, `peak_t`, `drop`, `angle`.
High `drop` (negative) plus high `arch` plus affine `brow_height` of -9 px stacked into
a surprised caricature with the eyes. Live recipes keep `drop` ≥ 0 and `arch` ≤ 8.5
(baker clamps the same). Affine `brow_height` is **-2..+4** extra on top of following
the eyes, biased toward sitting closer to the lid, not flying up the forehead. Soft fill
stays `crisp=False`; a modest keyline on the crisp silhouette (scale **0.52**) keeps the
brow from melting into the forehead.

**Noses** (`NOSE_RECIPES`): bake a **skin silhouette** (`nose_contour`) plus keyline and
nostril ticks. Multiply-only ticks all look identical at portrait size. Vary `bridge`,
`tip`, `flare`, `len`, `hook`, `upturn`, `bulb`. Feature keyline scale **0.76** so the
nose does not dissolve into the head skin.

**Necks** (`NECK_RECIPES`): five discrete widths, picked on stream `identity.neck` so
other identity draws do not shift. Keep `neck_01` as the medium recipe. Continuous
`shape.neck_thickness` is a tiny affine on top (~0.97–1.03), not the source of variety.

**Facial hair** (`FACIAL_RECIPES`): `cov` = `full`/`short`/`chin`/`strap`/`moustache`,
`alpha`, `soft`, `min_age`, and `tags: ("beard_dense",)` for anything that should suppress
the shaved-stubble skin detail.

**A jersey override** (classification kit): add a key to `JERSEY_OVERRIDES` in
`avatarlab/render.py` with `team_primary` / `team_secondary` / `team_accent`, and a band
overlay in `BAND_OVERLAYS` if it needs stripes. Keep old names working via
`OVERRIDE_ALIASES` — a save written earlier must still resolve.

**A team**: `TEAMS` in `pack.py` (`primary`, `secondary`, `accent`, `nation_colors`).

**Weights are relative, not probabilities.** Adding an asset does not require renormalising
the others, and it must not disturb the riders who keep their old asset. Selection is an
exponential race over per-asset hashed draws (`weighted_pick` in `generate.py`): appending
weight `w` moves only `w / (W + w)` of the pool, all of it to the new asset. Never replace
this with a cumulative-weight walk - that reshuffles existing faces on every pack update.
Never compute the Exp(1) key with libm `log`; use `neg_log2_q32`, because the game and the
tools must agree bit for bit.

Common traits must stay common: check `out/demo/report.txt` after a change (short crops
should dominate hairstyles, ~70 % of riders have no facial hair).

## Changing the style

Never restyle by editing individual recipes. Style is one dataclass, `StyleProfile` in
`avatarlab/bake/draw.py`, and every recipe reads it:

| field | effect |
|---|---|
| `tone_steps` / `tone_floor` | 0 = smooth gradients, 2-3 = cel shading and how dark the second tone is |
| `form_strength` / `highlight_strength` | global multipliers on all shading |
| `gradient_scale` | compresses full-canvas gradients (they band when posterised) |
| `edge_hardness` | soft painted falloff vs crisp vector edge |
| `line_art` / `outline` | ink keyline width / darker-tint inner outline |
| `detail_alpha` | wrinkles, tan lines, freckles intensity |
| `line_features` | nose and lips drawn as line work |
| `feature_boost` | scales eyes and brows for a more graphic read |

The approved `poster` values: `tone_steps=2`, `tone_floor=0.78`, `form_strength=0.60`,
`highlight_strength=0.0`, `edge_hardness=1.0`, `gradient_scale=0.16`, `detail_alpha=0.22`,
`line_art=5.0`, `feature_boost=1.10`, `line_features=True`.

The owner locked this look on 2026-08-26 (*"tamten styl graficzny był idealny"*). Do not
offer restyles. Variety comes from **more face recipes** (heads, hair, mouths, noses,
ears, brows, necks), never from a new `StyleProfile`.

To add a look the owner asked for, append recipes in `pack.py`. Do not silently change
`poster`. Unused neighbour profiles (`poster_thin` / `poster_cut` / `poster_comic` /
`poster_stencil`) may stay in `STYLES` for history; they are not the default.

## Art traps already paid for — do not rediscover them

- **Do not harden every alpha.** Stubble and eyebrows are soft on purpose; posterising
  and hardening them turns a gradient into an amoeba. Use `gray_layer(..., crisp=False)`.
- **Do not posterise a full-canvas gradient.** It bands across the whole face. A flat
  style needs a small number of deliberate shading shapes, not many soft ones.
- **Fake texture reads as damage.** Random ink strokes meant to suggest hair strands look
  like scratches at portrait size; one deliberate second-tone shape reads as hair.
- **Keylines go inside the silhouette** (`keyline()`), never centred on the edge: an outer
  stroke overlaps neighbouring layers and drifts when a continuous parameter scales.
  Feature keyline scale **0.55** (eyes/nose) melts the part into skin at 48 px; full
  **1.0** after the 0.12 pass reads a bit heavy. Current poster scales: head / hair /
  jersey / helmet **0.82**, ears **0.80**, eyes / nose / open envelope **0.76**, closed
  mouth **0.78**, open inner **0.55**, brows **0.52**, neck **0.78**. Do not answer
  "smaller contours" by changing `poster` `line_art`. The contour part is named
  `keyline`, never `line`: a later lip seam or nostril tick also called `line`
  silently overwrites the PNG and the face parts melt together again.
- **A ring is jewellery.** A full collar ellipse around the neck reads as a necklace; keep
  the front arc only.
- **Small dark shapes near the lips read as an open mouth.** A moustache must be wider
  than tall and sit clear of the lip line.
- **Downscaling wastes pixels.** For icons under ~120 px crop with `render.crop_head()`
  first, then resize.
- **Slit eyes and thread mouths flatten the peloton.** If a pass looks "all the same",
  check `th`/`bh` and lip `upper`/`lower` before adding more hair. Mouth width past the
  pupils (`hw` > `EYE_DX`) reads as a billboard; a philtrum collapsed to `upper * 0.22`
  reads as a thread no matter what the recipe says. Eye `hw` above ~22, or an affine
  that stretches `scale_x` past 1.03 / pushes the pair ±9 px toward the temples, also
  reads as a billboard. A compact `hw` with `th`/`bh` under ~12/10 reads as a
  flattened slot, which the owner rejected after the width pass. Opening `th`/`bh`
  into the 15–16 band, raising the brows 8 px, and flooring every recipe to that
  tall band makes the whole peloton look startled. Meet in the middle, then a
  small further relax: mid-under aperture, brows on `BROW_Y`, `iris_dy` ~2.0 so
  the lid covers the iris. Shocked leftover is too much sclera above the iris /
  too little `iris_dy`, not a missing slit. Do not answer "too wide" by collapsing
  into slits, "too flat" by stretching `hw`, or "too shocked" by shrinking back
  to the 0.8.0 slits. Same `iris_r` (~11) and pupil fraction (0.42) on every
  recipe makes the peloton one sticker; vary both (`iris_r` 8.6–12.4, `pupil`
  0.28–0.56) without opening the lids back up.
- **A nose without a silhouette is invisible.** Multiply ticks at the nostrils do not
  carry width, length or hook. Give the nose a skin mask and a keyline.
- **Same-polygon brows stay identical.** Changing only `th` by 2 px does not read; move
  inner gap, outer length, taper and arch peak. A high `drop` (negative) plus a tall
  `arch` plus affine `brow_height` of -9 px parks the brow on the forehead and reads
  as a shocked caricature next to any eye. Keep `drop` ≥ 0, `arch` ≤ 8.5, and the
  extra brow affine a couple of pixels either way.

## Cache and versioning

```text
avatar-cache/{rider_id}_{asset_pack_version}_{blake2b8(appearance)}.png
```

The key covers schema/pack/seed versions, rider id, salt and all four appearance blocks.
A birthday inside the same `age_stage` is a cache hit; a transfer, a new hairstyle or a
pack update is a miss. Three versions exist and mean different things:
`avatar_schema_version` (JSON shape, needs migration code), `asset_pack_version` (pixels
and weights, invalidates the cache only), `seed_version` (the hash namespace — changes
every face, migration only).

In the shipping build the save also stores the materialised `identity` + `shape` blocks
(~30 small values, not the PNG), so an existing rider is never regenerated and no algorithm
change can move his face. Append-stable selection is the second line of defence for riders
that do get regenerated.

Retired assets keep their id with `weight: 0`; never re-use an id for different art.

## Renderer target (decided)

Composite in C#, cache PNGs, let Godot display a texture. One code path for card and
list, testable headless with `dotnet test`, no world logic in the UI, no shaders. The
Python prototype is the reference implementation, not the shipping one — keep the logic
portable: integer hashing, no library-specific randomness, no data hidden in code that
belongs in the manifest.

## Do not

- Change the master reference to fix a single asset.
- Add an AI image call to any runtime path.
- Introduce `random`, `DateTime.Now` or unseeded RNG into generation.
- Let nationality pick a trait outright.
- Make the portraits more photographic, or drop facial features entirely (both
  extremes were rejected). Mouth assets may be open or laughing — that is identity
  variety the owner asked for, not a runtime emotion system.
- Promote this experiment into a canonical design doc, add it to `DOCS.md`, or wire it
  into `PelotonManager.sln` without a separately scoped task from the owner.
