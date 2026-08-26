# Avatar system — design sketch (EXPERIMENT, not a contract)

**Status:** EXPERIMENT. Not listed in `DOCS.md`, not a locked design. It describes what the
prototype in this folder actually implements, plus the decisions that are still open.
Nothing here overrides `DECISIONS.md`, `ARCHITECTURE.md` or `DATA_MODEL_v0.1.md`.

The prototype is Python because pixels need to be judged before a stack is committed. Every
piece of logic below is deliberately portable to C#: integer hashing, no floating-point
accumulation across frames, no library-specific randomness, no data hidden in code that
should live in the manifest.

**Operating manual for other agents:** `.cursor/skills/peloton-avatars/SKILL.md`. It
carries the owner's taste decisions, the recipe tables, the mandatory gate and the art
traps already paid for. Read it before editing assets or the style.

## 0. Owner answers locked on 2026-08-26

| Question | Answer | Consequence |
|---|---|---|
| view | front-facing | eye / brow / ear assets are single-sided and mirrored: half the files |
| helmet in the portrait | off by default | `helmet_worn = false`; helmet stays an optional layer |
| peloton | male riders + manager outfits | `role` field on riders and assets; no women's pack |
| size in the UI | rider card, up to ~1/6 of a laptop page | 512x512 master, `head_crop` for 48-96 px icons |
| art direction | flat vector, but "too realistic for what an avatar does" | default profile `poster`: ink keylines, two flat tones |
| expression | slightly more smiling | every mouth carries a small smile; two wider-smile variants |
| hair | more variety | 25 hairstyles, incl. fringe, quiff, undercut, fade, mid part |
| kits | teams plus Tour / Giro / Vuelta GC leader | `jersey_override`: `tour` / `giro` / `vuelta` / `world` / `national` |

### Alignment with the merged UI lab (PR #18)

The dashboard direction is constructivist poster: paper `#f3ede1`, red `#d11f1f`, ink
`#0c0c0d`, 3 px black borders, hard offset shadows, Anton display type. The avatar pack now
borrows from that lab instead of inventing a parallel palette:

- skin stops and hair colours were lifted from `09-avatar-lab.html` before the owner
  rejected and deleted that lab; the values now live in `SKIN_RAMP_FLAT` / `HAIR_COLORS`,
- jersey override keys and colours are the lab's: `team`, `tour` (yellow), `giro` (pink),
  `vuelta` (red), `world` (rainbow bands), `national` (two bands), with the pre-lab names
  (`leader`, `world_champion`, `national_champion`) kept as aliases,
- keyline weight (~4 px on a 512 master) is chosen against the 3 px panel borders,
- review sheets render on paper with bordered cards, because a portrait judged on a dark
  background tells you nothing about how it sits in a light UI.

Open: which of the five styles wins, and whether the pack is authored procedurally or by an
artist. Both are answered by looking at `demo/07_styles.png`, not by writing more code.

---

## 1. Master reference (immutable)

One canonical framing that every asset in a pack must match. It lives in
`avatarlab/bake/draw.py` and is copied into `manifest.canvas`, so the runtime can assert it:

| property | value |
|---|---|
| canvas | 512 x 512 RGBA, transparent background |
| view | front-facing, head and shoulders, fixed camera distance |
| centre line | x = 256 |
| skull top | y = 72 |
| brow line | y = 186 |
| **eye line** | **y = 204** (never moves; the composite pivot) |
| nose tip | y = 268 |
| mouth line | y = 303 |
| chin | y = 352 |
| head half width | 109 px (head 218 x 280, ratio 0.78) |
| single-eye anchor | x = 256 + 47 |
| neck top / shoulder line | y = 330 / y = 456 |
| torso half width | 246 px |
| head crop for small UI sizes | (96, 44, 416, 364) |
| light | key from upper-left, ~35 degrees elevation |

Changing any of these invalidates the whole pack: it is an art-direction reset, not a tweak.

## 1b. Style profiles

Art direction is a property of the pack, not of the game code. One `StyleProfile`
(`avatarlab/bake/draw.py`) drives every recipe:

| field | what it controls |
|---|---|
| `tone_steps` | 0 = smooth gradients, 3 = cel shading |
| `tone_floor` | darkest tone the quantiser keeps |
| `form_strength`, `highlight_strength` | global multipliers on every shading term |
| `gradient_scale` | full-canvas gradients band when posterised, so flat styles compress them |
| `edge_hardness` | soft painted falloff vs crisp vector edge |
| `outline`, `outline_darkness` | inner outline, drawn *inside* the silhouette so it cannot break alignment |
| `detail_alpha` | wrinkles / tan lines / freckles intensity |
| `line_features` | nose and lips get line work instead of pure shading |

