import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminDebugPage } from "./AdminDebugPage";
import { DunningBanner } from "./DunningBanner";
import { GatedActions } from "./GatedActions";
import {
  createCheckoutSession,
  createPortalSession,
  getAdminBillingDebug,
  getCurrentEntitlements,
  resetDemo,
} from "./billingApi";
import { PricingPage } from "./PricingPage";
import type { CurrentEntitlements } from "./types";

vi.mock("./billingApi", () => ({
  createCheckoutSession: vi.fn(),
  createPortalSession: vi.fn(),
  getAdminBillingDebug: vi.fn(),
  getCurrentEntitlements: vi.fn(),
  resetDemo: vi.fn(),
}));

const freeEntitlements: CurrentEntitlements = {
  plan: "Free",
  status: "None",
  entitlements: {
    canCreateCases: false,
    canViewAnalytics: false,
    maxCases: 10,
    maxSeats: 1,
  },
};

const proEntitlements: CurrentEntitlements = {
  plan: "Pro",
  status: "Active",
  entitlements: {
    canCreateCases: true,
    canViewAnalytics: true,
    maxCases: 250,
    maxSeats: 1,
  },
};

const pastDueEntitlements: CurrentEntitlements = {
  plan: "Pro",
  status: "PastDue",
  entitlements: {
    canCreateCases: false,
    canViewAnalytics: false,
    maxCases: 10,
    maxSeats: 1,
  },
};

describe("billing UI", () => {
  beforeEach(() => {
    vi.mocked(createCheckoutSession).mockReset();
    vi.mocked(createPortalSession).mockReset();
    vi.mocked(getAdminBillingDebug).mockReset();
    vi.mocked(getCurrentEntitlements).mockReset();
    vi.mocked(resetDemo).mockReset();
  });

  it("renders the Free, Pro, and Team pricing plans", async () => {
    vi.mocked(getCurrentEntitlements).mockResolvedValue(freeEntitlements);

    renderWithProviders(<PricingPage token="token" onUnauthorized={vi.fn()} />);

    expect(await screen.findByRole("heading", { name: "Free" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Pro" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Team" })).toBeTruthy();
    expect(screen.getByText("4242 4242 4242 4242")).toBeTruthy();
    expect(screen.getByText("4000 0000 0000 0002")).toBeTruthy();
    expect(screen.getByText("4000 0000 0000 0341")).toBeTruthy();
  });

  it("starts checkout and redirects from the upgrade action", async () => {
    const redirectTo = vi.fn();
    vi.mocked(getCurrentEntitlements).mockResolvedValue(freeEntitlements);
    vi.mocked(createCheckoutSession).mockResolvedValue({ url: "https://billing.test/checkout" });

    renderWithProviders(<PricingPage token="token" redirectTo={redirectTo} onUnauthorized={vi.fn()} />);

    fireEvent.click(await screen.findByRole("button", { name: "Upgrade to Pro" }));

    await waitFor(() => {
      expect(createCheckoutSession).toHaveBeenCalledWith("token", "Pro", expect.any(Function));
      expect(redirectTo).toHaveBeenCalledWith("https://billing.test/checkout");
    });
  });

  it("hides analytics on Free and shows it on Pro", async () => {
    vi.mocked(getCurrentEntitlements).mockResolvedValue(freeEntitlements);
    const { unmount } = renderWithProviders(<GatedActions token="token" onUnauthorized={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Create case" })).toHaveProperty("disabled", true);
    });
    expect(screen.queryByRole("link", { name: "Analytics" })).toBeNull();
    unmount();

    vi.mocked(getCurrentEntitlements).mockResolvedValue(proEntitlements);
    renderWithProviders(<GatedActions token="token" onUnauthorized={vi.fn()} />);

    expect(await screen.findByRole("link", { name: "Analytics" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Create case" })).toHaveProperty("disabled", false);
  });

  it("shows the dunning banner when billing is past due", async () => {
    vi.mocked(getCurrentEntitlements).mockResolvedValue(pastDueEntitlements);

    renderWithProviders(<DunningBanner token="token" onUnauthorized={vi.fn()} />);

    expect(await screen.findByText("Payment failed")).toBeTruthy();
    expect(screen.getByRole("link", { name: "Manage billing" })).toBeTruthy();
  });

  it("renders synced subscription state and recent webhook events", async () => {
    vi.mocked(getAdminBillingDebug).mockResolvedValue({
      subscription: {
        plan: "Pro",
        status: "Active",
        stripeCustomerId: "cus_test",
        stripeSubscriptionId: "sub_test",
        stripePriceId: "price_pro",
        currentPeriodEnd: "2026-07-16T00:00:00Z",
        cancelAtPeriodEnd: false,
        updatedAt: "2026-06-16T00:00:00Z",
      },
      webhookEvents: [
        {
          stripeEventId: "evt_test",
          type: "customer.subscription.updated",
          receivedAt: "2026-06-16T00:00:00Z",
          processedAt: "2026-06-16T00:00:01Z",
          processingError: null,
        },
      ],
    });

    renderWithProviders(<AdminDebugPage token="token" onUnauthorized={vi.fn()} />);

    expect(await screen.findByRole("heading", { name: "Synced subscription" })).toBeTruthy();
    expect(screen.getByText("sub_test")).toBeTruthy();
    expect(screen.getByText("evt_test")).toBeTruthy();
    expect(screen.getByText("customer.subscription.updated")).toBeTruthy();
  });
});

function renderWithProviders(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
      mutations: {
        retry: false,
      },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>,
  );
}
