# Notes

## Ledger Consolidation

Planet credit balances now live in `IPlanetEconomyRepository`, replacing direct `Planet.Credits` mutations for a single, consistent ledger.

## Features

### Social & Personal

[x] **Court** – grow romance arcs and inter‑faction ties.
[x] **VisitLover** – triggers intimacy/opinion growth and conception gating.
[x] **VisitFamily** – stabilizes morale and loyalties; softens faction tensions.
[x] **Quarrel** – generates interpersonal drama and rivalries.
[ ] **Reconcile** – resolves feuds; lowers volatility in crews and courts.
[ ] **Gift** – micro‑diplomacy between characters; converts money → opinion.
[ ] **RequestFavor** – seeds future obligations/plot hooks.
[ ] **PublicAppearance** – small, safe morale/legitimacy bumps.

### Intelligence & Intrigue

[x] **Spy(Character/Faction)** – feeds secrets economy and casus belli.
[ ] **Counterintelligence** – burns enemy assets; reduces leak risk.
[ ] **Sabotage (Industry/Defense/Logistics)** – non‑direct warfare pressure on economies.
[ ] **PropagandaCampaign** – shifts public/faction opinion; unrest steering.
[ ] **LeakInformation** – weaponizes secrets to damage enemies or corrupt rivals.
[ ] **InfiltrateFaction** – plants sleepers for later coups/sabotage.
[ ] **InterrogatePrisoner** – (your “torture” gate) intel chance with ethics toggle.
[x] **Assassinate** – high‑stakes power rebalancing tool.
[x] **Bribe** – flips votes, officers, or gatekeepers.
[x] **Recruit** – character flow between factions without war.
[x] **Defect** – dramatic allegiance shifts; story fuel.

### Diplomacy

[x] **Negotiate (Trade/Research/NAP/Alliance/Ceasefire)** – core statecraft loop.
[ ] **HostSummit** – burst of trust; unlocks multi‑party deals.
[ ] **PrisonerExchange** – morale/honor mechanics; reduces reprisals.
[ ] **Guarantee/Protectorate** – scaffolds coalition and deterrence play.
[ ] **Denounce** – formalizes grievances; nudges toward conflict.
[ ] **Sanction/Embargo** – economic pressure alternative to war.

### Governance & Planetary

[ ] **AppointGovernor** – leadership placement; affects stability/taxes.
[ ] **EnactPolicy/Edict (Tax, Conscription, Rationing)** – macro knobs for economy/morale.
[ ] **SuppressUnrest** – short‑term order vs long‑term resentment tradeoff.
[ ] **SupportRelief** – disaster/epidemic aid; goodwill generation.
[ ] **UrbanProjects (Hospitals/Schools/Ports)** – long arc development bonuses.
[ ] **AuditCorruption** – cleans economy; may anger elites.
[ ] **Amnesty/Pardon** – resets unrest or flips dissidents.

### Economy & Logistics

[ ] **TradeRun (ad‑hoc)** – cashflow and soft relations.
[ ] **EstablishTradeRoute** – recurring income; piracy targets emerge.
[ ] **ConvoyEscort** – defensive counterpart to raids; merit source.
[ ] **ProcureSupplies** – readiness gate for fleets/armies.
[ ] **Smuggle** – high risk/reward black market loop with reputation.
[ ] **BuildShip/RefitFleet** – capacity growth; unlocks hull techs.
[ ] **UpgradeDefenseGrid** – anti‑raid/anti‑bombard resilience.

- Trade value now scales with distance between systems.
- Smuggling payouts credit the nearest pirate faction.

### Exploration & Science

[ ] **SurveyPlanet** – reveals resources/habitability hooks.
[ ] **ExploreAnomaly** – story seeds, artifacts, breakthroughs, risks.
[ ] **SalvageDebris** – post‑battle loot + tech fragments.
[ ] **ResearchProject** – steady science progress; event breakthroughs.
[ ] **MapHyperlane/DeepScan** – unlocks safer routes/faster travel.
[ ] **FirstContact** (if applicable) – diplomatic tech/ethics checks.
[ ] **TerraformStage** – long‑term colony quality arc.

### Colonization & Infrastructure

[ ] **FoundOutpost/Colony** – strategic footprint and logistics hubs.
[ ] **BuildStation (Trade/Science/Defense)** – local bonuses and targets.
[ ] **ClaimResource (Asteroid/Moon)** – mining economy pillars.
[ ] **ConstructRelay/Gate** – network speed; reshapes strategic map.

### Military & Operations

[ ] **PatrolSystem** – reduces piracy; raises civilian confidence.
[ ] **HuntPirates** – counters pirate loop; merit + loot.
[ ] **BlockadeSystem** – coercion without invasion; war pressure.
[ ] **BreakBlockade** – set‑piece battles; relief narrative.
[ ] **SiegePlanet** – territorial change path without instant wipe.
[ ] **LiftSiege** – heroic defense moments.
[x] **RaidConvoy** – your existing pirate pillar; dynamic economy hit.
[ ] **StrikeTarget (Depot/Shipyard/Relay)** – surgical objectives; war tempo control.
[ ] **EscortVIP** – mobile, time‑boxed defense missions.
[ ] **RescuePrisoners** – morale splash + story; ethical alternative.
[ ] **ScorchedEarth** (config‑locked) – dark lever; heavy diplomatic cost.

### Piracy & Underworld (toggle as “dark/gray economy”)

[x] **BecomePirate** – opt‑in career switch; opens pirate tree.
[ ] **FenceLoot/Launder** – monetizes raids; risk of exposure.
[ ] **BlackOpsHeist** – high‑risk tech/credit grab without open war.
[ ] **InciteUnrest (Covert)** – proxy destabilization instead of armies.

### Fleet & Travel

[x] **TravelToPlanet/System** – your backbone for presence and story.
[ ] **RedeployFleet** – posture shifts; threat projection.
[ ] **TrainCrew/Drill** – slow, safe combat readiness gain.
[ ] **FormTaskForce** – temporary multi‑fleet operations package.

### Narrative / Rare “Set Pieces”

[ ] **BrokerAllianceAtWar** – big diplomacy swing; “major/legendary” merit.
[ ] **LiberatePlanet** – dramatic ownership flips with population reaction.
[ ] **SaveLeader** – emergency rescue arc; huge political aftermath.
[ ] **UncoverMegaSecret** – galaxy‑shaking intel → multi‑faction response.
[ ] **FoundGreatHouse/Clan** – late‑game identity/power reframe.


## Earn Merrits

Think in bands so tuning stays predictable:

* Routine actions (1–3 merit):
  Successful small talks that matter (minor diplomacy tick), completing logistics tasks, minor intel reports, small trade runs, minor patrols.

* Notable operations (5–10 merit):
  Successful recruit, bribe, espionage with useful intel, negotiating a treaty proposal, winning a small skirmish, resolving planetary unrest event, profitable convoy escort/raid (depending on side), successful long‑distance travel supporting a mission.

* Major achievements (15–25 merit):
  Defeating a strong fleet, capturing a convoy with significant loot, brokering a ceasefire/alliance at war, cracking a high‑value secret, liberating a planet, rescuing prisoners, completing a multi‑step questline.

* Legendary (+30 merit):
  War‑deciding battle, founding a colony, uncovering a mega‑secret, saving a leader’s life, top‑tier scientific breakthrough.

### Guidelines:

* Rank multiplier: small upward bias at higher ranks (they take bigger risks / lead larger ops), but avoid snowballing.

* Diminishing returns on repeated spammy actions (e.g., the 5th “easy bribe” this month yields less).

* Faction alignment: award a small bonus if action aligns with faction doctrine (e.g., pirates value raids, technocrats value research).

* Penalties: failed plots, insubordination, harming allies/civilians, getting caught for crimes (bigger hit if doctrine-discordant).

* This keeps merit as a meaningful, legible resource that reflects impact, not just activity spam.

## About the game

* Scope & style: character-driven 4X-ish sci‑fi sim with factions, planets, fleets, piracy, travel, diplomacy, espionage, and interpersonal relationships.

* Characters: have Big Five‑like personalities + named traits/combos (e.g., Assertive, ThrillSeeker, ImpulsiveAnger), skills (Military/Intelligence/Economy/Research), ranks, opinions, relationships, ambitions (GainPower/BuildWealth/EnsureFamilyLegacy/SeekAdventure), merit, traumas, pregnancy state.

* Planning loop: monthly IntentPlanner scores intents (Court, VisitFamily, VisitLover, Spy, Bribe, Recruit, Defect, Negotiate, Quarrel, Assassinate, Torture/Rape of prisoners, TravelToPlanet, BecomePirate, RaidConvoy), resolves conflicts, and emits a small set.

* Resolution loop: InteractionResolver executes intents → adjusts opinions/diplomacy/merit, creates secrets/events, triggers travel/battles, logs intimacy for Lifecycle.

* Lifecycle: handles birthdays, conception (now driven by intimacy log + IPregnancyPolicy), pregnancy progression/twins/complications, birth, and mortality; places newborns via ILocationService and wires lineage.

* Pregnancy policy: centralized, tunable rules (opinion gates, co‑location, postpartum cooldown, optional coercion toggle), plus chance curves.

* Intimacy log: month‑bucketed, now should be consume‑on-read (previous month) to avoid off‑by‑one and memory growth.

* Piracy: distinct pirate faction(s), system security model (pirate activity/traffic/patrols), ambush/raid flows; BattleOutcomeService processes quick sim outcomes.

* Events/Secrets: an event bus publishes social/battle/logistics events; secrets track espionage/corruption/plots.