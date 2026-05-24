# Control Manifest

> **Engine**: Unity 2022.3.51 LTS
> **Last Updated**: 2026-05-24
> **Manifest Version**: 2026-05-24
> **ADRs Covered**: ADR-0001, ADR-0002, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010
> **Status**: Active — regenerate with `/create-control-manifest` when ADRs change

This manifest is a programmer's quick-reference extracted from all Accepted ADRs,
technical preferences, and engine reference docs. For the reasoning behind each
rule, see the referenced ADR.

---

## Foundation Layer Rules

*Applies to: scene management, event architecture, physics timestep, input infrastructure, engine initialisation*

### Required Patterns

**Physics Timestep (ADR-0001)**
- `Time.fixedDeltaTime` must be set to `0.0166667` (1/60 second) via Project Settings, never via runtime code — source: ADR-0001
- `Maximum Allowed Timestep` must be set to `0.0333333` (2 physics steps) — source: ADR-0001
- All character `Rigidbody2D.gravityScale` must be set to `0` permanently; Unity's automatic gravity must never be used — source: ADR-0001
- Gravity applied manually in FixedUpdate: `velocity.y -= Gravity * Time.fixedDeltaTime` with terminal velocity clamp — source: ADR-0001
- All velocity updates must use direct assignment `Rigidbody2D.velocity = newVelocity`, never `AddForce` — source: ADR-0001
- All character `Rigidbody2D.interpolation` must be set to `RigidbodyInterpolation2D.Interpolate` — source: ADR-0001
- All movement formulas must use `Time.fixedDeltaTime` (1/60) as dt, never `Time.deltaTime` — source: ADR-0001
- All GDD formulas must be frame-based (1/60s units), not deltaTime-based — source: ADR-0001
- `Rigidbody2D.velocity` is the sole write point for all velocity updates across all systems — source: ADR-0001
- Project Settings physics configuration must be documented and CI-verified to prevent accidental override — source: ADR-0001
- Physics2D settings (autoSyncTransforms, Layer Collision Matrix) must be confirmed and verified — source: ADR-0001

**Input System (ADR-0005)**
- Must use `com.unity.inputsystem` 1.7.x (Unity 2022.3 LTS bundled version) — source: ADR-0005
- Must define a single `.inputactions` asset with exactly two Action Maps: "Gameplay" and "UI" — source: ADR-0005
- Gameplay Map: Move (Value, Vector2), Jump (Button), Attack (Button), Dash (Button), Skill1-4 (Button) — source: ADR-0005
- UI Map: Navigate (Value, Vector2), Submit (Button), Cancel (Button), Pause (Button) — source: ADR-0005
- Must define exactly 3 Control Schemes: Gamepad, KeyboardLeft (WASD), KeyboardRight (arrows) — source: ADR-0005
- Pause must be in UI Map, handled via independent callback, must never enter combat input buffer — source: ADR-0005
- PlayerInputManager singleton with `joinBehavior: JoinPlayersWhenButtonIsPressed`, `maxPlayerCount: 2` (MVP) — source: ADR-0005
- PlayerInput Behavior must be `InvokeCSharpEvents`; `SendMessage` mode must never be used — source: ADR-0005
- Device pairing flow: character select → EnableJoining → onPlayerJoined callback → auto-assign device+index → DisableJoining — source: ADR-0005
- Device disconnect: `onDeviceLost` → hold current state → notify UI — source: ADR-0005
- Control scheme change: `onControlsChanged` → auto-switch — source: ADR-0005
- Global frame counter (`FrameCounter`) must be incremented in FixedUpdate only — source: ADR-0005

