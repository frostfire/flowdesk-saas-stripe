import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "./App";

describe("App", () => {
  it("renders the FlowDesk shell", () => {
    render(<App />);

    expect(screen.getByRole("link", { name: /flowdesk/i })).toBeTruthy();
  });
});
