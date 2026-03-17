# Creating Client Components

Guidelines and best practices for creating Blazor components in EcoPortal.Client.

---

## Where to Put Components

Components go in different places depending on how they're used:

| If the component is... | Put it in... |
|------------------------|--------------|
| Reusable across multiple features | `/Components` |
| Specific to one feature | `/Features/{Feature}/Components` |
| A routable page | `/Features/{Feature}/Pages` |
| A modal dialog | `/Features/{Feature}/Dialogs` |
| A layout wrapper | `/Layout` |

**Rule of thumb:** Start in the feature folder. Only move to `/Components` when a second feature needs it.

---

## Styling

### The Rules

1. **No custom CSS files** — Don't create `.css` files for components
2. **No inline styles** — Don't use `style="..."` attributes
3. **Use MudBlazor** — For all UI components and layout
4. **Use utility classes** — For spacing, flexbox, and alignment

### Exceptions

Custom CSS is allowed only for:

- **Landing pages** (Home, marketing pages) — These need unique visual identity
- **Shared reusable components** (SearchHeader, etc.) — Encapsulated styling that's used everywhere

### What to Use Instead

Use MudBlazor's utility classes for common needs:

- **Spacing:** `pa-4`, `mt-2`, `mb-4`, `px-2`, `gap-2`
- **Flexbox:** `d-flex`, `flex-column`, `align-center`, `justify-space-between`
- **Typography:** Use `<MudText Typo="...">` instead of raw HTML
- **Colors:** Use `Color.Primary`, `Class="mud-text-secondary"`
- **Responsive:** Use `<MudHidden>` and `<MudGrid>` with breakpoints

---

## Component Practices

### Always Handle Loading States

Never show a blank screen while fetching data. Use skeletons that match the shape of the content:

- Use `<MudSkeleton>` to show placeholders
- Match the skeleton dimensions to what will be rendered
- Keep the layout stable — no jumping when content loads

### Always Handle Error States

Every component that fetches data should handle failures gracefully:

- Show appropriate error messages
- Use `Snackbar` for transient errors
- Use inline alerts for blocking errors
- Never leave users wondering what went wrong

### Always Handle Empty States

When a list has no items:

- Show a helpful message, not a blank space
- Consider suggesting an action ("No sensors yet. Add one?")
- Don't show "No results" when the user hasn't searched yet

### Keep Components Focused

Each component should do one thing well:

- If it's getting hard to follow, split it
- Presentational components shouldn't fetch data
- Container components manage data and pass it down
- Forms should be reusable for both create and edit

---

## Forms

### Use Nested Form Models

Keep the form's data model inside the component as a nested class. This keeps everything together and makes the component self-contained.

### Support Both Create and Edit

Forms should accept initial data for edit mode. The same form component works for creating new items and editing existing ones.

### Disable During Submission

When a form is submitting:

- Disable all inputs and buttons
- Show a loading indicator on the submit button
- Prevent double-submission

### Validate Before Submit

Don't let users submit invalid data. Show validation errors inline, next to the fields that have problems.

---

## Dialogs

### Confirm Destructive Actions

Before deleting anything, show a confirmation dialog. Be specific about what will be deleted.

### Return Data, Not Just Confirmation

When a dialog collects information, return the data to the caller. Don't make the dialog responsible for what happens next.

### Keep Dialogs Simple

Dialogs should be quick interactions. If you need a complex form or multi-step process, consider using a full page instead.

---

## Authorization

### Hide What Users Can't Do

Don't show buttons for actions the user doesn't have permission to perform. Use the permission-checking components to conditionally render UI.

### Handle Permission Changes

Permissions can change while the user is on the page. Subscribe to auth state changes and update the UI accordingly.

### Fail Gracefully

Even with UI hiding, the backend enforces permissions. Handle 403 responses gracefully — the user's role may have changed.

---

## Data Fetching

### Use the HTTP Clients

Never make raw HTTP calls. Use the typed HTTP clients from the module's `Application.Client` project.

### Stream All Lists

Use `IAsyncEnumerable<T>` for all list endpoints. The only exception is lookups (small reference data like dropdowns).

### Cancel Ongoing Requests

When the user navigates away or changes filters, cancel the previous request. Use `CancellationToken` properly.

---

## State Management

### Keep State Local When Possible

Don't reach for global state unless you need it. Component-level state is simpler and easier to reason about.

### Use Signals for Reactive Updates

When state changes need to trigger re-renders automatically, use `[Signal]` attributes. This is cleaner than manual `StateHasChanged()` calls.

### Use RelayCommand for Async Actions

For buttons that trigger async operations, use `[RelayCommand]`. It handles loading states and prevents double-clicks automatically.

---

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Pages | `{Resource}Page` or `{Resource}{Action}Page` | `SensorPage`, `SensorCreatePage` |
| Lists | `{Resource}List` | `SensorList` |
| Cards | `{Resource}Card` | `SensorCard` |
| Forms | `{Resource}Form` | `SensorForm` |
| Dialogs | `{Action}{Resource}Dialog` | `DeleteSensorDialog` |

---

## Checklist

Before considering a component done:

- [ ] Placed in the correct folder
- [ ] No custom CSS (unless it's an exception)
- [ ] No inline styles
- [ ] Loading state handled
- [ ] Error state handled
- [ ] Empty state handled (if applicable)
- [ ] Permissions checked (if applicable)
- [ ] Uses MudBlazor components consistently
- [ ] Follows naming conventions
