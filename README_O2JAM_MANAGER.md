# O2Jam Music Manager (Direct Music Folder Manager) - C# WPF (.NET 8)

Application de bureau Windows native haute performance avec thème moderne sombre (Violet, Rose, Bleu), conçue pour gérer **directement et exclusivement le dossier `Music`** d'O2Jam sans aucun fichier d'index externe (`OJNList.dat`).

---

## 🎧 Lecteur Audio Épuré & Haute Fidélité (Cas A Exclusif)

Le lecteur audio extrait et joue directement la piste audio principale extraite du conteneur `.ojm` :

1. **Extraction Directe du Sample Principal (Cas A Pur) :**
   - Analyse automatique du conteneur `.ojm` (`M30` ou `OMC`).
   - Sélection du sample audio le plus volumineux et complet (la piste principale / BGM).
   - Décodage Ogg Vorbis / PCM pur en mémoire avec démarrage instantané (< 5 ms).
   - Suppression définitive de tout mixage hybride pour un son studio limpide, sans bruit ni désynchronisation.

2. **Normalisation Anti-Clipping à 98% :**
   - Dès le décodage, le pic maximal est mesuré et normalisé avec une marge de sécurité :
     $$\text{buffer} = \text{buffer} \times \left(\frac{0.98}{\text{maxPeak}}\right)$$
   - Zéro saturation, son fort, clair et parfaitement constant d'un morceau à l'autre.

3. **Curseur de Volume à 60% par Défaut :**
   - Le slider de volume démarre à **60%** au lancement de l'application.
   - Ajustement direct $\text{gain} = \frac{\text{slider}}{100.0}$.

---

## 🌐 Système Multilingue Dynamique (FR / EN / JA)

Un sélecteur de langue placé dans la barre supérieure permet de basculer instantanément en temps réel (sans redémarrage) entre 3 langues :
- **Français (`FR`)** (par défaut)
- **English (`EN`)**
- **日本語 (`JA` - Japonais)**

Traduction intégrale en direct : titres des colonnes, boutons d'action, menus contextuels, filtres, étiquettes du panneau d'édition, détails techniques, statuts et boîte de dialogue de réindexation.

---

## 🎨 Interface Sombre Intégrale & Barres de Défilement Élargies

- **Menus Déroulants (ComboBox - Statuts, Genre & Langues) :**
  - Fond sombre `#1E1B2E` avec écriture blanche `#FFFFFF` sur les sélecteurs fermés et ouverts.
  - Popup déroulante sombre avec bordure `#3B3B64` et survol/sélection violet `#7B2CBF` pour une lisibilité parfaite.
- **Barres de Défilement Ergonomiques Élargies (15px) :**
  - Largeur (verticale) et hauteur (horizontale) augmentées à **15px** pour une préhension facile à la souris.
  - Piste sombre `#151320`, curseur violet `#6C5CE7` avec marge de 2px et coins arrondis (`border-radius: 6px`).
  - Effet au survol violet vif `#E056FD` et au clic/glissement rose `#F43F5E`.
  - Flèches encombrantes supprimées pour un design fluide et moderne.

---

## 🌟 Informations Techniques Détaillees : Tailles .OJN & .OJM

Dans le panneau de droite, les informations techniques affichent désormais les tailles réelles distinctes sur le disque :
- **`Taille fichier .OJN :`** Taille exacte du chart `.ojn` (en Ko ou Mo avec le compte exact d'octets).
- **`Taille fichier .OJM :`** Taille réelle sur le disque du fichier audio `.ojm` associé (en Ko ou Mo, ou indication *"Manquant"* / *"Missing"* / *"なし"* s'il est absent).

---

## 🌟 Fonctionnalités Principales

1. **Scan Direct du Dossier `Music` & DataGrid Interactif :**
   - Détection automatique et synchronisée des paires `o2maXXX.ojn` et `o2maXXX.ojm`.
   - Affichage dynamique : Song ID, Noms de fichiers, Titre, Artiste, Noter, BPM, Niveaux (Ex/Nx/Hx), Durée, Statut Audio OJM.
   - Tri par colonnes, recherche instantanée multi-champs et filtre par présence audio OJM.

2. **Édition Directe des Métadonnées du Header .OJN :**
   - Modification en temps réel du Titre, Artiste, Noter, Genre, BPM, et des niveaux de difficulté (Easy, Normal, Hard).
   - Encodage CP949 avec fallback UTF-8 et Latin1 pour une compatibilité parfaite avec les charts coréens et internationaux.
   - Préservation stricte au bit près des partitions de notes (`0x12C` jusqu'à `note_offset[3]`).

3. **Gestion & Remplacement des Jaquettes :**
   - Extraction et affichage en direct de la jaquette JPG et de la miniature pixel-art BMP 8x8.
   - Importation d'une nouvelle image (PNG, JPG, BMP, WEBP) avec conversion automatique en JPEG et génération du BMP 8x8 24bpp standard de 246 octets.
   - Réinjection sans corruption à la fin du fichier avec mise à jour des tailles et offsets dans le header.

4. **Gestion de l'Ordre, Réindexation & Synchronisation Physique :**
   - Boutons ▲ Monter / ▼ Descendre pour réorganiser la liste.
   - Renommage d'ID individuel ou Réindexation séquentielle complète (ex: `101, 102, 103...`).
   - Algorithme atomique en 3 phases avec fichiers temporaires (`__tmp_...`) évitant toute collision de fichiers sur le disque.
   - Mise à jour simultanée du champ interne `song_id`, `old_song_id`, de la chaîne audio `"o2maXXX.ojm"`, et des fichiers physiques sur le disque.

---

## 🚀 Lancement Rapide

### Option 1 : Double-clic sur le lanceur
Double-cliquez sur [`launch_native_wpf.bat`](file:///A:/Antigravity%20app/launch_native_wpf.bat).

### Option 2 : Exécution directe du binaire compilé
- **Exécutable Ultra-Léger :** [`publish\O2JamMusicManager.exe`](file:///A:/Antigravity%20app/publish/O2JamMusicManager.exe) (~931 Ko, requiert le runtime .NET 8).
- **Exécutable Autonome (Standalone) :** [`publish_standalone\O2JamMusicManager.exe`](file:///A:/Antigravity%20app/publish_standalone/O2JamMusicManager.exe) (~72.0 Mo, 100% autonome, zéro prérequis).

---

## 🛠 Compilation & Tests

Utilisez le script interactif [`build_exe.bat`](file:///A:/Antigravity%20app/build_exe.bat) ou [`build.bat`](file:///A:/Antigravity%20app/build.bat) :
- `[1]` Compiler l'exécutable Ultra-Léger (`publish\O2JamMusicManager.exe`).
- `[2]` Compiler l'exécutable Standalone Autonome (`publish_standalone\O2JamMusicManager.exe`).
- `[3]` Exécuter la suite de tests unitaires MSTest (13 tests réussis).
- `[4]` Compiler la version Python via PyInstaller.
