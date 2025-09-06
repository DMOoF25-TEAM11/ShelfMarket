# OC002-EditShelf

**Operation**: Gem(ReolId, ReolNummer, ReolType)

**Cross References**: UseCase001-AdministrateShelf

**Preconditions**:
- Ejer er logget ind og har adgang til at redigere en eksisterende reol.
- Har navigeret til "Rediger Reol" siden.
- ReolId eksisterer i systemet.
- ReolNummer er unikt og ikke allerede i brug af andre reoler.
- NyReolType er gyldig og accepteret af systemet.

**Postconditions**:
- Den eksisterende reol med det angivne ReolId er opdateret med den nye ReolType.
