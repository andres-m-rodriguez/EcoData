# Bioacoustics

## What it is

Bioacoustics is the study of sound produced by living organisms — birds, bats, frogs, insects, mammals, even fish. In an ecological monitoring context, it means deploying microphones in the environment, recording continuously (or on a schedule), and extracting biological information from the resulting audio: which species are present, how many, when they are active, and how the soundscape changes over time.

It sits at the intersection of ecology, signal processing, and (increasingly) machine learning. A single recorder in one location can generate weeks of data covering dozens of species without a human ever being present — which is exactly why it fits a solo, low-budget research program.

## Why it matters for research

- **Non-invasive.** No trapping, no disturbance. The organism vocalizes; you listen.
- **Scales without people.** One $50 recorder can replace dozens of point-count surveys.
- **Works at night and in dense vegetation.** Camera traps miss vocal species; bioacoustics catches them.
- **Detects cryptic species.** Many bats, frogs, and nocturnal birds are near-impossible to survey visually but loud on a spectrogram.
- **Generates reusable data.** The audio files themselves are archivable and re-analyzable as better classifiers appear.
- **Publishable on small datasets.** A single well-documented deployment is enough for a data paper or a site-descriptive study.

## Core concepts

### Soundscape

The total acoustic environment of a place, usually decomposed into three components:

- **Biophony** — sounds from living organisms (the part you usually care about).
- **Geophony** — wind, rain, rivers, thunder.
- **Anthrophony** — human-made sound: roads, planes, machinery, voices.

A lot of bioacoustic research is really about the *ratio* between these, not any single species.

### Spectrogram

A 2D plot of frequency (y-axis) vs. time (x-axis), with amplitude encoded as color. Every vocalization has a characteristic shape on a spectrogram — a bird song might be a swept chirp, a frog call a stack of horizontal bands, a bat echolocation a steep downward sweep. Most classification (human or ML) happens on spectrograms rather than raw waveforms.

### Acoustic indices

Scalar summaries computed over a window of audio that attempt to describe soundscape complexity without identifying species. The common ones:

| Index | What it captures |
|---|---|
| **ACI** (Acoustic Complexity Index) | How much the amplitude varies over time — high for bird-rich dawn choruses, low for constant noise |
| **BI** (Bioacoustic Index) | Energy in the bird frequency band (2–8 kHz) |
| **H** (Acoustic Entropy) | How evenly energy is spread across frequencies and time |
| **NDSI** (Normalized Difference Soundscape Index) | Biophony vs. anthrophony ratio |
| **ADI** (Acoustic Diversity Index) | Shannon diversity across frequency bins |

Indices are cheap to compute, don't need training data, and are a standard first-pass analysis. Many papers are built entirely on index comparisons across sites, seasons, or disturbance gradients.

### Species identification

Two broad approaches:

1. **Template / rule-based** — you define what a species sounds like (frequency range, duration, pattern) and match against it. Good for a handful of target species.
2. **Machine learning classifiers** — trained on labeled audio, output species probabilities. This is where the field is now.

Important pretrained models you can use for free:

- **BirdNET** — Cornell's bird classifier, 6000+ species, runs on a Raspberry Pi. The de facto standard.
- **Perch** (Google) — successor-generation bird classifier with strong transfer learning.
- **BatDetect2** — ultrasonic bat call classifier.
- **AnuraSet** / regional frog classifiers — growing, especially for Neotropics.
- **Koogu**, **opensoundscape** — Python libraries for training your own.

## Hardware

### Audible-range recorders (birds, frogs, most mammals, insects)

- **AudioMoth** (~$80) — the community standard. Open hardware, SD card, weeks of battery, schedulable.
- **Song Meter Micro** (~$150) — friendlier, weatherproof, app-driven.
- **DIY Raspberry Pi + USB mic** — cheapest, most flexible, needs a weatherproof enclosure and power solution. Fits naturally into an EcoData sensor node.

### Ultrasonic recorders (bats, some insects)

- **AudioMoth in ultrasonic mode** — records up to 384 kHz, adequate for most bats.
- **Echo Meter Touch** — phone-attached, good for active (walking) surveys.
- Full-spec bat detectors run into the thousands but are rarely necessary for a solo program.

### Hydrophones (underwater — fish, cetaceans, aquatic insects)

- **Aquarian H2a** (~$150) — entry-level, works with any recorder.
- Niche but a real opportunity if you already have a water-sensor program.

## Typical workflow

1. **Deploy** recorder(s) on a schedule — e.g. 1 minute every 10 minutes, or continuous during dawn/dusk.
2. **Retrieve** SD cards (or stream, if you have power/connectivity).
3. **Ingest** audio into storage with metadata: site, timestamp, sensor ID, weather.
4. **Compute acoustic indices** per window (fast, no ML needed).
5. **Run a classifier** (BirdNET etc.) to get per-species detection events with confidence scores.
6. **Validate** a sample of detections manually — essential for any paper.
7. **Analyze** — diel patterns, species accumulation curves, index vs. covariate regressions.

## How it fits EcoData / FaunaFinder

FaunaFinder already models species at a municipality level. Bioacoustics slots in as a new **detection source** alongside whatever manual or citizen-science observations currently feed it:

- A **BioacousticDevice** — another sensor type next to `WaterSensor`, with its own telemetry (battery, SD usage, recording schedule) and audio-file upload path.
- A **Detection** record — species, confidence, timestamp, device, audio-clip reference — which feeds species presence / activity data back into FaunaFinder.
- **Soundscape summaries** — per-site time series of acoustic indices, independent of species ID, useful on their own.

The data model is small; the infrastructure work is mostly around audio storage (blob/object store, not a relational DB) and a worker that runs classifiers on ingest.

## Research angles a solo operator can actually publish

1. **Site-descriptive study** — deploy at one site for a few months, report species list, diel activity, seasonal change. Regional journals accept these.
2. **Index comparison across a gradient** — urban vs. peri-urban vs. forest. Very tractable with 3–5 recorders.
3. **Classifier evaluation** — how does BirdNET perform on your local species? Precision/recall by species is a publishable contribution, especially in under-studied regions.
4. **Data paper** — release a labeled regional audio dataset on Zenodo / GBIF and publish a short descriptor in *Scientific Data* or *Data in Brief*.
5. **Methods / platform paper** — describe EcoData's bioacoustic pipeline as open-source infrastructure in *SoftwareX*, *Methods in Ecology and Evolution*, or *Ecological Informatics*.
6. **Pair with existing water/air sensors** — acoustic indices as a proxy for ecosystem health alongside chemistry. Multi-modal monitoring is a current trend.

## Gotchas

- **Storage grows fast.** A single AudioMoth on a light schedule can produce 10–50 GB/month. Budget for it.
- **Wind and rain** dominate outdoor recordings. Foam windshields help; scheduling around forecasts helps more.
- **Classifier confidence is not probability.** Validate a sample before reporting counts.
- **False positives cluster by species.** Some species are systematically over-called by BirdNET; know which ones in your region.
- **Legal / ethical.** Recording in public or private land may need permission; human voices in recordings are a privacy issue in some jurisdictions.

## Minimum viable first deployment

One AudioMoth, one tree, 1-minute-in-10 schedule, one month. Run BirdNET on the output, compute ACI and NDSI per hour, plot diel curves. That alone is enough material for a methods-oriented blog post, a conference poster, or the empirical section of a platform paper — and it validates the whole ingest pipeline end-to-end before you scale.
