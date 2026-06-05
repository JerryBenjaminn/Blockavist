# CLAUDE.md — Blockavist

## Projektin kuvaus

Blockavist on mobiili-puzzle-peli Androidille. Pelaaja ohjaa hahmoa joka liikkuu automaattisesti kentän läpi. Pelaajan tehtävä on napauttaa/tuhota kentän elementtejä sormella, jotta hahmo pääsee maaliin. Peli julkaistaan Google Play Storeen ilmaiseksi mainoksilla rahoitettuna.

**Engine:** Unity (LTS-versio)  
**Kohdeplatformi:** Android (Google Play)  
**Monetisaatio:** Ilmainen + interstitial-mainokset (Google AdMob)  
**Julkaisutavoite:** 20 kenttää, 2 maailmaa

---

## Core Loop

1. Pelaaja aloittaa kentän
2. Hahmo alkaa liikkua automaattisesti eteenpäin
3. Hahmo kääntyy törmätessään esteeseen
4. Hahmo tippuu alaspäin jos alla ei ole pintaa, jatkaa liikkumista seuraavalta tasolta
5. Pelaaja napauttaa elementtejä sormella muuttaakseen kentän rakennetta
6. Hahmo saavuttaa maalin → kenttä läpäisty → seuraava kenttä avautuu

**Game over:** Hahmo osuu vaaraan tai tippuu kentän ulkopuolelle.

---

## Arkkitehtuuri

### Kenttäsysteemi

Kentät rakennetaan **ScriptableObject**-pohjaisesti. Jokainen kenttä on oma `.asset`-tiedostonsa joka sisältää:
- Kenttädata (tiilikartta / elementtien sijainnit)
- Kentän nimi ja numero
- Maailma johon kenttä kuuluu
- Tähtiraja-arvot (optionaalinen myöhemmälle versiolla)

Uuden kentän lisääminen = uusi ScriptableObject-asset. Ei koodimuutoksia.

### LevelManager

```
LevelManager
├── Lataa kentän ScriptableObjectista
├── Instantioi elementit
├── Seuraa hahmon tilaa (elossa / kuollut / maalissa)
└── Triggeröi kentän läpäisyn / game overin
```

### Elementtisysteemi

Kaikki kenttäelementit perivät yhteisen `TileElement`-baser classin:

```
TileElement (abstract)
├── IsDestructible : bool
├── OnPlayerTouch()
├── OnPlayerCollide()
└── Render()
```

Uuden elementin lisääminen = uusi luokka joka perii `TileElement`:in. Ei muutoksia olemassa olevaan koodiin.

---

## MVP-elementit

| Elementti | Kuvaus | Interaktio |
|---|---|---|
| **Tuhottava tiili** | Normaali tiili joka hajoaa | Napauta → häviää |
| **Tuhoamaton tiili** | Kiinteä este | Ei voi tuhota, hahmo kääntyy |
| **Piikit** | Staattinen vaara | Hahmo kuolee kosketuksesta |
| **Maali** | Kentän läpäisypiste | Hahmo saavuttaa → voitto |

### Post-MVP elementit (tuleville versioille)
- Launch pad (ponnauttaa hahmon ylös)
- Liikkuva vaara
- Lukittu tiili (vaatii avaimen)
- Liukas pinta
- Pomppupinta/trampoliini

---

## Kenttärakenne

**Julkaisu: 20 kenttää, 2 maailmaa**

| Maailma | Kentät | Teema | Elementit |
|---|---|---|---|
| Maailma 1 | 1–10 | Peruskivet | Tuhottava tiili, tuhoamaton tiili, piikit |
| Maailma 2 | 11–20 | *(suunnitellaan myöhemmin)* | Kaikki MVP-elementit yhdisteltynä |

**Vaikeuskäyrä:**
- Kentät 1–3: tutoriaali, yksi mekaniikka kerrallaan
- Kentät 4–7: yhdistelmiä, ratkaisut selkeitä
- Kentät 8–10: vaatii suunnittelua
- Maailma 2: nouseva vaikeus, "aha-hetket" jokaisessa kentässä

