# DRIFTLIGHT story bible

The lore behind the 15-minute run: who vanished, what the signal is, why it's calling you, and the three signal-fragment logs exactly as they appear in game.

---

## Premise

You're a lone scavenger pilot who makes a living stripping wrecks at the edge of charted space. Your ship's old navigation core, salvaged years ago and never quite trusted, picks up a faint signal from a dead star system nobody goes to.

Every planet in that system once held a thriving civilization. Every one of them went silent on the same day. No war, no plague, no wreckage. Just empty cities, gardens still growing, and a signal that has been repeating ever since.

You follow it in.

---

## The world

### The vanished people: the Seren

The Seren spread across the system's worlds: gardeners on Verdance, forge-keepers on Cinder, and the signal-keepers who lived near the star. They weren't conquerors. They were builders and singers, and their whole culture was organised around the star at the centre of the system, which they called **the Source**.

Their language was half song. Their buildings were tuned like instruments. When they talked about the future, they talked about "answering."

### The signal

For generations the Seren listened to a faint, steady pulse from the Source. Their scholars believed the star was alive in some way they couldn't measure.

Then, one season, the pulse changed. It became a song, and every Seren could hear it: through the soil, through stone, in their own heads. It asked them to come.

**The Day of Answering:** on one day, at the same moment, every Seren on every world stepped into the light and was gone. They didn't die. They became the light: the faint glow that drifts through the system's empty places. Pilots who stray close call it the *driftlight*.

### The Warden

Not everyone wanted to answer. The forge-keepers of Cinder built **the Warden**, a network of machines meant to protect the Seren, and gave it one order: *keep the people safe*.

When the Seren answered anyway, the Warden decided the signal was the threat. It has spent every year since guarding the signal relays and planets, shooting down anything that comes close, so that no one else is ever taken.

That's what you fight: the drones at the relay in The Drift, the defences on the planets, and finally the Warden itself at The Source.

### The twist: why the signal is calling you

On the Day of Answering, one Seren mother on Cinder couldn't bring herself to take her child into the light. She put the baby in a small courier ship, set it to drift out of the system, and then answered alone.

**You are that child.**

The courier was found years later by scavengers. You grew up among them with no idea where you came from. The ship's "salvaged" navigation core was the courier's own. It has always been listening.

The signal isn't a trap and it isn't a weapon. It's your people, and your mother among them, calling the one Seren who never heard the song. They can't come back. They want you to know who you are, and to choose for yourself whether to follow.

### The ending

At The Source you defeat the Warden. Not out of hatred: it's the last thing the Seren built that still believes it's protecting them. Fragment 3 is your mother's log.

You stand in front of the light with the choice the Seren had. The game ends just before you choose: you lift off, the system lights up behind you, and the signal changes for the first time in years. It isn't calling any more. It's singing.

---

## The three signal fragments

Each fragment is one short log from the Seren, shown on screen when you recover it. They're written to be read in about 12 seconds.

### Fragment 1: Verdance

**In game:** `SIGNAL FRAGMENT 1 / 3`
**Who:** an old gardener, the night before the Day of Answering.

> "The gardens sang again tonight. Every flower on Verdance turned to the sky at the same moment, as if something up there had called our names. The elders say it is only the season. But the signal is in the soil now, in the roots, in us. My granddaughter asked if it would hurt. I told her I didn't know. I think it won't. I think it sounds like coming home. Tomorrow, all of us will answer it."

*(78 words. It builds on Miguel's placeholder `VerdanceLore` in `DriftlightBuilder.Verdance.cs`; the first three sentences and the last are unchanged.)*

**After it's recovered:** "Fragment recovered. The signal goes deeper into the system."

### Fragment 2: Cinder

**In game:** `SIGNAL FRAGMENT 2 / 3`
**Who:** a forge-keeper, on the Day of Answering itself.

> "Forge log, final entry. The Warden is awake. We built it to keep us safe, and now it turns its guns on the sky, as if it could shoot down a song. The others are already walking into the light. I can hear it too. I have put my little one in the last courier and set it to drift, far from here, where the song cannot reach. Forgive me. Someone has to remember us from the outside."

*(78 words.)*

**After it's recovered:** "Fragment recovered. Follow the signal to its source."

### Fragment 3: The Source (the reveal)

**In game:** `SIGNAL FRAGMENT 3 / 3`
**Who:** your mother, speaking from inside the signal.

> "If you can hear this, you came back. I am sorry I sent you away. I was afraid, and I wanted you to have a whole life before you had to choose. We are not gone, little one. We are the light that drifts between these worlds. The Warden kept everyone out to protect us. You were the only one it could not stop. You don't have to stay. You only had to know who you are. Welcome home."

*(79 words.)*

**After it's recovered:** "The signal is quiet. Leave the system."

---

## Tone guide

**Mysterious, melancholy, a little hopeful.**

- **Mysterious:** show, don't explain. Empty cities with lights still on, flowers all turned the same way, ruins tuned like instruments. The fragments are the only real explanation, and they come in order.
- **Melancholy:** the Seren aren't villains and neither is the Warden. Everyone was trying to protect someone. The sadness comes from the empty places, not from gore or horror.
- **A little hopeful:** nobody died. The ending is about belonging, not loss. The last thing the player hears is the signal singing, not calling.

**Writing rules for anything the player reads**

- Short sentences. Plain words. Logs read like real people talking, not fantasy prose.
- No lore dumps: one idea per log, 60 to 100 words.
- The Seren never say "died" or "vanished." They say "answered," "walked into the light," "the song."
- The player character never speaks. The world talks to them.

**Words to keep consistent**

| Term | Meaning |
|---|---|
| **The Seren** | The people who vanished |
| **The signal / the song** | What called them. Outsiders say "signal"; the Seren say "song" |
| **The Day of Answering** | The day everyone went |
| **The driftlight** | The faint glow the Seren became, drifting through the system |
| **The Warden** | The machine guardian you fight, built on Cinder |
| **The Source** | The star at the centre of the system, and the final stop |
| **Courier** | The small ship you were sent away in as a baby |
