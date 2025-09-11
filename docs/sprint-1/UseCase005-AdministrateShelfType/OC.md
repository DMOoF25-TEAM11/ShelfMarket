# OC - Operations Contracts

## Indholdsfortegnelse

- [OC000-Naviger Til Administere Reol Typer](#oc000-naviger-til-administere-reol-typer)
- [OC001-AddShelfType](#oc001-addshelftype)
- [OC002-Vælg Reol Type](#oc002-vælg-reol-type)
- [OC003-Rediger Reol Type](#oc003-rediger-reol-type)
- [OC004-Slet Reol Type](#oc004-slet-reol-type)
- [OC005-Naviger Til Forside](#oc005-naviger-til-forside)


## OC000-Naviger Til Administere Reol Typer

**Operation**: NavigerTilReolTyper()

**Cross References**: SSD.mmd (første Ejer->>System kald), UseCase-Casual.md trin 1–2

**Preconditions**:
- Ejer er logget ind.
- Hovednavigation er tilgængelig.

**Postconditions**:
- Reoltype-side er aktiv.
- Vis liste over eksisterende reoltyper.
- Klar til tilføj / vælg / redigér / slet handlinger.

# OC001-AddShelfType

**Operation**: TilføjNyReolType(ReolNavn, Beskrivelse)

**Cross References**: SSD.mmd (Ejer->>System: tilføjReolType), UseCase-Casual.md trin 3–9
**Preconditions**: 
- Ejer er logget ind og har adgang til at tilføje en ny reol.
- Har navigeret til "Rediger ReolType" siden.
- ReolNavn er unikt og ikke allerede i brug.
- ReolBeskrivelse ikke er tom.

**Postconditions**:
- En ny reoltype med det angivne ReolNavn og Beskrivelse.

## OC002-Vælg Reol Type

**Operation**: VælgReolType(ReolTypeId)

**Cross References**: SSD.mmd (Ejer->>System: vælgReolType), UseCase-Casual.md trin 10

**Preconditions**:
- Reoltypeliste er indlæst.
- ReolTypeId findes og er ikke arkiveret.
- Ingen igangværende ikke-gemte ændringer blokerer valg (eller er accepteret).

**Postconditions**:
- Formular felter udfyldt med valgt reoltypes data.
- Redigeringstilstand aktiveret (redigér / slet mulige).
- Eventuelle tidligere midlertidige input erstattet af de valgte data.

## OC003-Rediger Reol Type

**Operation**: GemReolType(ReolTypeId, ReolNavn, ReolBeskrivelse)

**Cross References**: SSD.mmd (Ejer->>System: gemReolType), UseCase-Casual.md trin 15–18

**Preconditions**:
- En eksisterende reoltype er valgt (ReolTypeId).
- ReolNavn er ikke tomt og opfylder formatkrav.
- ReolBeskrivelse kan være tom men ikke overskrider max længde.
- ReolNavn er unikt (ikke brugt af anden type) medmindre uændret.
- Ingen systemfejl i databasen (forbindelse OK).

**Postconditions**:
- Ændringer gemt i databasen (optimistisk concurrency passeret).
- Liste opdateret med nye værdier.
- Bekræftelse vist.
- Formular felter nulstillet.

## OC004-Slet Reol Type

**Operation**: SletReolType(ReolTypeId)

**Cross References**: SSD.mmd (Ejer->>System: sletReolType), UseCase-Casual.md trin 11–13

**Preconditions**:
- ReolTypeId findes.
- Ingen afhængige entiteter (fx aktive kontrakter) refererer typen.
- Bekræftelsesdialog accepteret (hvis UI kræver det).

**Postconditions**:
- Reoltype fjernet fra databasen (blød eller hård slet afhængigt af design).
- Liste opdateret uden posten.
- Bekræftelse eller fejl vist (afhængigt af udfald).

# OC005-Naviger Til Forside

**Operation**: NavigerTilForside()

**Cross References**: SSD.mmd (Ejer->>System: navigerTilForside), UseCase-Casual.md sidste trin

**Preconditions**:
- Reoltypeadministrationsvisning er aktiv.
- Ingen kritiske ubekræftede ændringer (eller bruger har accepteret at forlade).

**Postconditions**:
- Forside vises.
- Midlertidig redigeringstilstand afsluttet.
- Ingen yderligere operationer på reoltyper i nuværende kontekst.