**Scene & Game State Management (ADR-0007)**
- Must use exactly 2 Unity scenes: MenuScene + GameScene — source: ADR-0007
- GameScene must remain loaded and resident after first load; inter-round loops must not reload the scene — source: ADR-0007
- State transitions must be atomic; game logic must pause during transitions — source: ADR-0007
- PlayerInputManager must use DontDestroyOnLoad to persist across scene loads — source: ADR-0007
- GamePhase FSM must have exactly 7 states: MainMenu, CharacterSelect, MatchLoading, Countdown, Battle, BattleEnd, Results — source: ADR-0007
- PlayerSlot data must persist across all states within DontDestroyOnLoad GameStateManager — source: ADR-0007
- IGameState interface must use `SignalRoundEnd(int winnerIndex, bool matchOver)` — source: ADR-0007
- Async scene loading must use `Time.unscaledDeltaTime` for timeout tracking — source: ADR-0007
- System initialization during MatchLoading must follow strict 10-step sequence; any failure falls back to CharacterSelect — source: ADR-0007
- GameStateManager and GameManager must reside in DontDestroyOnLoad — source: ADR-0007
- Results → CharacterSelect (rematch) must not reload any scene; reset internal state within 0.5s — source: ADR-0007
- Countdown: 3 seconds; BattleEnd freeze: 60 frames; scene load timeout: 5 seconds — source: ADR-0007

**Event Architecture (ADR-0008)**
- Events must be declared on the interface provider class (MonoBehaviour implementing the interface), not on the interface itself — source: ADR-0008
- Event signatures: `On + EventName + (sender key info, event data)`. Data must use struct or primitive types — source: ADR-0008
- Parameterless `event Action OnSomething` signatures are forbidden — source: ADR-0008
- `event Action<object>` signatures are forbidden — source: ADR-0008
- `event Action<GameObject>` signatures are forbidden — source: ADR-0008
- All consumers must subscribe in OnEnable and unsubscribe in OnDisable — source: ADR-0008
- All game logic events must fire in FixedUpdate at 60Hz; consumers receive in same frame — source: ADR-0008
- For same-frame multi-event bursts: process all in order, only last update triggers visual animation — source: ADR-0008
- Exceptions to FixedUpdate: `OnStateChanged` after async ops; `OnPlayerJoined`/`OnPlayerLeft` from Input System callbacks — source: ADR-0008

### Forbidden Approaches

- **Never use Unity default 50Hz physics step + gravityScale** — 50Hz doesn't sync with 60fps, gravityScale can't adjust per-state — source: ADR-0001
- **Never use variable-length Update + deltaTime physics** — non-deterministic, frame-exact mechanisms impossible — source: ADR-0001
- **Never use old Input Manager** (`Input.GetAxis`/`GetButton`) — can't distinguish multiple same-type devices, no event callbacks, in maintenance mode — source: ADR-0005
- **Never use manual `Gamepad.current`/`Keyboard.current` direct device reading** — device order non-deterministic, 3-4x code volume vs PlayerInput — source: ADR-0005
- **Never use one-scene-per-state (7 scenes)** — sequential loads violate "learn and play in seconds" — source: ADR-0007
- **Never use single-scene architecture** — wastes memory when only menu is active — source: ADR-0007
- **Never use global EventBus (Publish/Subscribe)** — violates single-writer principle, C# event `+=` already provides decoupling — source: ADR-0008
- **Never use ScriptableObject event channels** — 13+ SO assets with Inspector-invisible dependencies, no compile-time checking — source: ADR-0008
- **Never use Unity Events (Inspector binding)** — suited for intra-prefab only, cannot span scenes — source: ADR-0008

### Performance Guardrails

- 3C + collision + knockback physics combined: < 3ms/frame (2-player) — source: ADR-0001
- 60Hz physics step adds ~20% more computation vs 50Hz, measured impact < 0.1ms — source: ADR-0001
- InputAction callback: < 0.01ms per input — source: ADR-0005
- InputBuffer.TryConsume O(N) with N<=8: < 1us — source: ADR-0005
- InputBuffer fixed at 8 x 24B = 192B per player — source: ADR-0005
- `.inputactions` asset load time: < 1ms — source: ADR-0005
- FSM evaluation: < 0.01ms per frame — source: ADR-0007
- MenuScene load: < 0.5 seconds — source: ADR-0007
- GameScene first load: < 2 seconds — source: ADR-0007
- CharacterSelect → Countdown: < 3 seconds total — source: ADR-0007
- Event dispatch: < 0.01ms per event; total < 0.1ms per frame — source: ADR-0008

