---
marp: true
theme: beyondallroyal
paginate: true
lang: de
size: 16:9
---

<!-- _class: lead -->

# BeyondAllRoyal

Ein mobile-first 2D-RTS in Unity

Inspiriert von *Beyond All Reason* und *Clash Royale*

---

<!-- _class: section -->

# Idee

---

## Was ist BeyondAllRoyal?

- Genre: 2D Echtzeit-Strategie, mobile-first
- Inspiration: Beyond All Reason + Clash Royale
- Setting: futuristisch
- Modus (MVP): 1 vs 1 gegen eine NPC-KI
- Sieg: feindliches HQ zerstören

---

## Kein Mikro nötig

- Einheiten kämpfen vollständig autonom
- Kein Klicken auf jede einzelne Einheit
- Spieler entscheidet was gebaut wird, nicht wie gekämpft wird

---

## Zielpriorität der Einheiten

1. Feinde auf eigener Kartenseite
2. Gebäude in Reichweite
3. Einheiten in Reichweite
4. Gebäude ausser Reichweite
5. Einheiten ausser Reichweite

---

## Ressource: Metal

- Globaler Pool
- Sofort fällig bei Bau- oder Produktionsstart
- Gemeinsamer Flaschenhals für alle Gebäude

---

## Ressource: Energy

- Pro Gebäude ein eigener Puffer
- Füllt sich passiv über Zeit
- Fertig, sobald genug Energy da ist
- **Kein fixer Timer**
- Tesla Tower beschleunigt Nachbargebäude

---

## Das Konter-System

5 Einheiten, 2 Türme, ein Kreis:
jede Einheit kontert genau 2, wird von genau 2 gekontert

| Einheit | Kontert | Gekontert von |
|---|---|---|
| Soldier | Gunner, Explosive | Hovercraft, Tank |
| Heavy Gunner | Explosive, Hovercraft | Tank, Soldier |
| Explosive Spec. | Hovercraft, Tank | Soldier, Gunner |
| Hovercraft | Tank, Soldier | Gunner, Explosive |
| Heavy Tank | Soldier, Gunner | Explosive, Hovercraft |

Keine Allzweckwaffe. Für jeden gibt es ein Gegenmittel.

---

<!-- _class: section -->

# Demo

---

## Ablauf einer Partie

1. Hauptmenü: Modus + KI-Schwierigkeit wählen
2. Match-Start: symmetrische Karte, 2 Lanes
3. Aufbau: Gebäude aus dem Shop platzieren
4. Kampf: Einheiten greifen autonom an
5. Ende: HQ fällt, Endscreen erscheint

*(Live-Demo im Unity-Editor)*

---

## Das zeigen wir live

- Ghost-Preview beim Platzieren
- Energie-/Produktionsbalken
- Tesla Tower im Einsatz
- Konter-Kämpfe (z. B. Hovercraft vs. Tank)
- Death-Explosion
- Die NPC beim Bauen und Angreifen

---

<!-- _class: section -->

# Technik

---

## Prinzip: Daten statt Code

- Alle Werte leben in `ScriptableObjects`
- `UnitData`, `BuildingData`, `CounterChartData`, `MapLayoutData`
- Code liest nur, enthält keine Zahlen
- Balancing ändern, ohne Code anzufassen

---

## Muster: Vererbung für Gebäude

- Basisklasse `Building`: Energie, Bau, Schaden, Grid-Position
- Darauf aufbauend: Production, Tower, Tesla, Factory, HQ
- `HQ` = `MetalFactory` + `TeslaTower` + Selbstverteidigung
- Wiederverwendung statt Copy-Paste

---

## Muster: Registry statt Suche

- Naiv: jeden Frame die ganze Szene durchsuchen
- Stattdessen: `UnitRegistry`, `BuildingRegistry`
- Einheiten melden sich selbst an/ab
- O(1) Lookup statt Suche, jeden Frame

---

## Entscheid: Prioritätenliste statt Zustandsmaschine

- Kampf-KI folgt einer festen Zielliste
- Keine komplexe State Machine, keine Utility-KI
- Leicht nachvollziehbar, leicht zu debuggen
- Komplexität nur, wo sie etwas bringt

---

## Prinzip: gleiche Werte, anderes Tempo

- NPC bekommt zufällig 3 von 5 Gebäudetypen
- Baut nur über einer Metal-Reserve-Schwelle
- Schwierigkeitsgrad ändert nur das Tempo der KI
- Einheitenwerte bleiben für Spieler und KI gleich

---

<!-- _class: section -->

# Test

---

## Wie getestet wird

- `TestScene`: vorplatziertes Start-Loadout
- Kein Aufbau von Null bei jedem Testlauf
- Manuelles Playtesting und Balancing
- Pre-Commit-Hook scannt auf Secrets/Keys

---

## Bug-Story: Slot-Leak

- Problem: zerstörte Gebäude gaben ihren Slot nicht frei
- Platz auf der Karte ging "verloren"
- Fix: `MapGrid.RemoveBuilding` läuft jetzt zuverlässig
- Lehre: Registrierung/Deregistrierung muss lückenlos sein

---

## Offene Punkte

- Multiplayer: bewusst Post-MVP
- Nur Platzhalter-Sprites und Sounds bisher

---

<!-- _class: section -->

# Reflexion

---

## Was gut lief

- Daten statt Code: Balancing schnell und risikoarm
- Autonome Einheiten: wenig Komplexität für Spieler
- Konter-System: einfach, aber schwierig intuitiv darzustellen

---

## Herausforderungen

- Metal und Energy als zwei Bottlenecks: viel Iteration und Tuning
- UI kostet viel Zeit

---

## Nächste Schritte

- Echte Sprites und Sounds statt Platzhalter
- Breiteres Playtesting für KI-Schwierigkeit und Balancing
- UI verschönern
- Danach: Multiplayer als nächster grosser Meilenstein

---

<!-- _class: lead -->

# Fragen?

Danke für die Aufmerksamkeit
