# OC002-EditShelfType

**Operation**: Gem(ReolTypeId, ReolNavn, Beskrivelse)

**Cross References**: UseCase005-AdministrateShelfType

**Preconditions**:
- Ejer er logget ind og har adgang til at redigere en eksisterende reoltype.
- Har navigeret til "Rediger ReolType" siden.
- ReolTypeId eksisterer i systemet.
- ReolNavn er unikt og ikke allerede i brug af andre reoltyper.
- NyBeskrivelse er gyldig og accepteret af systemet.

**Postconditions**:
- Den eksisterende reoltype med det angivne ReolTypeId er opdateret med den nye Beskrivelse.