---

## Core Layer Rules

*Applies to: core gameplay loop, main player systems, physics, collision, combat pipeline*

### Required Patterns

**Physics Timestep (ADR-0001)**
- FixedUpdate execution order: input buffer → state machine → velocity calc → velocity assignment → Unity physics step → physics callbacks — source: ADR-0001
- Different character states must apply different gravity multipliers — source: ADR-0001
- Terminal velocity clamped to 20.0 u/s; Gravity 32.0 u/s^2 — source: ADR-0001

**Dual FSM Architecture (ADR-0002)**
- Each character must have exactly two independent MonoBehaviour FSMs: `MovementController` (3C) and `CombatFSM` — source: ADR-0002
- `MovementState` enum: `{Idle, Running, Jumping, Falling, FastFalling, Dashing, AirDodging, Landing, PlatformDrop}` — source: ADR-0002
- `CombatState` enum: `{Idle, Attacking, HitStun, Knockback}` — source: ADR-0002
- Attacking sub-phases (Startup/Active/Recovery) driven by current AttackData frame data, not independent states — source: ADR-0002
- CombatFSM must control 3C through `IMovementController` interface only — source: ADR-0002
- `IMovementController`: `GetState()`, `IsGrounded()`, `GetFacing()`, `FreezeMovement(bool)`, `SetVelocity(Vector2)` — source: ADR-0002
- `ICombatStateProvider`: `GetCurrentState()`, `GetCurrentAttackPhase()`, `CanAcceptInput()`, `RegisterState(StateDefinition)`, `DeregisterAllSkillStates()`, `ResetToIdle(int)` — source: ADR-0002
- CombatState=Attacking/HitStun must call `FreezeMovement(true)` — source: ADR-0002
- CombatState=Knockback must call `SetVelocity(knockbackVector)` + `FreezeMovement(true)` during hitstun — source: ADR-0002
- Unfreezing at end of CombatFSM FixedUpdate when transitioning from non-Idle to Idle — source: ADR-0002
- Attack definitions use `Dictionary<string, StateDefinition>` for runtime extensibility — source: ADR-0002
- `StateDefinition` must contain: StateId, StartupFrames, ActiveFrames, RecoveryFrames, CancelTable, InputMapping — source: ADR-0002
- `RegisterState(StateDefinition)` adds to dictionary; `DeregisterAllSkillStates()` clears skill states, preserves base — source: ADR-0002
- `CharacterController` coordinator uses explicit dispatch (not Script Execution Order): Movement → CombatFSM → Attack → Knockback — source: ADR-0002
- 3C (MovementController) must always update before CombatFSM in same FixedUpdate — source: ADR-0002
- Input buffer: circular buffer, fixed size 8, `InputEntry[]` with Type, RecordedFrame, Consumed — source: ADR-0002
- Each frame traverses un-consumed buffer entries to find highest-priority acceptable input — source: ADR-0002

