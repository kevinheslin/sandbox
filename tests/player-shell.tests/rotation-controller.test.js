import { describe, it, expect } from "vitest";

// RotationController/PaneController don't exist as importable modules yet — player.js is
// currently a placeholder (see src/Seaway.Signage.Web/Player/wwwroot/player/player.js). Once the
// warm-set LRU and crossfade logic land (IMPLEMENTATION_PLAN.md milestone 15), extract them into
// testable modules here rather than leaving all the logic inline in player.js.
describe.skip("RotationController (Phase 1 — see IMPLEMENTATION_PLAN.md milestone 15)", () => {
  it("advances to the next item once its duration elapses", () => {
    expect.fail("not implemented");
  });

  it("keeps at most maxWarm panes warm at once", () => {
    expect.fail("not implemented");
  });

  it("falls back to the cached rotation when the SSE connection drops", () => {
    expect.fail("not implemented");
  });
});
