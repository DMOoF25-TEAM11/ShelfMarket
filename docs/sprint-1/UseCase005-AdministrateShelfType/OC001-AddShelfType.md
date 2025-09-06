# OC001-AddShelfType

**Operation**: TilføjNyReolType(ReolNavn, Beskrivelse)

**Cross References**: UseCase005-AdministrateShelfType

**Preconditions**: 
- Ejer er logget ind og har adgang til at tilføje en ny reol.
- Har navigeret til "Rediger ReolType" siden.
- ReolNavn er unikt og ikke allerede i brug.
- ReolBeskrivelse ikke er tom.

**Postconditions**:
- En ny reoltype med det angivne ReolNavn og Beskrivelse.