| `line_art`, `feature_boost` | true ink keyline width, and a small scale-up of eyes/brows |

Shipped profiles: `poster` (default), `flat`, `flat_outline`, `painted`, `soft`. Three lessons
the prototype learned the hard way, all encoded above:

- **Never harden every alpha.** Stubble and eyebrows are soft on purpose; posterising and
  hardening them turns a gradient into an amoeba. `gray_layer(..., crisp=False)` opts out.
- **Never posterise a full-canvas gradient.** It bands across the whole face. A flat style
  gets a small number of deliberate shading shapes instead of many soft ones.
- **Fake texture reads as damage.** Random ink strokes meant to suggest hair strands looked
  like scratches at portrait size; one deliberate second-tone shape reads as hair.

Keylines are drawn as their own part, inside the silhouette (`keyline()`), never centred on
the edge: an outer stroke would overlap neighbouring layers and drift when a continuous
parameter scales the feature.

## 2. Layer architecture

Z-order, from the manifest (`layer_order`). The recommended list from the brief was
reorganised in three places, for reasons the prototype demonstrates:

```text
neck            <- before the jersey, so the collar overlaps the neck
jersey          <- team template, recoloured at runtime
jersey_overlay  <- rainbow / champion bands, clipped to the torso silhouette
ears            <- before the head, so the head edge overlaps the ear
head            <- skin silhouette + baked form shading (tinted)
nose            <- shading + highlight only, no outline
mouth
eyes            <- 4 parts: sclera, iris (tinted), pupil, lids/lashes/catchlight
eyebrows
skin_details    <- cyclist tan lines, freckles, stubble shadow, moles, scars, road rash
wrinkles        <- one asset, 7 age-driven parts
facial_hair
hair            <- cast shadow part first, then hair, then sheen
glasses
helmet          <- cast shadow part first, then shell/stripe/vents/strap/glint
```

Changes vs the brief:

- `base_head` + `skin` merged. A separate skin layer would need its own silhouette per head
  shape, i.e. the same asset twice. Instead the head PNG stores **shading in RGB and the
  silhouette in alpha**, and the skin tone is a runtime multiply. One asset, infinite tones.
- `scars`, `skin_details` and `wrinkles` merged into two categories, because they are the
  same mechanic (a low-alpha multiply/screen overlay on the face region).
- `optional_details` dropped: an "everything else" category has no compatibility semantics.
- Added `jersey_overlay` and `neck` because the brief's list had no torso/neck at all.

An asset is a **set of parts**, not one PNG. A part carries its own blend mode, colour slot
and optional opacity driver. This is what lets 4 PNGs express one eye that is tintable per
rider without a PNG per eye colour.

## 3. Asset naming convention

```text
asset id     <category>_<nn>_<descriptor>      head_03_square, hair_12_receded, fh_04_beard_full
part file    <category>/<asset_id>__<part>.png head/head_03_square__skin.png
                                               eyes/eyes_04_hooded__iris.png
colour asset <category>_<nn>_<name>            hc_05_dark_blond, ic_05_blue  (no pixels)
```

`nn` is stable forever; a retired asset keeps its number and gets `"weight": 0` plus a
`retired_in` note, so old saves still resolve it.

## 4. Data schema

Manifest (`manifest.json`, hand-editable, one per asset pack):

```jsonc
{
  "pack_id": "peloton_placeholder",
  "asset_pack_version": "0.1.0-placeholder",
  "avatar_schema_version": 1,
  "seed_version": 1,
  "canvas": { "size": [512, 512], "eye_line_y": 204, "...": "master reference" },
  "layer_order": ["neck", "jersey", "..."],
  "palettes": { "hair_color": { "hc_01_black": [34, 29, 27] }, "iris_color": {} },
  "teams":    { "team_01_azure": { "primary": [26,74,148], "secondary": [], "accent": [] } },
  "assets": [
    {
      "id": "hair_12_receded",
      "category": "hair",
      "weight": 0.07,
      "anchor": [256, 156],
      "mirrored": false,
      "min_age": null, "max_age": null,
      "requires_tags": ["hairline_receded"],
      "excludes_tags": [],
      "region_weights": { "*": 1.0, "scandinavia": 1.2 },
      "parts": [
        { "file": "hair/hair_12_receded__cast_shadow.png", "blend": "multiply" },
        { "file": "hair/hair_12_receded__main.png", "blend": "normal", "color_slot": "hair" },
        { "file": "hair/hair_12_receded__sheen.png", "blend": "screen" }
      ]
    }
  ]
}
```