**Hitbox/Hurtbox Detection (ADR-0003)**
- Layer "Hitbox" (8) collides only with Hurtbox and SolidPlatform — source: ADR-0003
- Layer "Hurtbox" (9) collides only with Hitbox — source: ADR-0003
- Layer "SolidPlatform" (11) collides with Hitbox, Projectile, and characters — source: ADR-0003
- Hitbox/Hurtbox never collide with themselves — source: ADR-0003
- Melee hitbox is child of character Rigidbody2D, positioned via `Transform.localPosition` — source: ADR-0003
- Melee hitbox activated via `SetActive(true)` at Active phase, deactivated at Recovery/cancel — source: ADR-0003
- Projectile hitbox is independent GameObject, instantiated at Active phase start — source: ADR-0003
- Projectile position updated manually in FixedUpdate: `Position += Speed * dt * FacingDir` — source: ADR-0003
- `Physics2D.autoSyncTransforms` must be `true` — source: ADR-0003
- Single `CollisionDetector` MonoBehaviour per character receives all OnTriggerEnter2D — source: ADR-0003
- Hit detection pipeline: identity → self-hit exclusion → multi-hit check → hit point calc (AABB overlap center) → HitEvent → OnHitDetected — source: ADR-0003
- HitEvent struct: AttackerId, TargetId, AttackId, HitPoint, HitboxCenter, HurtboxCenter — source: ADR-0003
- KO character hurtbox disabled via `Collider2D.enabled = false`; re-enabled on new round — source: ADR-0003
- Melee hitbox OnTriggerEnter2D with SolidPlatform must be ignored — source: ADR-0003
- Projectile hitbox OnTriggerEnter2D with SolidPlatform triggers destroy (no HitEvent) — source: ADR-0003
- MinHitboxWidth formula enforced at creation: `Max(designerWidth, ProjectileSpeed * fixedDeltaTime * 2)` — source: ADR-0003

**Skill Data Architecture (ADR-0004)**
- All game data (ClassData, SkillData, ArenaConfig) must use ScriptableObject — source: ADR-0004
- All SO data must be read-only at runtime; runtime state held by separate system managers — source: ADR-0004
- AttackData is a unified struct shared by ClassData and SkillData — source: ADR-0004
- Attack system must consume AttackData without distinguishing source (class vs skill) — source: ADR-0004
- SkillDatabase is a single SO holding `List<SkillData>` with read-only `ISkillDatabase` interface — source: ADR-0004
- StateDefinition must be `readonly struct`, stack-allocated, zero GC — source: ADR-0004
- Each character has independent FSM dictionary; same SkillData registers independently per character — source: ADR-0004
- Each SO must implement `OnValidate()` for editor-time data integrity checks — source: ADR-0004

**Input Pipeline (ADR-0005)**
- Direction input (Move) read directly in FixedUpdate via `ReadValue<Vector2>()` with dead zone < 0.15 — source: ADR-0005
- Button input (Jump/Attack/Dash/Skill) callback-written to ring buffer, consumed in FixedUpdate — source: ADR-0005
- Jump handles both `performed` (write buffer) and `canceled` (shortHop flag); FixedUpdate checks flag — source: ADR-0005
- Component hierarchy per character: PlayerInput → InputReader → InputBuffer — source: ADR-0005
- InputReader is the sole input entry point; 3C and CombatFSM consume via `IInputReader` interface — source: ADR-0005
- Buffer validation: `BufferAge = CurrentFrame - RecordedFrame`; valid when `0 <= age <= bufferFrames && state accepts` — source: ADR-0005
- `BufferCapacity` (8 entries) and `BufferWindowFrames` (8 frames) must be named distinctly — source: ADR-0005

**Damage & Knockback Pipeline (ADR-0006)**
- All damage/knockback calculations execute in 60Hz FixedUpdate with no deltaTime multiplication (except knockback decay) — source: ADR-0006
- DamagePercent update and KnockbackMagnitude calculation must complete synchronously in same frame — source: ADR-0006
- KO detection every frame using strict inequality (`<` / `>`, not `<=` / `>=`) — source: ADR-0006
- Knockback system must not directly operate on Rigidbody2D; must delegate through `IMovementController.SetVelocity` — source: ADR-0006
- DamagePercent must only ever increase (MVP); stored as float — source: ADR-0006
- DamageFormulas and KnockbackFormulas must be pure static classes, 100% unit-testable without Unity runtime — source: ADR-0006
- Gravity (32.0 u/s^2) and TerminalVelocity (20.0 u/s) must use shared Constants class — source: ADR-0006
- DamageSystem subscribes to OnHitDetected from CombatFSM; dispatches OnHitProcessed + OnDamagePercentChanged — source: ADR-0006

