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
1. 
1. Hvis ejer vælger en reoltype.
1. Hvis ejer vælger slette.
1. Systemet fjerner reoltypen.
1. Systemet viser en bekræftelse på sletningen.
1. 
1. Hvis ejer vælger en reoltype fra listen og formularen udfyldes med reoltypens oplysninger
1. Ejer redigerer oplysningerne og vælger gem
1. Systemet opdaterer reoltypen i databasen.
1. Systemet viser en bekræftelse og opdaterer reoltypelisten.
1. Gentag trin 2-17 for at redigere/tilføje flere reoltyper.
1. Ejer går tilbage til forsiden ved at navigere til "Forside"