Rider appearance (derived data; only `rider_id` + the rider row are stored in the save):

```jsonc
{
  "avatar_schema_version": 1,
  "asset_pack_version": "0.1.0-placeholder",
  "seed_version": 1,
  "rider_id": 4242,
  "salt": 0,                       // duplicate-resolution salt, 0 for almost everyone
  "identity": {                    // permanent for the whole career
    "head": "head_03_square", "ears": "ears_01_medium", "eyes": "eyes_04_hooded",
    "eyebrows": "brows_03_thick", "nose": "nose_05_aquiline", "mouth": "mouth_01_medium",
    "iris_color": "ic_02_brown", "skin_tone": 0.3421, "build": 0.61,
    "aging": { "gray_onset_age": 34.2, "gray_speed": 0.82, "balding_propensity": 0.31,
               "wrinkle_propensity": 0.52, "sun_damage": 0.60, "beard_capability": 0.44 },
    "tags": ["jaw_wide"]
  },
  "shape": {                       // permanent continuous geometry, 0..1 normalised
    "face_width": 0.58, "face_height": 0.47, "eye_spacing": 0.62, "eye_height": 0.44,
    "eye_size": 0.51, "brow_height": 0.39, "nose_length": 0.55, "nose_width": 0.48,
    "mouth_width": 0.52, "mouth_height": 0.50, "mouth_y": 0.49, "ear_size": 0.53,
    "asymmetry": -0.34, "neck_thickness": 0.66, "shoulder_width": 0.63
  },
  "mutable": {                     // recomputed from age; never stored long-term
    "hair": "hair_02_crop", "hair_color": "hc_02_dark_brown", "hairline_recession": 0.12,
    "gray": 0.0, "facial_hair": "fh_01_stubble_light", "wrinkle_strength": 0.08,
    "tan_strength": 0.58, "skin_details": ["detail_01_helmet_tan"], "age_stage": 3
  },
  "equipment": {                   // recomputed from team/results
    "jersey_template": "jersey_01_raglan", "team_id": "team_01_azure",
    "jersey_override": null, "helmet": "helmet_01_vented", "helmet_worn": false,
    "glasses": "glasses_02_shield_mirror", "glasses_worn": false
  }
}
```

Improvements over the schema in the brief: `shape` is split out of `identity` (discrete asset
ids vs continuous parameters have different migration rules), `aging` genetics are permanent
per rider (so two riders age differently but each ages consistently), and `salt` is explicit
so a re-rolled rider stays reproducible.

## 5. Deterministic generation

```python
key  = blake2b(f"pmav|{seed_version}|{rider_id}|{domain}|{salt}")   # 64-bit
draw = splitmix64(key)                                             # per-domain stream
```

Three rules that matter more than the choice of hash:

1. **Domain separation.** Every trait draws from its own stream (`identity.nose`,
   `mutable.hair`, ...). Adding a new category later cannot shift the values already drawn
   for existing categories, so a pack update does not silently rewrite everyone's face.
2. **Salt scoping.** Only streams marked `salted=True` (hair, hair colour, beard) see the
   duplicate-resolution salt. Identity streams ignore it, so fixing a clone can never move
   a skull or a nose.
3. **No global RNG.** No `random`, no shared state, no order dependence — `generate_pool`
   sorts by `rider_id`, so the result never depends on iteration order.

Nationality/region only multiplies weights (`region_weights`) and biases the mean of the
skin-tone distribution. It never selects a trait, and every option stays reachable from every
region. `visual_seed` overrides `rider_id` as the seed for editor-authored riders.

## 6. Weighted trait selection

The obvious implementation - one roll walked across the cumulative weights - is wrong for a
live game. Appending an asset changes the total, so the same roll lands on a different asset
and a slice of the existing riders silently get a new face. That breaks the versioning lock
in section 12.

Instead every candidate gets its own hashed draw and the winner is the smallest
`-ln(u_i) / w_i` (an exponential race, i.e. Efraimidis-Spirakis weighted sampling):