**Focus System (ADR-0009, Core portion)**
- FocusFormulas must be pure static class, 100% unit-testable — source: ADR-0009
- Both attacker and defender focus updates occur in same OnAttackHit callback, same frame — source: ADR-0009
- FocusSystem uses `ClampFocus` to enforce FocusCap — source: ADR-0009
- FocusPoints reset to zero between rounds; UnlockedCount and AlreadyDrawnSkillIds persist across rounds — source: ADR-0009

### Forbidden Approaches

- **Never use single monolithic FSM** merging movement and combat — 36+ combination states, violates SRP — source: ADR-0002
- **Never use Hierarchical FSM (HFSM)** — over-engineered, Unity doesn't natively support, state interaction doesn't suit hierarchy — source: ADR-0002
- **Never use fully data-driven FSM** with all states as string/int IDs — top-level CombatState is fixed for MVP; hybrid approach balances safety and extensibility — source: ADR-0002
- **Never use manual AABB overlap detection** — object count <10, Unity Trigger detection already efficient — source: ADR-0003
- **Never use Physics2D.OverlapBox query API** — Trigger callbacks match physics timing naturally; OverlapBox adds overhead — source: ADR-0003
- **Never use JSON/CSV external data files** parsed at runtime — loses Inspector editing, can't store Sprite/GameObject refs — source: ADR-0004
- **Never use hardcoded C# constant classes** for game data — violates data-driven principle, tuning requires recompile — source: ADR-0004
- **Never merge DamageAndKnockbackSystem** — three responsibilities have different rates of change — source: ADR-0006
- **Never use ScriptableObject storing runtime DamagePercent** — SOs are shared assets; two characters would corrupt each other — source: ADR-0006
- **Never merge FocusAndDrawSystem** — focus accumulation (Core) and skill draw (Feature) have different change drivers — source: ADR-0009
- **Never merge Focus into DamageSystem** — focus has its own state, events, and tuning knobs — source: ADR-0009
- **Never use direct random equip** with no selection — three-choose-one is a core design pillar — source: ADR-0009

### Performance Guardrails

- Dictionary lookup per frame: O(1) < 1us — source: ADR-0002
- Circular buffer traversal (8 entries): < 1us — source: ADR-0002
- Two FSM state updates combined: < 0.5ms per frame — source: ADR-0002
- StateDefinition dict per character: 4-10 entries ~1KB; buffer: 8 x 16B = 128B — source: ADR-0002
- Collision system frame cost: < 0.5ms (2 characters + projectiles) — source: ADR-0003
- OnTriggerEnter2D callback: < 0.1ms — source: ADR-0003
- HitEvent struct ~80 bytes, stack-allocated, zero GC — source: ADR-0003
- Dictionary<string, StateDefinition> lookup: < 1us — source: ADR-0004
- ISkillDatabase query: < 0.01ms — source: ADR-0004
- Total SO memory: ~5KB; StateDefinition structs stack-allocated — source: ADR-0004
- DamageFormulas + KnockbackFormulas per-hit: < 1 microsecond — source: ADR-0006
- KnockbackSystem per-frame (2 players): < 0.1ms — source: ADR-0006
- Full pipeline (damage + knockback + KO, 2 players): < 0.2ms — source: ADR-0006
- FocusSystem per-hit processing: < 0.01ms — source: ADR-0009
- Full pipeline triggered by one hit: < 0.1ms — source: ADR-0009

---

## Feature Layer Rules

*Applies to: skill draw, skill equipment, match lifecycle management*

### Required Patterns

**Skill Data Architecture — Feature portion (ADR-0004)**
- Dynamic registration flow: OnSkillDrawn → EquipmentManager finds slot → create StateDefinition from SkillData.AttackData → RegisterState → input mapping activates — source: ADR-0004
- On round start: `DeregisterAllSkillStates()` clears all skill StateDefinitions, preserves base attacks — source: ADR-0004

