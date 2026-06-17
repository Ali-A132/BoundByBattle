# BOUND BY BATTLE

*A 2D Fighting Game built with Unity, C#, and Aseprite*

---

## Game Description

**Bound By Battle** is a simple, fundamentals-focused fighting game designed to be accessible while having some depth. Players must master the essentials like:

* Spacing
* Blocking
* Attacking
* Stamina management

Choose between two unique fighters, each with different strengths in speed, power, and technique.

### Core Mechanics

* **3 Basic Attacks**

  * Jab
  * Heavy
  * Kick

* **2 Special Moves**

  * Launcher
  * Uninterruptible Attack

* **Stamina System**

  * Every action consumes stamina.
  * If stamina reaches zero, the player becomes exhausted.
  * Exhausted players cannot attack or block until half of their stamina has regenerated.
  * Players can still move while exhausted, creating opportunities to unleash devastating combos.

Unlike many traditional fighting games, players cannot jump over opponents or switch sides, making spacing and positioning important. Push your opponent toward the corner while carefully managing your stamina.

---

# Development Process

C# was a natural choice for a fighting game due to its strong object-oriented capabilities. The project heavily utilizes inheritance and polymorphism, enabling reusable character systems while still supporting unique fighter abilities, animations, and stats.

## Player Controller

The Player Controller is the largest and most complex system in the game and manages:

* Character movement
* Health and stamina systems
* Input buffering for lenient controls
* Damage calculation
* Block chip damage
* Hitbox interactions
* Animation events
* Animation state machine
* Character specific abilities
* Visual effects (dust clouds, hit sparks)
* Multiplayer synchronization
* Sound effects

The input buffer allows players to queue actions, making the game more lenient and responsive even when button timing isn't perfect. Also the project has a dedicated hitbox system uses Unity colliders to determine successful attacks and apply damage when collisions occur with valid player targets where they have an opponent tag.

## Round Manager

The Round Manager handles:

* Stage loading
* Music assignment
* Round tracking
* Match timer logic
* Round resets
* Round transitions
* Victory sequences

---

# Art & Animation

All artwork and animations were created by me using Aseprite through frame-by-frame pixel animation.

### Inspirations

Visual inspirations include:

* Street Fighter III: 3rd Strike
* Tekken 4

The fighters are heavily inspired by MMA athletes, particularly Alex Pereira and Israel Adesanya Both characters wear relatively simple fighting attire with their shorts so strong value was placed on their distinct silhouettes, unique stances, individual move sets, different gameplay archetypes amd more.

## Character Design

### Mahsk

A powerful kickboxer focused on devastating punches and flying knee strikes. He has a higher power output, more rigid movement style and a stoic personality.

### Payet

A fast and unpredictable point fighter specializing in kicks and mobility. He has greater speed, a flowing movement style, offensive pressure and a happy go lucky personality.

## Animation Process
A technique I used was recording a video of myself performing these actions and then using it as reference when trying to draw the sprites to mimic, it would prove useful for subtle details (like muscle twitches, weight transfer, muscle movements, balance shifts) with the way the body moves when throwing kicks or punches.

Each fighter contains approximately 160 animation frames, including:

* Walking
* Attacking
* Taking damage
* Exhaustion states
* Special moves

Sprites were created on a 512x256 canvas, though in hindsight a smaller canvas would have made things easier.

---

# Stages

Moon Colony Arena: A futuristic lunar stadium built to host the world's greatest martial arts tournament. Has a visual reference to Berserk's Eclipse through the giant hand appearing in the background.

Room of Time & Space: A minimalist training environment inspired by classic fighting game practice stages. A place where time slows down for warriors to train in. 

Dojo in the Sky: A mountaintop training temple where warriors hone their skills.

House of Waffle: A waffle house parking lot battleground of where all great fighters converge.

---

# Music

Check out the original soundtrack and music assignments below.

## Menu Themes

| Screen           | Track   |
| ---------------- | ------- |
| Main Menu        | Saturn  |
| Character Select | Fight   |
| Online Menu      | Memento |

## Stage Themes

| Stage                | Tracks                          |
| -------------------- | ------------------------------- |
| Moon Colony Arena    | Atmo + ToTheTop                 |
| House of Waffle      | Royal Stage + Samba + Clockwork |
| Room of Time & Space | Cloud + Flow + Nudge            |
| Dojo in the Sky      | Ski + A La Vista + Hallow       |

---

# Credits

### Development

* Programming: Ali A. Malik
* Art & Animation: Ali A. Malik

### Audio

* Original Soundtrack: DJM1ck
* Sound Effects: DJM1ck

### Special Thanks

* HelioKing — Early playtesting and feedback

---

## Bound By Battle OST

Spotify Album:

https://open.spotify.com/album/06dzPcpYn3RcOewTK5jEOX?si=RBadX-3_SfqwrclPuQwRZQ

---

## Built With

* Unity
* C#
* Aseprite
