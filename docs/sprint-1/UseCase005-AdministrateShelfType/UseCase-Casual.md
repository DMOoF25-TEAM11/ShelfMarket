# Use Case 005 brief - Administrere reol typer

**Use case name:** UseCase005-AdministrateShelfType
**Scope:** Systemet er under udvikling  
**Level:** User goal  
**Primary Actors:** Ejer  
**Stakeholders and Interests:** 
- Ejer ønsker at adminstrere reol typer i systemet
- Personale ønsker at se opdaterede reoloplysninger
- Kunder (lejere) ønsker at ændre reoltype.

**Preconditions:**  
- Ejer er logget ind og har åbnet Windows-applikationen  
- Ejer har navigeret til administrations -> reol typer  
**Postconditions:**  
- hvis reoltype er tilføjet, slettet eller redigeret, er ændringerne gemt i databasen og vises i reoltypelisten.

**Main success scenario:**  
1. Ejer åbner programmet på navigerer til administrations -> reol typer
1. Systemet viser en formular med felter for navn og beskrivelse
1. Systemet viser eksisterende reoltyper med mulighed for at redigere eller slette hver reoltype.
1. Hvis ejer ønsker at tilføje en ny reoltype, indtastes oplysninger og vælger “Tilføj”
1. Systemet validerer input og opretter reoltypen og gemmer den i databasen.
1. Systemet viser en bekræftelse.
1. Ejer ser den nye reoltype.
1. Gentag trin 4-7 for at tilføje flere reoltyper.
1. Hvis ejer vælger en reoltype.
1. Hvis ejer vælger slette.
1. Systemet fjerner reoltypen.
1. Systemet viser en bekræftelse på sletningen.
1. Gentag trin 9-12 for at slette flere reoltyper.
1. Hvis ejer vælger en reoltype fra listen og formularen udfyldes med reoltypens oplysninger
1. Ejer redigerer oplysningerne og vælger gem
1. Systemet opdaterer reoltypen i databasen.
1. Systemet viser en bekræftelse og opdaterer reoltypelisten.
1. Gentag trin 9, 14-17 for at redigere flere reoltyper.
1. Ejer går tilbage til forsiden ved at navigere til "Forside"

**Extensions (alternatives):**  
- 4a./15a. Reoltype er ikke unik:  
  - Systemet viser en fejlmeddelelse og beder ejeren vælge en anden navn for reol type.  
- 5a./11a./16a. Systemfejl ved oprettelse eller opdatering (f.eks. databasefejl):  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  