```python
for asset in eligible(category, rider, chosen_tags):
    w = int(round(weight(asset, rider) * 10_000))           # integer shares
    e = neg_log2_q32(rng.u64_for(domain, asset.asset_id))    # Exp(1) in Q32 fixed point
    if e * best_w < best_e * w:                              # exact integer comparison
        best = asset
```

Two implementation details matter:

- `-ln(u)` comes from a bit-by-bit **fixed-point log2**, not from libm. A platform's `log`
  can differ in the last bits between the game and the tools, which would mean two machines
  disagreeing about a rider's face. The log2-vs-ln constant cancels in the ratio.
- Comparisons are integer cross-multiplications, so there is no float accumulation at all.

Measured properties, asserted in `selftest.py` over 8 000 riders:

| property | result |
|---|---|
| frequencies follow weights | worst deviation 0.29 pp |
| appending weight 0.10 to a total of 1.00 | 8.7 % of riders move (expected 9.1 %) |
| ... riders swapping between two *old* assets | **0** |
| retiring an asset (`weight: 0`) | only the riders who had it move |

Cost: one hash per candidate instead of one per category, so appearance generation went from
0.17 ms to 0.63 ms per rider in Python. Irrelevant next to rendering, and it buys the
versioning guarantee.

Weights stay relative, not probabilities, so an artist adds an asset without renormalising
the others. Measured on 20 000 riders: short crops dominate the hairstyles and ~70 % of
riders have no facial hair - common stays common, rare stays rare.

Continuous parameters use a 3-uniform mean (a cheap bell curve), so most riders are average
and extremes are rare. That is what stops the "artificially diverse" look.

## 7. Compatibility rules

Four mechanisms, all data-driven, no code per asset:

| mechanism | example in the pack |
|---|---|
| `min_age` / `max_age` | `fh_04_beard_full` needs 23+; `hair_09_spiky` stops at 32 |
| `requires_tags` | `hair_12_receded` only for a rider whose hairline actually receded |
| `excludes_tags` | `detail_04_stubble_shadow` is skipped when a dense beard is present |
| `roles` | jerseys, helmets and glasses are `rider` only; polo / softshell / suit are `manager` only |
| anchors + affine limits | glasses/helmet are placed from the eye line, so they cannot drift off the face |

Tags are produced by assets (`head_03_square` → `jaw_wide`) and by derived state
(`hairline_thinning`, `hairline_receded`, `beard_dense`). The validator proves that a legal
option always exists: it checks every age x hairline-state combination has at least one hair
asset, so generation can never dead-end.

Clipping is prevented structurally rather than per-pair: beard masks are cut away from the
lips, hair is a silhouette grown from the same skull contour every head asset uses, and the
helmet's own cast-shadow part grounds it on the forehead.

## 8. Age progression

Identity is frozen; only derived values move, and all of them are monotonic in age
(asserted in `selftest.py`):

```text
wear       = clamp((age - 25) / 24)
wrinkles   = wear * (0.40 + 0.80 * wrinkle_propensity)
gray       = clamp((age - gray_onset_age) / 18 * gray_speed)     # onset 27..46 per rider
recession  = f(age, balding_propensity)                          # drives hair asset gating
beard odds = (0.10 + 0.42 * beard_capability) * age_factor       # 0.35x under 21
tan        = 0.25 + 0.55 * sun_damage
```

`wrinkles` drives seven overlay parts with different per-part opacities, so lines appear in a
plausible order (nasolabial and eye bags first, forehead later) and volume loss (temple and
cheek hollowing) grows with them — at portrait size volume reads far better than lines.

`age_stage` (9 buckets) is the cache granularity: a birthday inside a bucket is a cache hit.

## 9. Duplicate detection

```python
similarity_key = (head, eyes, nose, mouth, bucket(skin_tone, 6), bucket(face_width, 3),
                  hair, hair_color, has_facial_hair)
```

Deliberately coarse — it models what a player notices in a 64 px list row, not the full
appearance. On collision the rider is regenerated with `salt += 1`, which only moves hair,
hair colour and beard; up to 8 attempts, then the collision is accepted and counted.

Measured on 20 000 riders: 38 look-alike pairs detected and resolved, 0 unresolved, and the
result is identical whichever order the riders are processed in.

## 10. Renderer

```text
render(appearance, pack) -> 512x512 RGBA
```

