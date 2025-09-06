# OC001-AddShelf

**Operation**: TilføjNyReol(ReolNummer, ReolType)

**Cross References**: UseCase001-AdministrateShelf

**Preconditions**: 
- Ejer er logget ind og har adgang til at tilføje en ny reol.
- Har navigeret til "Rediger Reol" siden.
- ReolNummer er unikt og ikke allerede i brug.
- ReolType er gyldig og accepteret af systemet.

**Postconditions**:
- En ny reol med det angivne ReolNummer og ReolType er oprettet i systemet.