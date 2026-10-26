---
status: offen            # offen | in-arbeit | blockiert | zur-abnahme | erfolgreich (setzt nur der Inhaber)
branch:                  # wp/<work-id>-<kurzname>, wird beim Beanspruchen eingetragen
bearbeiter:              # Name/Modell des Workers, wird beim Beanspruchen eingetragen
abhaengig_von: []        # Work-IDs, die vorher erfolgreich sein müssen
sicherheitsrelevant: nein  # ja, wenn Auth, Rechte, Krypto, Pipeline oder Deployment berührt werden
---

# <Titel des Arbeitspakets>

> Dateiname: `work/wp-<work-id>-<kurzname>.md`. Die Work-ID besteht aus 8 Hex-Zeichen (`openssl rand -hex 4`).
> Vor dem Anlegen prüfen, dass es die ID weder in `work/` noch in `work/archive/` schon gibt.

## Ziel
Was nach Abschluss anders ist, in 1–3 Sätzen. Warum das nötig ist.

## Hintergrund und Entscheidungen
Nur das, was der Worker wissen muss, um nicht selbst entscheiden zu müssen. Hier stehen getroffene Entscheidungen
samt Begründung. Offene Entscheidungen gehören nicht in ein Arbeitspaket, sondern werden vorher geklärt.

## Abgrenzung (gehört NICHT dazu)
- …

## Erlaubte Dateien
Nur diese Dateien dürfen im PR geändert oder angelegt werden (plus diese Paketdatei selbst):
- `…`

Muss etwas anderes geändert werden → **Abbruch** (siehe unten).

## Vorbedingungen
- [ ] Alle Pakete aus `abhaengig_von` liegen in `work/archive/`.
- [ ] Arbeit im DevContainer, `main` aktuell (`git switch main && git pull --ff-only`).
- [ ] …

## Beanspruchen
1. `git fetch origin --prune`
2. Prüfen, ob es den Branch schon gibt: `git branch -a --list "*wp/<work-id>*"`, außerdem `gh pr list --state all --search "<work-id>"`.
   Gibt es einen Treffer oder ist `status` nicht `offen` → **nicht anfangen**, dem Inhaber melden.
3. `git switch -c wp/<work-id>-<kurzname>`
4. Im Frontmatter `status: in-arbeit`, `branch` und `bearbeiter` eintragen.
5. `git commit -s -am "WP <work-id>: beansprucht"` und `git push -u origin HEAD`. Erst der gepushte Branch gilt als Beanspruchung.

## Einzelschritte
Jeder Schritt: was zu tun ist, der genaue Befehl bzw. die genaue Änderung und das erwartete Ergebnis.

### Schritt 1: …
- Tun: …
- Prüfen: `…` → erwartet: …

## Prüfroutinen (vor dem PR, alle müssen bestehen)
- [ ] `scripts/security-gate.sh` ist grün.
- [ ] `git diff --name-only origin/main...HEAD` enthält nur erlaubte Dateien.
- [ ] …

## Abnahmekriterien („erfolgreich“)
- [ ] Alle Prüfroutinen bestanden.
- [ ] Alle Pflicht-Checks im PR sind grün.
- [ ] …

## Abbruchbedingungen
Bei einem der folgenden Punkte **sofort aufhören**, `status: blockiert` setzen, Befund ins Protokoll schreiben und den Inhaber fragen. Nicht improvisieren, nichts abschwächen oder unterdrücken:
- Eine Änderung außerhalb der erlaubten Dateien wäre nötig.
- Ein Befehl liefert ein anderes Ergebnis als erwartet.
- Das Security Gate oder ein CI-Check ist rot, und die Ursache liegt nicht offensichtlich in einem eigenen Tippfehler.
- Die Aufgabe würde von `docs/SPEZIFIKATION.md` oder `docs/SICHERHEIT.md` abweichen.
- …

## Abschluss durch den Worker
1. Ergebnisprotokoll ausfüllen, `status: zur-abnahme` setzen.
2. Commit mit `git commit -s`, Push, PR gegen `main` mit Verweis auf diese Datei. Ist das Paket sicherheitsrelevant, im PR so kennzeichnen.
3. Nicht selbst mergen.

## Abnahme durch den Inhaber
Im PR-Branch `status: erfolgreich` setzen, `git mv work/wp-<work-id>-<kurzname>.md work/archive/`, danach Squash-Merge.

## Ergebnisprotokoll (füllt der Worker aus)
- Datum/Bearbeiter:
- PR:
- Ausgaben der Prüfroutinen (gekürzt):
- Abweichungen/Auffälligkeiten:
