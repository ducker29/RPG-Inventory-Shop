# RPG Inventory & Shop — Agent Instructions

## Project Goal

Build a small, polished, portfolio-ready Unity RPG Inventory & Shop demo demonstrating MVP architecture, Zenject dependency injection, clean C# design, and automated tests.

Prioritize code quality, maintainability, testability, and a working Unity demo over feature quantity.

## Tech Stack

- Unity 6, 2D project
- C#
- MVP (Model-View-Presenter)
- Zenject / Extenject for dependency injection
- ScriptableObjects for item definitions
- NUnit EditMode tests
- JSON persistence
- Unity MCP for Unity Editor operations

## Architecture Rules

### Domain

- Implement business logic in plain C# classes.
- Domain classes must not depend on `UnityEngine`, `MonoBehaviour`, Zenject, or Unity UI.
- Prefer constructor injection.
- Use interfaces at meaningful architectural boundaries.
- Avoid unnecessary abstractions, static global state, and service locators.

### Presentation — MVP

- Views are Unity `MonoBehaviour` components.
- Views handle UI rendering and user input only.
- Presenters are plain C# classes without `MonoBehaviour`.
- Presenters coordinate views and application services.
- Business rules must not be implemented in views or presenters.
- Use explicit initialization and disposal for event subscriptions.

### Dependency Injection

- Use Zenject as the composition root.
- Configure dependencies through installers.
- Prefer constructor injection for plain C# classes.
- Do not call `Container.Resolve()` from gameplay code.
- Avoid unnecessary singletons.

### ScriptableObjects

- Use ScriptableObjects for immutable item definitions and authoring data.
- Keep mutable inventory state in runtime domain models.
- Use stable item IDs for persistence.

### Persistence

- Save inventory, gold, and player health as JSON.
- Include a save-data version.
- Validate loaded data.
- Handle missing or corrupted saves gracefully.
- Do not serialize Unity scene references.

## MVP Scope

Implement only:

- 20-slot inventory
- Stackable and non-stackable items
- Item definitions with ID, name, icon, price, and stack limit
- Gold wallet
- Shop buying and selling
- Consumable health potions
- Player health
- JSON save/load
- Inventory and shop UI
- NUnit EditMode tests

Do not add combat, quests, crafting, equipment, multiplayer, or unrelated RPG systems.

## Testing Requirements

- Inventory logic must be testable without a Unity scene.
- ShopService must be testable with fake dependencies.
- Test buying, selling, insufficient gold, full inventory, stacking, item consumption, and persistence.
- Run compilation checks after meaningful changes.
- Run relevant EditMode tests after implementing business logic.
- Do not claim tests passed without executing them.

## Agent Skills

Use the installed skills when relevant:

- `unity-csharp-scripting`
- `unity-scriptableobjects`
- `rpg`
- `game-ui-ux`
- `save-systems`

Treat these skills as guidance. Project-specific architecture and scope rules take priority.

## Unity MCP

- Prefer Unity MCP for scene, prefab, asset, and editor operations.
- Verify results in the Unity Editor.
- Inspect Unity Console after changes.
- Avoid manually modifying Unity YAML files when editor tools can perform the operation safely.
- Preserve Unity `.meta` files and GUID references.

## Git Workflow — Explicit User Approval Required

- The agent MUST NOT create Git commits without explicit user approval.
- The agent MUST NOT execute `git commit`, `git commit --amend`, `git merge`, `git rebase`, `git reset`, `git cherry-pick`, or `git push` without explicit user approval.
- The agent MUST NOT create tags or modify Git history without explicit user approval.
- The agent MAY use read-only Git commands such as `git status`, `git diff`, and `git log`.
- The agent MUST NOT stage files using `git add` without explicit user approval.
- Completing a development milestone does NOT constitute permission to commit.
- General instructions such as "implement this feature" or "continue development" do NOT constitute Git approval.
- Approval applies only to the specific Git operation explicitly authorized by the user.

### Commit Approval Procedure

After completing a milestone, the agent MUST:

1. Report the files created, modified, and deleted.
2. Report compilation and test results.
3. Show a summary of the Git diff.
4. Propose a commit message.
5. Ask the user for explicit permission to create the commit.
6. Wait for confirmation before staging or committing any files.

If approval is not granted, leave all changes uncommitted.

Never automatically push changes to a remote repository.

## Development Workflow

1. Inspect the existing project before modifying it.
2. Propose an implementation plan.
3. Implement one milestone at a time.
4. Compile and run relevant tests.
5. Report changes, test results, and remaining issues.
6. Wait for approval before proceeding to the next major milestone.

Keep the solution simple and portfolio-ready.