1. Resolve appearance → ordered `(asset, part, mirrored)` list via `layer_order`.
2. For each part: take the cached crop, apply `global_face ∘ local_feature` (scale+translate
   only), tint by colour slot, blend (`normal` / `multiply` / `screen`).
3. Clip and return.

Two implementation details carry the performance:

- Layers are cached **cropped to their alpha bounding box**, and placed with
  `resize(box=...)` instead of a full-canvas affine transform. Same math, 31x faster:
  715 ms → 18.5 ms per portrait in Python.
- Blending happens only inside the destination sub-rectangle.

Mirrored assets (eye, brow, ear) are stored once and flipped at load, halving those
categories. Continuous parameters are applied as per-feature affines, so ~100 assets cover a
20 000 rider peloton without a combinatorial PNG explosion.

Measured: 54 portraits/s single-threaded in Python (~370 s for 20 000 cold). A C#/Godot port
with GPU or `System.Drawing`/SkiaSharp compositing should be one to two orders faster, and it
only ever runs on cache misses.

## 10b. Where the renderer should live (recommendation)

The owner is not a programmer, so this is a decision, not a menu.

**Recommendation: composite in C#, cache PNGs, let Godot display a texture.**

- One code path for every size. The big rider card uses the 512 master, list icons use the
  `head_crop` from the manifest; both come from the same file.
- Testable headless. The compositor is ordinary C# with no Godot dependency, so it is
  covered by `dotnet test` exactly like the rest of the skeleton, and `HANDOFF.md`'s rule
  that Godot holds no world logic keeps holding.
- Cheap. A portrait is only composited on a cache miss. Flat vector art at 512x512 is
  roughly 15-30 KB per PNG; an LRU cap of a few thousand entries is tens of megabytes,
  and the cache can be deleted at any time because it is derived data.
- Boring. No shader work, no GPU state, no Godot-version risk.

The alternative — stacking `Sprite2D` layers in Godot and letting the GPU composite —
avoids the cache but moves art assembly into the UI layer, cannot be tested without a
display, and would need the tint/blend logic reimplemented in shaders. Not worth it for a
still portrait.

Practical sizing: a `SkiaSharp` or `ImageSharp` compositor should land in the low
milliseconds per portrait (Python does 20 ms including PNG decode), so a 20 000 rider
world never needs a bulk pre-render; portraits appear as screens are opened.

## 11. Cache strategy

```text
avatar-cache/{rider_id}_{asset_pack_version}_{blake2b8(appearance)}.png
```

The hash covers schema/pack/seed versions, rider id, salt, and the identity, shape, mutable
and equipment blocks. Consequences:

- a birthday inside the same `age_stage` → same key → cache hit;
- transfer, new helmet, new hairstyle, entering a new age stage → new key;
- a pack update changes every key, so old files are simply orphaned (sweep by prefix).

Only the derived appearance and the PNG are cacheable; nothing in the save depends on them.

## 12. Versioning

| field | meaning | on bump |
|---|---|---|
| `avatar_schema_version` | shape of the appearance JSON | migration code path required |
| `asset_pack_version` | which pixels/weights | cache invalidated, faces unchanged if ids kept |
| `seed_version` | the hash namespace | **every face changes** — opt-in migration only |

Rule: adding assets must not change existing riders. Two independent defences:

1. **Materialise the appearance.** The save stores the `identity` + `shape` blocks (about 30
   small values, not the PNG) when the rider is created, so an existing rider is never
   regenerated and no algorithm change can move his face. This is the primary defence and
   the one to implement in C#.
2. **Append-stable selection** (section 6) for everything that still regenerates: riders
   created before materialisation existed, editor previews, bulk tooling. Adding an asset
   only moves the `w / (W + w)` share that lands on it.

Retired assets keep their ids with `weight: 0`. If a pack no longer resolves an id, the
loader falls back to the same category's highest-weight asset and logs it, rather than
silently re-rolling the face.

## 13. Folder structure

```text
experiments/avatar_prototype/
├── avatarlab/            portable logic (rng, generate, manifest, render, validate)
│   └── bake/             PLACEHOLDER art generator (replaced by the real art pipeline)
├── scripts/              bake_pack, validate_pack, selftest, render_demo
├── demo/                 committed review sheets
└── out/                  local output: pack/, demo/, cache/ (gitignored)

asset pack on disk (what an art pack ships):
pack/
├── manifest.json
├── head/  ears/  eyes/  eyebrows/  nose/  mouth/  hair/  facial_hair/
├── wrinkles/  skin_details/  glasses/  helmet/  jersey/  jersey_overlay/  neck/
```