**Maailmojen lisääminen päivityksissä:**  
Uusi maailma = uusi kansio ScriptableObject-asseteille + UI:hin uusi world-node. Ei arkkitehtuurimuutoksia.

---

## UI & Navigaatio

### Ruudut
1. **Main Menu** — logo, Play-nappi, Settings
2. **World Select** — maailmat ruudukkona, lukittu/auki visuaalisesti
3. **Level Select** — valitun maailman kentät ruudukkona (Candy Crush -tyylinen layout)
4. **Game** — pelattava näkymä
5. **Level Complete** — tulos, Next Level / World Select
6. **Game Over** — Retry / World Select

### Mobiili-UX
- Kaikki interaktio yhden sormen napautuksella
- Ei näytöllä olevia ohjauspelejä (hahmo liikkuu automaattisesti)
- Pause-nappi näytön kulmassa

---

## Monetisaatio

**Google AdMob — Interstitial-mainokset**
- Mainos näytetään kentän läpäisyn jälkeen joka 3. kenttä
- Ei mainoksia kesken pelin
- Toteutus: AdMob Unity Plugin

**Ei in-app purchaseja MVP:ssä.**

---

## Tekninen stack

| Asia | Valinta |
|---|---|
| Engine | Unity 6000.3.11f1 LTS |
| Kieli | C# |
| Kenttädata | ScriptableObjects |
| Kenttäeditori | Unity TileMap + custom inspector |
| Mainokset | Google AdMob (Google Mobile Ads Unity Plugin) |
| Versionhallinta | Git + GitHub |
| Build | Unity Android Build + Google Play Console |

---

## Kehitysvaiheet

### Vaihe 1 — Core (prototyyppi)
- [ ] Hahmon liikkumislogiikka (automaattinen eteneminen, kääntyminen, putoaminen)
- [ ] TileElement-arkkitehtuuri
- [ ] Tuhottava tiili + tuhoamaton tiili
- [ ] Piikit (game over)
- [ ] Maali (level complete)
- [ ] Yksi testikentttä

### Vaihe 2 — Kenttäsysteemi
- [ ] ScriptableObject-pohjainen kenttädata
- [ ] LevelManager
- [ ] 5 ensimmäistä kenttää (Maailma 1)

### Vaihe 3 — UI & navigaatio
- [ ] Main Menu
- [ ] World Select + Level Select
- [ ] Level Complete + Game Over -ruudut
- [ ] Kentän lukitus/avaussysteemi

### Vaihe 4 — Sisältö
- [ ] Kaikki 20 kenttää suunniteltu ja toteutettu
- [ ] Vaikeuskäyrän testaus

### Vaihe 5 — Monetisaatio & julkaisu
- [ ] AdMob integraatio
- [ ] Android build & optimointi
- [ ] Google Play Console -tili + Store listing
- [ ] Ikärajaluokittelu, privacy policy
- [ ] Julkaisu

---

## Devlog & markkinointi

**Alusta:** TikTok (ensisijainen), mahdollisesti YouTube Shorts

**Sisältöideoita:**
- "Tein mobiilipelin itse" -sarja
- Level design prosessi (ennen/jälkeen)
- Claude Code kehityksessä — miltä se näyttää
- Bugeja ja niiden korjauksia
- Julkaisuprosessi Google Playhin

**Tavoite:** Consistent upload kehityksen aikana, ei täydellisyyttä vaan aitoa prosessia.

---

## Tunnetut rajoitukset & riskit

| Riski | Mitigaatio |
|---|---|
| Android build -ongelmat | Varaa aikaa vaiheen 5 alussa |
| AdMob-hyväksyntä kestää | Hae AdMob-tili hyvissä ajoin |
| Kenttäsuunnittelu vie aikaa | Aloita kenttien suunnittelu paperilla jo vaihe 2:n aikana |
| Scope creep | Post-MVP-lista olemassa — uudet ideat sinne, ei MVP:hen |