**Skill Draw & Equipment (ADR-0009, Feature portion)**
- Three systems form serial pipeline: hit → FocusSystem accumulation → threshold → SkillDrawSystem draw → SkillEquipmentManager equip — source: ADR-0009
- Skill selection must not pause the game — runs in real-time — source: ADR-0009
- Selection timeout: 5 seconds (300 frames); auto-select first candidate on timeout — source: ADR-0009
- MVP: 6 skills per class, maximum 4 draws per match — source: ADR-0009
- Equipped skills persist across rounds; SkillEquipmentManager must NOT reset between rounds — source: ADR-0009
- Skill slots fill in sequential order (1→2→3→4) via `FindFirstEmptySlot` — source: ADR-0009
- Skill activation goes through combat FSM's standard input buffer — source: ADR-0009
- If eligible pool empty after filtering, draw must not consume unlock count — source: ADR-0009
- If only 1 candidate generated, auto-select and skip AwaitingSelection — source: ADR-0009
- SkillEquipmentManager registers each equipped skill to combat FSM via `ICombatStateProvider.RegisterState` — source: ADR-0009
- Reset methods distinguish round-level (`ResetForNewRound`) vs match-level (`ResetForNewMatch`); SkillEquipmentManager has NO ResetForNewRound — source: ADR-0009
- Match-management GDD takes precedence over skill-equipment-management GDD when conflict arises — source: ADR-0009
- SkillDrawSystem uses internal FSM: Idle, Drawing, AwaitingSelection, Complete — source: ADR-0009

**Match & Round Lifecycle (ADR-0010)**
- MatchManager is MonoBehaviour within GameScene (NOT DontDestroyOnLoad) — source: ADR-0010
- MatchFormat configurable from CharacterSelect (Bo1/Bo3/Bo5) — source: ADR-0010
- `WinsNeeded = Ceil(MatchFormat / 2.0)`; even formats clamped to odd; max 5 — source: ADR-0010
- Dual KO in same frame: both players get +1 score; can result in draw ending match — source: ADR-0010
- MatchManager internal FSM: Inactive, WaitingForBattle, RoundInProgress, RoundResolved, MatchComplete — source: ADR-0010
- State guards: extra KO events during RoundResolved/WaitingForBattle/MatchComplete must be ignored — source: ADR-0010
- Dual KO handled per-frame: count KO events, process all at FixedUpdate end — source: ADR-0010
- CoordinateRoundReset order: DamageSystem → FocusSystem → SkillDrawSystem → MovementControllers → CombatFSMs → KnockbackSystem; SkillEquipmentManager explicitly NOT reset — source: ADR-0010
- Initialization during MatchLoading extends to 13 steps — source: ADR-0010
- Full match reset (Results → CharacterSelect) clears scores, round counter, phase, calls ResetAllForNewMatch — source: ADR-0010
- Results → CharacterSelect does not reload scene; MatchManager stays in memory — source: ADR-0010
- MatchManager must not control freeze frames — that is GameStateManager's responsibility — source: ADR-0010
- Each Reset call in CoordinateRoundReset wrapped in try-catch: log errors but don't abort subsequent resets — source: ADR-0010
- MatchFormulas must be pure static class: CalculateWinsNeeded, CalculateMaxRounds, IsMatchOver, IsDraw, GetWinner, ClampMatchFormat — source: ADR-0010

### Forbidden Approaches

- **Never merge MatchManager into GameStateManager** — different layer concerns and change drivers — source: ADR-0010
- **Never reload scene between rounds** — adds 2-3 seconds, violates fast combat pillar — source: ADR-0010
- **Never use boolean flags instead of internal FSM** — N flags create 2^N illegal combinations — source: ADR-0010

### Performance Guardrails

- SkillDrawSystem pool construction: < 0.1ms (for 6 skills) — source: ADR-0009
- SkillEquipmentManager per-equip: < 0.01ms — source: ADR-0009
- MatchManager per-frame: < 0.01ms (active only during state transitions and KO) — source: ADR-0010
- CoordinateRoundReset: < 0.5ms (6 system resets x 2 players) — source: ADR-0010
- No allocations during match processing — arrays reused, structs operated by reference — source: ADR-0010

---

## Presentation Layer Rules

*Applies to: rendering, audio, UI, VFX, shaders, animations*

No Presentation-layer ADRs exist yet. ADR-0013 (UI Architecture) is deferred.
Presentation rules will be added when ADR-0011 (Camera), ADR-0012 (Projectile), and ADR-0013 (UI) are accepted.

---

## Global Rules (All Layers)

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `PlayerController` |
| Public Variables | PascalCase | `MoveSpeed` |
| Private Variables | _camelCase | `_currentHealth` |
| Signals/Events | PascalCase (On prefix) | `OnHealthChanged` |
| Files | PascalCase matching class | `PlayerController.cs` |
| Scenes/Prefabs | PascalCase | `ArenaLevel.unity` |
| Constants | PascalCase or UPPER_SNAKE_CASE | `MaxJumpCount` / `MAX_PLAYERS` |

Source: `.claude/docs/technical-preferences.md`

### Performance Budgets

| Target | Value |
|--------|-------|
| Target Framerate | 60 FPS |
| Frame Budget | 16.6ms |
| Draw Calls | < 300 (2D, URP) |
| Memory Ceiling | < 512MB |
| Physics Step | 60Hz (0.0166667s) |

Source: `.claude/docs/technical-preferences.md`

### Approved Libraries / Addons

- `com.unity.inputsystem` 1.7.x — player input (bundled with Unity 2022.3 LTS)
- `com.unity.render-pipelines.universal` — URP rendering
- `com.unity.2d.*` — 2D sprite, animation, tilemap support
- NUnit (Unity Test Framework) — unit and integration testing

Source: `Packages/manifest.json`, `.claude/docs/technical-preferences.md`

### Forbidden APIs (Unity 6.3 LTS Deprecated)

These APIs are deprecated in Unity 6.3 LTS. This project uses Unity 2022.3.51 (they still work), but should be avoided for future compatibility:

| Forbidden API | Use Instead | Reason |
|---------------|-------------|--------|
| `Input.GetKey()` / `GetKeyDown()` | New Input System (`InputAction`) | Cannot distinguish devices |
| `Input.GetAxis()` | `InputAction` callbacks | Old Input Manager |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |
| `Canvas` (UGUI) for new UI | `UIDocument` (UI Toolkit) | UI Toolkit production-ready |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering |
| `Resources.Load()` | Addressables | Better memory control |
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |
| `Animation.Play()` | `Animator.Play()` | Mecanim state machine |

Source: `docs/engine-reference/unity/deprecated-apis.md`

### Cross-Cutting Constraints

- **Pure computation formulas**: DamageFormulas, KnockbackFormulas, FocusFormulas, DrawFormulas, MatchFormulas — all pure static classes with no MonoBehaviour or ScriptableObject, 100% unit-testable without Unity runtime — source: ADR-0006, ADR-0009, ADR-0010
- **ScriptableObject read-only at runtime**: All SO data (ClassData, SkillData, ArenaConfig) is read-only after load; runtime state held by separate managers — source: ADR-0004
- **60Hz frame-based design**: All formulas and frame counts use 1/60s units; never deltaTime-based — source: ADR-0001
- **Explicit dispatch order**: CharacterController coordinator calls systems in defined order; never rely on Unity Script Execution Order — source: ADR-0002
- **Interface-driven coupling**: Systems communicate through interfaces (IMovementController, ICombatStateProvider, IInputReader, ISkillDatabase, IDamageSystem); never direct class references — source: ADR-0002, ADR-0004, ADR-0005, ADR-0006
- **Round vs Match reset semantics**: FocusPoints reset per round; UnlockedCount/AlreadyDrawnSkillIds persist across rounds; SkillEquipmentManager resets per match only — source: ADR-0009, ADR-0010
- **Architecture supports 4 players**: All systems designed for MVP 2 players but reserve capacity for 4 — source: ADR-0007
