# Card Back Store System - Complete Architecture & Implementation Guide

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Design](#architecture-design)
3. [Data Models & Schemas](#data-models--schemas)
4. [Card Back Catalog](#card-back-catalog)
5. [Implementation Roadmap](#implementation-roadmap)
6. [Backend Abstraction Layer](#backend-abstraction-layer)
7. [Security Considerations](#security-considerations)
8. [Cost Analysis](#cost-analysis)
9. [Steam Integration](#steam-integration-overview)

---

## System Overview

### Goals
- Allow players to purchase cosmetic card back designs
- Support custom photo uploads (3 max per player)
- Work offline-first with optional cloud sync
- Scale from indie to millions of users
- Enable backend switching (Azure → Firebase → Custom) without game code changes

### Key Features
- **195+ Country Flags** (UNESCO list)
- **150+ Sports Team Logos** (AFL, NFL, NHL, Premier League, Serie A, Ligue 1, etc.)
- **Custom Photo Support** (with auto-resize, local storage only)
- **One-Time Purchases** (platform-specific via StoreKit2/Google Play Billing)
- **Offline Capability** (cached store data, fallback when network unavailable)
- **Cross-Language Support** (card back names localized)

---

## Architecture Design

### High-Level System Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                        GAME CLIENT                               │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Presentation Layer (UI Screens)                             │ │
│  │ ├─ CardBackStoreScreen                                      │ │
│  │ ├─ CardBackUploadScreen                                     │ │
│  │ └─ SettingsScreen (Card Back selection)                     │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                            ↓ Uses                                 │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Business Logic Layer (CardBackManager)                      │ │
│  │ ├─ Inventory Management (CRUD)                              │ │
│  │ ├─ Custom Photo Processing (resize, validate)               │ │
│  │ └─ Selection & Persistence                                  │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                            ↓ Uses                                 │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Service Abstraction Layer (ICardBackStore)                  │ │
│  │ Enables backend-agnostic code                               │ │
│  └─────────────────────────────────────────────────────────────┘ │
│         ↓ Implemented By (Dependency Injection)                   │
│  ┌──────────────────┬──────────────────┬──────────────────┐      │
│  │ AzureCardBack    │ FirebaseCardBack │ LocalOnlyCardBack│      │
│  │ Store            │ Store            │ Store            │      │
│  └──────────────────┴──────────────────┴──────────────────┘      │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Local Storage                                               │ │
│  │ ├─ CardBacksInventory.json                                  │ │
│  │ ├─ CustomCardBacks/                                         │ │
│  │ │  ├─ custom_uuid_1.png                                     │ │
│  │ │  ├─ metadata.json                                         │ │
│  │ │  └─ ...                                                   │ │
│  │ └─ CachedStoreData/                                         │ │
│  │    ├─ CardBackStore.json                                    │ │
│  │    └─ Categories.json                                       │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
         │
         │ (Only on Store Access)
         │ HTTP/HTTPS
         ↓
┌──────────────────────────────────────────────────────────────────┐
│                    CLOUD BACKEND (Azure/Firebase)                │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Static Content Delivery (CDN)                               │ │
│  │ ├─ CardBackStore.json (catalog)                             │ │
│  │ ├─ Categories.json (metadata)                               │ │
│  │ └─ Official Card Back Images                                │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ IAP Validation (API)                                        │ │
│  │ ├─ Validate Apple Store receipts                            │ │
│  │ ├─ Validate Google Play receipts                            │ │
│  │ └─ Record ownership (optional database)                     │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```
User Opens Store
    ↓
CardBackStoreScreen.LoadContent()
    ↓
CardBackManager.GetAvailableCardBacks()
    ↓
ICardBackStore.GetAvailableCardBacksAsync()
    ├─ Try: Fetch from cloud (CardBackStore.json)
    ├─ Catch (Network Error): Use cached version
    └─ Parse & merge with local inventory
    ↓
Display store UI with:
├─ Downloaded official card backs
├─ Already owned items (marked)
├─ Already owned custom photos
└─ Available slots for new custom photos
    ↓
User Purchases Card Back
    ├─ Platform IAP (StoreKit2 / Google Play)
    ├─ Receive receipt
    ├─ Send to backend for validation
    ├─ Backend confirms ownership
    └─ Download card back image
    ↓
User Uploads Custom Photo
    ├─ Select photo from camera roll
    ├─ Resize to card dimensions (256x384)
    ├─ Compress as PNG
    ├─ Save to CustomCardBacks/
    └─ Update CardBacksInventory.json
```

---

## Data Models & Schemas

### 1. CardBacksInventory.json (Local)

**Location:** 
- Windows: `%AppData%/CartBlanche/Blackjack/CardBacksInventory.json`
- macOS: `~/Library/Application Support/CartBlanche/Blackjack/CardBacksInventory.json`
- Android: `/sdcard/Android/data/com.yourcompany.blackjack/files/CardBacksInventory.json`
- iOS: `~/Documents/CartBlanche/Blackjack/CardBacksInventory.json`

**Structure:**
```json
{
  "version": 1,
  "platform": "Windows",
  "lastUpdated": "2024-01-20T10:30:00Z",
  "ownedCardBacks": [
    {
      "id": "cardback_flags_usa",
      "name": "🇺🇸 USA Flag",
      "type": "official",
      "category": "flags",
      "purchaseDate": "2024-01-15T10:30:00Z",
      "price": 0.99,
      "currency": "USD"
    },
    {
      "id": "cardback_sports_nfl_patriots",
      "name": "🏈 New England Patriots",
      "type": "official",
      "category": "sports",
      "purchaseDate": "2024-01-15T10:35:00Z",
      "price": 0.99,
      "currency": "USD"
    }
  ],
  "selectedCardBack": "cardback_flags_usa",
  "customCardBacks": [
    {
      "id": "custom_550e8400-e29b-41d4-a716-446655440000",
      "name": "My Photo",
      "filename": "custom_550e8400.png",
      "createdDate": "2024-01-20T14:22:00Z",
      "imageSize": 45382,
      "originalWidth": 1024,
      "originalHeight": 1536,
      "resizedWidth": 256,
      "resizedHeight": 384
    }
  ],
  "customPhotoCount": 1,
  "maxCustomPhotos": 3
}
```

### 2. CustomCardBacks/metadata.json (Local)

**Purpose:** Index all custom photos with checksums for integrity checking

```json
{
  "version": 1,
  "customBacks": [
    {
      "id": "custom_550e8400-e29b-41d4-a716-446655440000",
      "filename": "custom_550e8400.png",
      "originalSize": {
        "width": 1024,
        "height": 1536
      },
      "resizedTo": {
        "width": 256,
        "height": 384
      },
      "fileSizeBytes": 45382,
      "uploadedDate": "2024-01-20T14:22:00Z",
      "sha256Checksum": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z"
    }
  ]
}
```

### 3. CardBackStore.json (Cloud)

**Location:** Azure Blob Storage / Firebase Cloud Storage
**CDN URL:** `https://cardbacks.azureedge.net/store/CardBackStore.json` (or Firebase equivalent)
**Updated:** Quarterly (manually)

```json
{
  "version": 2,
  "lastUpdated": "2024-01-20T10:00:00Z",
  "releaseNotes": "Q1 2024: Added 10 new country flags and 5 new sports teams",
  "basePrice": {
    "USD": 0.99,
    "EUR": 0.89,
    "GBP": 0.79,
    "JPY": 110,
    "CAD": 1.29,
    "AUD": 1.49
  },
  "cardBacks": [
    {
      "id": "cardback_flags_usa",
      "name": "🇺🇸 USA Flag",
      "description": "Stars and stripes",
      "category": "flags",
      "subcategory": "north_america",
      "rarity": "common",
      "price": {
        "USD": 0.99,
        "EUR": 0.89,
        "GBP": 0.79
      },
      "images": {
        "thumbnail": "https://cardbacks.azureedge.net/official/cardback_flags_usa/thumbnail.jpg",
        "preview": "https://cardbacks.azureedge.net/official/cardback_flags_usa/preview.jpg",
        "full": "https://cardbacks.azureedge.net/official/cardback_flags_usa/full.png"
      },
      "isAvailable": true,
      "releaseDate": "2024-01-01T00:00:00Z",
      "region": "all",
      "tags": ["flag", "patriotic", "usa"]
    },
    {
      "id": "cardback_sports_nfl_patriots",
      "name": "🏈 New England Patriots",
      "description": "NFL team logo",
      "category": "sports",
      "subcategory": "nfl",
      "rarity": "common",
      "price": {
        "USD": 0.99
      },
      "images": {
        "thumbnail": "https://cardbacks.azureedge.net/official/cardback_sports_nfl_patriots/thumbnail.jpg",
        "preview": "https://cardbacks.azureedge.net/official/cardback_sports_nfl_patriots/preview.jpg",
        "full": "https://cardbacks.azureedge.net/official/cardback_sports_nfl_patriots/full.png"
      },
      "isAvailable": true,
      "releaseDate": "2024-01-15T00:00:00Z",
      "region": "all",
      "tags": ["nfl", "sports", "football"]
    }
  ],
  "categories": [
    {
      "id": "flags",
      "name": "Country Flags",
      "icon": "https://cardbacks.azureedge.net/categories/flags.png",
      "description": "Represent your nation",
      "itemCount": 195,
      "subcategories": [
        { "id": "north_america", "name": "North America" },
        { "id": "south_america", "name": "South America" },
        { "id": "europe", "name": "Europe" },
        { "id": "asia", "name": "Asia" },
        { "id": "africa", "name": "Africa" },
        { "id": "oceania", "name": "Oceania" }
      ]
    },
    {
      "id": "sports",
      "name": "Sports Teams",
      "icon": "https://cardbacks.azureedge.net/categories/sports.png",
      "description": "Support your favorite teams",
      "itemCount": 150,
      "subcategories": [
        { "id": "nfl", "name": "NFL", "teams": 32 },
        { "id": "nhl", "name": "NHL", "teams": 32 },
        { "id": "premier_league", "name": "Premier League", "teams": 20 },
        { "id": "serie_a", "name": "Serie A", "teams": 20 },
        { "id": "ligue_1", "name": "Ligue 1", "teams": 20 },
        { "id": "afl", "name": "AFL", "teams": 18 },
        { "id": "other_european", "name": "Other European", "teams": 8 }
      ]
    }
  ]
}
```

### 4. Categories.json (Cloud)

**Lightweight version for faster loading**

```json
{
  "version": 1,
  "lastUpdated": "2024-01-20T10:00:00Z",
  "categories": [
    {
      "id": "flags",
      "name": "Country Flags",
      "icon": "https://cardbacks.azureedge.net/categories/flags.png",
      "itemCount": 195,
      "subcategories": [
        { "id": "north_america", "name": "North America", "count": 23 },
        { "id": "south_america", "name": "South America", "count": 12 },
        { "id": "europe", "name": "Europe", "count": 44 },
        { "id": "asia", "name": "Asia", "count": 48 },
        { "id": "africa", "name": "Africa", "count": 54 },
        { "id": "oceania", "name": "Oceania", "count": 14 }
      ]
    },
    {
      "id": "sports",
      "name": "Sports Teams",
      "icon": "https://cardbacks.azureedge.net/categories/sports.png",
      "itemCount": 150,
      "subcategories": [
        { "id": "nfl", "name": "NFL", "count": 32 },
        { "id": "nhl", "name": "NHL", "count": 32 },
        { "id": "premier_league", "name": "Premier League", "count": 20 },
        { "id": "serie_a", "name": "Serie A", "count": 20 },
        { "id": "ligue_1", "name": "Ligue 1", "count": 20 },
        { "id": "afl", "name": "AFL", "count": 18 },
        { "id": "other_european", "name": "Other European", "count": 8 }
      ]
    }
  ]
}
```

---

## Card Back Catalog

### Pricing Structure

**All card backs:** $0.99 USD (converted to local currency)

**Conversion rates (example):**
- 1 USD = 0.89 EUR
- 1 USD = 0.79 GBP
- 1 USD = 110 JPY
- 1 USD = 1.29 CAD
- 1 USD = 1.49 AUD

### Category 1: Country Flags (195 items)

**Total Flags:** 195 (all UN-recognized nations + 3 territories)

**Organized by Region:**

#### North America (23 flags)
- 🇺🇸 United States
- 🇨🇦 Canada
- 🇲🇽 Mexico
- 🇧🇿 Belize
- 🇨🇷 Costa Rica
- 🇸🇻 El Salvador
- 🇬🇹 Guatemala
- 🇭🇳 Honduras
- 🇳🇮 Nicaragua
- 🇵🇦 Panama
- 🇦🇬 Antigua and Barbuda
- 🇧🇸 Bahamas
- 🇧🇧 Barbados
- 🇩🇲 Dominica
- 🇩🇴 Dominican Republic
- 🇬🇩 Grenada
- 🇭🇹 Haiti
- 🇯🇲 Jamaica
- 🇰🇳 Saint Kitts and Nevis
- 🇱🇨 Saint Lucia
- 🇻🇨 Saint Vincent and the Grenadines
- 🇹🇹 Trinidad and Tobago
- 🇨🇺 Cuba

#### South America (12 flags)
- 🇦🇷 Argentina
- 🇧🇴 Bolivia
- 🇧🇷 Brazil
- 🇨🇱 Chile
- 🇨🇴 Colombia
- 🇪🇨 Ecuador
- 🇬🇾 Guyana
- 🇵🇪 Peru
- 🇵🇾 Paraguay
- 🇸🇷 Suriname
- 🇺🇾 Uruguay
- 🇻🇪 Venezuela

#### Europe (44 flags)
- 🇦🇱 Albania
- 🇦🇩 Andorra
- 🇦🇹 Austria
- 🇧🇾 Belarus
- 🇧🇪 Belgium
- 🇧🇦 Bosnia and Herzegovina
- 🇧🇬 Bulgaria
- 🇭🇷 Croatia
- 🇨🇾 Cyprus
- 🇨🇿 Czech Republic
- 🇩🇰 Denmark
- 🇪🇪 Estonia
- 🇫🇮 Finland
- 🇫🇷 France
- 🇩🇪 Germany
- 🇬🇷 Greece
- 🇭🇺 Hungary
- 🇮🇸 Iceland
- 🇮🇪 Ireland
- 🇮🇹 Italy
- 🇽🇰 Kosovo
- 🇱🇻 Latvia
- 🇱🇮 Liechtenstein
- 🇱🇹 Lithuania
- 🇱🇺 Luxembourg
- 🇲🇰 North Macedonia
- 🇲🇹 Malta
- 🇲🇩 Moldova
- 🇲🇨 Monaco
- 🇲🇪 Montenegro
- 🇳🇱 Netherlands
- 🇳🇴 Norway
- 🇵🇱 Poland
- 🇵🇹 Portugal
- 🇷🇴 Romania
- 🇷🇺 Russia
- 🇸🇲 San Marino
- 🇷🇸 Serbia
- 🇸🇰 Slovakia
- 🇸🇮 Slovenia
- 🇪🇸 Spain
- 🇸🇪 Sweden
- 🇨🇭 Switzerland
- 🇺🇦 Ukraine
- 🇬🇧 United Kingdom

#### Asia (48 flags)
- 🇦🇫 Afghanistan
- 🇦🇿 Azerbaijan
- 🇧🇭 Bahrain
- 🇧🇩 Bangladesh
- 🇧🇹 Bhutan
- 🇧🇳 Brunei
- 🇰🇭 Cambodia
- 🇨🇳 China
- 🇮🇳 India
- 🇮🇩 Indonesia
- 🇮🇷 Iran
- 🇮🇶 Iraq
- 🇮🇱 Israel
- 🇯🇵 Japan
- 🇯🇴 Jordan
- 🇰🇿 Kazakhstan
- 🇰🇵 North Korea
- 🇰🇷 South Korea
- 🇰🇼 Kuwait
- 🇰🇬 Kyrgyzstan
- 🇱🇦 Laos
- 🇱🇧 Lebanon
- 🇲🇾 Malaysia
- 🇲🇻 Maldives
- 🇲🇳 Mongolia
- 🇲🇲 Myanmar
- 🇳🇵 Nepal
- 🇴🇲 Oman
- 🇵🇰 Pakistan
- 🇵🇸 Palestine
- 🇵🇭 Philippines
- 🇶🇦 Qatar
- 🇸🇦 Saudi Arabia
- 🇸🇬 Singapore
- 🇱🇰 Sri Lanka
- 🇸🇾 Syria
- 🇹🇼 Taiwan
- 🇹🇯 Tajikistan
- 🇹🇭 Thailand
- 🇹🇱 East Timor
- 🇹🇷 Turkey
- 🇹🇲 Turkmenistan
- 🇦🇪 United Arab Emirates
- 🇺🇿 Uzbekistan
- 🇻🇳 Vietnam
- 🇾🇪 Yemen
- 🇬🇪 Georgia
- 🇦🇲 Armenia

#### Africa (54 flags)
All 54 African nation flags in standard order (Algeria, Angola, Benin, Botswana, Burkina Faso, Burundi, Cameroon, Cape Verde, Central African Republic, Chad, Comoros, Congo, Democratic Republic of the Congo, Djibouti, Egypt, Equatorial Guinea, Eritrea, Eswatini, Ethiopia, Gabon, Gambia, Ghana, Guinea, Guinea-Bissau, Ivory Coast, Kenya, Lesotho, Liberia, Libya, Madagascar, Malawi, Mali, Mauritania, Mauritius, Morocco, Mozambique, Namibia, Niger, Nigeria, Rwanda, Sao Tome and Principe, Senegal, Seychelles, Sierra Leone, Somalia, South Africa, South Sudan, Sudan, Togo, Tunisia, Uganda, Zambia, Zimbabwe)

#### Oceania (14 flags)
- 🇦🇺 Australia
- 🇫🇯 Fiji
- 🇰🇮 Kiribati
- 🇲🇭 Marshall Islands
- 🇫🇲 Micronesia
- 🇳🇷 Nauru
- 🇳🇿 New Zealand
- 🇵🇦 Palau
- 🇵🇬 Papua New Guinea
- 🇼🇸 Samoa
- 🇸🇧 Solomon Islands
- 🇹🇴 Tonga
- 🇹🇻 Tuvalu
- 🇻🇺 Vanuatu

### Category 2: Sports Teams (150+ items)

#### NFL (32 teams) - $0.99 each

**AFC East (4 teams)**
- 🏈 Buffalo Bills
- 🏈 Miami Dolphins
- 🏈 New England Patriots
- 🏈 New York Jets

**AFC North (4 teams)**
- 🏈 Baltimore Ravens
- 🏈 Pittsburgh Steelers
- 🏈 Cleveland Browns
- 🏈 Cincinnati Bengals

**AFC South (4 teams)**
- 🏈 Houston Texans
- 🏈 Indianapolis Colts
- 🏈 Jacksonville Jaguars
- 🏈 Tennessee Titans

**AFC West (4 teams)**
- 🏈 Denver Broncos
- 🏈 Kansas City Chiefs
- 🏈 Los Angeles Chargers
- 🏈 Las Vegas Raiders

**NFC East (4 teams)**
- 🏈 Dallas Cowboys
- 🏈 Philadelphia Eagles
- 🏈 Washington Commanders
- 🏈 New York Giants

**NFC North (4 teams)**
- 🏈 Chicago Bears
- 🏈 Detroit Lions
- 🏈 Green Bay Packers
- 🏈 Minnesota Vikings

**NFC South (4 teams)**
- 🏈 Atlanta Falcons
- 🏈 Carolina Panthers
- 🏈 New Orleans Saints
- 🏈 Tampa Bay Buccaneers

**NFC West (4 teams)**
- 🏈 Arizona Cardinals
- 🏈 Los Angeles Rams
- 🏈 San Francisco 49ers
- 🏈 Seattle Seahawks

#### NHL (32 teams) - $0.99 each

**Atlantic Division (8 teams)**
- 🏒 Boston Bruins
- 🏒 Buffalo Sabres
- 🏒 Detroit Red Wings
- 🏒 Florida Panthers
- 🏒 Montreal Canadiens
- 🏒 Ottawa Senators
- 🏒 Tampa Bay Lightning
- 🏒 Toronto Maple Leafs

**Metropolitan Division (8 teams)**
- 🏒 Carolina Hurricanes
- 🏒 Columbus Blue Jackets
- 🏒 New Jersey Devils
- 🏒 New York Islanders
- 🏒 New York Rangers
- 🏒 Philadelphia Flyers
- 🏒 Pittsburgh Penguins
- 🏒 Washington Capitals

**Central Division (8 teams)**
- 🏒 Chicago Blackhawks
- 🏒 Colorado Avalanche
- 🏒 Dallas Stars
- 🏒 Minnesota Wild
- 🏒 Nashville Predators
- 🏒 St. Louis Blues
- 🏒 Winnipeg Jets
- 🏒 Arizona Coyotes

**Pacific Division (8 teams)**
- 🏒 Anaheim Ducks
- 🏒 Calgary Flames
- 🏒 Edmonton Oilers
- 🏒 Los Angeles Kings
- 🏒 San Jose Sharks
- 🏒 Seattle Kraken
- 🏒 Vancouver Canucks
- 🏒 Vegas Golden Knights

#### Premier League (20 teams) - $0.99 each

- ⚽ Arsenal
- ⚽ Aston Villa
- ⚽ Bournemouth
- ⚽ Brentford
- ⚽ Brighton and Hove Albion
- ⚽ Chelsea
- ⚽ Crystal Palace
- ⚽ Everton
- ⚽ Fulham
- ⚽ Ipswich Town
- ⚽ Leicester City
- ⚽ Liverpool
- ⚽ Manchester City
- ⚽ Manchester United
- ⚽ Newcastle United
- ⚽ Nottingham Forest
- ⚽ Southampton
- ⚽ Tottenham Hotspur
- ⚽ West Ham United
- ⚽ Wolverhampton Wanderers

#### Serie A (20 teams) - $0.99 each

- ⚽ AC Milan
- ⚽ Atalanta
- ⚽ Bologna
- ⚽ Como
- ⚽ Empoli
- ⚽ Fiorentina
- ⚽ Frosinone
- ⚽ Genoa
- ⚽ Inter Milan
- ⚽ Juventus
- ⚽ Lazio
- ⚽ Lecce
- ⚽ Monza
- ⚽ Napoli
- ⚽ Parma
- ⚽ Roma
- ⚽ Salernitana
- ⚽ Sassuolo
- ⚽ Torino
- ⚽ Udinese

#### Ligue 1 (20 teams) - $0.99 each

- ⚽ AS Monaco
- ⚽ Auxerre
- ⚽ Brest
- ⚽ ESTAC Troyes
- ⚽ FC Lorient
- ⚽ FC Nantes
- ⚽ Lens
- ⚽ Lille
- ⚽ Limoge
- ⚽ Marseille
- ⚽ Metz
- ⚽ Montpellier
- ⚽ Nice
- ⚽ Olympique Lyonnais
- ⚽ Paris Saint-Germain
- ⚽ RC Strasbourg
- ⚽ RCSA Lens
- ⚽ Reims
- ⚽ Rennes
- ⚽ Toulouse

#### AFL (18 teams) - $0.99 each

- 🏉 Adelaide Crows
- 🏉 Brisbane Lions
- 🏉 Carlton Blues
- 🏉 Collingwood Magpies
- 🏉 Essendon Bombers
- 🏉 Fremantle Dockers
- 🏉 Geelong Cats
- 🏉 Gold Coast Suns
- 🏉 Greater Western Sydney Giants
- 🏉 Hawthorn Hawks
- 🏉 Melbourne Demons
- 🏉 North Melbourne Kangaroos
- 🏉 Port Adelaide Power
- 🏉 Richmond Tigers
- 🏉 St Kilda Saints
- 🏉 Sydney Swans
- 🏉 West Coast Eagles
- 🏉 Western Bulldogs

#### Other European Leagues (8 teams) - $0.99 each

**La Liga (Spain)**
- ⚽ Real Madrid
- ⚽ Barcelona
- ⚽ Atlético Madrid

**Bundesliga (Germany)**
- ⚽ Bayern Munich
- ⚽ Borussia Dortmund

**Eredivisie (Netherlands)**
- ⚽ Ajax
- ⚽ PSV Eindhoven

**Primeira Liga (Portugal)**
- ⚽ SL Benfica

---

## Implementation Roadmap

### Phase 1: Local-Only Foundation (Weeks 1-2)

**Goal:** Full offline-capable system without cloud dependency

**Files to Create:**
1. `Core/Game/CardBackSystem/CardBackManager.cs` - Core business logic
2. `Core/Game/CardBackSystem/CardBackSerializer.cs` - File I/O
3. `Core/Game/CardBackSystem/CustomPhotoProcessor.cs` - Image resize/validate
4. `Core/Game/Screens/CardBackStoreScreen.cs` - Browse/download UI
5. `Core/Game/Screens/CardBackUploadScreen.cs` - Photo upload UI

**Files to Modify:**
1. `Core/Game/Misc/GameSettings.cs` - Add SelectedCardBackId
2. `Core/Game/Screens/SettingsScreen.cs` - Add card back management option

**Deliverables:**
- ✅ Local inventory system
- ✅ Custom photo upload/delete (max 3)
- ✅ Card back selection persistence
- ✅ Integration with settings screen

**Testing:**
- Create/delete custom photos
- Select different card backs
- Verify persistence across app restarts
- Test all image resize scenarios

### Phase 2: Cloud Integration - Azure (Weeks 3-4)

**Goal:** Add cloud store catalog with fallback offline support

**Infrastructure Setup:**
1. Create Azure Storage Account
2. Upload CardBackStore.json
3. Upload card back images to blob storage
4. Set up Azure CDN (optional but recommended)
5. Create Azure Function for IAP validation

**Files to Create:**
1. `Core/Game/CardBackSystem/ICardBackStore.cs` - Service interface
2. `Core/Game/CardBackSystem/AzureCardBackStore.cs` - Azure implementation
3. `Core/Game/CardBackSystem/CardBackStoreClient.cs` - HTTP client
4. `Core/Game/IAP/IAPManager.cs` - Platform IAP wrapper
5. `Core/Game/Network/AzureIAPValidator.cs` - Receipt validation

**Configuration:**
Create `appsettings.json` or environment variables:
```json
{
  "CardBackStore": {
    "Provider": "Azure",
    "AzureStorageUrl": "https://cardbacks.azureedge.net/",
    "CatalogPath": "store/CardBackStore.json",
    "ImageCachePath": "CachedCardBacks/",
    "CacheDurationHours": 24
  },
  "IAP": {
    "ValidationEndpoint": "https://your-function.azurewebsites.net/api/validateIAP"
  }
}
```

**Deliverables:**
- ✅ Fetch CardBackStore.json from cloud
- ✅ Cache locally for offline use
- ✅ Download purchased card backs
- ✅ Validate IAP receipts
- ✅ Fallback to cached data on network error

**Testing:**
- Fetch store with network available
- Fetch store with network unavailable (use cache)
- Purchase card back via IAP
- Validate receipt server-side
- Delete card back from server, verify cache still works

### Phase 3: Firebase Alternative (Parallel, Weeks 5-6)

**Goal:** Provide Firebase implementation without modifying game code

**Files to Create:**
1. `Core/Game/CardBackSystem/FirebaseCardBackStore.cs` - Firebase implementation
2. `Core/Game/CardBackSystem/FirebaseIAPValidator.cs` - Firebase Cloud Functions

**Firebase Setup:**
1. Create Firestore collection for card backs
2. Upload images to Cloud Storage
3. Deploy Cloud Functions for IAP validation
4. Set up Realtime Database for optional player progress sync

**Configuration:**
```json
{
  "CardBackStore": {
    "Provider": "Firebase",
    "FirebaseProjectId": "your-project-id",
    "FirebaseStorageBucket": "your-project.appspot.com"
  }
}
```

**Deliverables:**
- ✅ Complete Firebase implementation
- ✅ No game code changes needed (swappable via config)
- ✅ Feature parity with Azure version

**Testing:**
- All Phase 2 tests with Firebase backend
- Verify both implementations work identically

### Phase 4: Advanced Features (Weeks 7-8)

**Optional Enhancements:**

#### Analytics
```csharp
public interface ICardBackAnalytics
{
    Task RecordCardBackViewedAsync(string cardBackId);
    Task RecordCardBackPurchasedAsync(string cardBackId);
    Task RecordCustomPhotoUploadedAsync();
}
```

Track:
- Most viewed card backs
- Most purchased card backs
- Custom photo upload frequency
- Peak shopping times

#### Gifting System (Future)
```csharp
public async Task<bool> GiftCardBackAsync(string cardBackId, string recipientGamerId)
{
    // Send gift notification
    // Record gift in database
    // Grant ownership to recipient
}
```

#### Seasonal Events
```json
{
  "activePromotion": {
    "name": "Summer 2024",
    "discount": 0.50,
    "applicableCategories": ["sports"],
    "startDate": "2024-06-01",
    "endDate": "2024-08-31"
  }
}
```

#### User Reviews/Ratings
Allow players to rate card backs (5-star system)

### Phase 5: Steam Integration (Weeks 9-12, Parallel with Phases 2-4)

**Goal:** Add Steam support for Windows/Linux/macOS distribution with hybrid IAP handling

**Why Add Steam?**
- Access millions of Steam users
- Automatic regional pricing
- Zero backend payment handling (Steam manages all payments)
- Same code architecture, just add new implementation
- Can launch simultaneously with app stores or after

**Infrastructure Setup:**
1. Create Steam App ID (free, just sign up for Steamworks)
2. Add product IDs for each card back to Steamworks admin
3. Upload card back images to Steam CDN (automatic)
4. Configure regional pricing in Steam (Steam auto-converts)
5. Set up Steam rich presence (optional, shows "Customizing Cards" in Discord)

**Files to Create:**
1. `Core/Game/CardBackSystem/SteamCardBackStore.cs` - Steam implementation
2. `Core/Game/CardBackSystem/HybridCardBackStore.cs` - Multi-platform orchestrator
3. `Core/Game/Platform/SteamPlatformDetector.cs` - Runtime platform detection
4. `Core/Game/IAP/SteamIAPHandler.cs` - Steam-specific IAP logic

**NuGet Dependencies:**
```xml
<PackageReference Include="Steamworks.NET" Version="20.0.0" />
```

**Configuration:**
```json
{
  "CardBackStore": {
    "Provider": "Hybrid",
    "EnableSteam": true,
    "SteamAppId": 1234567,
    "AzureStorageUrl": "https://cardbacks.azureedge.net/",
    "FallbackProvider": "Azure"
  },
  "IAP": {
    "Platforms": ["Steam", "iOS", "Android"],
    "ValidationEndpoint": "https://your-function.azurewebsites.net/api/validateIAP"
  }
}
```

**Implementation Details:**

#### Hybrid Card Back Store (Platform-Aware)

```csharp
/// <summary>
/// Multi-platform card back store that routes to appropriate handler based on platform.
/// Supports Steam, iOS (StoreKit2), Android (Google Play), and local-only fallback.
/// </summary>
public class HybridCardBackStore : ICardBackStore
{
    private readonly Steamworks.SteamInventory _steamInventory;
    private readonly IAPManager _iapManager;
    private readonly ICardBackStore _contentStore; // Azure/Firebase for images
    private readonly CardBacksInventory _inventory;
    private readonly string _currentPlatform;
    
    public HybridCardBackStore(
        IAPManager iapManager,
        ICardBackStore contentStore,
        CardBacksInventory inventory)
    {
        _iapManager = iapManager;
        _contentStore = contentStore;
        _inventory = inventory;
        _currentPlatform = DetectCurrentPlatform();
        
        // Initialize Steam if available
        if (_currentPlatform == "Steam")
        {
            try
            {
                Steamworks.SteamClient.Init(GameSettings.Instance.SteamAppId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Steam initialization failed: {ex.Message}");
                _currentPlatform = "Offline"; // Fallback
            }
        }
    }
    
    public async Task<ServiceResult<List<CardBackInfo>>> GetAvailableCardBacksAsync()
    {
        // Get catalog from content store (Azure/Firebase)
        var contentResult = await _contentStore.GetAvailableCardBacksAsync();
        
        if (!contentResult.Success)
            return contentResult;
        
        // Enrich with platform-specific ownership info
        foreach (var cardBack in contentResult.Data)
        {
            cardBack.IsOwned = await CheckOwnershipAsync(cardBack.Id);
            cardBack.PlatformPrice = GetPlatformPrice(cardBack);
        }
        
        return contentResult;
    }
    
    public async Task<ServiceResult<bool>> ValidatePurchaseAsync(
        string productId,
        string receiptData,
        string platform)
    {
        return platform switch
        {
            "Steam" => await ValidateSteamPurchaseAsync(productId),
            "iOS" => await _iapManager.ValidateAppleReceiptAsync(productId, receiptData),
            "Android" => await _iapManager.ValidateGooglePlayReceiptAsync(productId, receiptData),
            _ => ServiceResult<bool>.FromError("Unknown platform", ServiceErrorType.Unknown)
        };
    }
    
    private async Task<ServiceResult<bool>> ValidateSteamPurchaseAsync(string productId)
    {
        try
        {
            if (!Steamworks.SteamClient.IsValid)
                return ServiceResult<bool>.FromError("Steam not available", ServiceErrorType.ServerError);
            
            // Check if player owns item in Steam inventory
            bool ownsSteamItem = _steamInventory.GetItemCount(productId) > 0;
            
            if (!ownsSteamItem)
                return ServiceResult<bool>.FromError("Item not owned on Steam", ServiceErrorType.Unauthorized);
            
            // Get card back details from content store
            var cardBackResult = await _contentStore.GetCardBackDetailsAsync(productId);
            if (!cardBackResult.Success)
                return ServiceResult<bool>.FromError("Card back not found", ServiceErrorType.NotFound);
            
            // Record ownership locally
            _inventory.OwnedCardBacks.Add(new OwnedCardBackInfo
            {
                Id = productId,
                Name = cardBackResult.Data.Name,
                Type = "official",
                Category = cardBackResult.Data.Category,
                PurchaseDate = DateTime.UtcNow,
                Price = 0.99m,
                Currency = "USD",
                Platform = "Steam"
            });
            
            _inventory.SaveToFile();
            
            return ServiceResult<bool>.FromSuccess(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Steam validation error: {ex.Message}");
            return ServiceResult<bool>.FromError("Validation failed", ServiceErrorType.ServerError);
        }
    }
    
    public async Task<ServiceResult<bool>> CheckOwnershipAsync(string cardBackId, string playerGamerId = null)
    {
        return _currentPlatform switch
        {
            "Steam" => Task.FromResult(ServiceResult<bool>.FromSuccess(
                _steamInventory.GetItemCount(cardBackId) > 0
            )).Result,
            
            "iOS" or "Android" => Task.FromResult(ServiceResult<bool>.FromSuccess(
                _inventory.OwnedCardBacks.Any(cb => cb.Id == cardBackId)
            )).Result,
            
            _ => Task.FromResult(ServiceResult<bool>.FromSuccess(false)).Result
        };
    }
    
    public bool IsOnline
    {
        get
        {
            if (_currentPlatform == "Steam")
                return Steamworks.SteamClient.IsValid && Steamworks.SteamClient.IsLoggedOn;
            
            return _contentStore.IsOnline;
        }
    }
    
    private string DetectCurrentPlatform()
    {
        // Check if running on Steam
        if (Environment.GetEnvironmentVariable("SteamAppId") != null ||
            File.Exists("steam_appid.txt"))
            return "Steam";
        
        // Check platform OS
        if (OperatingSystem.IsIOS())
            return "iOS";
        
        if (OperatingSystem.IsAndroid())
            return "Android";
        
        // Default to online store
        return "Online";
    }
    
    private string GetPlatformPrice(CardBackInfo cardBack)
    {
        return _currentPlatform switch
        {
            "Steam" => "$0.99 USD", // Or regional Steam price
            "iOS" => "$0.99",
            "Android" => "$0.99 USD",
            _ => cardBack.Price?.USD.ToString("C") ?? "—"
        };
    }
    
    public event EventHandler<StoreRefreshedEventArgs> StoreRefreshed;
    
    protected virtual void OnStoreRefreshed()
    {
        StoreRefreshed?.Invoke(this, new StoreRefreshedEventArgs());
    }
}
```

#### Steam-Specific UI Considerations

```csharp
public class CardBackStoreScreen : GameScreen
{
    private HybridCardBackStore _store;
    private bool _isSteamPlatform;
    
    public override void LoadContent()
    {
        _store = CardBackStoreFactory.GetStore() as HybridCardBackStore;
        _isSteamPlatform = _store.CurrentPlatform == "Steam";
        
        // Adjust UI based on platform
        if (_isSteamPlatform)
        {
            // Hide price if using Steam (Steam displays it via overlay)
            ShowSteamPriceWarning();
            // Show "Open in Steam" button instead of in-game purchase
            ShowSteamCheckout();
        }
        else
        {
            // Standard in-app purchase UI
            ShowStandardCheckout();
        }
    }
    
    private async void OnPurchaseClicked(CardBackInfo cardBack)
    {
        if (_isSteamPlatform)
        {
            // Redirect to Steam Community Hub or Steam Store
            Steamworks.SteamFriends.OpenWebOverlay("https://store.steampowered.com/app/" + SteamAppId);
        }
        else
        {
            // Use platform IAP as normal
            var result = await _store.ValidatePurchaseAsync(
                cardBack.Id,
                GetPlatformReceiptData(),
                _store.CurrentPlatform
            );
            
            if (result.Success)
                ShowPurchaseSuccess();
            else
                ShowError(result.Error);
        }
    }
}
```

**Deliverables:**
- ✅ Steam integration via Steamworks.NET
- ✅ Hybrid platform detection (Steam/iOS/Android/Web)
- ✅ No code changes to game logic
- ✅ Works offline (Steam caches ownership locally)
- ✅ Fallback to Azure if Steam unavailable
- ✅ Regional pricing support

**Testing:**
- Launch via Steam and verify ownership check
- Purchase via Steam and verify local sync
- Test fallback (disconnect from Steam)
- Test on non-Steam platforms (verify Azure IAP works)
- Test Steam overlay functionality
- Regional pricing verification

**Steam-Specific Considerations:**
- Steam App ID required (get from Steamworks)
- `steam_appid.txt` file needed in game root
- Steamworks SDK binaries (SDK64/SDK32) included
- Product IDs must match between game and Steamworks admin
- Test in both "Steam" and "Developer" launch modes

---

## Implementation Timeline (Recommended Approach)

**Estimated Total: 12 weeks for full multi-platform support**

- **Weeks 1-2:** Phase 1 (Local Foundation)
- **Weeks 3-4:** Phase 2 (Azure Integration)
- **Weeks 5-6:** Phase 3 (Firebase Alternative) OR Phase 5 (Steam) - **Choose one first**
- **Weeks 7-8:** Phase 4 (Advanced Features)
- **Weeks 9-12:** Phase 5 (Steam) - if not done earlier

**Fast Track (App Store First):**
- Weeks 1-4: Phases 1-2 (ready to launch on mobile)
- Weeks 5-8: Phases 3-4 (Firebase + analytics)
- Weeks 9-12: Phase 5 (add Steam for desktop)

**Steam-First Launch (Desktop):**
- Weeks 1-4: Phases 1-2 (local + Azure)
- Weeks 5-8: Phase 5 (add Steam integration)
- Weeks 9-12: Phases 3-4 + mobile IAP support

---

## Backend Abstraction Layer

### Service Interface Design

```csharp
/// <summary>
/// Backend-agnostic interface for card back store operations.
/// Enables swapping between Azure, Firebase, or custom implementations.
/// </summary>
public interface ICardBackStore
{
    /// <summary>
    /// Get all available card backs from store catalog.
    /// Falls back to cached data if network unavailable.
    /// </summary>
    Task<ServiceResult<List<CardBackInfo>>> GetAvailableCardBacksAsync();
    
    /// <summary>
    /// Get detailed information about a specific card back.
    /// </summary>
    Task<ServiceResult<CardBackInfo>> GetCardBackDetailsAsync(string cardBackId);
    
    /// <summary>
    /// Download card back image from cloud storage.
    /// Caches locally for offline use.
    /// </summary>
    Task<ServiceResult<Stream>> DownloadCardBackImageAsync(string cardBackId, ImageSize size);
    
    /// <summary>
    /// Validate IAP receipt with platform and grant ownership.
    /// </summary>
    Task<ServiceResult<bool>> ValidatePurchaseAsync(
        string productId, 
        string receiptData, 
        string platform); // "iOS" or "Android"
    
    /// <summary>
    /// Get catalog categories and metadata.
    /// </summary>
    Task<ServiceResult<List<Category>>> GetCategoriesAsync();
    
    /// <summary>
    /// Check if player owns a specific card back.
    /// (Optional - mainly for server-validation in networked games)
    /// </summary>
    Task<ServiceResult<bool>> CheckOwnershipAsync(string cardBackId, string playerGamerId);
    
    /// <summary>
    /// Get current online status (for UI feedback).
    /// </summary>
    bool IsOnline { get; }
    
    /// <summary>
    /// Event fired when store data is refreshed.
    /// </summary>
    event EventHandler<StoreRefreshedEventArgs> StoreRefreshed;
}

/// <summary>
/// Result wrapper for all service calls.
/// Provides consistent error handling across backends.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
    public ServiceErrorType ErrorType { get; set; } // Network, NotFound, Unauthorized, etc.
    public DateTime Timestamp { get; set; }
    
    public static ServiceResult<T> FromSuccess(T data) =>
        new() { Success = true, Data = data, Timestamp = DateTime.UtcNow };
    
    public static ServiceResult<T> FromError(string error, ServiceErrorType errorType) =>
        new() { Success = false, Error = error, ErrorType = errorType, Timestamp = DateTime.UtcNow };
}

public enum ServiceErrorType
{
    None,
    NetworkUnavailable,
    Unauthorized,
    NotFound,
    InvalidReceipt,
    ServerError,
    Unknown
}
```

### Factory Pattern Implementation

```csharp
/// <summary>
/// Factory for creating backend-specific store implementations.
/// Configuration-driven to enable runtime backend switching.
/// </summary>
public static class CardBackStoreFactory
{
    private static ICardBackStore _instance;
    
    public static ICardBackStore GetStore()
    {
        if (_instance == null)
        {
            var config = GameSettings.Instance.CardBackStoreConfig;
            
            _instance = config.Provider switch
            {
                BackendProvider.Azure => new AzureCardBackStore(
                    config.AzureStorageUrl,
                    config.CatalogPath),
                    
                BackendProvider.Firebase => new FirebaseCardBackStore(
                    config.FirebaseProjectId,
                    config.FirebaseStorageBucket),
                    
                BackendProvider.LocalOnly => new LocalOnlyCardBackStore(),
                
                _ => throw new InvalidOperationException($"Unknown backend: {config.Provider}")
            };
            
            // Subscribe to language changes for catalog refresh
            LanguageManager.LanguageChanged += (lang) =>
            {
                _instance?.RefreshCatalogAsync().ConfigureAwait(false);
            };
        }
        
        return _instance;
    }
    
    /// <summary>
    /// Swap backend at runtime (for testing or user migration).
    /// </summary>
    public static void SwitchBackend(BackendProvider newProvider)
    {
        _instance = null;
        GameSettings.Instance.CardBackStoreConfig.Provider = newProvider;
        // Force reload next time GetStore() is called
    }
}

public enum BackendProvider
{
    Azure,
    Firebase,
    LocalOnly
}
```

### Dependency Injection Setup

```csharp
// In your game's initialization (e.g., CardsGame.cs)
public class CardsGame : Game
{
    public void Initialize()
    {
        // Register services
        var cardBackStore = CardBackStoreFactory.GetStore();
        var cardBackManager = new CardBackManager(cardBackStore);
        
        // Make available to screens
        Services.AddSingleton<ICardBackStore>(cardBackStore);
        Services.AddSingleton<CardBackManager>(cardBackManager);
        
        // Screens can now inject:
        // public CardBackStoreScreen(ICardBackStore store, CardBackManager manager)
    }
}
```

### Migration Path Example

**Scenario:** You start with Azure but later want to switch to Firebase

**Step 1:** Implement Firebase service
```csharp
public class FirebaseCardBackStore : ICardBackStore
{
    // Implementation...
}
```

**Step 2:** Update configuration
```json
{
  "CardBackStore": {
    "Provider": "Firebase"  // Changed from "Azure"
  }
}
```

**Step 3:** No game code changes needed!
```csharp
var store = CardBackStoreFactory.GetStore(); // Returns Firebase instance
// Everything works the same
```

---

## Security Considerations

### 1. Offline Security

**Risk:** Players could manually edit inventory files to add items they don't own

**Mitigation:**
- Store checksum of inventory in GameSettings (encrypted)
- Validate checksum on load; if mismatch, reset to default
- Log suspicious activity (for future anti-cheat)

```csharp
private bool ValidateIntegrity()
{
    if (_inventory == null) return false;
    
    string json = JsonConvert.SerializeObject(_inventory);
    string calculatedHash = ComputeSHA256(json);
    
    if (calculatedHash != _storedHash)
    {
        Debug.WriteLine("WARNING: Card back inventory tampered with!");
        _inventory = CreateDefaultInventory();
        return false;
    }
    
    return true;
}
```

### 2. IAP Security

**Risk:** Players claim purchases they didn't make

**Mitigation:**
- Always validate receipts server-side
- Never trust client-side purchase claims
- Implement receipt signature verification
- Rate-limit validation requests per player

```csharp
public async Task<ServiceResult<bool>> ValidatePurchaseAsync(
    string productId, 
    string receiptData, 
    string platform)
{
    try
    {
        // Step 1: Rate limit
        if (!RateLimiter.AllowValidation(playerId))
            return ServiceResult<bool>.FromError("Rate limited", ServiceErrorType.Unauthorized);
        
        // Step 2: Validate with platform
        bool isValid = platform switch
        {
            "iOS" => await ValidateAppleReceiptAsync(receiptData),
            "Android" => await ValidateGooglePlayReceiptAsync(receiptData),
            _ => false
        };
        
        if (!isValid)
            return ServiceResult<bool>.FromError("Invalid receipt", ServiceErrorType.InvalidReceipt);
        
        // Step 3: Record ownership
        await recordPurchaseAsync(productId, playerId);
        
        return ServiceResult<bool>.FromSuccess(true);
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"IAP validation error: {ex.Message}");
        return ServiceResult<bool>.FromError("Validation failed", ServiceErrorType.ServerError);
    }
}
```

### 3. Custom Photo Security

**Risk:** Players upload inappropriate content or exploit with massive files

**Mitigation:**
- Validate file format (PNG/JPG only)
- Enforce size limits (max 5MB before compression)
- Auto-resize to card dimensions (prevents metadata exploitation)
- Local-only (no server = no moderation needed)

```csharp
public async Task<(bool Success, Image ResizedImage)> ValidateAndResizePhotoAsync(
    string imagePath)
{
    try
    {
        // Validate file size
        var fileInfo = new FileInfo(imagePath);
        const long MaxSizeBytes = 5 * 1024 * 1024; // 5MB
        
        if (fileInfo.Length > MaxSizeBytes)
            return (false, null);
        
        // Load and validate format
        using (var image = Image.Load(imagePath))
        {
            if (!(image is PngImage or JpegImage))
                return (false, null);
            
            // Resize to card dimensions
            const int CardWidth = 256;
            const int CardHeight = 384;
            
            image.Mutate(x => x.Resize(
                new ResizeOptions
                {
                    Size = new Size(CardWidth, CardHeight),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
            
            return (true, image);
        }
    }
    catch
    {
        return (false, null);
    }
}
```

### 4. Network Security

**Risk:** Man-in-the-middle attacks, data tampering

**Mitigation:**
- HTTPS only (enforced by platform)
- Azure CDN handles SSL/TLS
- Firebase enforces HTTPS
- Verify certificate pinning (optional)

```csharp
// Enforce HTTPS in HTTP client configuration
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
{
    // In production, implement certificate pinning
    #if DEBUG
        return true; // Allow self-signed certs in development
    #else
        // Verify against known production certificates
        return cert.Thumbprint == KnownProductionThumbprint;
    #endif
};

var httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(10)
};
```

### 5. Data Privacy

**Risk:** Player data exposed

**Mitigation:**
- Store minimal data locally (inventory only)
- No personal data in CardBackStore.json
- Platform IAP handles payment security (you don't store receipts long-term)
- Custom photos local-only (never transmitted)

---

## Cost Analysis

### Azure Backend (Recommended for Scale)

**Monthly Costs at Different Scales:**

| Scale | Storage | CDN | Functions | API | Total |
|-------|---------|-----|-----------|-----|-------|
| 1k players | $0.20 | $0.15 | $0.10 | $0.05 | **$0.50** |
| 10k players | $2.00 | $1.50 | $1.00 | $0.50 | **$5.00** |
| 100k players | $20 | $15 | $10 | $5 | **$50** |
| 1M players | $200 | $150 | $100 | $50 | **$500** |

**Breakdown:**
- **Blob Storage:** $0.018/GB/month - 100GB @ $1.80/month
- **CDN:** $0.15/GB transfer - assume 10GB/month average
- **Functions:** $0.20 per million executions - 1M requests/month
- **API Management:** Minimal cost

**Initial Setup:** ~$50-100 (one-time)

### Firebase Backend

**Monthly Costs (Firebase free tier is generous):**

| Scale | Free Tier Limits | Firestore | Storage | Functions | Total |
|-------|---|---|---|---|---|
| < 1k | Included | Free | Free | Free | **$0** |
| 10k | Moderate overage | ~$2 | ~$1 | ~$1 | **$4** |
| 100k | High overage | ~$20 | ~$10 | ~$10 | **$40** |
| 1M | Extensive | ~$200 | ~$100 | ~$100 | **$400** |

**Comparison:**
- ✅ Firebase cheaper at low-to-mid scale
- ❌ Firebase more expensive at very high scale
- ✅ Azure more predictable costs
- ❌ Azure requires more manual setup

### Local-Only Backend (Zero Cost)

**Cost:** $0/month forever
**Trade-off:** No cloud store, manual card back updates via app patches

### Steam Backend

**Monthly Costs:**
- **Payment Processing:** 30% of revenue (Steam standard cut)
- **Image Hosting:** Free (Steam CDN)
- **Backend:** $0 (Steam handles all transactions)
- **Total:** 30% of sales only

| Scale | Monthly Revenue | Steam Cut | Net Revenue |
|-------|---|---|---|
| 100 sales @ $0.99 | $99 | $30 | $69 |
| 1,000 sales @ $0.99 | $990 | $297 | $693 |
| 10,000 sales @ $0.99 | $9,900 | $2,970 | $6,930 |

**Advantages:**
- Zero infrastructure cost
- Automatic regional pricing conversion
- Fraud protection handled by Steam
- Works offline (local inventory)

**Considerations:**
- 30% platform fee is higher than mobile (typically 30% same)
- Approval process takes 1-3 weeks
- Must maintain parity with other versions

---

## Next Steps

1. **Approve Architecture:** Review and confirm design approach
2. **Prepare Content:** Create card back designs for Phase 1 launch (~50 items minimum)
3. **Setup Infrastructure:** Create Azure resources or Firebase project
4. **Choose Platform Strategy:**
   - Mobile-first: Start with Phases 1-4, add Steam later
   - Steam-first: Integrate Steam with Phase 2
   - Multi-platform: Implement Phases 1, 2, and 5 simultaneously (12 weeks)
5. **Begin Implementation:** Start with Phase 1 (local foundation)

---

## Steam Integration Overview

### Why Steam Makes Sense for This Game

Steam provides access to millions of desktop gamers and handles all the complexity of payment processing, regional pricing, and fraud prevention automatically. With the `HybridCardBackStore` architecture, you can support Steam alongside mobile platforms with a single codebase.

### Quick Comparison Table

| Aspect | Mobile (iOS/Android) | Steam | Web/Custom |
|--------|---|---|---|
| **Payment Processing** | Apple/Google handles | Steam handles | You build |
| **Regional Pricing** | Manual per region | Automatic | Manual |
| **Fraud Prevention** | Handled by platform | Handled by Steam | You build |
| **Platform Fee** | 30% | 30% | Variable |
| **Audience Size** | Billions (mobile) | 120M+ active | Varies |
| **Offline Support** | Yes (local receipts) | Yes (local inventory) | No |
| **Setup Effort** | Medium | Medium | High |
| **Time to Launch** | 1-2 weeks review | 1-3 weeks review | Variable |

### Hybrid Architecture Advantages

The `HybridCardBackStore` you'll implement in Phase 5 means:

✅ **Single Codebase:** No separate "Steam version" of the game
✅ **Runtime Detection:** Automatically detects platform and routes correctly
✅ **Fallback Support:** Works offline if Steam unavailable
✅ **Content Sharing:** Same card back images used across all platforms
✅ **Inventory Sync:** Local inventory works on all platforms
✅ **No Code Changes:** Just swap config to support new platform

### Implementation Philosophy

Rather than thinking "How do I add Steam support?", think:
- "I have a card back store service interface"
- "Steam is just another implementation of that interface"
- "My game code doesn't care which implementation is used"

This is why we built the abstraction layer first.

### Steam-Specific Features You Get For Free

1. **Rich Presence:** Shows in Discord/Steam what you're doing
   ```csharp
   Steamworks.SteamFriends.SetRichPresence("status", "Customizing Card Backs");
   ```

2. **Achievements:** Award achievements for purchases
   ```csharp
   Steamworks.SteamUserStats.SetAchievement("BoughtFirstCardBack");
   ```

3. **Statistics:** Track player behavior
   ```csharp
   Steamworks.SteamUserStats.SetStat("CardBacksPurchased", count);
   ```

4. **Screenshots:** Players can easily share their cards
   ```csharp
   Steamworks.SteamScreenshots.TriggerScreenshot();
   ```

5. **Workshop (Future):** Let players create/share custom designs
   ```
   Allow community to upload card back designs
   Vote on best designs
   Revenue sharing with creators
   ```

### Launch Strategy: Multi-Platform Simultaneously

**Recommended Timeline: 12 weeks total**

```
Weeks 1-4: Phases 1-2 (Local + Azure)
  └─ Playable on mobile/web

Weeks 5-8: Phase 5 (Steam Integration)
  └─ Add Steam without touching mobile code
  └─ Same IAP manager handles both

Weeks 9-12: Phase 3-4 (Firebase + Analytics)
  └─ Optional backend alternatives
  └─ Advanced features
```

### Real-World Example: Game Loop Pseudocode

```csharp
public class BlackjackGame : Game
{
    private HybridCardBackStore _cardBackStore;
    
    protected override void Initialize()
    {
        // Initialize ONCE - factory handles platform detection
        _cardBackStore = CardBackStoreFactory.GetStore();
        
        // That's it. Everything else works automatically.
        base.Initialize();
    }
    
    protected override void LoadContent()
    {
        // Load current card back - works same on all platforms
        string selectedId = GameSettings.Instance.SelectedCardBackId;
        var texture = _cardBackStore.GetCardBackTexture(selectedId);
        
        // Game doesn't care if it came from Steam, Apple, or local custom
        base.LoadContent();
    }
    
    public async void OnStoreScreenOpened()
    {
        // Fetch available card backs - works on all platforms
        var result = await _cardBackStore.GetAvailableCardBacksAsync();
        
        // Display store with platform-aware pricing
        DisplayStore(result.Data);
    }
    
    public async void OnBuyButtonClicked(string cardBackId)
    {
        // Validate purchase - routes to correct platform handler
        var result = await _cardBackStore.ValidatePurchaseAsync(
            cardBackId,
            GetPlatformReceiptData(),
            _cardBackStore.CurrentPlatform
        );
        
        // Handle result same way on all platforms
        if (result.Success)
            GrantCardBack(cardBackId);
        else
            ShowError(result.Error);
    }
}
```

### When to Launch Where

**Option A: Mobile First (Lower Risk)**
1. Launch Phases 1-2 on iOS/Android
2. Get real user feedback
3. Iterate based on data
4. Add Steam in Phase 5 once proven

**Option B: Multi-Launch (Faster Growth)**
1. Launch Phases 1-2 simultaneously on mobile + web
2. Launch Phase 5 (Steam) alongside
3. Leverage each audience (mobile users → desktop friends)
4. Add Firebase in Phase 3 once you know what works

**Option C: Steam Exclusive Launch (Desktop Only)**
1. Focus entirely on Steam
2. Later add mobile support
3. Use same backend architecture

### Common Steam Integration Questions

**Q: Do I need separate Steam product IDs for each card back?**
A: Yes, but you can bulk-create them in Steamworks admin. Each card back gets an ID like `cardback_001`, `cardback_flags_usa`, etc.

**Q: How do players purchase?**
A: On Steam, they purchase in the Steam store client. Your game just checks Steam inventory. On mobile, they use native IAP (Apple/Google).

**Q: Can I have exclusive card backs for Steam vs mobile?**
A: Yes! Use the `region` or `platform` field in CardBackStore.json to specify which card backs appear where.

**Q: What about regional pricing?**
A: Steam handles it automatically. You set price in $USD, Steam auto-converts to local currencies (EUR, GBP, JPY, etc.).

**Q: Do I need a separate backend for Steam?**
A: No. Steam IS your backend for payment. You use Azure/Firebase just for card back images and metadata.

**Q: Can Steam players trade card backs with each other?**
A: Not with the current architecture (single-player purchases). You could add this later with Steam Inventory Service.

---

## Recommended Launch Sequence

### **Month 1: MVP Launch**
- Implement Phases 1-2
- Launch on mobile (iOS/Android)
- Simple UI, no Steam yet
- **Result:** Validate market + monetization

### **Month 2: Desktop & Steam**
- Implement Phase 5 (Steam integration)
- Re-use Phase 1-2 backend
- Launch on Steam
- Launch web version (if desired)
- **Result:** Multi-platform coverage

### **Month 3-4: Scale & Optimize**
- Implement Phase 3 (Firebase alternative)
- Implement Phase 4 (Analytics, seasonal events)
- A/B test pricing on different platforms
- Expand card back catalog
- **Result:** Automated scaling, rich features

### **Month 5+: Enhancement**
- User reviews/ratings
- Seasonal events
- Workshop (if community interest)
- Cross-platform purchases (advanced)
- **Result:** Long-term engagement engine


