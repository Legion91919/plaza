# Lessons

Generic, reusable lessons only. No client names, data or secrets.
- SignalR runs `OnConnectedAsync` before any hub invocation, so a tiny query method (e.g. `Subscriptions()`) is a reliable "ready" barrier for tests and clients.
- A raw WebSocket client speaking the SignalR JSON protocol (negotiate, `{"protocol":"json","version":1}\u001e` handshake) gives true end-to-end hub tests without the client package.
- After a mutation test, rebuild before `dotnet run --no-build`: the test build leaves the mutated DLL in the project's bin.