## 14. Asset-pack workflow (AI and non-AI)

AI is a **content tool**, never a runtime dependency. With flat vector as the front runner
there are two viable pipelines, and the manifest is identical for both:

**A. Procedural / vector authoring (recommended for flat vector).** The placeholder baker in
`avatarlab/bake/` is already this pipeline: recipes are parameter dictionaries, alignment is
exact by construction, a new head shape is six numbers, and the whole pack rebuilds in ~25 s.
Ported to C# (or kept as an offline tool that ships PNGs), it needs no AI at all. A vector
artist can replace individual layers file by file because the contract is per-part PNGs.

**B. AI-generated assets (needed for painted or photoreal).** Requires masked inpainting;
plain text-to-image will not hold the framing. Loop per asset:

1. Start from the approved master avatar PNG plus a layer-specific mask (only the region the
   asset owns is unlocked). Inpainting with a locked mask is what keeps the rest of the face
   identical — text-to-image from scratch will not hold framing.
2. Prompt changes exactly one component; camera, head position, proportions, lighting and
   grading come from the reference, not from the prompt.
3. Export at 512x512 with alpha, no resampling, no crop.
4. Separate the layer from the reference face (difference mask + manual cleanup), so the file
   contains only the new component.
5. Split into parts where a colour slot is needed: grayscale shading + alpha for anything
   tintable (skin, hair, team kit), full colour only for things that never recolour.
6. Run `validate_pack.py`, then review on the asset-explorer sheet
   (`demo/05_trait_variants.png`) which renders the new asset against one fixed base rider.
7. Reject if any unrelated feature moved. A diff against the master reference outside the
   asset's own region is a hard fail.
8. Assign `weight`, age limits, tags and anchors in the manifest — that is authoring work, not
   AI work.

Practical notes: generate in families (one prompt seed, 6-8 hairstyle variants) so a whole
category shares a look; keep every prompt and seed next to the asset id for reproducibility;
budget roughly 2-3x the target asset count for rejections.

With only a short window of image-model access, spend it on a **style bible** (4-6 reference
portraits in the exact master framing) rather than on assets: the references pin the art
direction, and pipeline A can then match them. `README.md` carries ready-to-paste prompts,
including the decisive test - ask the model to change only the hairstyle on an existing face
and see whether the face survives.

## 15. Validation script

`avatarlab/validate.py` (`scripts/validate_pack.py`) fails the build on:

- manifest canvas ≠ 512x512, duplicate asset ids, unknown category, `min_age > max_age`;
- missing PNG, mode ≠ RGBA, size ≠ 512x512;
- fully transparent layer (empty asset) or fully opaque layer (no transparency);
- content bounding box escaping the category's allowed region — this is the alignment check
  that catches an AI asset drawn at the wrong scale or offset;
- unknown blend mode or colour slot;
- an empty required category;
- `requires_tags` referencing a tag nothing produces;
- **no legal hair option** for some age x hairline-state combination (dead-end generation).

Warnings for reachability (`weight <= 0`). Current placeholder pack: 223 files, 0 errors.

`scripts/selftest.py` adds 31 behavioural assertions (determinism, byte-identical renders,
identity stability across ages 18-45 and transfers, monotonic aging, tag gating, clone
resolution, order independence, transparent background, cache-key movement).

---

## Open questions before this becomes a real system

1. **Which style profile wins?** `demo/07_styles.png` is the old five-way sheet.
   `demo/10_look_proposals.png` is the neighbour-of-poster review (thin / woodcut /
   comic / stencil). `poster` stays default until the owner picks. Everything else
   is already style-agnostic.
2. **Who authors the final pack?** Procedural/vector (pipeline A) or an artist replacing
   layers file by file. Both fit the same manifest.
3. **Do we ship the pack or bake it on first run?** ~245 small PNGs is ~2-3 MB shipped, or
   ~25 s of one-off work on first launch.
4. **Asset counts for v1.** The placeholder pack now has 18 heads, 16 eyes, 18 noses,
   18 mouths, 14 brows, 10 ears, 40 hair, 11 facial hair (plus kits / helmets / glasses).
   Doubling heads and hair was the cheapest way to raise perceived variety after the
   owner kept `poster` and asked for more face combinations, not a restyle.
5. **Does the rider card need an expression or a neutral face only?** Everything here is
   neutral; a second expression would double the eye/mouth categories.
