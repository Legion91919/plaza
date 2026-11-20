// Scripted walkthrough for the README demo. Run: node demo/record.mjs .
// The recorded page is Pieter's browser; Naledi posts through the API as a separate client.
const ALERT = "Safety alert: a white van is checking car doors on Duinroosstraat. Lock up and keep your porch light on tonight.";

const switchTo = async (page, userId) => {
  await page.locator("#demo-user").selectOption(userId);
  await page.getByText("● Live").waitFor();
};

export default async ({ page, step, shot }) => {
  await switchTo(page, "u-pieter");
  await page.getByText("you are a member").waitFor();
  await step("Plaza: one verified notice board per neighbourhood, newest first", async () => {
    await page.getByRole("heading", { name: "Lindehof Residents" }).waitFor();
  });
  await shot("group");
  await step("Pieter is online: 'Live' is a SignalR connection to his groups");

  await step("Meanwhile, from her own phone, moderator Naledi posts a safety alert", async () => {
    // Naledi's client is a separate caller of the same API (dev sign-in header), not this browser.
    const res = await fetch("http://localhost:5102/api/groups/lindehof/posts", {
      method: "POST",
      headers: { "X-Demo-User": "u-naledi", "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({ body: ALERT, kind: "safetyAlert" }),
    });
    if (!res.ok) throw new Error(`Naledi's post failed: ${res.status}`);
  });
  await step("Seconds later: a live toast for Pieter, and the alert tops his feed", async () => {
    await page.locator(".toasts").getByText(/Duinroosstraat/).waitFor();
    await page.waitForTimeout(600);
  });
  await shot("live-alert");

  await step("Ayesha moderates Kiepersol Park, but she is not a Lindehof member…", async () => {
    await switchTo(page, "u-ayesha");
    await page.getByRole("heading", { name: /Kiepersol Park/ }).waitFor();
  });
  await step("…so the server refuses her (403) and Lindehof's feed stays private", async () => {
    await page.getByRole("navigation", { name: "Neighbourhood groups" }).getByRole("button", { name: "Lindehof Residents" }).click();
    await page.getByRole("alert").waitFor();
  });
  await shot("not-a-member");
};

export async function mobile({ page, shot }) {
  await page.getByRole("heading", { name: "Lindehof Residents" }).waitFor();
  await page.waitForTimeout(800);
  await shot("mobile");
}
