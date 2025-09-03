# Use Case 001 causal

**Use case name:** Tilføj ny reol  
**Scope:** Systemet er under udvikling  
**Level:** User goal  
**Primary Actors:** Ejer  
**Stakeholders and Interests:** 
 - Ejer ønsker at tilføje en ny reol for at udvide udlejningsmulighederne

**Preconditions:** Ejer er logget ind og har åbnet Windows-applikationen  
**Postconditions:** En ny reol er tilføjet til systemet  
**Main success scenario:**  
1. Ejer åbner programmet pg navigerer til administrations -> reol
1. Systemet viser en formular med felter for placering og type
1. Programmet viser tydeligt hvilke felter der er påkrævet og ikke er udfyldt. 
    - Tjekker at type er valg
    - Tjekker at placering er udfyldt
    - Tjekker at placering er unik
1. Ejeren indtaster oplysninger og klikker “Tilføj”
1. Systemet opretter reolen og gemmer den i databasen.
1. Systemet viser en bekræftelse og opdaterer reollisten.

**Extensions (alternatives):**  
- 3a.  Placering er ikke unik:  
  - Systemet viser en fejlmeddelelse og beder ejeren vælge en anden placering.  
- 4a. Systemfejl ved oprettelse (f.eks. databasefejl)  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  
  