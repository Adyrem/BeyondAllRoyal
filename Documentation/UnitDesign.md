# Unit & Tower Design Sheet

Fill in **names**, **roles**, and **stats** where marked. Counter relationships are pre-filled.

---

## Counter Chart

Read each row as: *"This entity performs [result] against the column entity."*

- **STRONG** — deals bonus damage / wins the matchup
- **WEAK** — takes bonus damage / loses the matchup
- EVEN — neutral matchup
- — — not applicable (towers do not fight each other)

|            | [Soldier] | [Heavy Gunner] | [Explosive Specialist] | [Hovercraft] | [Heavy Tank] | [Machinegun Turret] | [Railgun Turret] |
|------------|----------|----------|----------|----------|----------|-----------|-----------|
| **[Soldier]**   | —        | **STRONG**   | **STRONG**   | WEAK     | WEAK     | WEAK      | **STRONG**    |
| **[Heavy Gunner]**   | WEAK     | —        | **STRONG**   | **STRONG**   | WEAK     | WEAK      | **STRONG**    |
| **[Explosive Specialist]**   | WEAK     | WEAK     | —        | **STRONG**   | **STRONG**   | EVEN      | EVEN      |
| **[Hovercraft]**   | **STRONG**   | WEAK     | WEAK     | —        | **STRONG**   | **STRONG**    | WEAK      |
| **[Heavy Tank]**   | **STRONG**   | **STRONG**   | WEAK     | WEAK     | —        | **STRONG**    | WEAK      |
| **[Machinegun Turret]**  | **STRONG**   | **STRONG**   | EVEN     | WEAK     | WEAK     | —         | —         |
| **[Railgun Turret]**  | WEAK     | WEAK     | EVEN     | **STRONG**   | **STRONG**   | —         | —         |

### Counter Summary

| Entity     | Counters                        | Countered By                    |
|------------|---------------------------------|---------------------------------|
| [Soldier]   | Heavy Gunner, Explosive Specialist, Railgun Turret         | Hovercraft, Heavy Tank, Machinegun Turret         |
| [Heavy Gunner]   | Explosive Specialist, Hovercraft, Railgun Turret         | Heavy Tank, Soldier, Machinegun Turret         |
| [Explosive Specialist]   | Hovercraft, Heavy Tank                  | Soldier, Heavy Gunner                  |
| [Hovercraft]   | Heavy Tank, Soldier, Machinegun Turret         | Heavy Gunner, Explosive Specialist, Railgun Turret         |
| [Heavy Tank]   | Soldier, Heavy Gunner, Machinegun Turret         | Explosive Specialist, Hovercraft, Railgun Turret         |
| [Machinegun Turret]  | Soldier, Heavy Gunner                  | Hovercraft, Heavy Tank                  |
| [Railgun Turret]  | Hovercraft, Heavy Tank                  | Soldier, Heavy Gunner                  |

> Note: Explosive Specialist is neutral against both towers — the generalist option for tearing down defenses without a clear weakness.

---

## Unit Stats

> Metal is paid upfront when production starts. The unit is completed once the required Energy has accumulated in the building's buffer — no fixed timer exists.

### [Soldier]
- **Name:** Soldier
- **Role:** Light infantry
- **Health:** low
- **Damage:** medium
- **Attack Range:** low
- **Attack Speed:** high attacks/sec
- **Move Speed:** medium
- **Metal Cost (per unit):** low
- **Energy Cost (per unit):** low

### [Heavy Gunner]
- **Name:** Heavy Gunner
- **Role:** Heavy infantry
- **Health:** medium
- **Damage:** medium
- **Attack Range:** high
- **Attack Speed:** high attacks/sec
- **Move Speed:** low
- **Metal Cost (per unit):** medium
- **Energy Cost (per unit):** medium

### [Explosive Specialist]
- **Name:** Explosive Specialist
- **Role:** Generalist
- **Health:** medium
- **Damage:** medium
- **Attack Range:** high
- **Attack Speed:** low attacks/sec
- **Move Speed:** medium
- **Metal Cost (per unit):** medium
- **Energy Cost (per unit):** medium

### [Hovercraft]
- **Name:** Hovercraft
- **Role:** Fast Vehicle
- **Health:** high
- **Damage:** high
- **Attack Range:** medium
- **Attack Speed:** medium attacks/sec
- **Move Speed:** high
- **Metal Cost (per unit):** high
- **Energy Cost (per unit):** high

### [Heavy Tank]
- **Name:** Heavy Tank
- **Role:** Tanky Vehicle
- **Health:** high
- **Damage:** high
- **Attack Range:** high
- **Attack Speed:** low attacks/sec
- **Move Speed:** low
- **Metal Cost (per unit):** high
- **Energy Cost (per unit):** high

---

## Tower Stats

Towers are static defensive structures placed on building slots. They attack nearby enemy units automatically.

> Towers drain their energy buffer each time they fire. A tower with an empty buffer cannot shoot. Tesla Tower support is recommended to keep turrets firing consistently.

### [Machinegun Turret]
- **Name:** Machinegun Turret
- **Role:** Counter foot soldier
- **Health:** medium
- **Damage:** medium
- **Attack Range:** medium
- **Attack Speed:** high attacks/sec
- **Metal Cost (to build):** medium
- **Energy Cost (to build):** medium
- **Energy Cost per shot:** low
- **Slot Size:** 2x2 slots

### [Railgun Turret]
- **Name:** Railgun Turret
- **Role:** Counter vehicle
- **Health:** high
- **Damage:** high
- **Attack Range:** high
- **Attack Speed:** low attacks/sec
- **Metal Cost (to build):** high
- **Energy Cost (to build):** high
- **Energy Cost per shot:** high
- **Slot Size:** 3x3 slots

---

## Production Buildings

Each unit has one dedicated production building. Metal and Energy costs listed are to **construct the building itself**. Unit production costs are in the Unit Stats section above.

| Unit | Building Name | Metal Cost (build) | Energy Cost (build) | Slot Size |
|---|---|---|---|---|
| Soldier | Barracks | medium | medium | 1x1 |
| Heavy Gunner | Gun Range | low | medium | 2x2 |
| Explosive Specialist | Laboratory | low | medium | 1x1 |
| Hovercraft | Skimmer Pad | medium | high | 2x2 |
| Heavy Tank | Ironworks | high | high | 3x3 |

---

## Tesla Tower

Support building that injects energy into adjacent buildings, accelerating their construction and production and keeping defensive towers firing.

- **Metal Cost (to build):** high
- **Energy Cost (to build):** low
- **Injection Rate:** high energy/sec (distributed to each adjacent non-full building)
- **Slot Size:** 1x1


## Metal Factory

Support building that produces metal.

- **Metal Cost (to build):** low
- **Energy Cost (to build):** high
- **Metal Rate:** high metal/sec
- **Slot Size:** 1x1

## HQ

Base building producing a base amount of ressources

- **Metal Cost (to build):** free
- **Energy Cost (to build):** free
- **Metal Rate:** low metal/sec
- **Injection Rate:** low energy/sec (distributed to each adjacent non-full building)
- **Slot Size:** 5x5