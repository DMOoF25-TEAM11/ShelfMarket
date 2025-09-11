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
1. Systemet viser en liste over eksisterende reoltyper med mulighed for at redigere eller slette hver reoltype.
1. Hvis ejer ønsker at tilføje en ny reoltype, indtastes oplysninger og vælger “Tilføj”
1. Systemet validerer input
1. Systemet opretter reoltypen og gemmer den i databasen.
1. Systemet viser en bekræftelse og opdaterer reoltypelisten.
1. Ejer ser den nye reoltype i listen
1. Gentag trin 4-8 for at tilføje flere reoltyper.
1. Hvis ejer vælger en reoltype fra listen og formularen udfyldes med reoltypens oplysninger
1. Hvis ejer vælger "Slet"
1. Systemet fjerner reoltypen fra databasen og opdaterer listen.
1. Systemet viser en bekræftelse på sletningen.
1. Hvis ejer vælger en reoltype fra listen og formularen udfyldes med reoltypens oplysninger
1. Ejer redigerer oplysningerne og klikker "Gem"
1. Systemet validerer input
1. Systemet opdaterer reoltypen i databasen.
1. Systemet viser en bekræftelse og opdaterer reoltypelisten.
1. Ejer ser de opdaterede oplysninger i listen
1. Gentag trin 10-15 for at redigere flere reoltyper.
1. Ejer går tilbage til forsiden ved at navigere til "Forside"
